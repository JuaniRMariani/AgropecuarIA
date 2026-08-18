[CmdletBinding()]
param(
    [string]$PostgreSqlBin = $env:AGRO_IDENTITY_POSTGRES_BIN,
    [ValidateRange(1, 65535)]
    [int]$ApiPort = 5080
)

$ErrorActionPreference = 'Stop'
$repoRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..\..')).Path
$tempRoot = [IO.Path]::GetFullPath((Join-Path ([IO.Path]::GetTempPath()) 'AgropecuarIA.Identity.E2E'))
$runDirectory = [IO.Path]::GetFullPath((Join-Path $tempRoot ([Guid]::NewGuid().ToString('N'))))
$dataDirectory = [IO.Path]::GetFullPath((Join-Path $runDirectory 'data'))
$logDirectory = [IO.Path]::GetFullPath((Join-Path $runDirectory 'logs'))
$passwordPath = [IO.Path]::GetFullPath((Join-Path $runDirectory 'postgres.pwfile'))
$apiProcess = $null
$clusterStarted = $false
$previousEnvironment = @{
    ASPNETCORE_ENVIRONMENT = $env:ASPNETCORE_ENVIRONMENT
    ASPNETCORE_URLS = $env:ASPNETCORE_URLS
    ConnectionStrings__Identity = $env:ConnectionStrings__Identity
    ConnectionStrings__Territory = $env:ConnectionStrings__Territory
    ConnectionStrings__ProductiveCore = $env:ConnectionStrings__ProductiveCore
    AGRO_API_ORIGIN = $env:AGRO_API_ORIGIN
    Identity__ApplyMigrations = $env:Identity__ApplyMigrations
    Territory__ApplyMigrations = $env:Territory__ApplyMigrations
    ProductiveCore__ApplyMigrations = $env:ProductiveCore__ApplyMigrations
    Territory__Reference__CoordinateResolutionEnabled = $env:Territory__Reference__CoordinateResolutionEnabled
    Identity__DevelopmentProvider__Enabled = $env:Identity__DevelopmentProvider__Enabled
    Identity__DevelopmentProvider__SyntheticProfileCount = $env:Identity__DevelopmentProvider__SyntheticProfileCount
    Identity__StrongAuthentication__Enabled = $env:Identity__StrongAuthentication__Enabled
    Identity__OrganizationBootstrap__Enabled = $env:Identity__OrganizationBootstrap__Enabled
    Identity__OrganizationBootstrap__CurrentKeyVersion = $env:Identity__OrganizationBootstrap__CurrentKeyVersion
    Identity__OrganizationBootstrap__IdempotencyHmacKeys__e2e_v1 = $env:Identity__OrganizationBootstrap__IdempotencyHmacKeys__e2e_v1
    Identity__OrganizationOwnerInvitations__Enabled = $env:Identity__OrganizationOwnerInvitations__Enabled
    Identity__OrganizationOwnerInvitations__Lifetime = $env:Identity__OrganizationOwnerInvitations__Lifetime
    Identity__OrganizationOwnerInvitations__CurrentKeyVersion = $env:Identity__OrganizationOwnerInvitations__CurrentKeyVersion
    Identity__OrganizationOwnerInvitations__HmacKeys__e2e_v1 = $env:Identity__OrganizationOwnerInvitations__HmacKeys__e2e_v1
    Identity__RateLimits__PerIpPerMinute = $env:Identity__RateLimits__PerIpPerMinute
    Identity__RateLimits__PerSessionPerMinute = $env:Identity__RateLimits__PerSessionPerMinute
    Identity__RateLimits__StepUpPerSessionPerFiveMinutes = $env:Identity__RateLimits__StepUpPerSessionPerFiveMinutes
    ProductiveCore__ManagementUnitCreation__Enabled = $env:ProductiveCore__ManagementUnitCreation__Enabled
    ProductiveCore__ManagementUnitCreation__CurrentKeyVersion = $env:ProductiveCore__ManagementUnitCreation__CurrentKeyVersion
    ProductiveCore__ManagementUnitCreation__HmacKeys__e2e_v1 = $env:ProductiveCore__ManagementUnitCreation__HmacKeys__e2e_v1
    ProductiveCore__ManagementUnitRename__Enabled = $env:ProductiveCore__ManagementUnitRename__Enabled
    ProductiveCore__ManagementUnitRename__CurrentKeyVersion = $env:ProductiveCore__ManagementUnitRename__CurrentKeyVersion
    ProductiveCore__ManagementUnitRename__HmacKeys__e2e_v1 = $env:ProductiveCore__ManagementUnitRename__HmacKeys__e2e_v1
    ProductiveCore__RateLimits__PerSessionPerMinute = $env:ProductiveCore__RateLimits__PerSessionPerMinute
    AGRO_E2E_REUSE_SERVER = $env:AGRO_E2E_REUSE_SERVER
    PGPASSWORD = $env:PGPASSWORD
}

function Resolve-PostgreSqlBin {
    param([string]$ConfiguredPath)

    if ($ConfiguredPath -and
        @('initdb.exe', 'pg_ctl.exe', 'psql.exe').Where({
            -not (Test-Path -LiteralPath (Join-Path $ConfiguredPath $_))
        }).Count -eq 0) {
        return [IO.Path]::GetFullPath($ConfiguredPath)
    }

    $installationRoot = 'C:\Program Files\PostgreSQL'
    if (Test-Path -LiteralPath $installationRoot) {
        $candidate = Get-ChildItem -LiteralPath $installationRoot -Directory |
            Sort-Object Name -Descending |
            ForEach-Object { Join-Path $_.FullName 'bin' } |
            Where-Object {
                $path = $_
                @('initdb.exe', 'pg_ctl.exe', 'psql.exe').Where({
                    -not (Test-Path -LiteralPath (Join-Path $path $_))
                }).Count -eq 0
            } |
            Select-Object -First 1
        if ($candidate) {
            return [IO.Path]::GetFullPath($candidate)
        }
    }

    throw 'PostgreSQL binaries were not found. Set AGRO_IDENTITY_POSTGRES_BIN.'
}

function Test-HttpReady {
    param([string]$Uri)

    try {
        $response = Invoke-WebRequest -Uri $Uri -Method Get -TimeoutSec 2 -UseBasicParsing
        return $response.StatusCode -eq 200
    }
    catch {
        return $false
    }
}

function Assert-TcpPortAvailable {
    param([int]$Port)

    $listener = [Net.Sockets.TcpListener]::new([Net.IPAddress]::Loopback, $Port)
    $listener.Server.ExclusiveAddressUse = $true
    try {
        $listener.Start()
    }
    catch {
        throw "Identity API port $Port is already in use; refusing to reuse an unrelated server."
    }
    finally {
        $listener.Stop()
    }
}

function New-EphemeralSecret {
    $bytes = New-Object byte[] 32
    $generator = [Security.Cryptography.RandomNumberGenerator]::Create()
    try {
        $generator.GetBytes($bytes)
        return [Convert]::ToBase64String($bytes)
    }
    finally {
        [Array]::Clear($bytes, 0, $bytes.Length)
        $generator.Dispose()
    }
}

function Set-OwnerOnlyAcl {
    param([string]$Path)

    $currentUser = [Security.Principal.WindowsIdentity]::GetCurrent().User
    $item = Get-Item -LiteralPath $Path
    if ($item.PSIsContainer) {
        $acl = [Security.AccessControl.DirectorySecurity]::new()
        $inheritance = [Security.AccessControl.InheritanceFlags]::ContainerInherit -bor
            [Security.AccessControl.InheritanceFlags]::ObjectInherit
        $rule = [Security.AccessControl.FileSystemAccessRule]::new(
            $currentUser,
            [Security.AccessControl.FileSystemRights]::FullControl,
            $inheritance,
            [Security.AccessControl.PropagationFlags]::None,
            [Security.AccessControl.AccessControlType]::Allow)
    }
    else {
        $acl = [Security.AccessControl.FileSecurity]::new()
        $rule = [Security.AccessControl.FileSystemAccessRule]::new(
            $currentUser,
            [Security.AccessControl.FileSystemRights]::FullControl,
            [Security.AccessControl.AccessControlType]::Allow)
    }

    $acl.SetOwner($currentUser)
    $acl.SetAccessRuleProtection($true, $false)
    [void]$acl.AddAccessRule($rule)
    Set-Acl -LiteralPath $Path -AclObject $acl
}

function Test-PasswordlessPostgreSqlRejected {
    param(
        [string]$Executable,
        [int]$Port
    )

    $previousPreference = $ErrorActionPreference
    try {
        $ErrorActionPreference = 'SilentlyContinue'
        Remove-Item Env:PGPASSWORD -ErrorAction SilentlyContinue
        & $Executable --host=127.0.0.1 --port=$Port --username=postgres `
            --dbname=postgres --no-password --command='select 1' 1>$null 2>$null
        return $LASTEXITCODE -ne 0
    }
    finally {
        $ErrorActionPreference = $previousPreference
    }
}

try {
    Assert-TcpPortAvailable -Port $ApiPort
    $postgresBin = Resolve-PostgreSqlBin -ConfiguredPath $PostgreSqlBin
    New-Item -ItemType Directory -Path $logDirectory -Force | Out-Null
    Set-OwnerOnlyAcl -Path $runDirectory

    $postgresPassword = New-EphemeralSecret
    $idempotencyHmacKey = New-EphemeralSecret
    $ownerInvitationHmacKey = New-EphemeralSecret
    $fieldIdempotencyHmacKey = New-EphemeralSecret
    $fieldRenameHmacKey = New-EphemeralSecret
    if (@($postgresPassword, $idempotencyHmacKey, $ownerInvitationHmacKey, $fieldIdempotencyHmacKey, $fieldRenameHmacKey) |
        Group-Object | Where-Object Count -gt 1) {
        throw 'Cryptographic secret generation produced a duplicate value.'
    }

    & dotnet build (Join-Path $repoRoot 'AgropecuarIA.slnx') -c Release --nologo
    if ($LASTEXITCODE -ne 0) {
        throw "Release build failed with exit code $LASTEXITCODE."
    }

    [IO.File]::WriteAllText(
        $passwordPath,
        $postgresPassword,
        [Text.UTF8Encoding]::new($false))
    Set-OwnerOnlyAcl -Path $passwordPath

    & (Join-Path $postgresBin 'initdb.exe') --pgdata=$dataDirectory `
        --username=postgres --auth-local=scram-sha-256 --auth-host=scram-sha-256 `
        --pwfile=$passwordPath --encoding=UTF8 --no-locale
    if ($LASTEXITCODE -ne 0) {
        throw "initdb failed with exit code $LASTEXITCODE."
    }
    Remove-Item -LiteralPath $passwordPath -Force

    $hbaPath = Join-Path $dataDirectory 'pg_hba.conf'
    if (Select-String -LiteralPath $hbaPath -Pattern '^\s*(local|host)\s+.*\strust\s*$' -Quiet) {
        throw 'Ephemeral PostgreSQL pg_hba.conf contains a trust authentication rule.'
    }

    $listener = [Net.Sockets.TcpListener]::new([Net.IPAddress]::Loopback, 0)
    $listener.Start()
    $postgresPort = ([Net.IPEndPoint]$listener.LocalEndpoint).Port
    $listener.Stop()

    & (Join-Path $postgresBin 'pg_ctl.exe') start -D $dataDirectory -o "-h 127.0.0.1 -p $postgresPort" -w
    if ($LASTEXITCODE -ne 0) {
        throw "pg_ctl start failed with exit code $LASTEXITCODE."
    }
    $clusterStarted = $true

    if (-not (Test-PasswordlessPostgreSqlRejected `
        -Executable (Join-Path $postgresBin 'psql.exe') `
        -Port $postgresPort)) {
        throw 'Passwordless PostgreSQL authentication unexpectedly succeeded.'
    }

    $env:ASPNETCORE_ENVIRONMENT = 'Test'
    $env:ASPNETCORE_URLS = "http://127.0.0.1:$ApiPort"
    $env:ConnectionStrings__Identity = "Host=127.0.0.1;Port=$postgresPort;Database=postgres;Username=postgres;Password=$postgresPassword;Pooling=false"
    $env:ConnectionStrings__Territory = $env:ConnectionStrings__Identity
    $env:ConnectionStrings__ProductiveCore = $env:ConnectionStrings__Identity
    $env:AGRO_API_ORIGIN = "http://127.0.0.1:$ApiPort"
    $env:Identity__ApplyMigrations = 'true'
    $env:Territory__ApplyMigrations = 'true'
    $env:ProductiveCore__ApplyMigrations = 'true'
    $env:Territory__Reference__CoordinateResolutionEnabled = 'false'
    $env:Identity__DevelopmentProvider__Enabled = 'true'
    $env:Identity__DevelopmentProvider__SyntheticProfileCount = '4'
    $env:Identity__StrongAuthentication__Enabled = 'true'
    $env:Identity__OrganizationBootstrap__Enabled = 'true'
    $env:Identity__OrganizationBootstrap__CurrentKeyVersion = 'e2e_v1'
    $env:Identity__OrganizationBootstrap__IdempotencyHmacKeys__e2e_v1 = $idempotencyHmacKey
    $env:Identity__OrganizationOwnerInvitations__Enabled = 'true'
    $env:Identity__OrganizationOwnerInvitations__Lifetime = '7.00:00:00'
    $env:Identity__OrganizationOwnerInvitations__CurrentKeyVersion = 'e2e_v1'
    $env:Identity__OrganizationOwnerInvitations__HmacKeys__e2e_v1 = $ownerInvitationHmacKey
    $env:Identity__RateLimits__PerIpPerMinute = '600'
    $env:Identity__RateLimits__PerSessionPerMinute = '300'
    $env:Identity__RateLimits__StepUpPerSessionPerFiveMinutes = '100'
    $env:ProductiveCore__ManagementUnitCreation__Enabled = 'true'
    $env:ProductiveCore__ManagementUnitCreation__CurrentKeyVersion = 'e2e_v1'
    $env:ProductiveCore__ManagementUnitCreation__HmacKeys__e2e_v1 = $fieldIdempotencyHmacKey
    $env:ProductiveCore__ManagementUnitRename__Enabled = 'true'
    $env:ProductiveCore__ManagementUnitRename__CurrentKeyVersion = 'e2e_v1'
    $env:ProductiveCore__ManagementUnitRename__HmacKeys__e2e_v1 = $fieldRenameHmacKey
    $env:ProductiveCore__RateLimits__PerSessionPerMinute = '300'
    $env:AGRO_E2E_REUSE_SERVER = 'false'

    $apiProcess = Start-Process dotnet -ArgumentList @(
        'run',
        '--project',
        (Join-Path $repoRoot 'apps\AgropecuarIA.Api\AgropecuarIA.Api.csproj'),
        '-c',
        'Release',
        '--no-build'
    ) -WorkingDirectory $repoRoot -WindowStyle Hidden -PassThru `
        -RedirectStandardOutput (Join-Path $logDirectory 'api.stdout.log') `
        -RedirectStandardError (Join-Path $logDirectory 'api.stderr.log')

    $deadline = [DateTimeOffset]::UtcNow.AddSeconds(45)
    while ([DateTimeOffset]::UtcNow -lt $deadline) {
        if ($apiProcess.HasExited) {
            throw "Identity API exited before readiness. See $logDirectory."
        }
        if (Test-HttpReady -Uri "http://127.0.0.1:$ApiPort/api/identity/capabilities") {
            break
        }
        Start-Sleep -Milliseconds 250
    }
    if (-not (Test-HttpReady -Uri "http://127.0.0.1:$ApiPort/api/identity/capabilities")) {
        throw "Identity API did not become ready. See $logDirectory."
    }

    Push-Location (Join-Path $repoRoot 'apps\web')
    try {
        & pnpm test:e2e
        if ($LASTEXITCODE -ne 0) {
            throw "Playwright failed with exit code $LASTEXITCODE."
        }
    }
    finally {
        Pop-Location
    }
}
finally {
    if ($apiProcess -and -not $apiProcess.HasExited) {
        Stop-Process -Id $apiProcess.Id -Force
        $apiProcess.WaitForExit()
    }

    if ($postgresBin -and
        ($clusterStarted -or (Test-Path -LiteralPath (Join-Path $dataDirectory 'postmaster.pid')))) {
        & (Join-Path $postgresBin 'pg_ctl.exe') stop -D $dataDirectory -m fast -w | Out-Null
    }

    if (Test-Path -LiteralPath $passwordPath) {
        Remove-Item -LiteralPath $passwordPath -Force
    }

    foreach ($entry in $previousEnvironment.GetEnumerator()) {
        if ($null -eq $entry.Value) {
            Remove-Item -LiteralPath "Env:$($entry.Key)" -ErrorAction SilentlyContinue
        }
        else {
            Set-Item -LiteralPath "Env:$($entry.Key)" -Value $entry.Value
        }
    }

    if (Test-Path -LiteralPath $runDirectory) {
        $resolvedRunDirectory = [IO.Path]::GetFullPath((Resolve-Path -LiteralPath $runDirectory).Path)
        if (-not $resolvedRunDirectory.StartsWith($tempRoot + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) {
            throw 'Refusing to remove an E2E directory outside the expected temporary root.'
        }
        Remove-Item -LiteralPath $resolvedRunDirectory -Recurse -Force
    }

    $postgresPassword = $null
    $idempotencyHmacKey = $null
    $ownerInvitationHmacKey = $null
    $fieldIdempotencyHmacKey = $null
    $fieldRenameHmacKey = $null
}
