[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$evidenceRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..')).Path

$schemas = Get-ChildItem -LiteralPath (Join-Path $evidenceRoot 'contracts') -Filter '*.schema.json'
if ($schemas.Count -ne 4) { throw "Expected 4 contract schemas, found $($schemas.Count)." }
foreach ($schemaFile in $schemas) {
    $schema = Get-Content -Raw -LiteralPath $schemaFile.FullName | ConvertFrom-Json
    if ($schema.'$schema' -ne 'https://json-schema.org/draft/2020-12/schema') { throw "$($schemaFile.Name) is not JSON Schema 2020-12." }
    if ([string]::IsNullOrWhiteSpace([string]$schema.'$id')) { throw "$($schemaFile.Name) has no stable ID." }
    if (@($schema.required).Count -eq 0) { throw "$($schemaFile.Name) has no required fields." }
}

$territory = Get-Content -Raw -LiteralPath (Join-Path $evidenceRoot 'fixtures\territory\national-points.json') | ConvertFrom-Json
if ($territory.points.Count -ne 24) { throw 'National fixture does not contain 24 jurisdictions.' }
if (($territory.points.id | Sort-Object -Unique).Count -ne 24) { throw 'National fixture IDs are not unique.' }
foreach ($point in $territory.points) {
    $latitude = [double]$point.latitude
    $longitude = [double]$point.longitude
    if ([double]::IsNaN($latitude) -or [double]::IsInfinity($latitude) -or [double]::IsNaN($longitude) -or [double]::IsInfinity($longitude)) { throw "Non-finite coordinate for $($point.id)." }
    if ($point.latitude -lt -90 -or $point.latitude -gt 90 -or $point.longitude -lt -180 -or $point.longitude -gt 180) { throw "Coordinate out of WGS84 range for $($point.id)." }
}

$providerEvidence = Get-Content -Raw -LiteralPath (Join-Path $evidenceRoot 'results\provider-probes.json') | ConvertFrom-Json
if ($providerEvidence.providerRuns.Count -ne 5) { throw 'Provider evidence must contain five provider decisions.' }
$expectedProviders = @('argenmap', 'georef', 'open-meteo', 'smn-cap', 'smn-wrf')
$actualProviders = @($providerEvidence.providerRuns.provider | Sort-Object)
if (($actualProviders -join ',') -ne ($expectedProviders -join ',')) { throw 'Provider decision set is incomplete.' }
foreach ($run in $providerEvidence.providerRuns) {
    if ($run.contractVersion -ne '1.0' -or [string]::IsNullOrWhiteSpace([string]$run.sourceUrl)) { throw "Provider run $($run.provider) does not match contract v1." }
    if ($run.status -notin @('success', 'degraded', 'failed', 'not-authorized')) { throw "Provider run $($run.provider) has invalid status." }
    if ($run.decision -notin @('go', 'conditional-go', 'postpone', 'no-go')) { throw "Provider run $($run.provider) has invalid decision." }
}
if (($providerEvidence.providerRuns | Where-Object provider -eq 'smn-wrf').decision -ne 'postpone') { throw 'WRF must remain postponed for this spike.' }
$weatherRun = $providerEvidence.providerRuns | Where-Object provider -eq 'open-meteo'
if ($weatherRun.durationMs -gt $weatherRun.coverage.smokeTargetMs -and $weatherRun.status -ne 'degraded') {
    throw 'Open-Meteo missed its latency target without recording degraded status.'
}
$mapRun = $providerEvidence.providerRuns | Where-Object provider -eq 'argenmap'
if ($mapRun.durationMs -gt $mapRun.coverage.p75TargetMs -and $mapRun.status -ne 'degraded') {
    throw 'Argenmap missed its latency target without recording degraded status.'
}

$wrf = Get-Content -Raw -LiteralPath (Join-Path $evidenceRoot 'results\wrf-sample.json') | ConvertFrom-Json
if (-not $wrf.withinSpikeBudgets.fileBytes -or -not $wrf.withinSpikeBudgets.gridCells -or -not $wrf.withinSpikeBudgets.parseDuration -or -not $wrf.withinSpikeBudgets.pythonMemory -or -not $wrf.withinSpikeBudgets.processWorkingSet) { throw 'WRF sample exceeded a spike budget.' }
if ([int64]$wrf.fileBytes * 73 -le 1GB) { throw 'WRF evidence no longer supports the recorded >1 GiB per-run estimate.' }

Write-Output "Evidence validation passed: 4 schemas, 24 jurisdictions, 5 provider decisions and WRF budgets."
