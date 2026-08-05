[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$CatalogVersion = '1.0.0-candidate.1'
$EvidenceDirectory = $PSScriptRoot
$SchemaPath = Join-Path $EvidenceDirectory 'catalog-entry.schema.json'
$SourcesPath = Join-Path $EvidenceDirectory 'sources-v1.json'
$SourceEvidencePath = Join-Path $EvidenceDirectory 'source-evidence-v1.json'
$CoverageOraclePath = Join-Path $EvidenceDirectory 'coverage-oracle-v1.json'
$ExceptionsPath = Join-Path $EvidenceDirectory 'exceptions-v1.json'
$PublicationContractPath = Join-Path $EvidenceDirectory 'catalog-publication-contract.json'
$ManifestPath = Join-Path $EvidenceDirectory 'catalog-v1.manifest.json'
$PrototypeDirectory = Join-Path $EvidenceDirectory 'prototype'
$PrototypePackagePath = Join-Path $PrototypeDirectory 'package.json'
$PrototypePackageLockPath = Join-Path $PrototypeDirectory 'package-lock.json'
$PrototypeStaleFixturePath = Join-Path $PrototypeDirectory 'tests\fixtures\source-stale.json'
$PrototypeNextEnvPath = Join-Path $PrototypeDirectory 'next-env.d.ts'
$ExpectedEvidenceArtifacts = @(
    'README.md',
    'catalog-entry.schema.json',
    'catalog-plants-v1.json',
    'catalog-animals-v1.json',
    'catalog-publication-contract.json',
    'sources-v1.json',
    'source-evidence-v1.json',
    'coverage-oracle-v1.json',
    'exceptions-v1.json',
    'governance.md',
    'validate-catalog.ps1',
    'validation-report.md'
)
$ExpectedPrototypeArtifacts = @(
    'prototype/.gitignore',
    'prototype/AGENTS.md',
    'prototype/CLAUDE.md',
    'prototype/README.md',
    'prototype/package.json',
    'prototype/package-lock.json',
    'prototype/eslint.config.mjs',
    'prototype/next.config.ts',
    'prototype/tsconfig.json',
    'prototype/app/catalog-explorer.tsx',
    'prototype/app/error.tsx',
    'prototype/app/globals.css',
    'prototype/app/icon.svg',
    'prototype/app/layout.tsx',
    'prototype/app/loading.tsx',
    'prototype/app/page.tsx',
    'prototype/lib/catalog-data.ts',
    'prototype/lib/catalog-types.ts',
    'prototype/lib/search.ts',
    'prototype/lib/view-state.ts',
    'prototype/tests/search.test.ts',
    'prototype/tests/view-state.test.ts',
    'prototype/tests/fixtures/source-stale.json'
)
$ExpectedArtifacts = @($ExpectedEvidenceArtifacts) + @($ExpectedPrototypeArtifacts)
$DatasetDefinitions = @(
    @{ Path = (Join-Path $EvidenceDirectory 'catalog-plants-v1.json'); Domain = 'VEGETAL' },
    @{ Path = (Join-Path $EvidenceDirectory 'catalog-animals-v1.json'); Domain = 'ANIMAL' }
)

$Failures = New-Object 'System.Collections.Generic.List[string]'
$AllEntries = New-Object 'System.Collections.Generic.List[object]'
$FamilyDimensionRecords = New-Object 'System.Collections.Generic.List[object]'
$OracleDimensionRecords = New-Object 'System.Collections.Generic.List[object]'
$FamilyDimensionsByFamily = @{}
$SearchFixtureCount = 0
$EntrySearchIndex = @{}
$FamilyDimensionSearchIndex = @{}
$DimensionFields = @('systems', 'purposes', 'trackingUnits', 'products', 'categories')
$ValidJurisdictions = @(
    'AR', 'AR-A', 'AR-B', 'AR-C', 'AR-D', 'AR-E', 'AR-F', 'AR-G', 'AR-H',
    'AR-J', 'AR-K', 'AR-L', 'AR-M', 'AR-N', 'AR-P', 'AR-Q', 'AR-R', 'AR-S',
    'AR-T', 'AR-U', 'AR-V', 'AR-W', 'AR-X', 'AR-Y', 'AR-Z'
)

function Add-Failure {
    param([Parameter(Mandatory = $true)][string] $Message)

    $Failures.Add($Message)
}

function Get-PropertyNames {
    param([Parameter(Mandatory = $true)] $Object)

    @($Object.PSObject.Properties | ForEach-Object { $_.Name })
}

function Test-PropertySet {
    param(
        [Parameter(Mandatory = $true)] $Object,
        [Parameter(Mandatory = $true)][string[]] $Required,
        [Parameter(Mandatory = $true)][string[]] $Allowed,
        [Parameter(Mandatory = $true)][string] $Context
    )

    $actual = @(Get-PropertyNames -Object $Object)
    foreach ($name in $Required) {
        if ($actual -notcontains $name) {
            Add-Failure "${Context}: falta el campo requerido '$name'."
        }
    }

    foreach ($name in $actual) {
        if ($Allowed -notcontains $name) {
            Add-Failure "${Context}: el campo '$name' no está permitido por el schema."
        }
    }
}

function Read-StrictJson {
    param([Parameter(Mandatory = $true)][string] $Path)

    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        Add-Failure "Falta el archivo requerido '$Path'."
        return $null
    }

    try {
        $bytes = [System.IO.File]::ReadAllBytes($Path)
        if ($bytes.Length -eq 0) {
            Add-Failure "El archivo '$Path' está vacío."
            return $null
        }

        $utf8 = New-Object System.Text.UTF8Encoding($false, $true)
        $text = $utf8.GetString($bytes)
        if ($text.Length -gt 0 -and $text[0] -eq [char]0xFEFF) {
            $text = $text.Substring(1)
        }

        if ([string]::IsNullOrWhiteSpace($text)) {
            Add-Failure "El archivo '$Path' no contiene JSON."
            return $null
        }

        return ($text | ConvertFrom-Json -ErrorAction Stop)
    }
    catch {
        Add-Failure "El archivo '$Path' no es UTF-8/JSON válido: $($_.Exception.Message)"
        return $null
    }
}

function Read-StrictUtf8Text {
    param([Parameter(Mandatory = $true)][string] $Path)

    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        Add-Failure "Falta el archivo requerido '$Path'."
        return $null
    }

    try {
        $bytes = [System.IO.File]::ReadAllBytes($Path)
        if ($bytes.Length -eq 0) {
            Add-Failure "El archivo '$Path' esta vacio."
            return $null
        }

        $utf8 = New-Object System.Text.UTF8Encoding($false, $true)
        $text = $utf8.GetString($bytes)
        if ($text.Length -gt 0 -and $text[0] -eq [char]0xFEFF) {
            $text = $text.Substring(1)
        }
        if ([string]::IsNullOrWhiteSpace($text)) {
            Add-Failure "El archivo '$Path' no contiene texto."
            return $null
        }

        return $text
    }
    catch {
        Add-Failure "El archivo '$Path' no es UTF-8 valido: $($_.Exception.Message)"
        return $null
    }
}

function Read-StrictJsonDictionary {
    param([Parameter(Mandatory = $true)][string] $Path)

    $text = Read-StrictUtf8Text -Path $Path
    if ($null -eq $text) {
        return $null
    }

    try {
        Add-Type -AssemblyName System.Web.Extensions -ErrorAction Stop
        $serializer = New-Object System.Web.Script.Serialization.JavaScriptSerializer
        $serializer.MaxJsonLength = [int]::MaxValue
        $serializer.RecursionLimit = 256
        $document = $serializer.DeserializeObject($text)
        if ($document -isnot [System.Collections.IDictionary]) {
            Add-Failure "El archivo '$Path' debe contener un objeto JSON raiz."
            return $null
        }
        return $document
    }
    catch {
        Add-Failure "El archivo '$Path' no es JSON valido: $($_.Exception.Message)"
        return $null
    }
}

function ConvertTo-SearchKey {
    param([AllowNull()][string] $Value)

    if ([string]::IsNullOrWhiteSpace($Value)) {
        return ''
    }

    $decomposed = $Value.Trim().Normalize([Text.NormalizationForm]::FormD)
    $builder = New-Object Text.StringBuilder
    foreach ($character in $decomposed.ToCharArray()) {
        $category = [Globalization.CharUnicodeInfo]::GetUnicodeCategory($character)
        if ($category -ne [Globalization.UnicodeCategory]::NonSpacingMark) {
            [void]$builder.Append($character)
        }
    }

    $normalized = $builder.ToString().Normalize([Text.NormalizationForm]::FormC).ToLowerInvariant()
    return ([regex]::Replace($normalized, '[^a-z0-9]+', ' ').Trim())
}

function Remove-Diacritics {
    param([Parameter(Mandatory = $true)][string] $Value)

    $decomposed = $Value.Normalize([Text.NormalizationForm]::FormD)
    $builder = New-Object Text.StringBuilder
    foreach ($character in $decomposed.ToCharArray()) {
        $category = [Globalization.CharUnicodeInfo]::GetUnicodeCategory($character)
        if ($category -ne [Globalization.UnicodeCategory]::NonSpacingMark) {
            [void]$builder.Append($character)
        }
    }

    return $builder.ToString().Normalize([Text.NormalizationForm]::FormC)
}

function Test-UniqueValues {
    param(
        [AllowNull()][object[]] $Values,
        [Parameter(Mandatory = $true)][string] $Context,
        [switch] $Normalize
    )

    $seen = @{}
    foreach ($value in @($Values)) {
        if ($null -eq $value) {
            Add-Failure "${Context}: contiene un valor null."
            continue
        }

        $key = [string]$value
        if ($Normalize) {
            $key = ConvertTo-SearchKey -Value $key
        }

        if ([string]::IsNullOrWhiteSpace($key)) {
            Add-Failure "${Context}: contiene un valor vacío."
            continue
        }

        if ($seen.ContainsKey($key)) {
            Add-Failure "${Context}: el valor '$value' está duplicado."
        }
        else {
            $seen[$key] = $true
        }
    }
}

function Test-ExactStringSet {
    param(
        [AllowNull()][object[]] $Actual,
        [Parameter(Mandatory = $true)][string[]] $Expected,
        [Parameter(Mandatory = $true)][string] $Context
    )

    Test-UniqueValues -Values $Actual -Context $Context
    $actualStrings = @($Actual | ForEach-Object { [string]$_ } | Sort-Object -CaseSensitive)
    $expectedStrings = @($Expected | Sort-Object -CaseSensitive)
    if ((@($actualStrings) -join [char]31) -cne (@($expectedStrings) -join [char]31)) {
        Add-Failure "${Context}: conjunto inesperado. actual=[$($actualStrings -join ', ')], esperado=[$($expectedStrings -join ', ')]."
    }
}

function Test-DictionaryKeySet {
    param(
        [AllowNull()] $Dictionary,
        [Parameter(Mandatory = $true)][string[]] $Expected,
        [Parameter(Mandatory = $true)][string] $Context
    )

    if ($Dictionary -isnot [System.Collections.IDictionary]) {
        Add-Failure "${Context}: debe ser un objeto JSON."
        return
    }
    $actual = @($Dictionary.Keys | ForEach-Object { [string]$_ })
    Test-ExactStringSet -Actual $actual -Expected $Expected -Context $Context
}

function Test-PinnedDependencyMap {
    param(
        [AllowNull()] $DependencyObject,
        [Parameter(Mandatory = $true)][string] $Context
    )

    if ($null -eq $DependencyObject) {
        Add-Failure "${Context}: no puede ser null."
        return
    }
    foreach ($property in $DependencyObject.PSObject.Properties) {
        if ($property.Value -isnot [string] -or $property.Value -notmatch '^\d+\.\d+\.\d+(?:-[0-9A-Za-z.-]+)?$') {
            Add-Failure "${Context}.$($property.Name): debe fijar una version exacta semver sin rangos."
        }
    }
}

function Test-StringValue {
    param(
        [AllowNull()] $Value,
        [Parameter(Mandatory = $true)][string] $Context,
        [int] $MinimumLength = 0,
        [AllowNull()][string] $Pattern
    )

    if ($Value -isnot [string]) {
        Add-Failure "${Context}: debe ser string."
        return
    }

    if ($Value.Length -lt $MinimumLength) {
        Add-Failure "${Context}: debe tener al menos $MinimumLength caracteres."
    }

    if ($Pattern -and $Value -notmatch $Pattern) {
        Add-Failure "${Context}: '$Value' no cumple el patrón '$Pattern'."
    }
}

function Test-HttpsUrl {
    param(
        [AllowNull()] $Value,
        [Parameter(Mandatory = $true)][string] $Context
    )

    if ($Value -isnot [string]) {
        Add-Failure "${Context}: debe ser una URL HTTPS en formato string."
        return
    }

    $uri = $null
    if (-not [uri]::TryCreate($Value, [UriKind]::Absolute, [ref]$uri) -or
        $uri.Scheme -ne [Uri]::UriSchemeHttps -or
        [string]::IsNullOrWhiteSpace($uri.Host)) {
        Add-Failure "${Context}: '$Value' no es una URL HTTPS absoluta válida."
    }
}

function Test-IsoDateValue {
    param(
        [AllowNull()] $Value,
        [Parameter(Mandatory = $true)][string] $Context
    )

    if ($Value -isnot [string] -or $Value -notmatch '^\d{4}-\d{2}-\d{2}(?:T\d{2}:\d{2}:\d{2}(?:\.\d+)?(?:Z|[+-]\d{2}:\d{2}))?$') {
        Add-Failure "${Context}: debe ser una fecha ISO 8601 (yyyy-MM-dd o timestamp con zona)."
        return
    }

    $parsed = [datetimeoffset]::MinValue
    if (-not [datetimeoffset]::TryParse(
        $Value,
        [Globalization.CultureInfo]::InvariantCulture,
        [Globalization.DateTimeStyles]::None,
        [ref]$parsed
    )) {
        Add-Failure "${Context}: '$Value' no representa una fecha ISO 8601 válida."
    }
}

function Get-FileSha256 {
    param([Parameter(Mandatory = $true)][string] $Path)

    $stream = [System.IO.File]::OpenRead($Path)
    try {
        $algorithm = [Security.Cryptography.SHA256]::Create()
        try {
            $hash = $algorithm.ComputeHash($stream)
            return (($hash | ForEach-Object { $_.ToString('x2') }) -join '')
        }
        finally {
            $algorithm.Dispose()
        }
    }
    finally {
        $stream.Dispose()
    }
}

$schema = Read-StrictJson -Path $SchemaPath
$sourcesDocument = Read-StrictJson -Path $SourcesPath
$sourceEvidenceDocument = Read-StrictJson -Path $SourceEvidencePath
$coverageOracleDocument = Read-StrictJson -Path $CoverageOraclePath
$exceptionsDocument = Read-StrictJson -Path $ExceptionsPath
$publicationContractDocument = Read-StrictJson -Path $PublicationContractPath
$prototypePackageDocument = Read-StrictJson -Path $PrototypePackagePath
$prototypePackageLockDocument = Read-StrictJsonDictionary -Path $PrototypePackageLockPath
$prototypeStaleFixtureDocument = Read-StrictJson -Path $PrototypeStaleFixturePath
$manifestDocument = Read-StrictJson -Path $ManifestPath
$manifestCounts = $null

$actualEvidenceArtifacts = @(Get-ChildItem -LiteralPath $EvidenceDirectory -Force -File |
    Where-Object { $_.Name -ne 'catalog-v1.manifest.json' } |
    ForEach-Object { $_.Name } |
    Sort-Object -CaseSensitive)
Test-ExactStringSet -Actual $actualEvidenceArtifacts -Expected $ExpectedEvidenceArtifacts -Context 'artefactos raiz de evidencia (manifest excluido)'

if (-not (Test-Path -LiteralPath $PrototypeDirectory -PathType Container)) {
    Add-Failure "Falta el directorio requerido '$PrototypeDirectory'."
}
else {
    $prototypeRoot = (Resolve-Path -LiteralPath $PrototypeDirectory).Path.TrimEnd('\', '/')
    $evidenceRoot = (Resolve-Path -LiteralPath $EvidenceDirectory).Path.TrimEnd('\', '/')
    $actualPrototypeArtifacts = @(Get-ChildItem -LiteralPath $PrototypeDirectory -Recurse -Force -File |
        Where-Object {
            $relativeToPrototype = $_.FullName.Substring($prototypeRoot.Length).TrimStart('\', '/')
            $relativeToPrototype -ne 'next-env.d.ts' -and
            $relativeToPrototype -notmatch '^(?:node_modules|\.next)(?:[\\/]|$)'
        } |
        ForEach-Object { $_.FullName.Substring($evidenceRoot.Length).TrimStart('\', '/').Replace('\', '/') } |
        Sort-Object -CaseSensitive)
    Test-ExactStringSet -Actual $actualPrototypeArtifacts -Expected $ExpectedPrototypeArtifacts -Context 'prototype (excluye solo node_modules, .next y next-env.d.ts autogenerado)'
}

if (Test-Path -LiteralPath $PrototypeNextEnvPath -PathType Leaf) {
    $nextEnvText = Read-StrictUtf8Text -Path $PrototypeNextEnvPath
    if ($null -ne $nextEnvText) {
        $normalizedNextEnvText = $nextEnvText.Replace("`r`n", "`n")
        foreach ($requiredPattern in @(
            '(?m)^/// <reference types="next" />$',
            '(?m)^/// <reference types="next/image-types/global" />$',
            '(?m)^import "\./\.next/(?:dev/)?types/routes\.d\.ts";$',
            '(?m)^import "\./\.next/(?:dev/)?types/root-params\.d\.ts";$',
            '(?m)^// NOTE: This file should not be edited$',
            '(?m)^// see https://nextjs\.org/docs/app/api-reference/config/typescript for more information\.$'
        )) {
            if ($normalizedNextEnvText -notmatch $requiredPattern) {
                Add-Failure "prototype/next-env.d.ts: falta la referencia o aviso autogenerado '$requiredPattern'."
            }
        }

        $routeVariant = [regex]::Match($normalizedNextEnvText, '(?m)^import "\./\.next/(?<variant>dev/)?types/routes\.d\.ts";$')
        $rootParamsVariant = [regex]::Match($normalizedNextEnvText, '(?m)^import "\./\.next/(?<variant>dev/)?types/root-params\.d\.ts";$')
        if ($routeVariant.Success -and $rootParamsVariant.Success -and
            $routeVariant.Groups['variant'].Value -cne $rootParamsVariant.Groups['variant'].Value) {
            Add-Failure 'prototype/next-env.d.ts: routes y root-params deben pertenecer a la misma variante build/dev.'
        }
    }
}

if ($null -ne $manifestDocument) {
    $manifestRootFields = @('catalogVersion', 'createdAt', 'hashAlgorithm', 'counts', 'artifacts')
    Test-PropertySet -Object $manifestDocument -Required $manifestRootFields -Allowed $manifestRootFields -Context 'catalog-v1.manifest.json'
    $manifestProperties = @(Get-PropertyNames -Object $manifestDocument)

    if ($manifestProperties -contains 'catalogVersion' -and $manifestDocument.catalogVersion -ne $CatalogVersion) {
        Add-Failure "catalog-v1.manifest.json.catalogVersion: debe ser '$CatalogVersion'."
    }
    if ($manifestProperties -contains 'createdAt') {
        Test-IsoDateValue -Value $manifestDocument.createdAt -Context 'catalog-v1.manifest.json.createdAt'
    }
    if ($manifestProperties -contains 'hashAlgorithm' -and $manifestDocument.hashAlgorithm -ne 'SHA-256') {
        Add-Failure 'catalog-v1.manifest.json.hashAlgorithm: debe ser SHA-256.'
    }

    $manifestCountFields = @(
        'sources', 'sourceEvidence', 'plantEntries', 'animalEntries', 'totalEntries',
        'regulatedEntries', 'exceptions', 'oracleFamilies', 'oracleDimensionTerms'
    )
    if ($manifestProperties -contains 'counts') {
        if ($null -eq $manifestDocument.counts) {
            Add-Failure 'catalog-v1.manifest.json.counts: no puede ser null.'
        }
        else {
            Test-PropertySet -Object $manifestDocument.counts -Required $manifestCountFields -Allowed $manifestCountFields -Context 'catalog-v1.manifest.json.counts'
            $manifestCounts = $manifestDocument.counts
            $countProperties = @(Get-PropertyNames -Object $manifestCounts)
            foreach ($field in $manifestCountFields) {
                if ($countProperties -contains $field -and
                    ($manifestCounts.$field -isnot [int] -and $manifestCounts.$field -isnot [long] -or $manifestCounts.$field -lt 0)) {
                    Add-Failure "catalog-v1.manifest.json.counts.${field}: debe ser un entero no negativo."
                }
            }
            if ($countProperties -contains 'totalEntries' -and
                $countProperties -contains 'plantEntries' -and
                $countProperties -contains 'animalEntries' -and
                $manifestCounts.totalEntries -ne ($manifestCounts.plantEntries + $manifestCounts.animalEntries)) {
                Add-Failure 'catalog-v1.manifest.json.counts.totalEntries: no coincide con plantEntries + animalEntries.'
            }
        }
    }

    if ($manifestProperties -contains 'artifacts') {
        $artifacts = @($manifestDocument.artifacts)
        $artifactPaths = @{}
        if ($artifacts.Count -ne $ExpectedArtifacts.Count) {
            Add-Failure "catalog-v1.manifest.json.artifacts: se esperaban $($ExpectedArtifacts.Count) artefactos y se obtuvieron $($artifacts.Count)."
        }

        for ($artifactIndex = 0; $artifactIndex -lt $artifacts.Count; $artifactIndex++) {
            $artifact = $artifacts[$artifactIndex]
            $context = "catalog-v1.manifest.json.artifacts[$artifactIndex]"
            if ($null -eq $artifact) {
                Add-Failure "${context}: el artefacto no puede ser null."
                continue
            }

            $artifactFields = @('path', 'size', 'sha256')
            Test-PropertySet -Object $artifact -Required $artifactFields -Allowed $artifactFields -Context $context
            $properties = @(Get-PropertyNames -Object $artifact)
            if ($properties -notcontains 'path') {
                continue
            }

            Test-StringValue -Value $artifact.path -Context "$context.path" -MinimumLength 1
            $relativePath = [string]$artifact.path
            if ($ExpectedArtifacts -notcontains $relativePath) {
                Add-Failure "$context.path: '$relativePath' no pertenece al conjunto exacto de artefactos esperado."
            }
            if ($relativePath -match '\\' -or $relativePath.StartsWith('/') -or $relativePath -match '(^|/)\.\.?(/|$)') {
                Add-Failure "$context.path: debe ser una ruta relativa local canonica con separadores '/'."
            }
            if ($artifactPaths.ContainsKey($relativePath)) {
                Add-Failure "$context.path: artefacto duplicado '$relativePath'."
            }
            else {
                $artifactPaths[$relativePath] = $true
            }

            if ($properties -contains 'size' -and
                (($artifact.size -isnot [int] -and $artifact.size -isnot [long]) -or $artifact.size -le 0)) {
                Add-Failure "$context.size: debe ser un entero mayor que cero."
            }
            if ($properties -contains 'sha256' -and
                ($artifact.sha256 -isnot [string] -or $artifact.sha256 -notmatch '^[0-9a-f]{64}$')) {
                Add-Failure "$context.sha256: debe ser SHA-256 lowercase de 64 caracteres hexadecimales."
            }

            $artifactPath = Join-Path $EvidenceDirectory $relativePath
            if (-not (Test-Path -LiteralPath $artifactPath -PathType Leaf)) {
                Add-Failure "$context.path: falta el artefacto '$relativePath'."
                continue
            }

            if ($properties -contains 'size') {
                $actualSize = (Get-Item -LiteralPath $artifactPath).Length
                if ($artifact.size -ne $actualSize) {
                    Add-Failure "$context.size: manifest=$($artifact.size), real=$actualSize para '$relativePath'."
                }
            }
            if ($properties -contains 'sha256' -and $artifact.sha256 -match '^[0-9a-f]{64}$') {
                $actualHash = Get-FileSha256 -Path $artifactPath
                if ($artifact.sha256 -cne $actualHash) {
                    Add-Failure "$context.sha256: no coincide con el archivo '$relativePath'."
                }
            }
        }

        foreach ($expectedArtifact in $ExpectedArtifacts) {
            if (-not $artifactPaths.ContainsKey($expectedArtifact)) {
                Add-Failure "catalog-v1.manifest.json.artifacts: falta '$expectedArtifact'."
            }
        }
    }
}

$prototypeTextByPath = @{}
foreach ($relativePath in $ExpectedPrototypeArtifacts) {
    if ($relativePath -notmatch '\.json$') {
        $artifactPath = Join-Path $EvidenceDirectory ($relativePath.Replace('/', '\'))
        $prototypeTextByPath[$relativePath] = Read-StrictUtf8Text -Path $artifactPath
    }
}

if ($null -ne $prototypePackageDocument) {
    $packageFields = @('name', 'version', 'private', 'description', 'scripts', 'dependencies', 'devDependencies', 'engines')
    Test-PropertySet -Object $prototypePackageDocument -Required $packageFields -Allowed $packageFields -Context 'prototype/package.json'
    $packageProperties = @(Get-PropertyNames -Object $prototypePackageDocument)
    if ($packageProperties -contains 'name' -and $prototypePackageDocument.name -ne 'agro-dis-001-catalog-prototype') {
        Add-Failure 'prototype/package.json.name: nombre inesperado.'
    }
    if ($packageProperties -contains 'version' -and $prototypePackageDocument.version -notmatch '^\d+\.\d+\.\d+$') {
        Add-Failure 'prototype/package.json.version: debe ser semver estable.'
    }
    if ($packageProperties -contains 'private' -and ($prototypePackageDocument.private -isnot [bool] -or $prototypePackageDocument.private -ne $true)) {
        Add-Failure 'prototype/package.json.private: debe ser true para un prototipo descartable no publicable.'
    }
    if ($packageProperties -contains 'scripts') {
        if ($null -eq $prototypePackageDocument.scripts) {
            Add-Failure 'prototype/package.json.scripts: no puede ser null.'
        }
        else {
            $scriptProperties = @(Get-PropertyNames -Object $prototypePackageDocument.scripts)
            foreach ($requiredScript in @('build', 'lint', 'typecheck', 'test')) {
                if ($scriptProperties -notcontains $requiredScript) {
                    Add-Failure "prototype/package.json.scripts: falta '$requiredScript'."
                }
                else {
                    Test-StringValue -Value $prototypePackageDocument.scripts.$requiredScript -Context "prototype/package.json.scripts.$requiredScript" -MinimumLength 3
                }
            }
        }
    }
    if ($packageProperties -contains 'dependencies') {
        Test-PinnedDependencyMap -DependencyObject $prototypePackageDocument.dependencies -Context 'prototype/package.json.dependencies'
    }
    if ($packageProperties -contains 'devDependencies') {
        Test-PinnedDependencyMap -DependencyObject $prototypePackageDocument.devDependencies -Context 'prototype/package.json.devDependencies'
    }
}

if ($null -ne $prototypePackageLockDocument -and $null -ne $prototypePackageDocument) {
    Test-DictionaryKeySet -Dictionary $prototypePackageLockDocument -Expected @('name', 'version', 'lockfileVersion', 'requires', 'packages') -Context 'prototype/package-lock.json'
    if ($prototypePackageLockDocument['lockfileVersion'] -ne 3) {
        Add-Failure 'prototype/package-lock.json.lockfileVersion: debe ser 3.'
    }
    if ($prototypePackageLockDocument['requires'] -isnot [bool] -or $prototypePackageLockDocument['requires'] -ne $true) {
        Add-Failure 'prototype/package-lock.json.requires: debe ser true.'
    }
    if ($prototypePackageLockDocument['name'] -ne $prototypePackageDocument.name -or
        $prototypePackageLockDocument['version'] -ne $prototypePackageDocument.version) {
        Add-Failure 'prototype/package-lock.json: name/version no coinciden con package.json.'
    }

    $lockPackages = $prototypePackageLockDocument['packages']
    if ($lockPackages -isnot [System.Collections.IDictionary] -or -not $lockPackages.ContainsKey('')) {
        Add-Failure 'prototype/package-lock.json.packages: falta el paquete raiz.'
    }
    else {
        $lockRoot = $lockPackages['']
        Test-DictionaryKeySet -Dictionary $lockRoot -Expected @('name', 'version', 'dependencies', 'devDependencies', 'engines') -Context 'prototype/package-lock.json.packages[""]'
        if ($lockRoot['name'] -ne $prototypePackageDocument.name -or $lockRoot['version'] -ne $prototypePackageDocument.version) {
            Add-Failure 'prototype/package-lock.json.packages[""]: name/version no coinciden con package.json.'
        }
        foreach ($dependencyKind in @('dependencies', 'devDependencies')) {
            $packageDependencies = $prototypePackageDocument.$dependencyKind
            if ($null -eq $packageDependencies) {
                continue
            }
            $packageDependencyNames = @(Get-PropertyNames -Object $packageDependencies)
            $lockDependencies = $lockRoot[$dependencyKind]
            Test-DictionaryKeySet -Dictionary $lockDependencies -Expected $packageDependencyNames -Context "prototype/package-lock.json.packages[root].$dependencyKind"
            if ($lockDependencies -is [System.Collections.IDictionary]) {
                foreach ($dependencyName in $packageDependencyNames) {
                    if ($lockDependencies.ContainsKey($dependencyName) -and $lockDependencies[$dependencyName] -ne $packageDependencies.$dependencyName) {
                        Add-Failure "prototype/package-lock.json.packages[root].${dependencyKind}.${dependencyName}: no coincide con package.json."
                    }
                }
            }
        }
    }
}

if ($null -ne $prototypeStaleFixtureDocument) {
    $staleFixtureFields = @('capturedAt', 'evaluatedAt', 'staleAfterDays', 'expectedFreshness', 'canPublish')
    Test-PropertySet -Object $prototypeStaleFixtureDocument -Required $staleFixtureFields -Allowed $staleFixtureFields -Context 'prototype/tests/fixtures/source-stale.json'
    $staleFixtureProperties = @(Get-PropertyNames -Object $prototypeStaleFixtureDocument)
    foreach ($dateField in @('capturedAt', 'evaluatedAt')) {
        if ($staleFixtureProperties -contains $dateField) {
            Test-IsoDateValue -Value $prototypeStaleFixtureDocument.$dateField -Context "prototype/tests/fixtures/source-stale.json.$dateField"
        }
    }
    if ($staleFixtureProperties -contains 'staleAfterDays' -and
        (($prototypeStaleFixtureDocument.staleAfterDays -isnot [int] -and $prototypeStaleFixtureDocument.staleAfterDays -isnot [long]) -or $prototypeStaleFixtureDocument.staleAfterDays -lt 0)) {
        Add-Failure 'prototype/tests/fixtures/source-stale.json.staleAfterDays: debe ser un entero no negativo.'
    }
    if ($staleFixtureProperties -contains 'expectedFreshness' -and $prototypeStaleFixtureDocument.expectedFreshness -ne 'STALE') {
        Add-Failure 'prototype/tests/fixtures/source-stale.json.expectedFreshness: debe ser STALE.'
    }
    if ($staleFixtureProperties -contains 'canPublish' -and
        ($prototypeStaleFixtureDocument.canPublish -isnot [bool] -or $prototypeStaleFixtureDocument.canPublish -ne $false)) {
        Add-Failure 'prototype/tests/fixtures/source-stale.json.canPublish: debe ser false.'
    }

    if ($staleFixtureProperties -contains 'capturedAt' -and $staleFixtureProperties -contains 'evaluatedAt' -and
        $staleFixtureProperties -contains 'staleAfterDays') {
        $capturedAt = [datetimeoffset]::MinValue
        $evaluatedAt = [datetimeoffset]::MinValue
        $capturedValid = [datetimeoffset]::TryParse([string]$prototypeStaleFixtureDocument.capturedAt, [Globalization.CultureInfo]::InvariantCulture, [Globalization.DateTimeStyles]::None, [ref]$capturedAt)
        $evaluatedValid = [datetimeoffset]::TryParse([string]$prototypeStaleFixtureDocument.evaluatedAt, [Globalization.CultureInfo]::InvariantCulture, [Globalization.DateTimeStyles]::None, [ref]$evaluatedAt)
        if ($capturedValid -and $evaluatedValid -and $prototypeStaleFixtureDocument.staleAfterDays -is [ValueType]) {
            $actualFreshness = if ($evaluatedAt -ge $capturedAt -and ($evaluatedAt - $capturedAt).TotalDays -le [double]$prototypeStaleFixtureDocument.staleAfterDays) { 'FRESH' } else { 'STALE' }
            if ($actualFreshness -ne $prototypeStaleFixtureDocument.expectedFreshness) {
                Add-Failure "prototype/tests/fixtures/source-stale.json: la clasificacion deterministica es '$actualFreshness', no '$($prototypeStaleFixtureDocument.expectedFreshness)'."
            }
            $actualCanPublish = $actualFreshness -eq 'FRESH'
            if ($actualCanPublish -ne $prototypeStaleFixtureDocument.canPublish) {
                Add-Failure "prototype/tests/fixtures/source-stale.json: canPublish no coincide con la clasificacion fail-closed '$actualFreshness'."
            }
        }
    }
}

$viewStateTestText = $prototypeTextByPath['prototype/tests/view-state.test.ts']
if ($null -ne $viewStateTestText) {
    foreach ($requiredSignal in @('./fixtures/source-stale.json', 'classifySourceFreshness', 'canPublishFromSource', 'STALE', 'assert.equal(parsed.canPublish, false)')) {
        if ($viewStateTestText -cnotlike "*$requiredSignal*") {
            Add-Failure "prototype/tests/view-state.test.ts: falta la evidencia documental '$requiredSignal'."
        }
    }
}
$viewStateSourceText = $prototypeTextByPath['prototype/lib/view-state.ts']
if ($null -ne $viewStateSourceText) {
    foreach ($requiredSignal in @('classifySourceFreshness', 'canPublishFromSource', 'FRESH', 'STALE')) {
        if ($viewStateSourceText -cnotlike "*$requiredSignal*") {
            Add-Failure "prototype/lib/view-state.ts: falta la senal contractual '$requiredSignal'."
        }
    }
}

$sourceIds = @{}
$sourceEvidenceById = @{}
if ($null -ne $sourcesDocument) {
    $sourceRootFields = @('catalogVersion', 'capturedAt', 'hashMethod', 'localEvidenceFile', 'sources')
    Test-PropertySet -Object $sourcesDocument -Required $sourceRootFields -Allowed $sourceRootFields -Context 'sources-v1.json'
    $sourceProperties = @(Get-PropertyNames -Object $sourcesDocument)

    if ($sourceProperties -contains 'catalogVersion' -and $sourcesDocument.catalogVersion -ne $CatalogVersion) {
        Add-Failure "sources-v1.json: catalogVersion debe ser '$CatalogVersion'."
    }
    if ($sourceProperties -contains 'capturedAt') {
        Test-IsoDateValue -Value $sourcesDocument.capturedAt -Context 'sources-v1.json.capturedAt'
    }
    if ($sourceProperties -contains 'hashMethod') {
        Test-StringValue -Value $sourcesDocument.hashMethod -Context 'sources-v1.json.hashMethod' -MinimumLength 10
    }
    if ($sourceProperties -contains 'localEvidenceFile' -and $sourcesDocument.localEvidenceFile -ne 'source-evidence-v1.json') {
        Add-Failure 'sources-v1.json.localEvidenceFile: debe ser source-evidence-v1.json.'
    }

    if ($sourceProperties -contains 'sources') {
        $sources = @($sourcesDocument.sources)
        if ($sources.Count -eq 0) {
            Add-Failure 'sources-v1.json: sources debe contener al menos una fuente.'
        }

        $sourceFields = @(
            'id', 'authority', 'scope', 'url', 'resolvedUrl', 'retrievedAt',
            'httpStatus', 'contentType', 'contentLength', 'sha256', 'freshness'
        )
        for ($sourceIndex = 0; $sourceIndex -lt $sources.Count; $sourceIndex++) {
            $source = $sources[$sourceIndex]
            $context = "sources-v1.json.sources[$sourceIndex]"
            if ($null -eq $source) {
                Add-Failure "${context}: la fuente no puede ser null."
                continue
            }

            Test-PropertySet -Object $source -Required $sourceFields -Allowed $sourceFields -Context $context
            $properties = @(Get-PropertyNames -Object $source)

            if ($properties -contains 'id') {
                Test-StringValue -Value $source.id -Context "$context.id" -Pattern '^SRC-[A-Z0-9-]+$'
                if ($sourceIds.ContainsKey([string]$source.id)) {
                    Add-Failure "${context}: source id duplicado '$($source.id)'."
                }
                else {
                    $sourceIds[[string]$source.id] = $true
                }
            }
            foreach ($field in @('authority', 'scope', 'contentType', 'freshness')) {
                if ($properties -contains $field) {
                    Test-StringValue -Value $source.$field -Context "$context.$field" -MinimumLength 1
                }
            }
            foreach ($field in @('url', 'resolvedUrl')) {
                if ($properties -contains $field) {
                    Test-HttpsUrl -Value $source.$field -Context "$context.$field"
                }
            }
            if ($properties -contains 'retrievedAt') {
                Test-IsoDateValue -Value $source.retrievedAt -Context "$context.retrievedAt"
            }
            if ($properties -contains 'httpStatus' -and
                (($source.httpStatus -isnot [int] -and $source.httpStatus -isnot [long]) -or $source.httpStatus -ne 200)) {
                Add-Failure "$context.httpStatus: debe ser el entero 200."
            }
            if ($properties -contains 'contentLength' -and
                (($source.contentLength -isnot [int] -and $source.contentLength -isnot [long]) -or $source.contentLength -le 0)) {
                Add-Failure "$context.contentLength: debe ser un entero mayor que cero."
            }
            if ($properties -contains 'sha256' -and
                ($source.sha256 -isnot [string] -or $source.sha256 -notmatch '^[0-9a-f]{64}$')) {
                Add-Failure "$context.sha256: debe ser SHA-256 lowercase de 64 caracteres hexadecimales."
            }
        }
    }
}

if ($null -ne $sourceEvidenceDocument) {
    $evidenceRootFields = @('catalogVersion', 'capturedAt', 'evidenceKind', 'entries')
    Test-PropertySet -Object $sourceEvidenceDocument -Required $evidenceRootFields -Allowed $evidenceRootFields -Context 'source-evidence-v1.json'
    $evidenceRootProperties = @(Get-PropertyNames -Object $sourceEvidenceDocument)

    if ($evidenceRootProperties -contains 'catalogVersion' -and $sourceEvidenceDocument.catalogVersion -ne $CatalogVersion) {
        Add-Failure "source-evidence-v1.json.catalogVersion: debe ser '$CatalogVersion'."
    }
    if ($evidenceRootProperties -contains 'capturedAt') {
        Test-IsoDateValue -Value $sourceEvidenceDocument.capturedAt -Context 'source-evidence-v1.json.capturedAt'
    }
    if ($evidenceRootProperties -contains 'evidenceKind') {
        Test-StringValue -Value $sourceEvidenceDocument.evidenceKind -Context 'source-evidence-v1.json.evidenceKind' -MinimumLength 3
    }

    if ($evidenceRootProperties -contains 'entries') {
        $evidenceEntries = @($sourceEvidenceDocument.entries)
        $evidenceFields = @('sourceId', 'locator', 'documentReference', 'familyCodes', 'observations')
        for ($evidenceIndex = 0; $evidenceIndex -lt $evidenceEntries.Count; $evidenceIndex++) {
            $evidence = $evidenceEntries[$evidenceIndex]
            $context = "source-evidence-v1.json.entries[$evidenceIndex]"
            if ($null -eq $evidence) {
                Add-Failure "${context}: la evidencia no puede ser null."
                continue
            }

            Test-PropertySet -Object $evidence -Required $evidenceFields -Allowed $evidenceFields -Context $context
            $properties = @(Get-PropertyNames -Object $evidence)
            if ($properties -contains 'sourceId') {
                Test-StringValue -Value $evidence.sourceId -Context "$context.sourceId" -Pattern '^SRC-[A-Z0-9-]+$'
                if ($sourceEvidenceById.ContainsKey([string]$evidence.sourceId)) {
                    Add-Failure "$context.sourceId: evidencia duplicada para '$($evidence.sourceId)'."
                }
                else {
                    $sourceEvidenceById[[string]$evidence.sourceId] = $evidence
                }
                if (-not $sourceIds.ContainsKey([string]$evidence.sourceId)) {
                    Add-Failure "$context.sourceId: '$($evidence.sourceId)' no existe en sources-v1.json."
                }
            }
            if ($properties -contains 'locator') {
                Test-HttpsUrl -Value $evidence.locator -Context "$context.locator"
            }
            if ($properties -contains 'documentReference') {
                Test-StringValue -Value $evidence.documentReference -Context "$context.documentReference" -MinimumLength 3
            }
            if ($properties -contains 'familyCodes') {
                $familyCodes = @($evidence.familyCodes)
                if ($familyCodes.Count -eq 0) {
                    Add-Failure "$context.familyCodes: debe contener al menos una familia."
                }
                Test-UniqueValues -Values $familyCodes -Context "$context.familyCodes"
                foreach ($familyCode in $familyCodes) {
                    Test-StringValue -Value $familyCode -Context "$context.familyCodes" -Pattern '^[A-Z0-9-]+$'
                }
            }
            if ($properties -contains 'observations') {
                $observations = @($evidence.observations)
                if ($observations.Count -eq 0) {
                    Add-Failure "$context.observations: debe contener al menos una observación."
                }
                Test-UniqueValues -Values $observations -Context "$context.observations" -Normalize
                foreach ($observation in $observations) {
                    Test-StringValue -Value $observation -Context "$context.observations" -MinimumLength 3
                }
            }
        }
    }
}

foreach ($sourceId in $sourceIds.Keys) {
    if (-not $sourceEvidenceById.ContainsKey($sourceId)) {
        Add-Failure "source-evidence-v1.json: falta evidencia 1:1 para '$sourceId'."
    }
}
foreach ($sourceId in $sourceEvidenceById.Keys) {
    if (-not $sourceIds.ContainsKey($sourceId)) {
        Add-Failure "source-evidence-v1.json: evidencia huérfana para '$sourceId'."
    }
}

$globalCodes = @{}
$entryByCode = @{}
if ($null -ne $schema) {
    $rootAllowed = @($schema.properties.PSObject.Properties | ForEach-Object { $_.Name })
    $rootRequired = @($schema.required)
    $entrySchema = $schema.'$defs'.catalogEntry
    $entryAllowed = @($entrySchema.properties.PSObject.Properties | ForEach-Object { $_.Name })
    $entryRequired = @($entrySchema.required)
    $familyDimensionsSchema = $schema.'$defs'.familyDimensions
    $familyDimensionsAllowed = @($familyDimensionsSchema.properties.PSObject.Properties | ForEach-Object { $_.Name })
    $familyDimensionsRequired = @($familyDimensionsSchema.required)
    foreach ($definition in $DatasetDefinitions) {
        $dataset = Read-StrictJson -Path $definition.Path
        if ($null -eq $dataset) {
            continue
        }

        $datasetName = Split-Path -Leaf $definition.Path
        Test-PropertySet -Object $dataset -Required $rootRequired -Allowed $rootAllowed -Context $datasetName
        $datasetProperties = @(Get-PropertyNames -Object $dataset)

        if ($datasetProperties -contains 'catalogVersion' -and $dataset.catalogVersion -ne $schema.properties.catalogVersion.const) {
            Add-Failure "$datasetName.catalogVersion: debe ser '$($schema.properties.catalogVersion.const)'."
        }
        if ($datasetProperties -contains 'domain') {
            if (@($schema.properties.domain.enum) -notcontains $dataset.domain) {
                Add-Failure "$datasetName.domain: valor no permitido '$($dataset.domain)'."
            }
            elseif ($dataset.domain -ne $definition.Domain) {
                Add-Failure "$datasetName.domain: se esperaba '$($definition.Domain)' y se obtuvo '$($dataset.domain)'."
            }
        }
        if ($datasetProperties -contains 'status' -and $dataset.status -ne $schema.properties.status.const) {
            Add-Failure "$datasetName.status: debe ser '$($schema.properties.status.const)'."
        }
        if ($datasetProperties -contains 'denominatorDefinition') {
            Test-StringValue -Value $dataset.denominatorDefinition -Context "$datasetName.denominatorDefinition" -MinimumLength ([int]$schema.properties.denominatorDefinition.minLength)
        }

        $hasFamilyDimensions = $datasetProperties -contains 'familyDimensions'
        if ($definition.Domain -eq 'ANIMAL' -and -not $hasFamilyDimensions) {
            Add-Failure "$datasetName.familyDimensions: el dataset ANIMAL debe declarar 13 familias."
        }
        if ($hasFamilyDimensions) {
            $datasetFamilyDimensions = @($dataset.familyDimensions)
            if ($definition.Domain -eq 'ANIMAL' -and $datasetFamilyDimensions.Count -ne 13) {
                Add-Failure "$datasetName.familyDimensions: se esperaban 13 familias y se obtuvieron $($datasetFamilyDimensions.Count)."
            }

            for ($familyDimensionIndex = 0; $familyDimensionIndex -lt $datasetFamilyDimensions.Count; $familyDimensionIndex++) {
                $familyDimensions = $datasetFamilyDimensions[$familyDimensionIndex]
                $familyContext = "$datasetName.familyDimensions[$familyDimensionIndex]"
                if ($null -eq $familyDimensions) {
                    Add-Failure "${familyContext}: el objeto no puede ser null."
                    continue
                }

                Test-PropertySet -Object $familyDimensions -Required $familyDimensionsRequired -Allowed $familyDimensionsAllowed -Context $familyContext
                $familyProperties = @(Get-PropertyNames -Object $familyDimensions)
                if ($familyProperties -contains 'familyCode') {
                    Test-StringValue -Value $familyDimensions.familyCode -Context "$familyContext.familyCode" -Pattern ([string]$familyDimensionsSchema.properties.familyCode.pattern)
                    $familyKey = "$($definition.Domain)|$($familyDimensions.familyCode)"
                    if ($FamilyDimensionsByFamily.ContainsKey($familyKey)) {
                        Add-Failure "$familyContext.familyCode: familia duplicada '$($familyDimensions.familyCode)' en dominio '$($definition.Domain)'."
                    }
                    else {
                        $FamilyDimensionsByFamily[$familyKey] = $familyDimensions
                    }
                }

                foreach ($dimensionField in $DimensionFields) {
                    if ($familyProperties -notcontains $dimensionField) {
                        continue
                    }
                    $dimensionValues = @($familyDimensions.$dimensionField)
                    Test-UniqueValues -Values $dimensionValues -Context "$familyContext.$dimensionField" -Normalize
                    foreach ($dimensionValue in $dimensionValues) {
                        Test-StringValue -Value $dimensionValue -Context "$familyContext.$dimensionField" -MinimumLength 2
                        if ($dimensionValue -is [string] -and -not [string]::IsNullOrWhiteSpace($dimensionValue)) {
                            $FamilyDimensionRecords.Add([pscustomobject]@{
                                Domain = $definition.Domain
                                FamilyCode = [string]$familyDimensions.familyCode
                                Field = $dimensionField
                                Term = [string]$dimensionValue
                                Context = $familyContext
                            })
                        }
                    }
                }
            }
        }
        if ($datasetProperties -notcontains 'entries') {
            continue
        }

        $entries = @($dataset.entries)
        if ($entries.Count -lt [int]$schema.properties.entries.minItems) {
            Add-Failure "$datasetName.entries: debe contener al menos $($schema.properties.entries.minItems) entrada."
        }

        for ($entryIndex = 0; $entryIndex -lt $entries.Count; $entryIndex++) {
            $entry = $entries[$entryIndex]
            $context = "$datasetName.entries[$entryIndex]"
            if ($null -eq $entry) {
                Add-Failure "${context}: la entrada no puede ser null."
                continue
            }

            Test-PropertySet -Object $entry -Required $entryRequired -Allowed $entryAllowed -Context $context
            $entryProperties = @(Get-PropertyNames -Object $entry)

            if ($entryProperties -contains 'code') {
                Test-StringValue -Value $entry.code -Context "$context.code" -Pattern ([string]$entrySchema.properties.code.pattern)
                if ($entry.code -notlike "AR-$($definition.Domain.Substring(0, 3))*") {
                    $expectedPrefix = if ($definition.Domain -eq 'VEGETAL') { 'AR-VEG-' } else { 'AR-ANI-' }
                    if (-not ([string]$entry.code).StartsWith($expectedPrefix, [StringComparison]::Ordinal)) {
                        Add-Failure "$context.code: el código '$($entry.code)' no coincide con el dominio '$($definition.Domain)'."
                    }
                }
                if ($globalCodes.ContainsKey([string]$entry.code)) {
                    Add-Failure "$context.code: código global duplicado '$($entry.code)' (primero en $($globalCodes[[string]$entry.code]))."
                }
                else {
                    $globalCodes[[string]$entry.code] = $context
                }
            }
            if ($entryProperties -contains 'familyCode') {
                Test-StringValue -Value $entry.familyCode -Context "$context.familyCode" -Pattern ([string]$entrySchema.properties.familyCode.pattern)
            }
            if ($entryProperties -contains 'canonicalName') {
                Test-StringValue -Value $entry.canonicalName -Context "$context.canonicalName" -MinimumLength ([int]$entrySchema.properties.canonicalName.minLength)
            }
            if ($entryProperties -contains 'scientificName' -and $null -ne $entry.scientificName -and $entry.scientificName -isnot [string]) {
                Add-Failure "$context.scientificName: debe ser string o null."
            }
            if ($entryProperties -contains 'entryType' -and @($entrySchema.properties.entryType.enum) -notcontains $entry.entryType) {
                Add-Failure "$context.entryType: valor no permitido '$($entry.entryType)'."
            }
            if ($entryProperties -contains 'supportLevel' -and $entry.supportLevel -ne 'CATALOGADA') {
                Add-Failure "$context.supportLevel: toda entrada candidata debe ser CATALOGADA."
            }
            if ($entryProperties -contains 'reviewStatus' -and @($entrySchema.properties.reviewStatus.enum) -notcontains $entry.reviewStatus) {
                Add-Failure "$context.reviewStatus: valor no permitido '$($entry.reviewStatus)'."
            }
            if ($entryProperties -contains 'lifecycleStatus' -and @($entrySchema.properties.lifecycleStatus.enum) -notcontains $entry.lifecycleStatus) {
                Add-Failure "$context.lifecycleStatus: valor no permitido '$($entry.lifecycleStatus)'."
            }
            if ($entryProperties -contains 'jurisdictions') {
                $jurisdictions = @($entry.jurisdictions)
                if ($jurisdictions.Count -lt [int]$entrySchema.properties.jurisdictions.minItems) {
                    Add-Failure "$context.jurisdictions: debe contener al menos una jurisdicción."
                }
                Test-UniqueValues -Values $jurisdictions -Context "$context.jurisdictions"
                foreach ($jurisdiction in $jurisdictions) {
                    if ($jurisdiction -isnot [string] -or $jurisdiction -notmatch $entrySchema.properties.jurisdictions.items.pattern) {
                        Add-Failure "$context.jurisdictions: '$jurisdiction' no cumple el patrón del schema."
                    }
                    elseif ($ValidJurisdictions -notcontains $jurisdiction) {
                        Add-Failure "$context.jurisdictions: '$jurisdiction' no es un código ISO 3166-2 argentino permitido."
                    }
                }
            }
            if ($entryProperties -contains 'sourceIds') {
                $entrySourceIds = @($entry.sourceIds)
                if ($entrySourceIds.Count -lt [int]$entrySchema.properties.sourceIds.minItems) {
                    Add-Failure "$context.sourceIds: debe contener al menos una fuente."
                }
                Test-UniqueValues -Values $entrySourceIds -Context "$context.sourceIds"
                foreach ($sourceId in $entrySourceIds) {
                    if ($sourceId -isnot [string] -or $sourceId -notmatch $entrySchema.properties.sourceIds.items.pattern) {
                        Add-Failure "$context.sourceIds: '$sourceId' no cumple el patrón del schema."
                    }
                    elseif (-not $sourceIds.ContainsKey([string]$sourceId)) {
                        Add-Failure "$context.sourceIds: la fuente referenciada '$sourceId' no existe en sources-v1.json."
                    }
                    elseif ($entryProperties -contains 'familyCode' -and $sourceEvidenceById.ContainsKey([string]$sourceId)) {
                        $evidenceFamilies = @($sourceEvidenceById[[string]$sourceId].familyCodes)
                        if ($evidenceFamilies -notcontains $entry.familyCode) {
                            Add-Failure "$context.sourceIds: '$sourceId' no aporta evidencia para familyCode '$($entry.familyCode)'."
                        }
                    }
                }
            }
            if ($entryProperties -contains 'aliases') {
                $aliases = @($entry.aliases)
                Test-UniqueValues -Values $aliases -Context "$context.aliases" -Normalize
                foreach ($alias in $aliases) {
                    Test-StringValue -Value $alias -Context "$context.aliases" -MinimumLength ([int]$entrySchema.properties.aliases.items.minLength)
                }
            }
            if ($entryProperties -contains 'regulated' -and $entry.regulated -isnot [bool]) {
                Add-Failure "$context.regulated: debe ser boolean."
            }
            if ($entryProperties -contains 'requiresValidatedProfile' -and $entry.requiresValidatedProfile -isnot [bool]) {
                Add-Failure "$context.requiresValidatedProfile: debe ser boolean."
            }
            if ($entryProperties -contains 'regulated' -and $entry.regulated -eq $true) {
                if ($entryProperties -notcontains 'requiresValidatedProfile' -or $entry.requiresValidatedProfile -ne $true) {
                    Add-Failure "${context}: regulated=true exige requiresValidatedProfile=true."
                }
                if ($entryProperties -notcontains 'reviewStatus' -or $entry.reviewStatus -ne 'REVIEW_REQUIRED') {
                    Add-Failure "${context}: regulated=true exige reviewStatus=REVIEW_REQUIRED."
                }
            }
            if ($entryProperties -contains 'notes' -and $entry.notes -isnot [string]) {
                Add-Failure "$context.notes: debe ser string."
            }
            if ($entryProperties -contains 'successorCode' -and $null -ne $entry.successorCode) {
                Test-StringValue -Value $entry.successorCode -Context "$context.successorCode" -Pattern ([string]$entrySchema.properties.successorCode.pattern)
            }

            $AllEntries.Add([pscustomobject]@{
                Dataset = $datasetName
                Domain = $definition.Domain
                Context = $context
                Entry = $entry
            })
            if ($entryProperties -contains 'code' -and -not $entryByCode.ContainsKey([string]$entry.code)) {
                $entryByCode[[string]$entry.code] = $AllEntries[$AllEntries.Count - 1]
            }
        }
    }

    foreach ($wrappedEntry in $AllEntries) {
        $entry = $wrappedEntry.Entry
        $properties = @(Get-PropertyNames -Object $entry)
        $hasSuccessor = $properties -contains 'successorCode' -and $null -ne $entry.successorCode
        if ($properties -contains 'lifecycleStatus' -and $entry.lifecycleStatus -eq 'SUCCEEDED' -and -not $hasSuccessor) {
            Add-Failure "$($wrappedEntry.Context): lifecycleStatus=SUCCEEDED exige successorCode."
        }
        if (-not $hasSuccessor) {
            continue
        }
        if ($properties -notcontains 'lifecycleStatus' -or $entry.lifecycleStatus -ne 'SUCCEEDED') {
            Add-Failure "$($wrappedEntry.Context): successorCode solo está permitido con lifecycleStatus=SUCCEEDED."
        }
        if ($entry.successorCode -eq $entry.code) {
            Add-Failure "$($wrappedEntry.Context): successorCode no puede referenciar la propia entrada."
            continue
        }
        if (-not $entryByCode.ContainsKey([string]$entry.successorCode)) {
            Add-Failure "$($wrappedEntry.Context): successorCode '$($entry.successorCode)' no existe."
            continue
        }
        $target = $entryByCode[[string]$entry.successorCode]
        if ($target.Domain -ne $wrappedEntry.Domain) {
            Add-Failure "$($wrappedEntry.Context): successorCode '$($entry.successorCode)' pertenece a otro dominio."
        }
    }

    $reportedCycles = @{}
    foreach ($startCode in $entryByCode.Keys) {
        $path = New-Object 'System.Collections.Generic.List[string]'
        $positions = @{}
        $currentCode = $startCode
        while ($entryByCode.ContainsKey([string]$currentCode)) {
            if ($positions.ContainsKey([string]$currentCode)) {
                $cycle = @($path.GetRange([int]$positions[[string]$currentCode], $path.Count - [int]$positions[[string]$currentCode]))
                $cycleKey = (@($cycle | Sort-Object) -join '|')
                if (-not $reportedCycles.ContainsKey($cycleKey)) {
                    Add-Failure "Grafo successorCode cíclico: $($cycle -join ' -> ') -> $currentCode."
                    $reportedCycles[$cycleKey] = $true
                }
                break
            }
            $positions[[string]$currentCode] = $path.Count
            $path.Add([string]$currentCode)
            $current = $entryByCode[[string]$currentCode].Entry
            $currentProperties = @(Get-PropertyNames -Object $current)
            if ($currentProperties -notcontains 'successorCode' -or $null -eq $current.successorCode) {
                break
            }
            $currentCode = [string]$current.successorCode
        }
    }
}

if ($null -ne $publicationContractDocument) {
    $contractRootFields = @('$schema', '$id', 'title', 'description', 'contractVersion', 'catalogVersion', 'diff', 'event', 'example')
    Test-PropertySet -Object $publicationContractDocument -Required $contractRootFields -Allowed $contractRootFields -Context 'catalog-publication-contract.json'
    $contractProperties = @(Get-PropertyNames -Object $publicationContractDocument)
    foreach ($field in @('$schema', '$id', 'title', 'description')) {
        if ($contractProperties -contains $field) {
            Test-StringValue -Value $publicationContractDocument.$field -Context "catalog-publication-contract.json.$field" -MinimumLength 3
        }
    }
    if ($contractProperties -contains 'contractVersion' -and $publicationContractDocument.contractVersion -ne '1.0.0') {
        Add-Failure 'catalog-publication-contract.json.contractVersion: debe ser 1.0.0.'
    }
    if ($contractProperties -contains 'catalogVersion' -and $publicationContractDocument.catalogVersion -ne $CatalogVersion) {
        Add-Failure "catalog-publication-contract.json.catalogVersion: debe ser '$CatalogVersion'."
    }

    $expectedDiffRequired = @('fromVersion', 'toVersion', 'generatedAtUtc', 'changes')
    $expectedChangeKinds = @('ADDED', 'UPDATED', 'INACTIVATED', 'SUCCEEDED')
    $expectedChangeRequired = @('code', 'domain', 'kind', 'changedFields', 'sourceIds')
    if ($contractProperties -contains 'diff' -and $null -ne $publicationContractDocument.diff) {
        $diffFields = @('required', 'changeKinds', 'change')
        Test-PropertySet -Object $publicationContractDocument.diff -Required $diffFields -Allowed $diffFields -Context 'catalog-publication-contract.json.diff'
        $diffProperties = @(Get-PropertyNames -Object $publicationContractDocument.diff)
        if ($diffProperties -contains 'required') {
            Test-ExactStringSet -Actual @($publicationContractDocument.diff.required) -Expected $expectedDiffRequired -Context 'catalog-publication-contract.json.diff.required'
        }
        if ($diffProperties -contains 'changeKinds') {
            Test-ExactStringSet -Actual @($publicationContractDocument.diff.changeKinds) -Expected $expectedChangeKinds -Context 'catalog-publication-contract.json.diff.changeKinds'
        }
        if ($diffProperties -contains 'change' -and $null -ne $publicationContractDocument.diff.change) {
            $changeFields = @('required', 'invariants')
            Test-PropertySet -Object $publicationContractDocument.diff.change -Required $changeFields -Allowed $changeFields -Context 'catalog-publication-contract.json.diff.change'
            $changeProperties = @(Get-PropertyNames -Object $publicationContractDocument.diff.change)
            if ($changeProperties -contains 'required') {
                Test-ExactStringSet -Actual @($publicationContractDocument.diff.change.required) -Expected $expectedChangeRequired -Context 'catalog-publication-contract.json.diff.change.required'
            }
            if ($changeProperties -contains 'invariants') {
                $invariants = @($publicationContractDocument.diff.change.invariants)
                Test-UniqueValues -Values $invariants -Context 'catalog-publication-contract.json.diff.change.invariants'
                if ($invariants.Count -ne 4) {
                    Add-Failure 'catalog-publication-contract.json.diff.change.invariants: debe contener exactamente 4 invariantes.'
                }
                foreach ($requiredPattern in @(
                    'code.*estable.*irreutilizable',
                    'INACTIVATED.*referencias',
                    'SUCCEEDED.*successorCode.*dominio',
                    'supportLevel.*evidencia'
                )) {
                    if (@($invariants | Where-Object { $_ -is [string] -and $_ -match $requiredPattern }).Count -ne 1) {
                        Add-Failure "catalog-publication-contract.json.diff.change.invariants: falta una invariante única que cumpla '$requiredPattern'."
                    }
                }
            }
        }
    }

    $expectedEventRequired = @(
        'eventId', 'occurredAtUtc', 'catalogVersion', 'previousCatalogVersion',
        'manifestSha256', 'publishedBySubject', 'reason', 'counts', 'correlationId'
    )
    if ($contractProperties -contains 'event' -and $null -ne $publicationContractDocument.event) {
        $eventFields = @('name', 'version', 'required', 'privacy', 'idempotency', 'compatibility', 'rollback')
        Test-PropertySet -Object $publicationContractDocument.event -Required $eventFields -Allowed $eventFields -Context 'catalog-publication-contract.json.event'
        $eventProperties = @(Get-PropertyNames -Object $publicationContractDocument.event)
        if ($eventProperties -contains 'name' -and $publicationContractDocument.event.name -ne 'ProductCatalogPublished') {
            Add-Failure 'catalog-publication-contract.json.event.name: debe ser ProductCatalogPublished.'
        }
        if ($eventProperties -contains 'version' -and
            (($publicationContractDocument.event.version -isnot [int] -and $publicationContractDocument.event.version -isnot [long]) -or $publicationContractDocument.event.version -ne 1)) {
            Add-Failure 'catalog-publication-contract.json.event.version: debe ser el entero 1.'
        }
        if ($eventProperties -contains 'required') {
            Test-ExactStringSet -Actual @($publicationContractDocument.event.required) -Expected $expectedEventRequired -Context 'catalog-publication-contract.json.event.required'
        }
        if ($eventProperties -contains 'privacy' -and $null -ne $publicationContractDocument.event.privacy) {
            $privacyFields = @('tenantScoped', 'containsPersonalData', 'publishedBySubject')
            Test-PropertySet -Object $publicationContractDocument.event.privacy -Required $privacyFields -Allowed $privacyFields -Context 'catalog-publication-contract.json.event.privacy'
            $privacyProperties = @(Get-PropertyNames -Object $publicationContractDocument.event.privacy)
            if ($privacyProperties -contains 'tenantScoped' -and
                ($publicationContractDocument.event.privacy.tenantScoped -isnot [bool] -or $publicationContractDocument.event.privacy.tenantScoped -ne $false)) {
                Add-Failure 'catalog-publication-contract.json.event.privacy.tenantScoped: debe ser false para el catalogo nacional global.'
            }
            if ($privacyProperties -contains 'containsPersonalData' -and
                ($publicationContractDocument.event.privacy.containsPersonalData -isnot [bool] -or $publicationContractDocument.event.privacy.containsPersonalData -ne $true)) {
                Add-Failure 'catalog-publication-contract.json.event.privacy.containsPersonalData: debe ser true por publishedBySubject seudonimizado.'
            }
            if ($privacyProperties -contains 'publishedBySubject') {
                Test-StringValue -Value $publicationContractDocument.event.privacy.publishedBySubject -Context 'catalog-publication-contract.json.event.privacy.publishedBySubject' -MinimumLength 10
                $subjectPrivacy = [string]$publicationContractDocument.event.privacy.publishedBySubject
                foreach ($requiredPattern in @(
                    'dato personal.*seudonimizad',
                    'actor autenticado',
                    'nunca email.*nombre directo',
                    'acceso.*retenci.n restringid'
                )) {
                    if ($subjectPrivacy -notmatch $requiredPattern) {
                        Add-Failure "catalog-publication-contract.json.event.privacy.publishedBySubject: falta la semantica '$requiredPattern'."
                    }
                }
            }
        }
        if ($eventProperties -contains 'idempotency' -and $publicationContractDocument.event.idempotency -ne 'catalogVersion + manifestSha256') {
            Add-Failure 'catalog-publication-contract.json.event.idempotency: debe ser catalogVersion + manifestSha256.'
        }
        if ($eventProperties -contains 'compatibility') {
            Test-StringValue -Value $publicationContractDocument.event.compatibility -Context 'catalog-publication-contract.json.event.compatibility' -MinimumLength 20
        }
        if ($eventProperties -contains 'rollback') {
            $rollback = [string]$publicationContractDocument.event.rollback
            foreach ($requiredPattern in @('nuevo evento', 'versi.n previa', 'nunca borrar', 'reescribir eventos')) {
                if ($rollback -notmatch $requiredPattern) {
                    Add-Failure "catalog-publication-contract.json.event.rollback: falta la semántica '$requiredPattern'."
                }
            }
        }
    }

    if ($contractProperties -contains 'example' -and $null -ne $publicationContractDocument.example) {
        Test-PropertySet -Object $publicationContractDocument.example -Required @('diff', 'event') -Allowed @('diff', 'event') -Context 'catalog-publication-contract.json.example'
        $exampleProperties = @(Get-PropertyNames -Object $publicationContractDocument.example)
        if ($exampleProperties -contains 'diff' -and $null -ne $publicationContractDocument.example.diff) {
            Test-PropertySet -Object $publicationContractDocument.example.diff -Required $expectedDiffRequired -Allowed $expectedDiffRequired -Context 'catalog-publication-contract.json.example.diff'
            $exampleDiffProperties = @(Get-PropertyNames -Object $publicationContractDocument.example.diff)
            if ($exampleDiffProperties -contains 'toVersion' -and $publicationContractDocument.example.diff.toVersion -ne $CatalogVersion) {
                Add-Failure "catalog-publication-contract.json.example.diff.toVersion: debe ser '$CatalogVersion'."
            }
            if ($exampleDiffProperties -contains 'generatedAtUtc') {
                Test-IsoDateValue -Value $publicationContractDocument.example.diff.generatedAtUtc -Context 'catalog-publication-contract.json.example.diff.generatedAtUtc'
            }
            if ($exampleDiffProperties -contains 'changes') {
                $exampleChanges = @($publicationContractDocument.example.diff.changes)
                if ($exampleChanges.Count -eq 0) {
                    Add-Failure 'catalog-publication-contract.json.example.diff.changes: debe contener al menos un cambio.'
                }
                for ($changeIndex = 0; $changeIndex -lt $exampleChanges.Count; $changeIndex++) {
                    $change = $exampleChanges[$changeIndex]
                    $context = "catalog-publication-contract.json.example.diff.changes[$changeIndex]"
                    Test-PropertySet -Object $change -Required $expectedChangeRequired -Allowed $expectedChangeRequired -Context $context
                    $changeProperties = @(Get-PropertyNames -Object $change)
                    if ($changeProperties -contains 'code') {
                        Test-StringValue -Value $change.code -Context "$context.code" -Pattern '^AR-(VEG|ANI)-[A-Z0-9-]+$'
                        if (-not $entryByCode.ContainsKey([string]$change.code)) {
                            Add-Failure "$context.code: '$($change.code)' no existe en el baseline."
                        }
                    }
                    if ($changeProperties -contains 'domain' -and @('VEGETAL', 'ANIMAL') -notcontains $change.domain) {
                        Add-Failure "$context.domain: dominio no permitido '$($change.domain)'."
                    }
                    if ($changeProperties -contains 'kind' -and $expectedChangeKinds -notcontains $change.kind) {
                        Add-Failure "$context.kind: tipo de cambio no permitido '$($change.kind)'."
                    }
                    foreach ($arrayField in @('changedFields', 'sourceIds')) {
                        if ($changeProperties -contains $arrayField) {
                            $values = @($change.$arrayField)
                            if ($values.Count -eq 0) { Add-Failure "$context.${arrayField}: no puede estar vacío." }
                            Test-UniqueValues -Values $values -Context "$context.$arrayField"
                        }
                    }
                    if ($changeProperties -contains 'sourceIds') {
                        foreach ($sourceId in @($change.sourceIds)) {
                            if (-not $sourceIds.ContainsKey([string]$sourceId)) {
                                Add-Failure "$context.sourceIds: '$sourceId' no existe en sources-v1.json."
                            }
                        }
                    }
                }
            }
        }
        if ($exampleProperties -contains 'event' -and $null -ne $publicationContractDocument.example.event) {
            Test-PropertySet -Object $publicationContractDocument.example.event -Required $expectedEventRequired -Allowed $expectedEventRequired -Context 'catalog-publication-contract.json.example.event'
            $exampleEvent = $publicationContractDocument.example.event
            $exampleEventProperties = @(Get-PropertyNames -Object $exampleEvent)
            if ($exampleEventProperties -contains 'occurredAtUtc') {
                Test-IsoDateValue -Value $exampleEvent.occurredAtUtc -Context 'catalog-publication-contract.json.example.event.occurredAtUtc'
            }
            if ($exampleEventProperties -contains 'catalogVersion' -and $exampleEvent.catalogVersion -ne $CatalogVersion) {
                Add-Failure "catalog-publication-contract.json.example.event.catalogVersion: debe ser '$CatalogVersion'."
            }
            foreach ($field in @('eventId', 'previousCatalogVersion', 'manifestSha256', 'publishedBySubject', 'reason', 'correlationId')) {
                if ($exampleEventProperties -contains $field) {
                    Test-StringValue -Value $exampleEvent.$field -Context "catalog-publication-contract.json.example.event.$field" -MinimumLength 3
                }
            }
            if ($exampleEventProperties -contains 'counts' -and $null -ne $exampleEvent.counts) {
                $countFields = @('added', 'updated', 'inactivated', 'succeeded')
                Test-PropertySet -Object $exampleEvent.counts -Required $countFields -Allowed $countFields -Context 'catalog-publication-contract.json.example.event.counts'
                foreach ($field in $countFields) {
                    if ($exampleEvent.counts.PSObject.Properties.Name -contains $field -and
                        (($exampleEvent.counts.$field -isnot [int] -and $exampleEvent.counts.$field -isnot [long]) -or $exampleEvent.counts.$field -lt 0)) {
                        Add-Failure "catalog-publication-contract.json.example.event.counts.${field}: debe ser entero no negativo."
                    }
                }
            }
        }
    }
}

$catalogFamilyKeys = @{}
foreach ($wrappedEntry in $AllEntries) {
    $entryProperties = @(Get-PropertyNames -Object $wrappedEntry.Entry)
    if ($entryProperties -contains 'familyCode') {
        $catalogFamilyKeys["$($wrappedEntry.Domain)|$($wrappedEntry.Entry.familyCode)"] = $true
    }
}
foreach ($familyKey in $FamilyDimensionsByFamily.Keys) {
    if (-not $catalogFamilyKeys.ContainsKey($familyKey)) {
        Add-Failure "familyDimensions: '$familyKey' no posee ninguna entrada de catálogo en el mismo dominio."
    }
}

$oracleFamilyCount = 0
$oracleDimensionTermCount = 0
$seenOracleFamilies = @{}
if ($null -ne $coverageOracleDocument) {
    $oracleRootFields = @('catalogVersion', 'sourceDocument', 'scope', 'families')
    Test-PropertySet -Object $coverageOracleDocument -Required $oracleRootFields -Allowed $oracleRootFields -Context 'coverage-oracle-v1.json'
    $oracleProperties = @(Get-PropertyNames -Object $coverageOracleDocument)
    if ($oracleProperties -contains 'catalogVersion' -and $coverageOracleDocument.catalogVersion -ne $CatalogVersion) {
        Add-Failure "coverage-oracle-v1.json.catalogVersion: debe ser '$CatalogVersion'."
    }
    foreach ($field in @('sourceDocument', 'scope')) {
        if ($oracleProperties -contains $field) {
            Test-StringValue -Value $coverageOracleDocument.$field -Context "coverage-oracle-v1.json.$field" -MinimumLength 3
        }
    }

    if ($oracleProperties -contains 'families') {
        $oracleFamilies = @($coverageOracleDocument.families)
        $oracleFamilyCount = $oracleFamilies.Count
        if ($oracleFamilies.Count -eq 0) {
            Add-Failure 'coverage-oracle-v1.json.families: debe contener al menos una familia.'
        }
        for ($familyIndex = 0; $familyIndex -lt $oracleFamilies.Count; $familyIndex++) {
            $oracleFamily = $oracleFamilies[$familyIndex]
            $context = "coverage-oracle-v1.json.families[$familyIndex]"
            if ($null -eq $oracleFamily) {
                Add-Failure "${context}: la familia no puede ser null."
                continue
            }

            $familyAllowed = @('familyCode') + $DimensionFields
            Test-PropertySet -Object $oracleFamily -Required $familyAllowed -Allowed $familyAllowed -Context $context
            $familyProperties = @(Get-PropertyNames -Object $oracleFamily)
            if ($familyProperties -notcontains 'familyCode') {
                continue
            }
            Test-StringValue -Value $oracleFamily.familyCode -Context "$context.familyCode" -Pattern '^[A-Z0-9-]+$'
            $familyCode = [string]$oracleFamily.familyCode
            if ($seenOracleFamilies.ContainsKey($familyCode)) {
                Add-Failure "$context.familyCode: familia duplicada '$familyCode'."
            }
            else {
                $seenOracleFamilies[$familyCode] = $true
            }

            $familyKey = "ANIMAL|$familyCode"
            if (-not $FamilyDimensionsByFamily.ContainsKey($familyKey)) {
                Add-Failure "$context.familyCode: '$familyCode' no existe en familyDimensions del dataset ANIMAL."
            }

            foreach ($dimensionField in $DimensionFields) {
                if ($familyProperties -notcontains $dimensionField) {
                    continue
                }
                $expectedValues = @($oracleFamily.$dimensionField)
                $oracleDimensionTermCount += $expectedValues.Count
                Test-UniqueValues -Values $expectedValues -Context "$context.$dimensionField" -Normalize
                foreach ($expectedValue in $expectedValues) {
                    Test-StringValue -Value $expectedValue -Context "$context.$dimensionField" -MinimumLength 2
                    if ($expectedValue -is [string] -and -not [string]::IsNullOrWhiteSpace($expectedValue)) {
                        $OracleDimensionRecords.Add([pscustomobject]@{
                            Domain = 'ANIMAL'
                            FamilyCode = $familyCode
                            Field = $dimensionField
                            Term = [string]$expectedValue
                            Context = $context
                        })
                    }
                }

                if ($FamilyDimensionsByFamily.ContainsKey($familyKey)) {
                    $actualFamilyDimensions = $FamilyDimensionsByFamily[$familyKey]
                    $actualProperties = @(Get-PropertyNames -Object $actualFamilyDimensions)
                    $actualValues = if ($actualProperties -contains $dimensionField) { @($actualFamilyDimensions.$dimensionField) } else { @() }
                    $expectedSignature = (@($expectedValues | Sort-Object -CaseSensitive) -join [char]31)
                    $actualSignature = (@($actualValues | Sort-Object -CaseSensitive) -join [char]31)
                    if ($expectedSignature -cne $actualSignature) {
                        Add-Failure "${context}.${dimensionField}: familyDimensions no coincide exactamente con coverage-oracle para '$familyCode'."
                    }
                }
            }
        }
    }
}

foreach ($familyKey in $FamilyDimensionsByFamily.Keys) {
    if ($familyKey.StartsWith('ANIMAL|', [StringComparison]::Ordinal)) {
        $familyCode = $familyKey.Substring('ANIMAL|'.Length)
        if (-not $seenOracleFamilies.ContainsKey($familyCode)) {
            Add-Failure "coverage-oracle-v1.json: falta la familia '$familyCode' declarada en familyDimensions ANIMAL."
        }
    }
}

$exceptionCount = 0
if ($null -ne $exceptionsDocument) {
    $exceptionRootRequired = @('catalogVersion', 'exceptions')
    $exceptionRootAllowed = @('catalogVersion', 'exceptions')
    Test-PropertySet -Object $exceptionsDocument -Required $exceptionRootRequired -Allowed $exceptionRootAllowed -Context 'exceptions-v1.json'

    if ((Get-PropertyNames -Object $exceptionsDocument) -contains 'catalogVersion' -and $exceptionsDocument.catalogVersion -ne $CatalogVersion) {
        Add-Failure "exceptions-v1.json.catalogVersion: debe ser '$CatalogVersion'."
    }

    if ((Get-PropertyNames -Object $exceptionsDocument) -contains 'exceptions') {
        $exceptions = @($exceptionsDocument.exceptions)
        $exceptionCount = $exceptions.Count
        if ($exceptionCount -eq 0) {
            Add-Failure 'exceptions-v1.json.exceptions: debe contener al menos una excepción declarada.'
        }

        $exceptionIds = @{}
        $exceptionRequired = @('id', 'scope', 'type', 'reason', 'decision', 'approver', 'approvedAt', 'status')
        $exceptionAllowed = $exceptionRequired
        $validExceptionTypes = @('DEFERRED_DEPTH', 'CONTROLLED_PLACEHOLDER', 'REGULATED_ACTIVITY')
        $validExceptionStatuses = @('APPROVED', 'PENDING')

        for ($exceptionIndex = 0; $exceptionIndex -lt $exceptions.Count; $exceptionIndex++) {
            $exception = $exceptions[$exceptionIndex]
            $context = "exceptions-v1.json.exceptions[$exceptionIndex]"
            if ($null -eq $exception) {
                Add-Failure "${context}: la excepción no puede ser null."
                continue
            }

            Test-PropertySet -Object $exception -Required $exceptionRequired -Allowed $exceptionAllowed -Context $context
            $properties = @(Get-PropertyNames -Object $exception)
            foreach ($field in @('scope', 'reason', 'decision', 'approver')) {
                if ($properties -contains $field) {
                    Test-StringValue -Value $exception.$field -Context "$context.$field" -MinimumLength 3
                }
            }
            if ($properties -contains 'id') {
                Test-StringValue -Value $exception.id -Context "$context.id" -Pattern '^EXC-CAT-[0-9]{3}$'
                if ($exceptionIds.ContainsKey([string]$exception.id)) {
                    Add-Failure "$context.id: excepción duplicada '$($exception.id)'."
                }
                else {
                    $exceptionIds[[string]$exception.id] = $true
                }
            }
            if ($properties -contains 'type' -and $validExceptionTypes -notcontains $exception.type) {
                Add-Failure "$context.type: valor no permitido '$($exception.type)'."
            }
            if ($properties -contains 'status' -and $validExceptionStatuses -notcontains $exception.status) {
                Add-Failure "$context.status: valor no permitido '$($exception.status)'."
            }
            if ($properties -contains 'approvedAt') {
                $parsedDate = [datetime]::MinValue
                if ($exception.approvedAt -isnot [string] -or -not [datetime]::TryParseExact(
                    $exception.approvedAt,
                    'yyyy-MM-dd',
                    [Globalization.CultureInfo]::InvariantCulture,
                    [Globalization.DateTimeStyles]::None,
                    [ref]$parsedDate
                )) {
                    Add-Failure "$context.approvedAt: debe usar fecha ISO yyyy-MM-dd."
                }
            }
        }
    }
}

function Find-EntrySearchResults {
    param([Parameter(Mandatory = $true)][string] $Query)

    $queryKey = ConvertTo-SearchKey -Value $Query
    if ([string]::IsNullOrWhiteSpace($queryKey) -or -not $EntrySearchIndex.ContainsKey($queryKey)) {
        return @()
    }
    return @($EntrySearchIndex[$queryKey] | ForEach-Object { $_ })
}

function Find-FamilyDimensionSearchResults {
    param([Parameter(Mandatory = $true)][string] $Query)

    $queryKey = ConvertTo-SearchKey -Value $Query
    if ([string]::IsNullOrWhiteSpace($queryKey) -or -not $FamilyDimensionSearchIndex.ContainsKey($queryKey)) {
        return @()
    }
    return @($FamilyDimensionSearchIndex[$queryKey] | ForEach-Object { $_ })
}

function Test-EntrySearchFixture {
    param(
        [Parameter(Mandatory = $true)][string] $Query,
        [Parameter(Mandatory = $true)][string] $ExpectedCode,
        [Parameter(Mandatory = $true)][string] $Description
    )

    $script:SearchFixtureCount++
    $matches = @(Find-EntrySearchResults -Query $Query)
    if ($matches.Count -ne 1 -or $matches[0].Entry.code -ne $ExpectedCode) {
        $actualCodes = @($matches | ForEach-Object { $_.Entry.code }) -join ', '
        Add-Failure "Búsqueda ENTRY '$Description': '$Query' debía resolver inequívocamente '$ExpectedCode'; resultados=[$actualCodes]."
    }
}

function Test-FamilyDimensionSearchFixture {
    param(
        [Parameter(Mandatory = $true)][string] $Query,
        [Parameter(Mandatory = $true)][string] $ExpectedFamilyCode,
        [Parameter(Mandatory = $true)][string] $ExpectedField,
        [Parameter(Mandatory = $true)][string] $ExpectedTerm
    )

    $script:SearchFixtureCount++
    $matches = @(Find-FamilyDimensionSearchResults -Query $Query)
    $expectedKey = ConvertTo-SearchKey -Value $ExpectedTerm
    $expectedMatches = @($matches | Where-Object {
        $_.FamilyCode -eq $ExpectedFamilyCode -and
        $_.Field -eq $ExpectedField -and
        (ConvertTo-SearchKey -Value $_.Term) -eq $expectedKey
    })
    if ($expectedMatches.Count -ne 1) {
        Add-Failure "Búsqueda FAMILY_DIMENSION '$Query': no encontró exactamente una vez familia='$ExpectedFamilyCode', campo='$ExpectedField', término='$ExpectedTerm'."
    }
}

if ($AllEntries.Count -gt 0) {
    $identityClaims = @{}
    foreach ($wrappedEntry in $AllEntries) {
        $entry = $wrappedEntry.Entry
        $properties = @(Get-PropertyNames -Object $entry)
        if ($properties -notcontains 'code') {
            continue
        }
        $code = [string]$entry.code
        $searchValues = @($entry.code)
        if ($properties -contains 'canonicalName') { $searchValues += $entry.canonicalName }
        if ($properties -contains 'aliases') { $searchValues += @($entry.aliases) }
        foreach ($searchValue in $searchValues) {
            if ($searchValue -isnot [string] -or [string]::IsNullOrWhiteSpace($searchValue)) {
                continue
            }
            $searchKey = ConvertTo-SearchKey -Value $searchValue
            if (-not $EntrySearchIndex.ContainsKey($searchKey)) {
                $EntrySearchIndex[$searchKey] = New-Object 'System.Collections.Generic.List[object]'
            }
            if (@($EntrySearchIndex[$searchKey] | Where-Object { $_.Entry.code -eq $code }).Count -eq 0) {
                $EntrySearchIndex[$searchKey].Add($wrappedEntry)
            }
        }

        $identityValues = @()
        if ($properties -contains 'canonicalName') {
            $identityValues += [pscustomobject]@{ Field = 'canonicalName'; Value = $entry.canonicalName }
        }
        if ($properties -contains 'aliases') {
            foreach ($alias in @($entry.aliases)) {
                $identityValues += [pscustomobject]@{ Field = 'alias'; Value = $alias }
            }
        }
        foreach ($claim in $identityValues) {
            if ($claim.Value -isnot [string] -or [string]::IsNullOrWhiteSpace($claim.Value)) {
                continue
            }
            $claimKey = ConvertTo-SearchKey -Value ([string]$claim.Value)
            if (-not $identityClaims.ContainsKey($claimKey)) {
                $identityClaims[$claimKey] = New-Object 'System.Collections.Generic.List[object]'
            }
            $identityClaims[$claimKey].Add([pscustomobject]@{
                Code = $code
                Field = $claim.Field
                Value = $claim.Value
            })
        }
    }

    foreach ($wrappedEntry in $AllEntries) {
        $entry = $wrappedEntry.Entry
        $properties = @(Get-PropertyNames -Object $entry)
        if ($properties -notcontains 'code') {
            continue
        }
        $code = [string]$entry.code
        Test-EntrySearchFixture -Query $code.ToLowerInvariant() -ExpectedCode $code -Description "$code por código"
        if ($properties -contains 'canonicalName') {
            Test-EntrySearchFixture -Query (ConvertTo-SearchKey -Value ([string]$entry.canonicalName)) -ExpectedCode $code -Description "$code por canonicalName"
        }
        if ($properties -contains 'aliases') {
            foreach ($alias in @($entry.aliases)) {
                if ($alias -is [string] -and -not [string]::IsNullOrWhiteSpace($alias)) {
                    Test-EntrySearchFixture -Query (ConvertTo-SearchKey -Value $alias) -ExpectedCode $code -Description "$code por alias '$alias'"
                }
            }
        }
    }

    foreach ($claimKey in $identityClaims.Keys) {
        $claims = @($identityClaims[$claimKey] | ForEach-Object { $_ })
        $codes = @($claims | ForEach-Object { $_.Code } | Sort-Object -Unique)
        if ($codes.Count -gt 1) {
            $details = @($claims | ForEach-Object { "$($_.Code):$($_.Field)='$($_.Value)'" }) -join ', '
            Add-Failure "Colisión exacta de identidad no declarada para '$claimKey': $details."
        }
    }

    $expectedDimensionIndex = @{}
    foreach ($record in $FamilyDimensionRecords) {
        $termKey = ConvertTo-SearchKey -Value $record.Term
        if (-not $FamilyDimensionSearchIndex.ContainsKey($termKey)) {
            $FamilyDimensionSearchIndex[$termKey] = New-Object 'System.Collections.Generic.List[object]'
        }
        $FamilyDimensionSearchIndex[$termKey].Add($record)
        $recordProperties = @(Get-PropertyNames -Object $record)
        if ($recordProperties -contains 'EntryCode' -or $recordProperties -contains 'Code') {
            Add-Failure "FAMILY_DIMENSION '$($record.FamilyCode)/$($record.Field)/$($record.Term)' no puede atribuir un entry code."
        }
    }
    foreach ($record in $OracleDimensionRecords) {
        $termKey = ConvertTo-SearchKey -Value $record.Term
        if (-not $expectedDimensionIndex.ContainsKey($termKey)) {
            $expectedDimensionIndex[$termKey] = New-Object 'System.Collections.Generic.List[object]'
        }
        $expectedDimensionIndex[$termKey].Add($record)
    }

    $dimensionTermKeys = @($FamilyDimensionSearchIndex.Keys) + @($expectedDimensionIndex.Keys) | Sort-Object -Unique
    foreach ($termKey in $dimensionTermKeys) {
        $actualRecords = if ($FamilyDimensionSearchIndex.ContainsKey($termKey)) {
            @($FamilyDimensionSearchIndex[$termKey] | ForEach-Object { $_ })
        }
        else { @() }
        $expectedRecords = if ($expectedDimensionIndex.ContainsKey($termKey)) {
            @($expectedDimensionIndex[$termKey] | ForEach-Object { $_ })
        }
        else { @() }
        $actualSignatures = @($actualRecords | ForEach-Object {
            "$($_.Domain)|$($_.FamilyCode)|$($_.Field)|$(ConvertTo-SearchKey -Value $_.Term)"
        } | Sort-Object -Unique)
        $expectedSignatures = @($expectedRecords | ForEach-Object {
            "$($_.Domain)|$($_.FamilyCode)|$($_.Field)|$(ConvertTo-SearchKey -Value $_.Term)"
        } | Sort-Object -Unique)
        if ((@($actualSignatures) -join [char]31) -cne (@($expectedSignatures) -join [char]31)) {
            Add-Failure "Colisión o mapping FAMILY_DIMENSION no declarado para '$termKey': actual=[$($actualSignatures -join ', ')], esperado=[$($expectedSignatures -join ', ')]."
        }
    }

    foreach ($record in $OracleDimensionRecords) {
        Test-FamilyDimensionSearchFixture `
            -Query (ConvertTo-SearchKey -Value $record.Term) `
            -ExpectedFamilyCode $record.FamilyCode `
            -ExpectedField $record.Field `
            -ExpectedTerm $record.Term
    }

    $script:SearchFixtureCount++
    $missingQuery = 'zzzz-inexistente-9f4c2d'
    if (@(Find-EntrySearchResults -Query $missingQuery).Count -ne 0 -or
        @(Find-FamilyDimensionSearchResults -Query $missingQuery).Count -ne 0) {
        Add-Failure "Búsqueda inexistente: '$missingQuery' produjo resultados ENTRY o FAMILY_DIMENSION."
    }
}

$regulatedCount = @($AllEntries | Where-Object { $_.Entry.regulated -eq $true }).Count
$plantCount = @($AllEntries | Where-Object { $_.Domain -eq 'VEGETAL' }).Count
$animalCount = @($AllEntries | Where-Object { $_.Domain -eq 'ANIMAL' }).Count

if ($null -ne $manifestCounts) {
    $actualCounts = @{
        sources = $sourceIds.Count
        sourceEvidence = $sourceEvidenceById.Count
        plantEntries = $plantCount
        animalEntries = $animalCount
        totalEntries = $plantCount + $animalCount
        regulatedEntries = $regulatedCount
        exceptions = $exceptionCount
        oracleFamilies = $oracleFamilyCount
        oracleDimensionTerms = $oracleDimensionTermCount
    }
    $manifestCountProperties = @(Get-PropertyNames -Object $manifestCounts)
    foreach ($field in $actualCounts.Keys) {
        if ($manifestCountProperties -contains $field -and $manifestCounts.$field -ne $actualCounts[$field]) {
            Add-Failure "catalog-v1.manifest.json.counts.${field}: manifest=$($manifestCounts.$field), real=$($actualCounts[$field])."
        }
    }
}

Write-Host 'Validación Catálogo Nacional v1'
Write-Host "  Versión: $CatalogVersion"
Write-Host "  Fuentes: $($sourceIds.Count)"
Write-Host "  Entradas vegetales: $plantCount"
Write-Host "  Entradas animales: $animalCount"
Write-Host "  Entradas reguladas: $regulatedCount"
Write-Host "  Excepciones: $exceptionCount"
Write-Host "  Fixtures de búsqueda: $SearchFixtureCount"
Write-Host "  Fallos: $($Failures.Count)"

if ($Failures.Count -gt 0) {
    Write-Host ''
    Write-Host 'Hallazgos:'
    foreach ($failure in $Failures) {
        Write-Host "  - $failure"
    }
    exit 1
}

Write-Host 'Resultado: PASS'
exit 0
