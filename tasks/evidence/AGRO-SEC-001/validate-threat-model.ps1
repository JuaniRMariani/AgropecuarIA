[CmdletBinding()]
param(
    [switch] $SelfTest
)

$ErrorActionPreference = 'Stop'
$evidenceRoot = $PSScriptRoot
$repoRoot = (Resolve-Path -LiteralPath (Join-Path $evidenceRoot '..\..\..')).Path
$registerPath = Join-Path $evidenceRoot 'threat-register.json'
$surfaceRegisterPath = Join-Path $evidenceRoot 'runtime-surface-register.json'
$script:runtimeTestCorpus = $null

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
    if ($Register.currentRelease -notin @('R0', 'R1')) {
        $errors.Add('currentRelease must be R0 or R1.')
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

function Test-RuntimeSurfaceRegister {
    param(
        [object] $SurfaceRegister,
        [object] $ThreatRegister,
        [switch] $CheckEvidencePaths
    )

    $errors = [System.Collections.Generic.List[string]]::new()
    $allowedStatuses = @('integrated-local', 'development-test-only', 'external-no-go')
    $testCorpus = ''
    if ($CheckEvidencePaths) {
        if ($null -eq $script:runtimeTestCorpus) {
            $testFiles = @(
                Get-ChildItem -LiteralPath (Join-Path $repoRoot 'tests') -Recurse -File -Filter '*.cs'
                Get-ChildItem -LiteralPath (Join-Path $repoRoot 'apps\web\tests') -Recurse -File -Filter '*.ts'
                Get-ChildItem -LiteralPath (Join-Path $repoRoot 'apps\web\tests') -Recurse -File -Filter '*.tsx'
                Get-ChildItem -LiteralPath (Join-Path $repoRoot 'tasks\evidence\AGRO-FND-001\fitness\tests') -Recurse -File -Filter '*.cs'
            ) | Where-Object {
                $_.FullName -notmatch '[\\/](bin|obj|node_modules|TestResults|test-results|playwright-report|\.next)[\\/]'
            }
            $script:runtimeTestCorpus = ($testFiles | ForEach-Object {
                Get-Content -LiteralPath $_.FullName -Raw -Encoding UTF8
            }) -join [Environment]::NewLine
        }
        $testCorpus = $script:runtimeTestCorpus
    }

    if ($SurfaceRegister.schemaVersion -ne '1.0') {
        $errors.Add('Runtime surface schemaVersion must be 1.0.')
    }
    if ($SurfaceRegister.taskId -ne 'AGRO-SEC-001') {
        $errors.Add('Runtime surface taskId must be AGRO-SEC-001.')
    }
    if ($SurfaceRegister.currentRelease -ne $ThreatRegister.currentRelease) {
        $errors.Add('Runtime surface release must match the threat register release.')
    }

    $knownThreatIds = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::Ordinal)
    foreach ($threat in @($ThreatRegister.threats)) {
        [void] $knownThreatIds.Add([string] $threat.id)
    }

    $surfaces = @($SurfaceRegister.surfaces)
    if ($surfaces.Count -lt 11) {
        $errors.Add('At least eleven R1 runtime and external surfaces are required.')
    }

    $ids = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::Ordinal)
    for ($index = 0; $index -lt $surfaces.Count; $index++) {
        $surface = $surfaces[$index]
        $expectedId = 'RS-{0:D3}' -f ($index + 1)
        if ($surface.id -ne $expectedId) {
            $errors.Add("Runtime surface at index $index must use stable sequential ID $expectedId.")
        }
        if (-not $ids.Add([string] $surface.id)) {
            $errors.Add("Duplicate runtime surface ID: $($surface.id).")
        }
        foreach ($field in @('name', 'owner', 'control', 'gate')) {
            if ([string]::IsNullOrWhiteSpace([string] $surface.$field)) {
                $errors.Add("$($surface.id): $field is required.")
            }
        }
        if ($surface.status -notin $allowedStatuses) {
            $errors.Add("$($surface.id): invalid status '$($surface.status)'.")
        }
        foreach ($field in @('threatIds', 'tests', 'evidence')) {
            $values = @($surface.$field)
            if ($values.Count -eq 0 -or @($values | Where-Object { [string]::IsNullOrWhiteSpace([string] $_) }).Count -gt 0) {
                $errors.Add("$($surface.id): $field must contain non-blank values.")
            }
        }
        foreach ($threatId in @($surface.threatIds)) {
            if (-not $knownThreatIds.Contains([string] $threatId)) {
                $errors.Add("$($surface.id): unknown threat ID $threatId.")
            }
        }
        if ($surface.id -in @('RS-001', 'RS-004', 'RS-009', 'RS-011')) {
            $contractPaths = @($surface.contractPaths)
            if ($contractPaths.Count -eq 0 -or @($contractPaths | Where-Object {
                [string]::IsNullOrWhiteSpace([string] $_)
            }).Count -gt 0) {
                $errors.Add("$($surface.id): contractPaths must contain non-blank values.")
            }
        }
        if ($surface.status -eq 'development-test-only') {
            $developmentBoundary = "$($surface.control) $($surface.gate)"
            if (-not $developmentBoundary.Contains('Development/Test') -or -not $developmentBoundary.Contains('only')) {
                $errors.Add("$($surface.id): development-test-only surface must state its Development/Test-only boundary.")
            }
        }
        if ($surface.status -eq 'external-no-go' -and -not ([string] $surface.gate).Contains('NO-GO')) {
            $errors.Add("$($surface.id): external-no-go surface must state an explicit NO-GO gate.")
        }
        if ($CheckEvidencePaths) {
            foreach ($anchor in @($surface.evidence)) {
                $relativePath = ([string] $anchor -split '#', 2)[0].Replace('/', [IO.Path]::DirectorySeparatorChar)
                if (-not (Test-Path -LiteralPath (Join-Path $repoRoot $relativePath) -PathType Leaf)) {
                    $errors.Add("$($surface.id): runtime surface evidence path does not exist: $anchor.")
                }
            }
            if ($surface.id -in @('RS-001', 'RS-002', 'RS-003', 'RS-004', 'RS-009', 'RS-010', 'RS-011')) {
                foreach ($declaredTest in @($surface.tests)) {
                    $testName = [string] $declaredTest
                    if ($testName.Contains('/')) {
                        $testPath = $testName.Replace('/', [IO.Path]::DirectorySeparatorChar)
                        if (-not (Test-Path -LiteralPath (Join-Path $repoRoot $testPath) -PathType Leaf)) {
                            $errors.Add("$($surface.id): declared test path does not exist: $testName.")
                        }
                    }
                    elseif ($testCorpus.IndexOf($testName, [StringComparison]::Ordinal) -lt 0) {
                        $errors.Add("$($surface.id): declared test symbol does not exist: $testName.")
                    }
                }
            }
        }
    }

    foreach ($status in $allowedStatuses) {
        if (@($surfaces | Where-Object { $_.status -eq $status }).Count -eq 0) {
            $errors.Add("Runtime surface register must include status $status.")
        }
    }

    return $errors
}

function Test-R1Evidence {
    param(
        [object] $Register,
        [object] $SurfaceRegister,
        [switch] $CheckArtifactPaths
    )

    $errors = [System.Collections.Generic.List[string]]::new()
    if ($Register.currentRelease -ne 'R1') {
        return $errors
    }

    $requiredArtifacts = @(
        'AgropecuarIA.slnx',
        'apps/AgropecuarIA.Api/AgropecuarIA.Api.csproj',
        'apps/AgropecuarIA.Api/Program.cs',
        'contracts/identity.openapi.yaml',
        'contracts/territory.openapi.yaml',
        'contracts/productive-core.openapi.yaml',
        'src/AgropecuarIA.Identity/AgropecuarIA.Identity.csproj',
        'src/AgropecuarIA.Identity/Infrastructure/IdentityDbContext.cs',
        'src/AgropecuarIA.Identity/Infrastructure/Migrations/IdentityDbContextModelSnapshot.cs',
        'apps/web/package.json',
        'apps/web/pnpm-lock.yaml',
        'src/AgropecuarIA.Territory/AgropecuarIA.Territory.csproj',
        'src/AgropecuarIA.Territory/packages.lock.json',
        'src/AgropecuarIA.Territory/Application/TerritoryReferenceService.cs',
        'src/AgropecuarIA.Territory/Application/TerritoryResolutionCache.cs',
        'src/AgropecuarIA.Territory/Delivery/TerritoryExceptionHandler.cs',
        'src/AgropecuarIA.Territory/Providers/Georef/GeorefTerritoryClient.cs',
        'src/AgropecuarIA.Territory/Infrastructure/Migrations/20260811223525_InitializeOfficialTerritory.cs',
        'src/AgropecuarIA.ProductiveCore/AgropecuarIA.ProductiveCore.csproj',
        'src/AgropecuarIA.ProductiveCore/packages.lock.json',
        'src/AgropecuarIA.ProductiveCore/Infrastructure/ProductiveCoreDbContext.cs',
        'src/AgropecuarIA.ProductiveCore/Infrastructure/PostgresProductiveCoreUnitOfWork.cs',
        'src/AgropecuarIA.ProductiveCore/Infrastructure/Migrations/20260818170935_InitializeProductiveCore.cs',
        'src/AgropecuarIA.Identity/Infrastructure/Migrations/20260818180000_AddProductiveCoreAuthorizationPort.cs',
        'apps/AgropecuarIA.Api/packages.lock.json',
        'src/AgropecuarIA.Identity/packages.lock.json',
        'tests/AgropecuarIA.Identity.Tests/packages.lock.json',
        'tests/AgropecuarIA.Identity.Tests/OidcConfigurationContractTests.cs',
        'tests/AgropecuarIA.Identity.Tests/StepUpApiIntegrationTests.cs',
        'tests/AgropecuarIA.Identity.Tests/IdentityDatabaseMigrationTests.cs',
        'tests/AgropecuarIA.Identity.Tests/IdentityTelemetryTests.cs',
        'tests/AgropecuarIA.Territory.Tests/packages.lock.json',
        'tests/AgropecuarIA.Territory.Tests/GeorefTerritoryClientTests.cs',
        'tests/AgropecuarIA.Territory.Tests/TerritoryEndpointsTests.cs',
        'tests/AgropecuarIA.Territory.Tests/TerritoryDatabaseSecurityTests.cs',
        'tests/AgropecuarIA.Territory.Tests/TerritorySnapshotValidatorTests.cs',
        'tests/AgropecuarIA.ProductiveCore.Tests/AgropecuarIA.ProductiveCore.Tests.csproj',
        'tests/AgropecuarIA.ProductiveCore.Tests/packages.lock.json',
        'tests/AgropecuarIA.ProductiveCore.Tests/ProductiveCoreApplicationServiceTests.cs',
        'tests/AgropecuarIA.ProductiveCore.Tests/ProductiveCoreDatabaseSecurityTests.cs',
        'tests/AgropecuarIA.ProductiveCore.Tests/ProductiveCoreEndpointContractTests.cs',
        'tests/AgropecuarIA.Identity.Tests/ProductiveCoreAuthorizationPortDatabaseTests.cs',
        'apps/web/tests/e2e/identity.spec.ts',
        'apps/web/tests/unit/territory-api.test.ts',
        'apps/web/tests/unit/territory-explorer.test.tsx',
        'tasks/evidence/AGRO-FND-001/fitness/tests/AgropecuarIA.ArchitectureFitness.Tests/IdentityPublishedEventContractTests.cs',
        'tasks/evidence/AGRO-FND-001/fitness/src/AgropecuarIA.ArchitectureFitness/packages.lock.json',
        'tasks/evidence/AGRO-FND-001/fitness/tests/AgropecuarIA.ArchitectureFitness.Tests/packages.lock.json'
    )
    if ($CheckArtifactPaths) {
        foreach ($artifact in $requiredArtifacts) {
            if (-not (Test-Path -LiteralPath (Join-Path $repoRoot $artifact) -PathType Leaf)) {
                $errors.Add("Missing required R1 artifact: $artifact.")
            }
        }

        $openApiPaths = @(
            Get-Content -LiteralPath (Join-Path $repoRoot 'contracts\identity.openapi.yaml') -Encoding UTF8 |
                ForEach-Object {
                    if ($_ -match '^\s{2}(/[^:]+):\s*$') {
                        $Matches[1]
                    }
                }
        )
        $registeredPaths = @()
        foreach ($surface in @($SurfaceRegister.surfaces | Where-Object {
            $_.id -in @('RS-001', 'RS-004')
        })) {
            $registeredPaths += @($surface.contractPaths)
        }
        $pathDrift = @(
            Compare-Object -ReferenceObject @($openApiPaths | Sort-Object -Unique) -DifferenceObject @($registeredPaths | Sort-Object -Unique)
        )
        if ($pathDrift.Count -gt 0) {
            $errors.Add('OpenAPI paths and runtime surface contractPaths must match exactly.')
        }

        $territoryOpenApiPaths = @(
            Get-Content -LiteralPath (Join-Path $repoRoot 'contracts\territory.openapi.yaml') -Encoding UTF8 |
                ForEach-Object {
                    if ($_ -match '^\s{2}(/[^:]+):\s*$') {
                        $Matches[1]
                    }
                }
        )
        $territorySurface = @($SurfaceRegister.surfaces | Where-Object { $_.id -eq 'RS-009' }) | Select-Object -First 1
        $territoryPathDrift = @(
            Compare-Object -ReferenceObject @($territoryOpenApiPaths | Sort-Object -Unique) -DifferenceObject @(@($territorySurface.contractPaths) | Sort-Object -Unique)
        )
        if ($territoryPathDrift.Count -gt 0) {
            $errors.Add('Territory OpenAPI paths and RS-009 contractPaths must match exactly.')
        }

        $productiveOpenApiPaths = @(
            Get-Content -LiteralPath (Join-Path $repoRoot 'contracts\productive-core.openapi.yaml') -Encoding UTF8 |
                ForEach-Object {
                    if ($_ -match '^\s{2}(/[^:]+):\s*$') {
                        $Matches[1]
                    }
                }
        )
        $productiveSurface = @($SurfaceRegister.surfaces | Where-Object { $_.id -eq 'RS-011' }) | Select-Object -First 1
        $productivePathDrift = @(
            Compare-Object -ReferenceObject @($productiveOpenApiPaths | Sort-Object -Unique) -DifferenceObject @(@($productiveSurface.contractPaths) | Sort-Object -Unique)
        )
        if ($productivePathDrift.Count -gt 0) {
            $errors.Add('Productive Core OpenAPI paths and RS-011 contractPaths must match exactly.')
        }
    }

    $requiredThreatEvidence = [ordered]@{
        'TM-001' = @('tests/AgropecuarIA.Identity.Tests/IdentityRequestContextTests.cs', 'tasks/evidence/AGRO-DIS-003/validation-report.md', 'tests/AgropecuarIA.ProductiveCore.Tests/ProductiveCoreDatabaseSecurityTests.cs', 'tests/AgropecuarIA.Identity.Tests/ProductiveCoreAuthorizationPortDatabaseTests.cs')
        'TM-002' = @('apps/AgropecuarIA.Api/OidcReauthentication.cs', 'tests/AgropecuarIA.Identity.Tests/StepUpApiIntegrationTests.cs', 'apps/web/tests/e2e/identity.spec.ts')
        'TM-004' = @('src/AgropecuarIA.Territory/Providers/Georef/GeorefTerritoryClient.cs', 'tests/AgropecuarIA.Territory.Tests/TerritorySnapshotValidatorTests.cs')
        'TM-006' = @('src/AgropecuarIA.Territory/TerritoryServiceCollectionExtensions.cs', 'tests/AgropecuarIA.Territory.Tests/GeorefTerritoryClientTests.cs')
        'TM-008' = @('src/AgropecuarIA.Identity/IdentityTelemetry.cs', 'tests/AgropecuarIA.Identity.Tests/IdentityTelemetryTests.cs', 'src/AgropecuarIA.Territory/Delivery/TerritoryExceptionHandler.cs', 'apps/web/tests/unit/territory-explorer.test.tsx', 'src/AgropecuarIA.ProductiveCore/ProductiveCoreTelemetry.cs')
        'TM-010' = @('apps/AgropecuarIA.Api/packages.lock.json', 'apps/web/pnpm-lock.yaml')
        'TM-011' = @('tests/AgropecuarIA.Territory.Tests/TerritoryDatabaseSecurityTests.cs', 'tests/AgropecuarIA.Territory.Tests/TerritorySnapshotValidatorTests.cs', 'tests/AgropecuarIA.ProductiveCore.Tests/ProductiveCoreDatabaseSecurityTests.cs')
        'TM-012' = @('tests/AgropecuarIA.Identity.Tests/IdentityDatabaseMigrationTests.cs', 'tasks/evidence/AGRO-FND-001/fitness/tests/AgropecuarIA.ArchitectureFitness.Tests/IdentityPublishedEventContractTests.cs', 'tests/AgropecuarIA.ProductiveCore.Tests/ProductiveCoreApplicationServiceTests.cs')
        'TM-013' = @('src/AgropecuarIA.Territory/Application/TerritoryReferenceService.cs', 'src/AgropecuarIA.Territory/Providers/Georef/GeorefTerritoryClient.cs')
        'TM-014' = @('tests/AgropecuarIA.Territory.Tests/TerritoryEndpointsTests.cs', 'tests/AgropecuarIA.Territory.Tests/GeorefTerritoryClientTests.cs', 'tests/AgropecuarIA.ProductiveCore.Tests/ProductiveCoreEndpointContractTests.cs')
    }
    foreach ($threatId in $requiredThreatEvidence.Keys) {
        $threat = @($Register.threats | Where-Object { $_.id -eq $threatId }) | Select-Object -First 1
        if ($null -eq $threat) {
            $errors.Add("Missing required R1 threat: $threatId.")
            continue
        }
        foreach ($anchor in $requiredThreatEvidence[$threatId]) {
            if ($anchor -notin @($threat.evidence)) {
                $errors.Add("$threatId must anchor R1 evidence: $anchor.")
            }
        }
    }

    $requiredThreatTests = [ordered]@{
        'TM-001' = @('IdentityRequestContextTests', 'ProductiveCoreDatabaseSecurityTests', 'TST-TENANT-NEG')
        'TM-002' = @('OidcConfigurationContractTests', 'TST-ID-AUTH')
        'TM-004' = @('GeorefTerritoryClientTests', 'TerritorySnapshotValidatorTests')
        'TM-006' = @('GeorefTerritoryClientTests', 'fixed host')
        'TM-008' = @('IdentityTelemetryTests', 'TST-OTEL-REDACTION', 'TST-TERRITORY-URL-REDACTION')
        'TM-010' = @('Frozen restore/install', 'TST-SEC-GATES')
        'TM-011' = @('TerritoryDatabaseSecurityTests', 'TST-RESTORE')
        'TM-012' = @('IdentityDatabaseMigrationTests', 'ProductiveCoreApplicationServiceTests', 'TST-IDEMPOTENCY')
        'TM-013' = @('TST-TERRITORY-URL-REDACTION', 'Provider data-flow review')
        'TM-014' = @('TerritoryEndpointsTests', 'GeorefTerritoryClientTests', 'ProductiveCoreEndpointContractTests')
    }
    foreach ($threatId in $requiredThreatTests.Keys) {
        $threat = @($Register.threats | Where-Object { $_.id -eq $threatId }) | Select-Object -First 1
        if ($null -eq $threat) {
            continue
        }
        $declaredTests = @($threat.requiredTests) -join ' '
        foreach ($requiredTest in $requiredThreatTests[$threatId]) {
            if (-not $declaredTests.Contains($requiredTest)) {
                $errors.Add("$threatId must keep its R1 gate/test explicit: $requiredTest.")
            }
        }
    }

    $requiredGapTokens = [ordered]@{
        'TM-001' = 'only integrated product tenant boundaries'
        'TM-002' = 'No Auth0 sandbox'
        'TM-004' = 'External Georef'
        'TM-006' = 'external Georef remains NO-GO'
        'TM-008' = 'No OTLP exporter'
        'TM-010' = 'No CI workflow'
        'TM-012' = 'AGRO-FND-002 shared delivery'
        'TM-013' = 'External Georef is NO-GO'
        'TM-014' = 'external Georef/provider quotas'
    }
    foreach ($threatId in $requiredGapTokens.Keys) {
        $threat = @($Register.threats | Where-Object { $_.id -eq $threatId }) | Select-Object -First 1
        if ($null -ne $threat -and -not ((@($threat.gaps) -join ' ').Contains($requiredGapTokens[$threatId]))) {
            $errors.Add("$threatId must keep its R1 residual gap explicit: $($requiredGapTokens[$threatId]).")
        }
    }

    $tm001 = @($Register.threats | Where-Object { $_.id -eq 'TM-001' }) | Select-Object -First 1
    if ($null -ne $tm001) {
        $tm001Controls = @($tm001.existingControls) -join ' '
        foreach ($requiredRlsEvidence in @(
            'Accepted ADR-PEND-007',
            '29/29 internal tests',
            'SCRAM-SHA-256',
            'four distinct ephemeral passwords',
            'owner-only ACLs',
            'discovery fails fast',
            'CreateOrganization implements forward-safe product migrations',
            'One-shot co-owner invitations derive actor and tenant server-side',
            'removal of another co-owner repeats those boundaries',
            'Productive Core non-spatial fields revalidate a live owner before lookup and replay')) {
            if (-not $tm001Controls.Contains($requiredRlsEvidence)) {
                $errors.Add("TM-001 must keep accepted disposable RLS evidence explicit: $requiredRlsEvidence.")
            }
        }
    }

    $surfaceExpectations = [ordered]@{
        'RS-001' = 'integrated-local'
        'RS-002' = 'integrated-local'
        'RS-003' = 'integrated-local'
        'RS-004' = 'development-test-only'
        'RS-005' = 'external-no-go'
        'RS-006' = 'external-no-go'
        'RS-007' = 'integrated-local'
        'RS-008' = 'external-no-go'
        'RS-009' = 'integrated-local'
        'RS-010' = 'external-no-go'
        'RS-011' = 'integrated-local'
    }
    foreach ($surfaceId in $surfaceExpectations.Keys) {
        $surface = @($SurfaceRegister.surfaces | Where-Object { $_.id -eq $surfaceId }) | Select-Object -First 1
        if ($null -eq $surface) {
            $errors.Add("Missing required R1 surface: $surfaceId.")
        }
        elseif ($surface.status -ne $surfaceExpectations[$surfaceId]) {
            $errors.Add("$surfaceId must have status $($surfaceExpectations[$surfaceId]).")
        }
    }

    $territorySurface = @($SurfaceRegister.surfaces | Where-Object { $_.id -eq 'RS-009' }) | Select-Object -First 1
    if ($null -ne $territorySurface) {
        $territoryEvidence = "$($territorySurface.control) $($territorySurface.gate)"
        foreach ($requiredToken in @('23 provinces plus CABA', 'FORCE RLS', 'Georef v2.0', 'never written to PostgreSQL', 'bounded volatile in-process cache', 'query-string redaction', 'RS-010')) {
            if (-not $territoryEvidence.Contains($requiredToken)) {
                $errors.Add("RS-009 must keep its local Territory boundary explicit: $requiredToken.")
            }
        }
    }

    $georefSurface = @($SurfaceRegister.surfaces | Where-Object { $_.id -eq 'RS-010' }) | Select-Object -First 1
    if ($null -ne $georefSurface) {
        $georefEvidence = "$($georefSurface.control) $($georefSurface.gate)"
        foreach ($requiredToken in @('https://apis.datos.gob.ar/georef/api/v2.0/', 'disables redirects', 'five-second timeout', '256 KiB', 'no tenant', 'NO-GO', 'end-to-end URL/query redaction', 'Passing local adapter tests does not approve the provider')) {
            if (-not $georefEvidence.Contains($requiredToken)) {
                $errors.Add("RS-010 must keep its external Georef boundary explicit: $requiredToken.")
            }
        }
    }

    $productiveSurface = @($SurfaceRegister.surfaces | Where-Object { $_.id -eq 'RS-011' }) | Select-Object -First 1
    if ($null -ne $productiveSurface) {
        $productiveEvidence = "$($productiveSurface.control) $($productiveSurface.gate)"
        foreach ($requiredToken in @('non-spatial', 'FORCE RLS', 'SECURITY DEFINER', 'active owner membership', 'before lookup/replay', 'atomic', 'append-only', 'excludes field name', 'NO-GO')) {
            if (-not $productiveEvidence.Contains($requiredToken)) {
                $errors.Add("RS-011 must keep its local Productive Core boundary explicit: $requiredToken.")
            }
        }
    }

    return $errors
}

function Test-ObsoleteDeclarations {
    param([string[]] $Contents)

    $errors = [System.Collections.Generic.List[string]]::new()
    $obsoleteDeclarations = @(
        'Documented target architecture and disposable R0 evidence; no production runtime exists.',
        'No existe todavía runtime productivo',
        'No production SDK, collector, backend access or retention policy exists',
        'No root product lockfiles, CI identity, provenance or artifact signing exists',
        'los lockfiles actuales pertenecen a spikes aislados',
        'No production mutation, outbox/inbox or N/N-1 migration drill exists',
        'ADR-PEND-007 and safe membership discovery remain open',
        'NO-GO tenant/RLS hasta ADR-PEND-007',
        'Los probes loopback con `trust` no se promueven.'
    )
    foreach ($declaration in $obsoleteDeclarations) {
        if (@($Contents | Where-Object { $_.Contains($declaration) }).Count -gt 0) {
            $errors.Add("Obsolete pre-R1 declaration remains: $declaration.")
        }
    }
    return $errors
}

function Copy-Register {
    param([object] $Register)
    return ($Register | ConvertTo-Json -Depth 100 | ConvertFrom-Json)
}

function Invoke-MutationTests {
    param(
        [object] $Register,
        [object] $SurfaceRegister
    )

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

    $missingSurface = Copy-Register $SurfaceRegister
    $missingSurface.surfaces = @($missingSurface.surfaces | Where-Object { $_.id -ne 'RS-001' })
    $cases['missing-runtime-surface'] = (Test-R1Evidence -Register $Register -SurfaceRegister $missingSurface).Count -gt 0

    $missingSurfacePath = Copy-Register $SurfaceRegister
    $missingSurfacePath.surfaces[0].evidence = @('apps/AgropecuarIA.Api/does-not-exist.cs')
    $cases['missing-runtime-path'] = (Test-RuntimeSurfaceRegister -SurfaceRegister $missingSurfacePath -ThreatRegister $Register -CheckEvidencePaths).Count -gt 0

    $missingSurfaceTest = Copy-Register $SurfaceRegister
    $missingSurfaceTest.surfaces[0].tests = @('MissingRuntimeSecurityTest')
    $cases['missing-runtime-test'] = (Test-RuntimeSurfaceRegister -SurfaceRegister $missingSurfaceTest -ThreatRegister $Register -CheckEvidencePaths).Count -gt 0

    $openApiPathDrift = Copy-Register $SurfaceRegister
    $openApiPathDrift.surfaces[0].contractPaths = @(
        $openApiPathDrift.surfaces[0].contractPaths | Select-Object -Skip 1
    )
    $cases['openapi-path-drift'] = (Test-R1Evidence -Register $Register -SurfaceRegister $openApiPathDrift -CheckArtifactPaths).Count -gt 0

    $territoryOpenApiPathDrift = Copy-Register $SurfaceRegister
    $territoryOpenApiSurface = @($territoryOpenApiPathDrift.surfaces | Where-Object { $_.id -eq 'RS-009' }) | Select-Object -First 1
    $territoryOpenApiSurface.contractPaths = @($territoryOpenApiSurface.contractPaths | Select-Object -Skip 1)
    $cases['territory-openapi-path-drift'] = (Test-R1Evidence -Register $Register -SurfaceRegister $territoryOpenApiPathDrift -CheckArtifactPaths).Count -gt 0

    $productiveOpenApiPathDrift = Copy-Register $SurfaceRegister
    $productiveOpenApiSurface = @($productiveOpenApiPathDrift.surfaces | Where-Object { $_.id -eq 'RS-011' }) | Select-Object -First 1
    $productiveOpenApiSurface.contractPaths = @($productiveOpenApiSurface.contractPaths | Select-Object -Skip 1)
    $cases['productive-openapi-path-drift'] = (Test-R1Evidence -Register $Register -SurfaceRegister $productiveOpenApiPathDrift -CheckArtifactPaths).Count -gt 0

    $missingThreatGateTest = Copy-Register $Register
    $missingThreatGateTest.threats[1].requiredTests = @('Only a generic test')
    $cases['missing-r1-gate-test'] = (Test-R1Evidence -Register $missingThreatGateTest -SurfaceRegister $SurfaceRegister).Count -gt 0

    $rlsEvidenceMutations = [ordered]@{
        'accepted-adr' = 'Accepted ADR-PEND-007'
        'internal-tests' = '29/29 internal tests'
        'scram' = 'SCRAM-SHA-256'
        'distinct-passwords' = 'four distinct ephemeral passwords'
        'owner-only-acl' = 'owner-only ACLs'
        'principal-fail-fast' = 'discovery fails fast'
        'organization-runtime' = 'CreateOrganization implements forward-safe product migrations'
        'owner-invitation-runtime' = 'One-shot co-owner invitations derive actor and tenant server-side'
        'owner-removal-runtime' = 'removal of another co-owner repeats those boundaries'
        'productive-core-runtime' = 'Productive Core non-spatial fields revalidate a live owner before lookup and replay'
    }
    foreach ($mutationName in $rlsEvidenceMutations.Keys) {
        $missingRlsEvidence = Copy-Register $Register
        $token = $rlsEvidenceMutations[$mutationName]
        $missingRlsEvidence.threats[0].existingControls = @(
            $missingRlsEvidence.threats[0].existingControls | ForEach-Object {
                ([string] $_).Replace($token, '[removed positive RLS evidence]')
            }
        )
        $cases["missing-r1-rls-evidence-$mutationName"] = (Test-R1Evidence `
            -Register $missingRlsEvidence `
            -SurfaceRegister $SurfaceRegister).Count -gt 0
    }

    $unsafeDevelopmentSurface = Copy-Register $SurfaceRegister
    $unsafeDevelopmentSurface.surfaces[3].control = 'Synthetic provider is available.'
    $unsafeDevelopmentSurface.surfaces[3].gate = 'Environment boundary is unspecified.'
    $cases['development-only-boundary'] = (Test-RuntimeSurfaceRegister -SurfaceRegister $unsafeDevelopmentSurface -ThreatRegister $Register).Count -gt 0

    $missingExternalGate = Copy-Register $SurfaceRegister
    $missingExternalGate.surfaces[4].gate = 'Provider may be enabled later.'
    $cases['external-no-go-gate'] = (Test-RuntimeSurfaceRegister -SurfaceRegister $missingExternalGate -ThreatRegister $Register).Count -gt 0

    $territoryBoundaryMutations = [ordered]@{
        'official-snapshot' = '23 provinces plus CABA'
        'force-rls' = 'FORCE RLS'
        'fixed-v2-adapter' = 'Georef v2.0'
        'no-coordinate-persistence' = 'never written to PostgreSQL'
        'volatile-coordinate-cache' = 'bounded volatile in-process cache'
        'shared-query-redaction' = 'query-string redaction'
    }
    foreach ($mutationName in $territoryBoundaryMutations.Keys) {
        $mutatedSurfaces = Copy-Register $SurfaceRegister
        $mutatedTerritory = @($mutatedSurfaces.surfaces | Where-Object { $_.id -eq 'RS-009' }) | Select-Object -First 1
        $token = $territoryBoundaryMutations[$mutationName]
        $mutatedTerritory.control = ([string] $mutatedTerritory.control).Replace($token, '[removed Territory boundary]')
        $mutatedTerritory.gate = ([string] $mutatedTerritory.gate).Replace($token, '[removed Territory boundary]')
        $cases["missing-territory-boundary-$mutationName"] = (Test-R1Evidence -Register $Register -SurfaceRegister $mutatedSurfaces).Count -gt 0
    }

    $georefBoundaryMutations = [ordered]@{
        'fixed-endpoint' = 'https://apis.datos.gob.ar/georef/api/v2.0/'
        'redirect-policy' = 'disables redirects'
        'timeout-budget' = 'five-second timeout'
        'response-budget' = '256 KiB'
        'tenant-minimization' = 'no tenant'
        'external-no-go' = 'NO-GO'
        'url-redaction' = 'end-to-end URL/query redaction'
        'local-tests-not-approval' = 'Passing local adapter tests does not approve the provider'
    }
    foreach ($mutationName in $georefBoundaryMutations.Keys) {
        $mutatedSurfaces = Copy-Register $SurfaceRegister
        $mutatedGeoref = @($mutatedSurfaces.surfaces | Where-Object { $_.id -eq 'RS-010' }) | Select-Object -First 1
        $token = $georefBoundaryMutations[$mutationName]
        $mutatedGeoref.control = ([string] $mutatedGeoref.control).Replace($token, '[removed Georef boundary]')
        $mutatedGeoref.gate = ([string] $mutatedGeoref.gate).Replace($token, '[removed Georef boundary]')
        $cases["missing-georef-boundary-$mutationName"] = (Test-R1Evidence -Register $Register -SurfaceRegister $mutatedSurfaces).Count -gt 0
    }

    $productiveBoundaryMutations = [ordered]@{
        'non-spatial' = 'non-spatial'
        'force-rls' = 'FORCE RLS'
        'identity-port' = 'SECURITY DEFINER'
        'live-owner' = 'active owner membership'
        'authorize-before-replay' = 'before lookup/replay'
        'atomic-write' = 'atomic'
        'append-only-journal' = 'append-only'
        'telemetry-redaction' = 'excludes field name'
        'external-no-go' = 'NO-GO'
    }
    foreach ($mutationName in $productiveBoundaryMutations.Keys) {
        $mutatedSurfaces = Copy-Register $SurfaceRegister
        $mutatedProductive = @($mutatedSurfaces.surfaces | Where-Object { $_.id -eq 'RS-011' }) | Select-Object -First 1
        $token = $productiveBoundaryMutations[$mutationName]
        $mutatedProductive.control = ([string] $mutatedProductive.control).Replace($token, '[removed Productive Core boundary]')
        $mutatedProductive.gate = ([string] $mutatedProductive.gate).Replace($token, '[removed Productive Core boundary]')
        $cases["missing-productive-boundary-$mutationName"] = (Test-R1Evidence -Register $Register -SurfaceRegister $mutatedSurfaces).Count -gt 0
    }

    $cases['obsolete-r0-declaration'] = (Test-ObsoleteDeclarations -Contents @('No root product lockfiles, CI identity, provenance or artifact signing exists')).Count -gt 0

    $staleAcceptedRlsDecision = Copy-Register $Register
    $staleAcceptedRlsDecision.threats[0].gaps += 'ADR-PEND-007 and safe membership discovery remain open'
    $cases['obsolete-accepted-rls-decision'] = (Test-ObsoleteDeclarations -Contents @(
        ($staleAcceptedRlsDecision | ConvertTo-Json -Depth 100)
    )).Count -gt 0
    $cases['obsolete-tenant-gate-awaits-adr'] = (Test-ObsoleteDeclarations -Contents @(
        'NO-GO tenant/RLS hasta ADR-PEND-007'
    )).Count -gt 0
    $cases['obsolete-trust-authentication'] = (Test-ObsoleteDeclarations -Contents @(
        'Los probes loopback con `trust` no se promueven.'
    )).Count -gt 0

    $failed = @($cases.GetEnumerator() | Where-Object { -not $_.Value })
    foreach ($case in $cases.GetEnumerator()) {
        Write-Output ("SELFTEST {0}: {1}" -f $case.Key, $(if ($case.Value) { 'PASS' } else { 'FAIL' }))
    }
    if ($failed.Count -gt 0) {
        throw "$($failed.Count) mutation self-test(s) failed."
    }
}

$register = Read-Register $registerPath
$surfaceRegister = Read-Register $surfaceRegisterPath
$validationErrors = [System.Collections.Generic.List[string]]::new()
foreach ($validationError in @(Test-Register $register -CheckEvidencePaths)) {
    $validationErrors.Add([string] $validationError)
}
foreach ($validationError in @(Test-Documents $register)) {
    $validationErrors.Add([string] $validationError)
}
foreach ($validationError in @(Test-RuntimeSurfaceRegister -SurfaceRegister $surfaceRegister -ThreatRegister $register -CheckEvidencePaths)) {
    $validationErrors.Add([string] $validationError)
}
foreach ($validationError in @(Test-R1Evidence -Register $register -SurfaceRegister $surfaceRegister -CheckArtifactPaths)) {
    $validationErrors.Add([string] $validationError)
}
$ownedEvidenceContents = @(
    (Get-Content -LiteralPath $registerPath -Raw -Encoding UTF8),
    (Get-Content -LiteralPath $surfaceRegisterPath -Raw -Encoding UTF8),
    (Get-Content -LiteralPath (Join-Path $evidenceRoot 'AgropecuarIA-threat-model.md') -Raw -Encoding UTF8),
    (Get-Content -LiteralPath (Join-Path $evidenceRoot 'README.md') -Raw -Encoding UTF8),
    (Get-Content -LiteralPath (Join-Path $evidenceRoot 'release-security-gates.md') -Raw -Encoding UTF8),
    (Get-Content -LiteralPath (Join-Path $evidenceRoot 'data-classification-and-privacy.md') -Raw -Encoding UTF8),
    (Get-Content -LiteralPath (Join-Path $evidenceRoot 'provider-processing-inventory.md') -Raw -Encoding UTF8)
)
foreach ($validationError in @(Test-ObsoleteDeclarations -Contents $ownedEvidenceContents)) {
    $validationErrors.Add([string] $validationError)
}

if ($validationErrors.Count -gt 0) {
    foreach ($validationError in $validationErrors) {
        Write-Error $validationError
    }
    throw "Threat-model validation failed with $($validationErrors.Count) error(s)."
}

if ($SelfTest) {
    Invoke-MutationTests -Register $register -SurfaceRegister $surfaceRegister
}

$criticalCount = @($register.threats | Where-Object { $_.priority -eq 'critical' }).Count
$highCount = @($register.threats | Where-Object { $_.priority -eq 'high' }).Count
Write-Output "VALIDATION PASS: $(@($register.threats).Count) threats; $criticalCount critical; $highCount high; 0 critical threats without owner/test/gate."
