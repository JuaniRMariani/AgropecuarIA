[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$evidenceRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..')).Path
$fixturePath = Join-Path $evidenceRoot 'fixtures\territory\national-points.json'
$runtimeRoot = Join-Path $evidenceRoot '.runtime\provider-probes'
$resultsRoot = Join-Path $evidenceRoot 'results'
New-Item -ItemType Directory -Force -Path $runtimeRoot, $resultsRoot | Out-Null

function Invoke-Probe {
    param(
        [Parameter(Mandatory)][string]$Name,
        [Parameter(Mandatory)][string]$Url,
        [int]$TimeoutSeconds = 30,
        [int64]$MaximumBytes = 8388608
    )

    $outputPath = Join-Path $runtimeRoot "$([Guid]::NewGuid().ToString('N')).body"
    try {
        $metrics = & curl.exe --fail-with-body --silent --show-error --max-redirs 0 --max-time $TimeoutSeconds --max-filesize $MaximumBytes `
            --user-agent 'AgropecuarIA-R0-Spike/1.0' --output $outputPath `
            --write-out '%{http_code}|%{time_total}|%{size_download}|%{content_type}|%{url_effective}' $Url
        $exitCode = $LASTEXITCODE
        if ($exitCode -ne 0) { throw "$Name failed with curl exit code $exitCode." }
        $parts = $metrics -split '\|', 5
        if ($parts.Count -ne 5) { throw "$Name returned malformed curl metrics." }
        if ([int64]$parts[2] -gt $MaximumBytes) { throw "$Name exceeded the $MaximumBytes byte response limit." }
        $hash = (Get-FileHash -LiteralPath $outputPath -Algorithm SHA256).Hash.ToLowerInvariant()
        [PSCustomObject]@{
            name = $Name
            statusCode = [int]$parts[0]
            durationMs = [Math]::Round(([double]::Parse($parts[1], [Globalization.CultureInfo]::InvariantCulture) * 1000), 3)
            responseBytes = [int64]$parts[2]
            contentType = $parts[3]
            finalUrl = $parts[4]
            sha256 = $hash
            bodyPath = $outputPath
        }
    }
    catch {
        if (Test-Path -LiteralPath $outputPath) { Remove-Item -LiteralPath $outputPath -Force }
        throw
    }
}

function Read-SafeXml {
    param(
        [Parameter(Mandatory)][string]$Path,
        [int64]$MaximumCharacters = 2097152
    )

    $settings = [Xml.XmlReaderSettings]::new()
    $settings.DtdProcessing = [Xml.DtdProcessing]::Prohibit
    $settings.XmlResolver = $null
    $settings.MaxCharactersInDocument = $MaximumCharacters
    $settings.MaxCharactersFromEntities = 0
    $reader = [Xml.XmlReader]::Create($Path, $settings)
    try {
        $document = [Xml.XmlDocument]::new()
        $document.XmlResolver = $null
        $document.Load($reader)
        return $document
    }
    finally {
        $reader.Dispose()
    }
}

function Get-Percentile75 {
    param([double[]]$Values)
    $ordered = @($Values | Sort-Object)
    if ($ordered.Count -eq 0) { return 0 }
    $index = [Math]::Ceiling(0.75 * $ordered.Count) - 1
    return [Math]::Round($ordered[[Math]::Max(0, $index)], 3)
}

function Get-TmsTile {
    param([double]$Latitude, [double]$Longitude, [int]$Zoom)
    $limitedLatitude = [Math]::Max(-85.05112878, [Math]::Min(85.05112878, $Latitude))
    $count = [Math]::Pow(2, $Zoom)
    $x = [Math]::Floor((($Longitude + 180) / 360) * $count)
    $radians = $limitedLatitude * [Math]::PI / 180
    $xyzY = [Math]::Floor((1 - [Math]::Log([Math]::Tan($radians) + (1 / [Math]::Cos($radians))) / [Math]::PI) / 2 * $count)
    [PSCustomObject]@{ x = [int]$x; y = [int](($count - 1) - $xyzY) }
}

$fixture = Get-Content -Raw -LiteralPath $fixturePath | ConvertFrom-Json
if ($fixture.points.Count -ne 24) { throw 'The national fixture must contain exactly 24 jurisdictions.' }
if (($fixture.points.id | Sort-Object -Unique).Count -ne 24) { throw 'The national fixture contains duplicate IDs.' }

$observedAt = [DateTimeOffset]::UtcNow.ToString('O')
$providerRuns = [Collections.Generic.List[object]]::new()

$georefUrl = 'https://apis.datos.gob.ar/georef/api/provincias?campos=id,nombre,centroide&max=24'
$georef = Invoke-Probe -Name 'georef-national' -Url $georefUrl
$georefBody = Get-Content -Raw -LiteralPath $georef.bodyPath | ConvertFrom-Json
if ($georef.statusCode -ne 200 -or $georefBody.cantidad -ne 24) { throw 'Georef did not return all 24 jurisdictions.' }
$providerRuns.Add([PSCustomObject]@{
    contractVersion = '1.0'; provider = 'georef'; sourceUrl = $georefUrl; observedAt = $observedAt; status = 'success'; durationMs = $georef.durationMs
    responseBytes = $georef.responseBytes; contentHash = $georef.sha256; coverage = @{ successful = 24; total = 24 }
    decision = 'conditional-go'; limitations = @('No contractual SLA; keep a versioned snapshot and manual-coordinate fallback.')
})

$latitudes = ($fixture.points | ForEach-Object { [string]::Format([Globalization.CultureInfo]::InvariantCulture, '{0:R}', [double]$_.latitude) }) -join ','
$longitudes = ($fixture.points | ForEach-Object { [string]::Format([Globalization.CultureInfo]::InvariantCulture, '{0:R}', [double]$_.longitude) }) -join ','
$weatherVariables = @('temperature_2m', 'precipitation', 'precipitation_probability', 'wind_speed_10m', 'relative_humidity_2m', 'wind_gusts_10m', 'et0_fao_evapotranspiration')
$weatherUrl = "https://api.open-meteo.com/v1/forecast?latitude=$latitudes&longitude=$longitudes&hourly=$($weatherVariables -join ',')&forecast_days=1&timezone=UTC"
$weather = Invoke-Probe -Name 'open-meteo-national' -Url $weatherUrl -TimeoutSeconds 45
$weatherBody = Get-Content -Raw -LiteralPath $weather.bodyPath | ConvertFrom-Json
if ($weather.statusCode -ne 200 -or @($weatherBody).Count -ne 24) { throw 'Open-Meteo did not return one response per jurisdiction.' }
foreach ($response in @($weatherBody)) {
    foreach ($variable in $weatherVariables) {
        if ($null -eq $response.hourly_units.$variable -or $null -eq $response.hourly.$variable) {
            throw "Open-Meteo response is missing $variable or its unit."
        }
    }
}
$weatherTargetMs = 2000
$weatherStatus = if ($weather.durationMs -le $weatherTargetMs) { 'success' } else { 'degraded' }
$weatherLimitations = [Collections.Generic.List[string]]::new()
$weatherLimitations.Add('Free endpoint is evaluation-only for this SaaS; production requires a commercial plan and backend-only key handling.')
$weatherLimitations.Add('A 24-point live probe is coverage evidence, not local forecast accuracy.')
if ($weatherStatus -eq 'degraded') {
    $weatherLimitations.Add("The observed 24-point batch took $($weather.durationMs) ms and missed the $weatherTargetMs ms spike target; caching/batching and a paid endpoint require validation.")
}
$providerRuns.Add([PSCustomObject]@{
    contractVersion = '1.0'; provider = 'open-meteo'; sourceUrl = 'https://api.open-meteo.com/v1/forecast'; observedAt = $observedAt; status = $weatherStatus; durationMs = $weather.durationMs
    responseBytes = $weather.responseBytes; contentHash = $weather.sha256; coverage = @{ successful = 24; total = 24; measuredBatchMs = $weather.durationMs; smokeTargetMs = $weatherTargetMs; sampleCount = 1 }
    decision = 'conditional-go'; limitations = $weatherLimitations.ToArray()
})

$capFeedUrl = 'https://ssl.smn.gob.ar/CAP/AR.php'
$capFeed = Invoke-Probe -Name 'smn-cap-feed' -Url $capFeedUrl -MaximumBytes 2097152
$capStatus = 'degraded'
$capDuration = $capFeed.durationMs
$capBytes = $capFeed.responseBytes
$capHash = $capFeed.sha256
$capCoverage = @{ feed = 0; alert = 0 }
$capLimitations = [Collections.Generic.List[string]]::new()
try {
    if (-not $capFeed.contentType.StartsWith('application/rss+xml', [StringComparison]::OrdinalIgnoreCase)) {
        throw "CAP endpoint returned $($capFeed.contentType) instead of application/rss+xml."
    }
    $capRss = Read-SafeXml -Path $capFeed.bodyPath
    $firstCapUrl = [string]$capRss.SelectSingleNode('/rss/channel/item[1]/link').InnerText
    if (-not $firstCapUrl.StartsWith('https://ssl.smn.gob.ar/', [StringComparison]::OrdinalIgnoreCase)) { throw 'CAP feed returned an unexpected alert host.' }
    $capAlert = Invoke-Probe -Name 'smn-cap-alert' -Url $firstCapUrl -MaximumBytes 2097152
    if ($capAlert.statusCode -ne 200 -or $capAlert.responseBytes -gt 2097152) { throw 'First CAP alert is unavailable or exceeds the 2 MiB guard.' }
    $capStatus = 'success'
    $capDuration = [Math]::Round($capFeed.durationMs + $capAlert.durationMs, 3)
    $capBytes += $capAlert.responseBytes
    $capHash = $capAlert.sha256
    $capCoverage = @{ feed = 1; alert = 1 }
}
catch {
    $capLimitations.Add($_.Exception.Message)
}
$capLimitations.Add('Official and authoritative for alerts, but no contractual SLA was evidenced; detect stale feed and preserve lifecycle.')
$providerRuns.Add([PSCustomObject]@{
    contractVersion = '1.0'; provider = 'smn-cap'; sourceUrl = $capFeedUrl; observedAt = $observedAt; status = $capStatus; durationMs = $capDuration
    responseBytes = $capBytes; contentHash = $capHash; coverage = $capCoverage
    decision = 'conditional-go'; limitations = $capLimitations.ToArray()
})

$tileDurations = [Collections.Generic.List[double]]::new()
$tileHashes = [Collections.Generic.List[string]]::new()
$tileBytes = [int64]0
foreach ($point in $fixture.points) {
    $tile = Get-TmsTile -Latitude ([double]$point.latitude) -Longitude ([double]$point.longitude) -Zoom 5
    $tileUrl = "https://wms.ign.gob.ar/geoserver/gwc/service/tms/1.0.0/capabaseargenmap@EPSG%3A3857@png/5/$($tile.x)/$($tile.y).png"
    $probe = Invoke-Probe -Name "argenmap-$($point.id)" -Url $tileUrl
    if ($probe.statusCode -ne 200 -or -not $probe.contentType.StartsWith('image/', [StringComparison]::OrdinalIgnoreCase)) { throw "Argenmap failed for jurisdiction $($point.id)." }
    $tileDurations.Add($probe.durationMs)
    $tileHashes.Add($probe.sha256)
    $tileBytes += $probe.responseBytes
}
$tileP75 = Get-Percentile75 -Values $tileDurations.ToArray()
$sha256 = [Security.Cryptography.SHA256]::Create()
try {
    $combinedTileHash = [BitConverter]::ToString($sha256.ComputeHash([Text.Encoding]::UTF8.GetBytes(($tileHashes -join '')))).Replace('-', '').ToLowerInvariant()
}
finally {
    $sha256.Dispose()
}
$providerRuns.Add([PSCustomObject]@{
    contractVersion = '1.0'; provider = 'argenmap'; sourceUrl = 'https://wms.ign.gob.ar/geoserver/gwc/service/tms/1.0.0/'; observedAt = $observedAt; status = $(if ($tileP75 -le 3000) { 'success' } else { 'degraded' }); durationMs = $tileP75
    responseBytes = $tileBytes; contentHash = $combinedTileHash; coverage = @{ successful = 24; total = 24; p75Ms = $tileP75; p75TargetMs = 3000; tileTemplate = 'capabaseargenmap@EPSG:3857@png/{z}/{x}/{-y}.png' }
    decision = 'conditional-go'; limitations = @('Official free map service; no contractual SLA evidenced. UI must keep attribution and tabular fallback.')
})

$wrfListUrl = 'https://smn-ar-wrf.s3.us-west-2.amazonaws.com/?list-type=2&max-keys=3'
$wrfList = Invoke-Probe -Name 'smn-wrf-list' -Url $wrfListUrl
if ($wrfList.statusCode -ne 200) { throw 'SMN WRF public bucket listing is unavailable.' }
$sampleBytes = 14758413
$estimatedRunBytes = $sampleBytes * 73
$providerRuns.Add([PSCustomObject]@{
    contractVersion = '1.0'; provider = 'smn-wrf'; sourceUrl = $wrfListUrl; observedAt = $observedAt; status = 'success'; durationMs = $wrfList.durationMs
    responseBytes = $wrfList.responseBytes; contentHash = $wrfList.sha256; coverage = @{ listedObjects = 3; sampleBytes = $sampleBytes; estimated73LeadBytes = $estimatedRunBytes }
    decision = 'postpone'; limitations = @('Estimated 73 hourly lead files exceed 1 GiB per run before daily/10-minute products.', 'Official sources disagree on 00/12 versus 00/06/12/18 cadence; discover run identity from inventory.', 'No approved operating budget or production authorization.')
})

foreach ($temporaryBody in Get-ChildItem -LiteralPath $runtimeRoot -Filter '*.body') {
    Remove-Item -LiteralPath $temporaryBody.FullName -Force
}

$result = [PSCustomObject]@{
    evidenceVersion = '1.0.0'
    observedAt = $observedAt
    fixtureVersion = $fixture.fixtureVersion
    fixtureSource = $fixture.source
    providerRuns = $providerRuns
}
$outputPath = Join-Path $resultsRoot 'provider-probes.json'
$resultJson = $result | ConvertTo-Json -Depth 8
[IO.File]::WriteAllText($outputPath, $resultJson + [Environment]::NewLine, [Text.UTF8Encoding]::new($false))
Write-Output "Provider evidence: $outputPath"
$providerRuns | Select-Object provider, status, durationMs, decision | Format-Table -AutoSize
