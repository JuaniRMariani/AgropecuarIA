[CmdletBinding()]
param([string]$ResultsDirectory = 'TestResults/ci')

$ErrorActionPreference = 'Stop'

function Get-TrxFailureSummary {
    param([Parameter(Mandatory)][string]$XmlText)

    $settings = [Xml.XmlReaderSettings]::new()
    $settings.DtdProcessing = [Xml.DtdProcessing]::Prohibit
    $settings.XmlResolver = $null
    $settings.MaxCharactersInDocument = 16MB
    $reader = [Xml.XmlReader]::Create([IO.StringReader]::new($XmlText), $settings)
    try {
        $document = [Xml.XmlDocument]::new()
        $document.XmlResolver = $null
        $document.Load($reader)
    }
    finally {
        $reader.Dispose()
    }

    $methods = @{}
    foreach ($definition in $document.SelectNodes("//*[local-name()='UnitTest']")) {
        $method = $definition.SelectSingleNode("*[local-name()='TestMethod']")
        if ($method -and
            $method.GetAttribute('className') -match '^[A-Za-z0-9_.+]+$' -and
            $method.GetAttribute('name') -match '^[A-Za-z0-9_]+$') {
            $name = $method.GetAttribute('className') + '.' + $method.GetAttribute('name')
            if ($name.Length -le 240) {
                $methods[$definition.GetAttribute('id')] = $name
            }
        }
    }

    $failures = @($document.SelectNodes("//*[local-name()='UnitTestResult']") | Where-Object {
        $_.GetAttribute('outcome') -in @('Failed', 'Error', 'Timeout', 'Aborted')
    })
    $names = @($failures | ForEach-Object {
        $methodName = $methods[$_.GetAttribute('testId')]
        if ($methodName) { $methodName } else { 'UnidentifiedTest' }
    } | Sort-Object -Unique)

    # Never include display names, parameter values, stdout, failure messages or stack traces.
    [pscustomobject]@{ FailureCount = $failures.Count; Names = $names }
}

if ($MyInvocation.InvocationName -eq '.') { return }

$reports = @(Get-ChildItem -LiteralPath $ResultsDirectory -Filter '*.trx' -File -Recurse -ErrorAction SilentlyContinue)
if ($reports.Count -eq 0) {
    Write-Output '::warning title=Backend test diagnostics::No TRX reports were produced; inspect the test runner step.'
    exit 0
}

$failedCount = 0
foreach ($report in $reports) {
    try {
        if ($report.Length -gt 16MB) { throw 'Report exceeds the diagnostic size limit.' }
        $summary = Get-TrxFailureSummary -XmlText (Get-Content -LiteralPath $report.FullName -Raw)
        $failedCount += $summary.FailureCount
        foreach ($name in $summary.Names) {
            Write-Output "::error title=Backend test failed::$name"
        }
    }
    catch {
        Write-Output '::warning title=Backend test diagnostics::A TRX report could not be parsed safely.'
    }
}
Write-Output "Test reports: $($reports.Count); failed cases: $failedCount."
