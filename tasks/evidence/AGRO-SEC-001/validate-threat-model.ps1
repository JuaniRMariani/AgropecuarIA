[CmdletBinding()]
param(
    [switch] $SelfTest
)

$ErrorActionPreference = 'Stop'
$evidenceRoot = $PSScriptRoot
$repoRoot = (Resolve-Path -LiteralPath (Join-Path $evidenceRoot '..\..\..')).Path
$registerPath = Join-Path $evidenceRoot 'threat-register.json'

function Read-Register {
    param([string] $Path)

    $raw = Get-Content -LiteralPath $Path -Raw -Encoding UTF8
    return $raw | ConvertFrom-Json
}

function Test-Register {
    param(
        [object] $Register,
        [switch] $CheckEvidencePaths
    )

    $errors = [System.Collections.Generic.List[string]]::new()
    $allowedRatings = @('low', 'medium', 'high')
    $allowedPriorities = @('critical', 'high', 'medium', 'low')
    $riskRegisterContent = ''
    if ($CheckEvidencePaths) {
        $riskRegisterContent = Get-Content -LiteralPath (Join-Path $repoRoot 'tasks\risk-register.md') -Raw -Encoding UTF8
    }

    if ($Register.schemaVersion -ne '1.0') {
        $errors.Add('schemaVersion must be 1.0.')
    }
    if ($Register.taskId -ne 'AGRO-SEC-001') {
        $errors.Add('taskId must be AGRO-SEC-001.')
    }
    if ($Register.currentRelease -ne 'R0') {
        $errors.Add('currentRelease must be R0 for this baseline.')
    }

    foreach ($question in @('Q-054', 'Q-055', 'Q-058', 'Q-060')) {
        if ($question -notin @($Register.openQuestions)) {
            $errors.Add("Open question $question must remain explicit.")
        }
    }

    $threats = @($Register.threats)
    if ($threats.Count -lt 5) {
        $errors.Add('At least five concrete threats are required.')
    }

    $ids = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::Ordinal)
    for ($index = 0; $index -lt $threats.Count; $index++) {
        $threat = $threats[$index]
        $expectedId = 'TM-{0:D3}' -f ($index + 1)

        if ($threat.id -ne $expectedId) {
            $errors.Add("Threat at index $index must use stable sequential ID $expectedId.")
        }
        if (-not $ids.Add([string] $threat.id)) {
            $errors.Add("Duplicate threat ID: $($threat.id).")
        }

        foreach ($field in @('title', 'threatSource', 'owner', 'residualRisk', 'blockingGate')) {
            if ([string]::IsNullOrWhiteSpace([string] $threat.$field)) {
                $errors.Add("$($threat.id): $field is required.")
            }
        }

        foreach ($field in @('boundaries', 'assets', 'riskIds', 'affectedCapabilities', 'existingControls', 'gaps', 'requiredTests', 'detection', 'evidence')) {
            $values = @($threat.$field)
            if ($values.Count -eq 0) {
                $errors.Add("$($threat.id): $field must not be empty.")
                continue
            }
            if (@($values | Where-Object { [string]::IsNullOrWhiteSpace([string] $_) }).Count -gt 0) {
                $errors.Add("$($threat.id): $field must not contain blank values.")
            }
        }

        if ($threat.likelihood -notin $allowedRatings) {
            $errors.Add("$($threat.id): invalid likelihood '$($threat.likelihood)'.")
        }
        if ($threat.impact -notin $allowedRatings) {
            $errors.Add("$($threat.id): invalid impact '$($threat.impact)'.")
        }
        if ($threat.priority -notin $allowedPriorities) {
            $errors.Add("$($threat.id): invalid priority '$($threat.priority)'.")
        }
        foreach ($riskId in @($threat.riskIds)) {
            if ([string] $riskId -notmatch '^RSK-\d{3}$') {
                $errors.Add("$($threat.id): invalid risk ID '$riskId'.")
                continue
            }
            if ($CheckEvidencePaths -and -not $riskRegisterContent.Contains("| $riskId |")) {
                $errors.Add("$($threat.id): risk ID does not exist in tasks/risk-register.md: $riskId.")
            }
        }

        if ($threat.priority -eq 'critical') {
            if ([string]::IsNullOrWhiteSpace([string] $threat.owner)) {
                $errors.Add("$($threat.id): critical threat has no owner.")
            }
            if (@($threat.requiredTests).Count -eq 0) {
                $errors.Add("$($threat.id): critical threat has no required test.")
            }
            if ([string]::IsNullOrWhiteSpace([string] $threat.blockingGate)) {
                $errors.Add("$($threat.id): critical threat has no blocking gate.")
            }
        }

        if ($CheckEvidencePaths) {
            foreach ($anchor in @($threat.evidence)) {
                $relativePath = ([string] $anchor -split '#', 2)[0].Replace('/', [IO.Path]::DirectorySeparatorChar)
                $resolvedPath = Join-Path $repoRoot $relativePath
                if (-not (Test-Path -LiteralPath $resolvedPath -PathType Leaf)) {
                    $errors.Add("$($threat.id): evidence path does not exist: $anchor.")
                }
            }
        }
    }

    return $errors
}

function Test-ThreatTable {
    param(
        [object] $Register,
        [string] $Markdown
    )

    $errors = [System.Collections.Generic.List[string]]::new()
    $rows = @{}
    $pattern = '(?m)^\|\s*(TM-\d{3})\s*\|(?:[^|\r\n]*\|){9}\s*(low|medium|high)\s*\|\s*(low|medium|high)\s*\|\s*(critical|high|medium|low)\s*\|\s*$'
    foreach ($match in [regex]::Matches($Markdown, $pattern)) {
        $id = $match.Groups[1].Value
        if ($rows.ContainsKey($id)) {
            $errors.Add("Threat table contains duplicate row $id.")
            continue
        }
        $rows[$id] = @{
            likelihood = $match.Groups[2].Value
            impact = $match.Groups[3].Value
            priority = $match.Groups[4].Value
        }
    }

    if ($rows.Count -ne @($Register.threats).Count) {
        $errors.Add("Threat table has $($rows.Count) rows but register has $(@($Register.threats).Count).")
    }
    foreach ($threat in @($Register.threats)) {
        if (-not $rows.ContainsKey([string] $threat.id)) {
            $errors.Add("Threat table is missing row $($threat.id).")
            continue
        }
        foreach ($field in @('likelihood', 'impact', 'priority')) {
            if ($rows[[string] $threat.id][$field] -ne [string] $threat.$field) {
                $errors.Add("Threat table $($threat.id) $field does not match the register.")
            }
        }
    }

    return $errors
}

function Test-Documents {
    param([object] $Register)

    $errors = [System.Collections.Generic.List[string]]::new()
    $documents = @(
        'README.md',
        'AgropecuarIA-threat-model.md',
        'data-classification-and-privacy.md',
        'provider-processing-inventory.md',
        'release-security-gates.md',
        'validation-report.md'
    )

    foreach ($document in $documents) {
        $documentPath = Join-Path $evidenceRoot $document
        if (-not (Test-Path -LiteralPath $documentPath -PathType Leaf)) {
            $errors.Add("Missing required document: $document.")
            continue
        }

        $content = Get-Content -LiteralPath $documentPath -Raw -Encoding UTF8
        foreach ($match in [regex]::Matches($content, '\]\(([^)]+)\)')) {
            $target = $match.Groups[1].Value.Trim()
            if ($target.StartsWith('http://') -or $target.StartsWith('https://') -or $target.StartsWith('#') -or $target.StartsWith('mailto:')) {
                continue
            }
            $relativeTarget = ($target -split '#', 2)[0].Replace('/', [IO.Path]::DirectorySeparatorChar)
            $resolvedTarget = Join-Path (Split-Path -Parent $documentPath) $relativeTarget
            if (-not (Test-Path -LiteralPath $resolvedTarget)) {
                $errors.Add("$document contains a broken local link: $target.")
            }
        }
    }

    $threatModelPath = Join-Path $evidenceRoot 'AgropecuarIA-threat-model.md'
    if (Test-Path -LiteralPath $threatModelPath -PathType Leaf) {
        $threatModel = Get-Content -LiteralPath $threatModelPath -Raw -Encoding UTF8
        $requiredHeadings = @(
            '## Executive summary',
            '## Scope and assumptions',
            '## System model',
            '### Primary components',
            '### Data flows and trust boundaries',
            '#### Diagram',
            '## Assets and security objectives',
            '## Attacker model',
            '## Entry points and attack surfaces',
            '## Top abuse paths',
            '## Threat model table',
            '## Criticality calibration',
            '## Focus paths for security review'
        )
        foreach ($heading in $requiredHeadings) {
            if (-not $threatModel.Contains($heading)) {
                $errors.Add("Threat model is missing heading: $heading.")
            }
        }
        if (-not $threatModel.Contains('```mermaid')) {
            $errors.Add('Threat model must include a Mermaid diagram.')
        }
        foreach ($threat in @($Register.threats)) {
            if (-not $threatModel.Contains([string] $threat.id)) {
                $errors.Add("Threat model does not reference $($threat.id).")
            }
        }
        foreach ($tableError in @(Test-ThreatTable -Register $Register -Markdown $threatModel)) {
            $errors.Add([string] $tableError)
        }
    }

    $questionRequirements = [ordered]@{
        'data-classification-and-privacy.md' = @('Q-054', 'Q-055', 'Q-058', 'Q-060')
        'provider-processing-inventory.md' = @('Q-058', 'Q-060')
        'release-security-gates.md' = @('Q-054', 'Q-055', 'Q-058', 'Q-060')
    }
    foreach ($document in $questionRequirements.Keys) {
        $documentPath = Join-Path $evidenceRoot $document
        if (Test-Path -LiteralPath $documentPath -PathType Leaf) {
            $content = Get-Content -LiteralPath $documentPath -Raw -Encoding UTF8
            foreach ($question in $questionRequirements[$document]) {
                if (-not $content.Contains($question)) {
                    $errors.Add("$document must keep $question explicit.")
                }
            }
        }
    }

    return $errors
}

function Copy-Register {
    param([object] $Register)
    return ($Register | ConvertTo-Json -Depth 100 | ConvertFrom-Json)
}

function Invoke-MutationTests {
    param([object] $Register)

    $cases = [ordered]@{}

    $missingOwner = Copy-Register $Register
    $missingOwner.threats[0].owner = ''
    $cases['critical-owner'] = (Test-Register $missingOwner).Count -gt 0

    $missingTest = Copy-Register $Register
    $missingTest.threats[0].requiredTests = @()
    $cases['critical-test'] = (Test-Register $missingTest).Count -gt 0

    $blankTest = Copy-Register $Register
    $blankTest.threats[0].requiredTests = @('')
    $cases['blank-array-value'] = (Test-Register $blankTest).Count -gt 0

    $duplicateId = Copy-Register $Register
    $duplicateId.threats[1].id = $duplicateId.threats[0].id
    $cases['duplicate-id'] = (Test-Register $duplicateId).Count -gt 0

    $invalidRisk = Copy-Register $Register
    $invalidRisk.threats[0].riskIds = @('RISK-1')
    $cases['risk-link'] = (Test-Register $invalidRisk).Count -gt 0

    $missingQuestion = Copy-Register $Register
    $missingQuestion.openQuestions = @($missingQuestion.openQuestions | Where-Object { $_ -ne 'Q-060' })
    $cases['open-question'] = (Test-Register $missingQuestion).Count -gt 0

    $tableDrift = Copy-Register $Register
    $tableDrift.threats[0].priority = 'low'
    $threatModel = Get-Content -LiteralPath (Join-Path $evidenceRoot 'AgropecuarIA-threat-model.md') -Raw -Encoding UTF8
    $cases['human-table-drift'] = (Test-ThreatTable -Register $tableDrift -Markdown $threatModel).Count -gt 0

    $failed = @($cases.GetEnumerator() | Where-Object { -not $_.Value })
    foreach ($case in $cases.GetEnumerator()) {
        Write-Output ("SELFTEST {0}: {1}" -f $case.Key, $(if ($case.Value) { 'PASS' } else { 'FAIL' }))
    }
    if ($failed.Count -gt 0) {
        throw "$($failed.Count) mutation self-test(s) failed."
    }
}

$register = Read-Register $registerPath
$validationErrors = [System.Collections.Generic.List[string]]::new()
foreach ($validationError in @(Test-Register $register -CheckEvidencePaths)) {
    $validationErrors.Add([string] $validationError)
}
foreach ($validationError in @(Test-Documents $register)) {
    $validationErrors.Add([string] $validationError)
}

if ($validationErrors.Count -gt 0) {
    foreach ($validationError in $validationErrors) {
        Write-Error $validationError
    }
    throw "Threat-model validation failed with $($validationErrors.Count) error(s)."
}

if ($SelfTest) {
    Invoke-MutationTests $register
}

$criticalCount = @($register.threats | Where-Object { $_.priority -eq 'critical' }).Count
$highCount = @($register.threats | Where-Object { $_.priority -eq 'high' }).Count
Write-Output "VALIDATION PASS: $(@($register.threats).Count) threats; $criticalCount critical; $highCount high; 0 critical threats without owner/test/gate."
