[CmdletBinding()]
param(
    [int]$PreferredPort = 55433
)

$ErrorActionPreference = 'Stop'

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$spikeRoot = [System.IO.Path]::GetFullPath((Join-Path $scriptRoot '..'))
$runtimeRoot = [System.IO.Path]::GetFullPath((Join-Path $spikeRoot '.runtime'))
$dataDirectory = Join-Path $runtimeRoot 'postgres-data'
$logPath = Join-Path $runtimeRoot 'postgres.log'
$environmentPath = Join-Path $runtimeRoot 'postgres.env.json'
$passwordPath = Join-Path $runtimeRoot 'postgres.pwfile'
$postgresBin = 'C:\Program Files\PostgreSQL\17\bin'
$expectedRuntimePrefix = $spikeRoot + [System.IO.Path]::DirectorySeparatorChar

if (-not $runtimeRoot.StartsWith(
    $expectedRuntimePrefix,
    [System.StringComparison]::OrdinalIgnoreCase)) {
    throw "Refusing to manage a runtime outside the spike: $runtimeRoot"
}

foreach ($executable in @('initdb.exe', 'pg_ctl.exe', 'psql.exe')) {
    $candidate = Join-Path $postgresBin $executable
    if (-not (Test-Path -LiteralPath $candidate)) {
        throw "PostgreSQL 17 executable not found: $candidate"
    }
}

if (Test-Path -LiteralPath $runtimeRoot) {
    throw "Ephemeral runtime already exists. Stop it first: $runtimeRoot"
}

function Test-PortAvailable([int]$Port) {
    $listener = [System.Net.Sockets.TcpListener]::new([System.Net.IPAddress]::Loopback, $Port)
    try {
        $listener.Start()
        return $true
    }
    catch {
        return $false
    }
    finally {
        $listener.Stop()
    }
}

function New-EphemeralPassword {
    $bytes = New-Object byte[] 32
    $generator = [System.Security.Cryptography.RandomNumberGenerator]::Create()
    try {
        $generator.GetBytes($bytes)
    }
    finally {
        $generator.Dispose()
    }
    return [Convert]::ToBase64String($bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_')
}

function Set-OwnerOnlyAcl([string]$Path) {
    $currentUser = [System.Security.Principal.WindowsIdentity]::GetCurrent().User
    $item = Get-Item -LiteralPath $Path

    if ($item.PSIsContainer) {
        $acl = [System.Security.AccessControl.DirectorySecurity]::new()
        $inheritance = [System.Security.AccessControl.InheritanceFlags]::ContainerInherit -bor
            [System.Security.AccessControl.InheritanceFlags]::ObjectInherit
        $rule = [System.Security.AccessControl.FileSystemAccessRule]::new(
            $currentUser,
            [System.Security.AccessControl.FileSystemRights]::FullControl,
            $inheritance,
            [System.Security.AccessControl.PropagationFlags]::None,
            [System.Security.AccessControl.AccessControlType]::Allow)
    }
    else {
        $acl = [System.Security.AccessControl.FileSecurity]::new()
        $rule = [System.Security.AccessControl.FileSystemAccessRule]::new(
            $currentUser,
            [System.Security.AccessControl.FileSystemRights]::FullControl,
            [System.Security.AccessControl.AccessControlType]::Allow)
    }

    $acl.SetOwner($currentUser)
    $acl.SetAccessRuleProtection($true, $false)
    [void]$acl.AddAccessRule($rule)
    Set-Acl -LiteralPath $Path -AclObject $acl
}

function Remove-EphemeralRuntime {
    if (-not (Test-Path -LiteralPath $runtimeRoot)) {
        return
    }

    [System.IO.Directory]::EnumerateFiles(
        $runtimeRoot,
        '*',
        [System.IO.SearchOption]::AllDirectories) |
        ForEach-Object {
            [System.IO.File]::SetAttributes($_, [System.IO.FileAttributes]::Normal)
        }
    [System.IO.Directory]::Delete($runtimeRoot, $true)
}

function Test-PsqlConnectionRejected(
    [string]$Executable,
    [int]$Port,
    [string]$UserName) {
    $previousPreference = $ErrorActionPreference
    try {
        $ErrorActionPreference = 'SilentlyContinue'
        & $Executable --host=127.0.0.1 --port=$Port --username=$UserName `
            --dbname=identity_spike --no-password --command='select 1' `
            1>$null 2>$null
        return $LASTEXITCODE -ne 0
    }
    finally {
        $ErrorActionPreference = $previousPreference
    }
}

$port = $PreferredPort
while ($port -lt ($PreferredPort + 20) -and -not (Test-PortAvailable -Port $port)) {
    $port++
}

if ($port -ge ($PreferredPort + 20)) {
    throw "No loopback port available in range $PreferredPort-$($PreferredPort + 19)."
}

New-Item -ItemType Directory -Path $runtimeRoot | Out-Null
Set-OwnerOnlyAcl -Path $runtimeRoot

$initdb = Join-Path $postgresBin 'initdb.exe'
$pgCtl = Join-Path $postgresBin 'pg_ctl.exe'
$psql = Join-Path $postgresBin 'psql.exe'
$utf8WithoutBom = [System.Text.UTF8Encoding]::new($false)
$postgresPassword = New-EphemeralPassword
$appPassword = New-EphemeralPassword
$jobPassword = New-EphemeralPassword
$discoveryPassword = New-EphemeralPassword
$passwords = [System.Collections.Generic.HashSet[string]]::new(
    [System.StringComparer]::Ordinal)
foreach ($password in @(
    $postgresPassword,
    $appPassword,
    $jobPassword,
    $discoveryPassword)) {
    [void]$passwords.Add($password)
}
if ($passwords.Count -ne 4) {
    Remove-EphemeralRuntime
    throw 'Cryptographic password generation produced a duplicate value.'
}

try {
    [System.IO.File]::WriteAllText($passwordPath, $postgresPassword, $utf8WithoutBom)
    Set-OwnerOnlyAcl -Path $passwordPath

    & $initdb --pgdata=$dataDirectory --username=postgres `
        --auth-local=scram-sha-256 --auth-host=scram-sha-256 `
        --pwfile=$passwordPath --encoding=UTF8 --no-locale
    if ($LASTEXITCODE -ne 0) {
        throw "initdb failed with exit code $LASTEXITCODE."
    }

    Remove-Item -LiteralPath $passwordPath -Force

    & $pgCtl --pgdata=$dataDirectory --log=$logPath `
        --options="-p $port -h 127.0.0.1" --wait start
    if ($LASTEXITCODE -ne 0) {
        throw "pg_ctl start failed with exit code $LASTEXITCODE."
    }

    $env:PGPASSWORD = $postgresPassword
    $env:AGRO_APP_PASSWORD = $appPassword
    $env:AGRO_JOB_PASSWORD = $jobPassword
    $env:AGRO_DISCOVERY_PASSWORD = $discoveryPassword

    & $psql --host=127.0.0.1 --port=$port --username=postgres --dbname=postgres --set=ON_ERROR_STOP=1 --command='CREATE DATABASE identity_spike;'
    if ($LASTEXITCODE -ne 0) {
        throw "Database creation failed with exit code $LASTEXITCODE."
    }

    $databaseRunner = Join-Path $spikeRoot 'database\run-all.psql'
    if (-not (Test-Path -LiteralPath $databaseRunner)) {
        throw "Database runner not found: $databaseRunner"
    }

    & $psql --host=127.0.0.1 --port=$port --username=postgres --dbname=identity_spike --set=ON_ERROR_STOP=1 --file=$databaseRunner
    if ($LASTEXITCODE -ne 0) {
        throw "Database verification failed: $databaseRunner"
    }

    Remove-Item Env:PGPASSWORD
    if (-not (Test-PsqlConnectionRejected `
        -Executable $psql `
        -Port $port `
        -UserName 'agro_app')) {
        throw 'Passwordless TCP authentication unexpectedly succeeded.'
    }

    $env:PGPASSWORD = $postgresPassword
    if (-not (Test-PsqlConnectionRejected `
        -Executable $psql `
        -Port $port `
        -UserName 'agro_unknown')) {
        throw 'Authentication unexpectedly accepted an unknown database user.'
    }

    $hbaPath = Join-Path $dataDirectory 'pg_hba.conf'
    if (Select-String -LiteralPath $hbaPath -Pattern '^\s*(local|host)\s+.*\strust\s*$' -Quiet) {
        throw 'Ephemeral PostgreSQL pg_hba.conf contains a trust authentication rule.'
    }

    $environment = [ordered]@{
        port = $port
        dataDirectory = $dataDirectory
        ownerConnectionString = "Host=127.0.0.1;Port=$port;Database=identity_spike;Username=postgres;Password=$postgresPassword;Pooling=false"
        appConnectionString = "Host=127.0.0.1;Port=$port;Database=identity_spike;Username=agro_app;Password=$appPassword;Pooling=true;Maximum Pool Size=1;No Reset On Close=false"
        jobConnectionString = "Host=127.0.0.1;Port=$port;Database=identity_spike;Username=agro_job;Password=$jobPassword;Pooling=true;Maximum Pool Size=1;No Reset On Close=false"
        discoveryConnectionString = "Host=127.0.0.1;Port=$port;Database=identity_spike;Username=agro_membership_discovery;Password=$discoveryPassword;Pooling=true;Maximum Pool Size=1;No Reset On Close=false"
    }

    [System.IO.File]::WriteAllText(
        $environmentPath,
        ($environment | ConvertTo-Json),
        $utf8WithoutBom)
    Set-OwnerOnlyAcl -Path $environmentPath
    Write-Output "Ephemeral PostgreSQL is ready on loopback port $port."
    Write-Output "Connection details were written with owner-only ACL: $environmentPath"
}
catch {
    if (Test-Path -LiteralPath (Join-Path $dataDirectory 'postmaster.pid')) {
        & $pgCtl --pgdata=$dataDirectory --mode=immediate --wait stop *> $null
    }
    Remove-EphemeralRuntime
    throw
}
finally {
    if (Test-Path -LiteralPath $passwordPath) {
        Remove-Item -LiteralPath $passwordPath -Force
    }
    Remove-Item Env:PGPASSWORD -ErrorAction SilentlyContinue
    Remove-Item Env:AGRO_APP_PASSWORD -ErrorAction SilentlyContinue
    Remove-Item Env:AGRO_JOB_PASSWORD -ErrorAction SilentlyContinue
    Remove-Item Env:AGRO_DISCOVERY_PASSWORD -ErrorAction SilentlyContinue
}
