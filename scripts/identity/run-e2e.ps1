[CmdletBinding()]
param(
    [string]$PostgreSqlBin = $env:AGRO_IDENTITY_POSTGRES_BIN,
    [int]$ApiPort = 5080
)

$ErrorActionPreference = 'Stop'
$repoRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..\..')).Path
$tempRoot = [IO.Path]::GetFullPath((Join-Path ([IO.Path]::GetTempPath()) 'AgropecuarIA.Identity.E2E'))
$runDirectory = [IO.Path]::GetFullPath((Join-Path $tempRoot ([Guid]::NewGuid().ToString('N'))))
$dataDirectory = [IO.Path]::GetFullPath((Join-Path $runDirectory 'data'))
$logDirectory = [IO.Path]::GetFullPath((Join-Path $runDirectory 'logs'))
$apiProcess = $null
$clusterStarted = $false
$previousEnvironment = @{
    ASPNETCORE_ENVIRONMENT = $env:ASPNETCORE_ENVIRONMENT
    ASPNETCORE_URLS = $env:ASPNETCORE_URLS
    ConnectionStrings__Identity = $env:ConnectionStrings__Identity
    AGRO_API_ORIGIN = $env:AGRO_API_ORIGIN
    Identity__ApplyMigrations = $env:Identity__ApplyMigrations
    Identity__DevelopmentProvider__Enabled = $env:Identity__DevelopmentProvider__Enabled
    Identity__StrongAuthentication__Enabled = $env:Identity__StrongAuthentication__Enabled
}

function Resolve-PostgreSqlBin {
    param([string]$ConfiguredPath)

    if ($ConfiguredPath -and (Test-Path -LiteralPath (Join-Path $ConfiguredPath 'initdb.exe'))) {
        return [IO.Path]::GetFullPath($ConfiguredPath)
    }

    $installationRoot = 'C:\Program Files\PostgreSQL'
    if (Test-Path -LiteralPath $installationRoot) {
        $candidate = Get-ChildItem -LiteralPath $installationRoot -Directory |
            Sort-Object Name -Descending |
            ForEach-Object { Join-Path $_.FullName 'bin' } |
            Where-Object { Test-Path -LiteralPath (Join-Path $_ 'initdb.exe') } |
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

try {
    $postgresBin = Resolve-PostgreSqlBin -ConfiguredPath $PostgreSqlBin
    New-Item -ItemType Directory -Path $logDirectory -Force | Out-Null

    & dotnet build (Join-Path $repoRoot 'AgropecuarIA.slnx') -c Release --nologo
    if ($LASTEXITCODE -ne 0) {
        throw "Release build failed with exit code $LASTEXITCODE."
    }

    & (Join-Path $postgresBin 'initdb.exe') -D $dataDirectory -A trust -U postgres --encoding=UTF8 --no-locale
    if ($LASTEXITCODE -ne 0) {
        throw "initdb failed with exit code $LASTEXITCODE."
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

    $env:ASPNETCORE_ENVIRONMENT = 'Test'
    $env:ASPNETCORE_URLS = "http://127.0.0.1:$ApiPort"
    $env:ConnectionStrings__Identity = "Host=127.0.0.1;Port=$postgresPort;Database=postgres;Username=postgres;Pooling=false"
    $env:AGRO_API_ORIGIN = "http://127.0.0.1:$ApiPort"
    $env:Identity__ApplyMigrations = 'true'
    $env:Identity__DevelopmentProvider__Enabled = 'true'
    $env:Identity__StrongAuthentication__Enabled = 'true'

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

    if ($clusterStarted) {
        & (Join-Path $postgresBin 'pg_ctl.exe') stop -D $dataDirectory -m fast -w | Out-Null
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
}
