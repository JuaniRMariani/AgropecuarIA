[CmdletBinding()]
param(
    [ValidateRange(1024, 65535)]
    [int]$Port = 55434,

    [switch]$KeepData
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$ProgressPreference = 'SilentlyContinue'

$postgisVersion = '3.6.2'
$bundleName = 'postgis-bundle-pg17-3.6.2x64.zip'
$bundleBaseUrl = 'https://download.osgeo.org/postgis/windows/pg17'
$expectedMd5 = 'faca768cc580c4ab2eef621be05b408e'
$expectedSha256 = '7ba180ee2a352987b9a2f194673652c59483b55852295ccf401dceccd8765425'

$scriptRoot = [IO.Path]::GetFullPath($PSScriptRoot)
$runtimeRoot = Join-Path $scriptRoot '.runtime'
$postgresRoot = Join-Path $runtimeRoot 'postgresql-17-postgis-3.6.2'
$downloadRoot = Join-Path $runtimeRoot 'downloads'
$bundlePath = Join-Path $downloadRoot $bundleName
$dataRoot = Join-Path $runtimeRoot "data-$Port"
$logRoot = Join-Path $runtimeRoot 'logs'
$serverLog = Join-Path $logRoot "postgres-$Port.log"
$databaseName = 'agro_dis_004_spatial_spike'
$serverStarted = $false
$runStartedAt = Get-Date

function Assert-ChildPath {
    param(
        [Parameter(Mandatory)] [string]$Candidate,
        [Parameter(Mandatory)] [string]$Parent
    )

    $candidateFull = [IO.Path]::GetFullPath($Candidate).TrimEnd('\') + '\'
    $parentFull = [IO.Path]::GetFullPath($Parent).TrimEnd('\') + '\'
    if (-not $candidateFull.StartsWith($parentFull, [StringComparison]::OrdinalIgnoreCase)) {
        throw "Refusing operation outside the expected runtime root: $candidateFull"
    }
}

function Remove-ValidatedDirectory {
    param([Parameter(Mandatory)] [string]$Path)

    Assert-ChildPath -Candidate $Path -Parent $runtimeRoot
    if (Test-Path -LiteralPath $Path) {
        Remove-Item -LiteralPath $Path -Recurse -Force
    }
}

function Invoke-CheckedNative {
    param(
        [Parameter(Mandatory)] [string]$Executable,
        [Parameter(Mandatory)] [string[]]$Arguments
    )

    & $Executable @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "Native command failed with exit code $LASTEXITCODE`: $Executable $($Arguments -join ' ')"
    }
}

function Assert-PortAvailable {
    param([Parameter(Mandatory)] [int]$RequestedPort)

    $listener = [Net.Sockets.TcpListener]::new([Net.IPAddress]::Loopback, $RequestedPort)
    try {
        $listener.Start()
    }
    catch {
        throw "TCP port $RequestedPort is not available on 127.0.0.1."
    }
    finally {
        $listener.Stop()
    }
}

function Get-WebContentText {
    param([Parameter(Mandatory)] [string]$Uri)

    $response = Invoke-WebRequest -UseBasicParsing -Uri $Uri -TimeoutSec 60 -MaximumRedirection 0
    if ($response.Content -is [byte[]]) {
        return [Text.Encoding]::UTF8.GetString($response.Content)
    }

    return [string]$response.Content
}

function Assert-SafeZipEntries {
    param(
        [Parameter(Mandatory)] [string]$ZipPath,
        [Parameter(Mandatory)] [string]$Destination
    )

    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $destinationFull = [IO.Path]::GetFullPath($Destination).TrimEnd('\') + '\'
    $archive = [IO.Compression.ZipFile]::OpenRead($ZipPath)
    try {
        foreach ($entry in $archive.Entries) {
            $entryDestination = [IO.Path]::GetFullPath((Join-Path $Destination $entry.FullName))
            if (-not ($entryDestination + '\').StartsWith($destinationFull, [StringComparison]::OrdinalIgnoreCase)) {
                throw "Unsafe ZIP entry rejected: $($entry.FullName)"
            }
        }
    }
    finally {
        $archive.Dispose()
    }
}

# A single shared runtime/extraction tree cannot be mutated safely by concurrent runs.
New-Item -ItemType Directory -Path $runtimeRoot -Force | Out-Null
$lockPath = Join-Path $runtimeRoot 'run.lock'
try {
    $lockStream = [IO.File]::Open($lockPath, [IO.FileMode]::OpenOrCreate, [IO.FileAccess]::ReadWrite, [IO.FileShare]::None)
}
catch {
    throw 'Another AGRO-DIS-004 PostGIS harness is already running; concurrent execution is refused.'
}

function Install-IsolatedPostgresRuntime {
    $markerPath = Join-Path $postgresRoot '.agro-dis-004-runtime.json'
    $extensionControl = Join-Path $postgresRoot 'share\extension\postgis.control'

    if ((Test-Path -LiteralPath $markerPath) -and (Test-Path -LiteralPath $extensionControl)) {
        $marker = Get-Content -Raw -LiteralPath $markerPath | ConvertFrom-Json
        $cachedBundleMd5 = if (Test-Path -LiteralPath $bundlePath) {
            (Get-FileHash -LiteralPath $bundlePath -Algorithm MD5).Hash.ToLowerInvariant()
        }
        $cachedBundleSha256 = if (Test-Path -LiteralPath $bundlePath) {
            (Get-FileHash -LiteralPath $bundlePath -Algorithm SHA256).Hash.ToLowerInvariant()
        }
        if (
            $marker.postgisVersion -eq $postgisVersion -and
            $marker.bundleMd5 -eq $expectedMd5 -and
            $marker.bundleSha256 -eq $expectedSha256 -and
            $cachedBundleMd5 -eq $expectedMd5 -and
            $cachedBundleSha256 -eq $expectedSha256
        ) {
            Write-Host "Reusing verified isolated PostgreSQL/PostGIS runtime: $postgresRoot"
            return
        }
    }

    $pgConfigCommand = Get-Command pg_config -ErrorAction Stop
    $systemBin = Split-Path -Parent $pgConfigCommand.Source
    $systemPostgresRoot = Split-Path -Parent $systemBin
    $systemVersion = (& $pgConfigCommand.Source --version).Trim()
    if ($LASTEXITCODE -ne 0 -or $systemVersion -notmatch '^PostgreSQL 17(?:\.|$)') {
        throw "PostgreSQL 17 is required; found '$systemVersion'."
    }

    Remove-ValidatedDirectory -Path $postgresRoot
    New-Item -ItemType Directory -Path $postgresRoot -Force | Out-Null

    foreach ($directoryName in @('bin', 'lib', 'share')) {
        $sourceDirectory = Join-Path $systemPostgresRoot $directoryName
        if (-not (Test-Path -LiteralPath $sourceDirectory)) {
            throw "Required PostgreSQL runtime directory is missing: $sourceDirectory"
        }

        Copy-Item -LiteralPath $sourceDirectory -Destination $postgresRoot -Recurse -Force
    }

    New-Item -ItemType Directory -Path $downloadRoot -Force | Out-Null
    $bundleUri = "$bundleBaseUrl/$bundleName"
    $checksumUri = "$bundleUri.md5"

    $publishedChecksum = Get-WebContentText -Uri $checksumUri
    $publishedMd5 = [regex]::Match($publishedChecksum, '(?i)\b[0-9a-f]{32}\b').Value.ToLowerInvariant()
    if ($publishedMd5 -ne $expectedMd5) {
        throw "The official checksum sidecar does not match the pinned MD5. Published=$publishedMd5 pinned=$expectedMd5"
    }

    if (-not (Test-Path -LiteralPath $bundlePath)) {
        Write-Host "Downloading official PostGIS bundle from $bundleUri"
        Invoke-WebRequest -UseBasicParsing -Uri $bundleUri -OutFile $bundlePath -TimeoutSec 600 -MaximumRedirection 0
    }

    $actualMd5 = (Get-FileHash -LiteralPath $bundlePath -Algorithm MD5).Hash.ToLowerInvariant()
    if ($actualMd5 -ne $expectedMd5) {
        throw "PostGIS bundle MD5 mismatch. Expected=$expectedMd5 actual=$actualMd5"
    }

    $actualSha256 = (Get-FileHash -LiteralPath $bundlePath -Algorithm SHA256).Hash.ToLowerInvariant()
    if ($expectedSha256 -and $actualSha256 -ne $expectedSha256) {
        throw "PostGIS bundle SHA-256 mismatch. Expected=$expectedSha256 actual=$actualSha256"
    }

    $extractRoot = Join-Path $runtimeRoot 'postgis-extract'
    Remove-ValidatedDirectory -Path $extractRoot
    New-Item -ItemType Directory -Path $extractRoot -Force | Out-Null
    Assert-SafeZipEntries -ZipPath $bundlePath -Destination $extractRoot
    Expand-Archive -LiteralPath $bundlePath -DestinationPath $extractRoot -Force

    $controlFile = Get-ChildItem -LiteralPath $extractRoot -Filter 'postgis.control' -Recurse -File |
        Select-Object -First 1
    if ($null -eq $controlFile) {
        throw 'The verified bundle does not contain share/extension/postgis.control.'
    }

    $bundleRoot = Split-Path -Parent (Split-Path -Parent $controlFile.DirectoryName)
    foreach ($directoryName in @('bin', 'lib', 'share')) {
        $sourceDirectory = Join-Path $bundleRoot $directoryName
        if (Test-Path -LiteralPath $sourceDirectory) {
            Copy-Item -Path (Join-Path $sourceDirectory '*') -Destination (Join-Path $postgresRoot $directoryName) -Recurse -Force
        }
    }

    if (-not (Test-Path -LiteralPath $extensionControl)) {
        throw "PostGIS extension control was not installed into the isolated runtime: $extensionControl"
    }

    [ordered]@{
        sourcePostgreSql = $systemVersion
        postgisVersion = $postgisVersion
        bundleUri = $bundleUri
        bundleMd5 = $actualMd5
        bundleSha256 = $actualSha256
        createdAtUtc = [DateTimeOffset]::UtcNow.ToString('O')
    } | ConvertTo-Json | Set-Content -LiteralPath $markerPath -Encoding UTF8

    Remove-ValidatedDirectory -Path $extractRoot
    Write-Host "Created isolated PostgreSQL/PostGIS runtime: $postgresRoot"
    Write-Host "Verified bundle SHA-256: $actualSha256"
}

try {
    Assert-PortAvailable -RequestedPort $Port
    Install-IsolatedPostgresRuntime

    $binRoot = Join-Path $postgresRoot 'bin'
    $initDb = Join-Path $binRoot 'initdb.exe'
    $pgCtl = Join-Path $binRoot 'pg_ctl.exe'
    $createdb = Join-Path $binRoot 'createdb.exe'
    $psql = Join-Path $binRoot 'psql.exe'
    foreach ($executable in @($initDb, $pgCtl, $createdb, $psql)) {
        Assert-ChildPath -Candidate $executable -Parent $postgresRoot
        if (-not (Test-Path -LiteralPath $executable)) {
            throw "Required isolated executable is missing: $executable"
        }
    }

    $env:PATH = "$binRoot;$env:PATH"
    Remove-ValidatedDirectory -Path $dataRoot
    New-Item -ItemType Directory -Path $logRoot -Force | Out-Null

    Invoke-CheckedNative -Executable $initDb -Arguments @(
        '--pgdata', $dataRoot,
        '--username', 'postgres',
        '--auth', 'trust',
        '--encoding', 'UTF8',
        '--no-locale'
    )

    @(
        '# AGRO-DIS-004: isolated, disposable and loopback-only.',
        "listen_addresses = '127.0.0.1'",
        "port = $Port",
        "max_connections = 20",
        "shared_buffers = '128MB'"
    ) | Add-Content -LiteralPath (Join-Path $dataRoot 'postgresql.conf') -Encoding UTF8

    @(
        '# TYPE  DATABASE  USER  ADDRESS         METHOD',
        'host    all       all   127.0.0.1/32    trust'
    ) | Set-Content -LiteralPath (Join-Path $dataRoot 'pg_hba.conf') -Encoding ASCII

    Invoke-CheckedNative -Executable $pgCtl -Arguments @(
        'start', '--wait', '--timeout', '30', '--pgdata', $dataRoot, '--log', $serverLog
    )
    $serverStarted = $true

    Invoke-CheckedNative -Executable $createdb -Arguments @(
        '--host', '127.0.0.1', '--port', "$Port", '--username', 'postgres', $databaseName
    )

    $testScript = Join-Path $scriptRoot 'test-spatial-contract.sql'
    Invoke-CheckedNative -Executable $psql -Arguments @(
        '--host', '127.0.0.1',
        '--port', "$Port",
        '--username', 'postgres',
        '--dbname', $databaseName,
        '--set', 'ON_ERROR_STOP=1',
        '--file', $testScript
    )

    $duration = (Get-Date) - $runStartedAt
    Write-Host ('AGRO-DIS-004 PostGIS spike PASS in {0:N2}s.' -f $duration.TotalSeconds)
}
finally {
    $pgCtl = Join-Path $postgresRoot 'bin\pg_ctl.exe'
    if ((Test-Path -LiteralPath $pgCtl) -and (Test-Path -LiteralPath $dataRoot)) {
        & $pgCtl status --pgdata $dataRoot *> $null
        $serverIsRunning = $LASTEXITCODE -eq 0
        if ($serverStarted -or $serverIsRunning) {
            & $pgCtl stop --wait --timeout 30 --mode fast --pgdata $dataRoot
            if ($LASTEXITCODE -ne 0) {
                throw "Failed to stop the isolated PostgreSQL server on port $Port."
            }
            $serverStarted = $false
        }
    }

    if ((Test-Path -LiteralPath $pgCtl) -and (Test-Path -LiteralPath $dataRoot)) {
        & $pgCtl status --pgdata $dataRoot *> $null
        if ($LASTEXITCODE -eq 0) {
            throw 'Teardown validation failed: the isolated PostgreSQL server is still running.'
        }
    }

    Assert-PortAvailable -RequestedPort $Port
    Write-Host "Teardown validated: PostgreSQL is stopped and 127.0.0.1:$Port is free."

    if (-not $KeepData -and (Test-Path -LiteralPath $dataRoot)) {
        Remove-ValidatedDirectory -Path $dataRoot
    }

    $lockStream.Dispose()
    $global:LASTEXITCODE = 0
}
