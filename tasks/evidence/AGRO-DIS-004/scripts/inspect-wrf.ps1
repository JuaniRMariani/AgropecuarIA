[CmdletBinding()]
param(
    [string]$ObjectKey = 'DATA/WRF/DET/2022/01/01/00/WRFDETAR_01H_20220101_00_000.nc',
    [string]$ExpectedSha256 = 'd2283cbe5b6aa68d1595806f0f39e27da28ff3df1b2158d605b94ee1d4a2879c'
)

$ErrorActionPreference = 'Stop'
$evidenceRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..')).Path
$runtimeRoot = Join-Path $evidenceRoot '.runtime\wrf'
$venvRoot = Join-Path $evidenceRoot '.runtime\wrf-venv'
$resultsRoot = Join-Path $evidenceRoot 'results'

New-Item -ItemType Directory -Force -Path $runtimeRoot, $resultsRoot | Out-Null

$fileName = [IO.Path]::GetFileName($ObjectKey)
$fixturePath = Join-Path $runtimeRoot $fileName
$sourceUrl = "https://smn-ar-wrf.s3.us-west-2.amazonaws.com/$ObjectKey"
if (-not (Test-Path -LiteralPath $fixturePath)) {
    & curl.exe --fail --silent --show-error --max-redirs 0 --max-time 120 --max-filesize 26214400 --output $fixturePath $sourceUrl
    if ($LASTEXITCODE -ne 0) { throw "WRF download failed with exit code $LASTEXITCODE." }
}

$actualHash = (Get-FileHash -LiteralPath $fixturePath -Algorithm SHA256).Hash.ToLowerInvariant()
if ($actualHash -ne $ExpectedSha256.ToLowerInvariant()) {
    throw "WRF SHA-256 mismatch. Expected $ExpectedSha256, actual $actualHash."
}

if (-not (Test-Path -LiteralPath $venvRoot)) {
    & python -m venv $venvRoot
    if ($LASTEXITCODE -ne 0) { throw 'Could not create the isolated WRF Python environment.' }
}

$python = Join-Path $venvRoot 'Scripts\python.exe'
& $python -m pip install --disable-pip-version-check --requirement (Join-Path $PSScriptRoot 'requirements-wrf.txt')
if ($LASTEXITCODE -ne 0) { throw 'Could not install the pinned WRF parser dependencies.' }

$output = Join-Path $resultsRoot 'wrf-sample.json'
& $python (Join-Path $PSScriptRoot 'inspect-wrf.py') $fixturePath --expected-sha256 $ExpectedSha256 --output $output
if ($LASTEXITCODE -ne 0) { throw 'WRF inspection failed.' }

Write-Output "WRF evidence: $output"
