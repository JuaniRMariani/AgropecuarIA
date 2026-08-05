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
$postgresBin = 'C:\Program Files\PostgreSQL\17\bin'

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

$port = $PreferredPort
while ($port -lt ($PreferredPort + 20) -and -not (Test-PortAvailable -Port $port)) {
    $port++
}

if ($port -ge ($PreferredPort + 20)) {
    throw "No loopback port available in range $PreferredPort-$($PreferredPort + 19)."
}

New-Item -ItemType Directory -Path $runtimeRoot | Out-Null

$initdb = Join-Path $postgresBin 'initdb.exe'
$pgCtl = Join-Path $postgresBin 'pg_ctl.exe'
$psql = Join-Path $postgresBin 'psql.exe'

& $initdb --pgdata=$dataDirectory --username=postgres --auth-local=trust --auth-host=trust --encoding=UTF8 --no-locale
if ($LASTEXITCODE -ne 0) {
    throw "initdb failed with exit code $LASTEXITCODE."
}

& $pgCtl --pgdata=$dataDirectory --log=$logPath --options="-p $port -h 127.0.0.1" --wait start
if ($LASTEXITCODE -ne 0) {
    throw "pg_ctl start failed with exit code $LASTEXITCODE."
}

try {
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

    $environment = [ordered]@{
        port = $port
        dataDirectory = $dataDirectory
        ownerConnectionString = "Host=127.0.0.1;Port=$port;Database=identity_spike;Username=postgres;Pooling=false"
        appConnectionString = "Host=127.0.0.1;Port=$port;Database=identity_spike;Username=agro_app;Pooling=true;Maximum Pool Size=1;No Reset On Close=false"
        jobConnectionString = "Host=127.0.0.1;Port=$port;Database=identity_spike;Username=agro_job;Pooling=true;Maximum Pool Size=1;No Reset On Close=false"
    }

    $environment | ConvertTo-Json | Set-Content -LiteralPath $environmentPath -Encoding UTF8
    $environment | ConvertTo-Json
}
catch {
    & $pgCtl --pgdata=$dataDirectory --mode=immediate --wait stop | Out-Null
    throw
}
