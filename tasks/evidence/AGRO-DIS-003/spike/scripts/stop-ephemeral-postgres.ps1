[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$spikeRoot = [System.IO.Path]::GetFullPath((Join-Path $scriptRoot '..'))
$runtimeRoot = [System.IO.Path]::GetFullPath((Join-Path $spikeRoot '.runtime'))
$expectedPrefix = $spikeRoot + [System.IO.Path]::DirectorySeparatorChar

if (-not $runtimeRoot.StartsWith($expectedPrefix, [System.StringComparison]::OrdinalIgnoreCase)) {
    throw "Refusing to remove a runtime outside the spike: $runtimeRoot"
}

if (-not (Test-Path -LiteralPath $runtimeRoot)) {
    Write-Output 'Ephemeral PostgreSQL runtime is already absent.'
    exit 0
}

$dataDirectory = Join-Path $runtimeRoot 'postgres-data'
$pgCtl = 'C:\Program Files\PostgreSQL\17\bin\pg_ctl.exe'

if (Test-Path -LiteralPath (Join-Path $dataDirectory 'postmaster.pid')) {
    & $pgCtl --pgdata=$dataDirectory --mode=fast --wait stop
    if ($LASTEXITCODE -ne 0) {
        throw "pg_ctl stop failed with exit code $LASTEXITCODE."
    }
}

[System.IO.Directory]::EnumerateFiles($runtimeRoot, '*', [System.IO.SearchOption]::AllDirectories) |
    ForEach-Object { [System.IO.File]::SetAttributes($_, [System.IO.FileAttributes]::Normal) }
[System.IO.Directory]::Delete($runtimeRoot, $true)

Write-Output "Removed ephemeral runtime: $runtimeRoot"
