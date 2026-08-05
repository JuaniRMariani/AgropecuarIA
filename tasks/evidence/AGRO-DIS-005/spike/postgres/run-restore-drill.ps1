[CmdletBinding()]
param(
    [ValidateRange(1024, 65533)]
    [int]$Port = 55435,
    [switch]$KeepRuntime
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$ProgressPreference = 'SilentlyContinue'

$scriptRoot = [IO.Path]::GetFullPath($PSScriptRoot)
$runtimeRoot = Join-Path $scriptRoot '.runtime'
$dataRoot = Join-Path $runtimeRoot "data-$Port"
$logRoot = Join-Path $runtimeRoot 'logs'
$sourceObjects = Join-Path $runtimeRoot 'objects-source'
$backupObjects = Join-Path $runtimeRoot 'objects-backup'
$restoredObjects = Join-Path $runtimeRoot 'objects-restored'
$backupRoot = Join-Path $runtimeRoot 'backup'
$serverLog = Join-Path $logRoot "postgres-$Port.log"
$sourceDatabase = 'agro_dis_005_source'
$restoredDatabase = 'agro_dis_005_restored'
$serverStarted = $false
$drillStarted = [DateTimeOffset]::UtcNow

function Assert-ChildPath {
    param([string]$Candidate, [string]$Parent)
    $candidateFull = [IO.Path]::GetFullPath($Candidate).TrimEnd('\') + '\'
    $parentFull = [IO.Path]::GetFullPath($Parent).TrimEnd('\') + '\'
    if (-not $candidateFull.StartsWith($parentFull, [StringComparison]::OrdinalIgnoreCase)) {
        throw "Refusing operation outside expected runtime root: $candidateFull"
    }
}

function Remove-ValidatedDirectory {
    param([string]$Path)
    Assert-ChildPath -Candidate $Path -Parent $runtimeRoot
    if (Test-Path -LiteralPath $Path) {
        Remove-Item -LiteralPath $Path -Recurse -Force
    }
}

function Invoke-CheckedNative {
    param([string]$Executable, [string[]]$Arguments)
    & $Executable @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "Native command failed ($LASTEXITCODE): $Executable $($Arguments -join ' ')"
    }
}

function Assert-PortAvailable {
    param([int]$RequestedPort)
    $listener = [Net.Sockets.TcpListener]::new([Net.IPAddress]::Loopback, $RequestedPort)
    try { $listener.Start() }
    finally { $listener.Stop() }
}

function Write-ObjectBytes {
    param([string]$Root, [string]$ObjectKey, [byte[]]$Bytes)
    $target = Join-Path $Root ($ObjectKey -replace '/', '\')
    Assert-ChildPath -Candidate $target -Parent $Root
    New-Item -ItemType Directory -Path (Split-Path -Parent $target) -Force | Out-Null
    [IO.File]::WriteAllBytes($target, $Bytes)
    return $target
}

function Get-ObjectInventory {
    param([string]$Root)
    if (-not (Test-Path -LiteralPath $Root)) { return @() }
    return @(Get-ChildItem -LiteralPath $Root -File -Recurse | ForEach-Object {
        [ordered]@{
            key = $_.FullName.Substring($Root.Length).TrimStart('\').Replace('\', '/')
            sha256 = (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash.ToLowerInvariant()
            sizeBytes = $_.Length
        }
    })
}

function Get-TextSha256 {
    param([Parameter(Mandatory)] [string]$Value)
    $hasher = [Security.Cryptography.SHA256]::Create()
    try {
        return ([BitConverter]::ToString($hasher.ComputeHash([Text.Encoding]::UTF8.GetBytes($Value))) -replace '-', '').ToLowerInvariant()
    }
    finally {
        $hasher.Dispose()
    }
}

function New-AuditEntryHash {
    param(
        [AllowNull()][AllowEmptyString()][string]$PreviousSha256,
        [Parameter(Mandatory)][string]$TenantId,
        [Parameter(Mandatory)][string]$TenantRef,
        [Parameter(Mandatory)][string]$ResourceId,
        [Parameter(Mandatory)][string]$Action,
        [Parameter(Mandatory)][string]$OccurredAt
    )

    return Get-TextSha256 -Value "$PreviousSha256|$TenantId|$TenantRef|$ResourceId|$Action|$OccurredAt"
}

Assert-PortAvailable -RequestedPort $Port
New-Item -ItemType Directory -Path $runtimeRoot -Force | Out-Null
$lockPath = Join-Path $runtimeRoot 'run.lock'
$lockStream = [IO.File]::Open($lockPath, [IO.FileMode]::OpenOrCreate, [IO.FileAccess]::ReadWrite, [IO.FileShare]::None)

$dis004PostgisRoot = [IO.Path]::GetFullPath((Join-Path $scriptRoot '..\..\..\AGRO-DIS-004\spike\postgis'))
$bootstrapScript = Join-Path $dis004PostgisRoot 'run-postgis-spike.ps1'
$postgresRoot = Join-Path $dis004PostgisRoot '.runtime\postgresql-17-postgis-3.6.2'

try {
    if (-not (Test-Path -LiteralPath (Join-Path $postgresRoot 'bin\postgres.exe'))) {
        Write-Host 'Bootstrapping the pinned PostgreSQL 17/PostGIS 3.6.2 test runtime through AGRO-DIS-004.'
        & $bootstrapScript -Port ($Port + 1)
        if ($LASTEXITCODE -ne 0) { throw 'AGRO-DIS-004 PostGIS runtime bootstrap failed.' }
    }

    $binRoot = Join-Path $postgresRoot 'bin'
    $initDb = Join-Path $binRoot 'initdb.exe'
    $pgCtl = Join-Path $binRoot 'pg_ctl.exe'
    $createdb = Join-Path $binRoot 'createdb.exe'
    $psql = Join-Path $binRoot 'psql.exe'
    $pgDump = Join-Path $binRoot 'pg_dump.exe'
    $pgRestore = Join-Path $binRoot 'pg_restore.exe'
    foreach ($executable in @($initDb, $pgCtl, $createdb, $psql, $pgDump, $pgRestore)) {
        if (-not (Test-Path -LiteralPath $executable)) { throw "Missing runtime executable: $executable" }
    }

    foreach ($path in @($dataRoot, $sourceObjects, $backupObjects, $restoredObjects, $backupRoot)) {
        Remove-ValidatedDirectory -Path $path
    }
    New-Item -ItemType Directory -Path $logRoot, $backupRoot, $sourceObjects -Force | Out-Null

    $keyOne = 'tenants/8e4aa79b0e4e8c2f/quarantine/10000000000040008000000000000001/v1'
    $keyTwo = 'tenants/6d188822a6966e53/quarantine/10000000000040008000000000000002/v1'
    $pathOne = Write-ObjectBytes -Root $sourceObjects -ObjectKey $keyOne -Bytes ([Text.Encoding]::ASCII.GetBytes("%PDF-1.7`nsynthetic field report`n%%EOF"))
    $pathTwo = Write-ObjectBytes -Root $sourceObjects -ObjectKey $keyTwo -Bytes ([byte[]](0x89,0x50,0x4E,0x47,0x0D,0x0A,0x1A,0x0A,0x00,0x00,0x00,0x00))
    $hashOne = (Get-FileHash -LiteralPath $pathOne -Algorithm SHA256).Hash.ToLowerInvariant()
    $hashTwo = (Get-FileHash -LiteralPath $pathTwo -Algorithm SHA256).Hash.ToLowerInvariant()
    $auditOneHash = New-AuditEntryHash -PreviousSha256 '' -TenantId 'aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa' -TenantRef '8e4aa79b0e4e8c2f' -ResourceId '10000000-0000-4000-8000-000000000001' -Action 'upload_completed' -OccurredAt '2026-08-05T12:00:01Z'
    $auditTwoHash = New-AuditEntryHash -PreviousSha256 $auditOneHash -TenantId 'aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa' -TenantRef '8e4aa79b0e4e8c2f' -ResourceId '10000000-0000-4000-8000-000000000001' -Action 'scan_clean' -OccurredAt '2026-08-05T12:00:02Z'
    $auditThreeHash = New-AuditEntryHash -PreviousSha256 '' -TenantId 'bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb' -TenantRef '6d188822a6966e53' -ResourceId '10000000-0000-4000-8000-000000000002' -Action 'upload_completed' -OccurredAt '2026-08-05T12:01:01Z'
    $auditFourHash = New-AuditEntryHash -PreviousSha256 $auditThreeHash -TenantId 'bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb' -TenantRef '6d188822a6966e53' -ResourceId '10000000-0000-4000-8000-000000000002' -Action 'scan_threat' -OccurredAt '2026-08-05T12:01:02Z'

    Invoke-CheckedNative -Executable $initDb -Arguments @('--pgdata', $dataRoot, '--username', 'postgres', '--auth', 'trust', '--encoding', 'UTF8', '--no-locale')
    @(
        '# AGRO-DIS-005: isolated, disposable and loopback-only.',
        "listen_addresses = '127.0.0.1'",
        "port = $Port",
        "max_connections = 20",
        "shared_buffers = '128MB'"
    ) | Add-Content -LiteralPath (Join-Path $dataRoot 'postgresql.conf') -Encoding UTF8
    @('# TYPE DATABASE USER ADDRESS METHOD', 'host all all 127.0.0.1/32 trust') |
        Set-Content -LiteralPath (Join-Path $dataRoot 'pg_hba.conf') -Encoding ASCII

    Invoke-CheckedNative -Executable $pgCtl -Arguments @('start', '--wait', '--timeout', '30', '--pgdata', $dataRoot, '--log', $serverLog)
    $serverStarted = $true
    Invoke-CheckedNative -Executable $createdb -Arguments @('--host', '127.0.0.1', '--port', "$Port", '--username', 'postgres', $sourceDatabase)

    $sourceSql = Join-Path $scriptRoot 'restore-source.sql'
    Invoke-CheckedNative -Executable $psql -Arguments @(
        '--host', '127.0.0.1', '--port', "$Port", '--username', 'postgres', '--dbname', $sourceDatabase,
        '--set', 'ON_ERROR_STOP=1', '--set', "object_one_hash=$hashOne", '--set', "object_one_size=$((Get-Item -LiteralPath $pathOne).Length)",
        '--set', "object_two_hash=$hashTwo", '--set', "object_two_size=$((Get-Item -LiteralPath $pathTwo).Length)",
        '--set', "audit_one_hash=$auditOneHash", '--set', "audit_two_hash=$auditTwoHash",
        '--set', "audit_three_hash=$auditThreeHash", '--set', "audit_four_hash=$auditFourHash", '--file', $sourceSql
    )

    $metadataQuery = "SELECT json_agg(json_build_object('tenantId', tenant_id, 'tenantRef', tenant_ref, 'fileId', file_id, 'version', version, 'objectKey', object_key, 'sha256', sha256, 'sizeBytes', size_bytes, 'state', state, 'legalHold', legal_hold, 'resourceType', resource_type, 'resourceId', resource_id, 'srid', ST_SRID(location), 'x', ST_X(location), 'y', ST_Y(location)) ORDER BY tenant_ref)::text FROM file_objects;"
    $auditQuery = "SELECT json_agg(json_build_object('sequence', sequence, 'tenantId', tenant_id, 'tenantRef', tenant_ref, 'resourceId', resource_id, 'action', action, 'occurredAt', to_char(occurred_at AT TIME ZONE 'UTC', 'YYYY-MM-DD') || 'T' || to_char(occurred_at AT TIME ZONE 'UTC', 'HH24:MI:SS') || 'Z', 'previousSha256', previous_sha256, 'entrySha256', entry_sha256) ORDER BY sequence)::text FROM audit_entries;"
    $sourceMetadataJson = (& $psql '--host' '127.0.0.1' '--port' "$Port" '--username' 'postgres' '--dbname' $sourceDatabase '--tuples-only' '--no-align' '--set' 'ON_ERROR_STOP=1' '--command' $metadataQuery).Trim()
    if ($LASTEXITCODE -ne 0) { throw 'Source metadata snapshot query failed.' }
    $sourceAuditJson = (& $psql '--host' '127.0.0.1' '--port' "$Port" '--username' 'postgres' '--dbname' $sourceDatabase '--tuples-only' '--no-align' '--set' 'ON_ERROR_STOP=1' '--command' $auditQuery).Trim()
    if ($LASTEXITCODE -ne 0) { throw 'Source audit snapshot query failed.' }
    $metadataSnapshotHash = Get-TextSha256 -Value $sourceMetadataJson
    $auditSnapshotHash = Get-TextSha256 -Value $sourceAuditJson
    $sourceAuditRows = ConvertFrom-Json -InputObject $sourceAuditJson
    $terminalAuditEntries = @($sourceAuditRows | Group-Object tenantRef | ForEach-Object {
        $lastEntry = $_.Group | Sort-Object { [long]$_.sequence } | Select-Object -Last 1
        [ordered]@{ tenantId = $lastEntry.tenantId; tenantRef = $_.Name; entrySha256 = $lastEntry.entrySha256 }
    } | Sort-Object tenantRef)

    $cutoff = [DateTimeOffset]::UtcNow
    $dumpPath = Join-Path $backupRoot 'postgres.dump'
    Invoke-CheckedNative -Executable $pgDump -Arguments @('--host', '127.0.0.1', '--port', "$Port", '--username', 'postgres', '--dbname', $sourceDatabase, '--format', 'custom', '--file', $dumpPath)
    Copy-Item -LiteralPath $sourceObjects -Destination $backupObjects -Recurse
    $dumpHash = (Get-FileHash -LiteralPath $dumpPath -Algorithm SHA256).Hash.ToLowerInvariant()
    $inventory = Get-ObjectInventory -Root $backupObjects
    if ($inventory.Count -ne 2) { throw "Expected 2 backed-up objects, found $($inventory.Count)." }

    $manifest = [ordered]@{
        schemaVersion = '1.0'
        backupId = [Guid]::NewGuid().ToString()
        cutoffUtc = $cutoff.ToString('O')
        postgres = [ordered]@{ dumpSha256 = $dumpHash; metadataSnapshotSha256 = $metadataSnapshotHash; postgisVersion = '3.6.2'; recordCount = 2 }
        objects = @(
            [ordered]@{ tenantId = 'aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa'; tenantRef = '8e4aa79b0e4e8c2f'; fileId = '10000000-0000-4000-8000-000000000001'; version = 1; objectKey = $keyOne; sha256 = $hashOne; sizeBytes = (Get-Item $pathOne).Length; state = 'available'; legalHold = $true; resourceType = 'field'; resourceId = '20000000-0000-4000-8000-000000000001'; srid = 4326; longitude = -60.6393; latitude = -32.9442 },
            [ordered]@{ tenantId = 'bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb'; tenantRef = '6d188822a6966e53'; fileId = '10000000-0000-4000-8000-000000000002'; version = 1; objectKey = $keyTwo; sha256 = $hashTwo; sizeBytes = (Get-Item $pathTwo).Length; state = 'quarantined'; legalHold = $false; resourceType = 'field'; resourceId = '20000000-0000-4000-8000-000000000002'; srid = 4326; longitude = -68.8458; latitude = -32.8895 }
        )
        audit = [ordered]@{ recordCount = $sourceAuditRows.Count; lastSequence = [long]$sourceAuditRows[-1].sequence; snapshotSha256 = $auditSnapshotHash; terminalEntries = $terminalAuditEntries }
        targets = [ordered]@{ rpoMinutes = 15; rtoMinutes = 120 }
    }
    $manifestPath = Join-Path $backupRoot 'backup-manifest.json'
    $manifest | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $manifestPath -Encoding UTF8

    [IO.File]::WriteAllBytes($pathOne, [Text.Encoding]::ASCII.GetBytes('corrupted-after-backup'))
    [void](Write-ObjectBytes -Root $sourceObjects -ObjectKey 'tenants/8e4aa79b0e4e8c2f/quarantine/orphan/v1' -Bytes ([byte[]](1,2,3,4)))
    $sourceInventory = Get-ObjectInventory -Root $sourceObjects
    $knownKeys = @($manifest.objects | ForEach-Object objectKey)
    $orphanCount = @($sourceInventory | Where-Object { $_.key -notin $knownKeys }).Count
    $corruptCount = @($sourceInventory | Where-Object { $item = $_; $expected = $manifest.objects | Where-Object objectKey -eq $item.key; $null -ne $expected -and $expected.sha256 -ne $item.sha256 }).Count
    if ($orphanCount -ne 1 -or $corruptCount -ne 1) { throw "Reconciliation did not detect expected orphan/corruption. orphans=$orphanCount corrupt=$corruptCount" }

    $restoreStarted = [DateTimeOffset]::UtcNow
    Invoke-CheckedNative -Executable $createdb -Arguments @('--host', '127.0.0.1', '--port', "$Port", '--username', 'postgres', $restoredDatabase)
    Invoke-CheckedNative -Executable $pgRestore -Arguments @('--host', '127.0.0.1', '--port', "$Port", '--username', 'postgres', '--dbname', $restoredDatabase, '--exit-on-error', $dumpPath)
    Copy-Item -LiteralPath $backupObjects -Destination $restoredObjects -Recurse

    $rowsJson = (& $psql '--host' '127.0.0.1' '--port' "$Port" '--username' 'postgres' '--dbname' $restoredDatabase '--tuples-only' '--no-align' '--set' 'ON_ERROR_STOP=1' '--command' $metadataQuery).Trim()
    if ($LASTEXITCODE -ne 0) { throw 'Restored metadata verification query failed.' }
    if ((Get-TextSha256 -Value $rowsJson) -ne $manifest.postgres.metadataSnapshotSha256) {
        throw 'Restored metadata snapshot differs from the backup manifest.'
    }
    $rows = $rowsJson | ConvertFrom-Json
    $rowCount = @($rows).Count
    $invalidSridCount = @($rows | Where-Object { [int]$_.srid -ne 4326 }).Count
    if ($rowCount -ne 2 -or $invalidSridCount -ne 0) {
        throw "Restored PostGIS rows/SRID mismatch. rows=$rowCount invalidSrid=$invalidSridCount payload=$rowsJson"
    }

    $expectedMetadata = @{
        '10000000-0000-4000-8000-000000000001' = [ordered]@{ tenantId = 'aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa'; tenantRef = '8e4aa79b0e4e8c2f'; version = 1; state = 'available'; legalHold = $true; resourceType = 'field'; resourceId = '20000000-0000-4000-8000-000000000001'; x = -60.6393; y = -32.9442 }
        '10000000-0000-4000-8000-000000000002' = [ordered]@{ tenantId = 'bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb'; tenantRef = '6d188822a6966e53'; version = 1; state = 'quarantined'; legalHold = $false; resourceType = 'field'; resourceId = '20000000-0000-4000-8000-000000000002'; x = -68.8458; y = -32.8895 }
    }
    foreach ($row in $rows) {
        $expectedRow = $expectedMetadata[[string]$row.fileId]
        if (($null -eq $expectedRow) -or
            ([string]$row.tenantId -ne $expectedRow.tenantId) -or
            ($row.tenantRef -ne $expectedRow.tenantRef) -or
            ([int]$row.version -ne $expectedRow.version) -or
            ($row.state -ne $expectedRow.state) -or
            ([bool]$row.legalHold -ne $expectedRow.legalHold) -or
            ([string]$row.resourceType -ne $expectedRow.resourceType) -or
            ([string]$row.resourceId -ne $expectedRow.resourceId) -or
            ([Math]::Abs([double]$row.x - $expectedRow.x) -gt 0.0000001) -or
            ([Math]::Abs([double]$row.y - $expectedRow.y) -gt 0.0000001)) {
            throw "Restored metadata/link/geometry mismatch for file $($row.fileId)."
        }
        $manifestObject = $manifest.objects | Where-Object fileId -eq $row.fileId
        if (($null -eq $manifestObject) -or
            ($manifestObject.tenantId -ne $row.tenantId) -or
            ($manifestObject.tenantRef -ne $row.tenantRef) -or
            ($manifestObject.version -ne $row.version) -or
            ($manifestObject.objectKey -ne $row.objectKey) -or
            ($manifestObject.sha256 -ne $row.sha256) -or
            ($manifestObject.sizeBytes -ne $row.sizeBytes) -or
            ($manifestObject.state -ne $row.state) -or
            ($manifestObject.legalHold -ne $row.legalHold) -or
            ($manifestObject.resourceType -ne $row.resourceType) -or
            ($manifestObject.resourceId -ne $row.resourceId) -or
            ($manifestObject.srid -ne $row.srid) -or
            ([Math]::Abs([double]$manifestObject.longitude - [double]$row.x) -gt 0.0000001) -or
            ([Math]::Abs([double]$manifestObject.latitude - [double]$row.y) -gt 0.0000001)) {
            throw "Restored object link differs from the manifest for file $($row.fileId)."
        }
        $objectPath = Join-Path $restoredObjects ($row.objectKey -replace '/', '\')
        if (-not (Test-Path -LiteralPath $objectPath)) { throw "Restored object missing: $($row.objectKey)" }
        $actualHash = (Get-FileHash -LiteralPath $objectPath -Algorithm SHA256).Hash.ToLowerInvariant()
        if ($actualHash -ne $row.sha256) { throw "Restored hash mismatch: $($row.objectKey)" }
    }

    $restoredAuditJson = (& $psql '--host' '127.0.0.1' '--port' "$Port" '--username' 'postgres' '--dbname' $restoredDatabase '--tuples-only' '--no-align' '--set' 'ON_ERROR_STOP=1' '--command' $auditQuery).Trim()
    if ($LASTEXITCODE -ne 0) { throw 'Restored audit snapshot query failed.' }
    if ((Get-TextSha256 -Value $restoredAuditJson) -ne $manifest.audit.snapshotSha256) {
        throw 'Restored audit snapshot differs from the backup manifest.'
    }
    $auditRows = ConvertFrom-Json -InputObject $restoredAuditJson
    $auditCount = $auditRows.Count
    if ($auditCount -ne $manifest.audit.recordCount -or [long]$auditRows[-1].sequence -ne $manifest.audit.lastSequence) {
        throw "Restored audit count/sequence mismatch: count=$auditCount sequence=$($auditRows[-1].sequence)"
    }
    foreach ($tenantAudit in ($auditRows | Group-Object tenantRef)) {
        $previousEntryHash = $null
        foreach ($entry in ($tenantAudit.Group | Sort-Object { [long]$_.sequence })) {
            if (($null -eq $previousEntryHash -and $null -ne $entry.previousSha256) -or
                ($null -ne $previousEntryHash -and $entry.previousSha256 -ne $previousEntryHash)) {
                throw "Audit chain linkage failed for tenant ref $($tenantAudit.Name) at sequence $($entry.sequence)."
            }
            $occurredAtCanonical = ([DateTimeOffset]$entry.occurredAt).ToUniversalTime().ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", [Globalization.CultureInfo]::InvariantCulture)
            $computedEntryHash = New-AuditEntryHash -PreviousSha256 ([string]$entry.previousSha256) -TenantId ([string]$entry.tenantId) -TenantRef ([string]$entry.tenantRef) -ResourceId ([string]$entry.resourceId) -Action ([string]$entry.action) -OccurredAt $occurredAtCanonical
            if ($computedEntryHash -ne $entry.entrySha256) {
                throw "Audit entry hash failed for tenant ref $($tenantAudit.Name) at sequence $($entry.sequence)."
            }
            $previousEntryHash = $entry.entrySha256
        }
        $terminal = $manifest.audit.terminalEntries | Where-Object tenantRef -eq $tenantAudit.Name
        if ($null -eq $terminal -or $terminal.tenantId -ne $tenantAudit.Group[0].tenantId -or $terminal.entrySha256 -ne $previousEntryHash) {
            throw "Audit terminal hash mismatch for tenant ref $($tenantAudit.Name)."
        }
    }

    $previousErrorPreference = $ErrorActionPreference
    $ErrorActionPreference = 'Continue'
    try {
        $holdProbe = & $psql '--host' '127.0.0.1' '--port' "$Port" '--username' 'postgres' '--dbname' $restoredDatabase '--set' 'ON_ERROR_STOP=1' '--command' "DELETE FROM file_objects WHERE file_id = '10000000-0000-4000-8000-000000000001';" 2>&1
        $holdExitCode = $LASTEXITCODE
    }
    finally {
        $ErrorActionPreference = $previousErrorPreference
    }
    if ($holdExitCode -eq 0 -or ($holdProbe -join "`n") -notmatch 'legal_hold prevents purge') { throw 'Legal hold did not fail closed during purge.' }
    $global:LASTEXITCODE = 0

    $ErrorActionPreference = 'Continue'
    try {
        $auditMutationProbe = & $psql '--host' '127.0.0.1' '--port' "$Port" '--username' 'postgres' '--dbname' $restoredDatabase '--set' 'ON_ERROR_STOP=1' '--command' 'UPDATE audit_entries SET action = ''tampered'' WHERE sequence = 1;' 2>&1
        $auditMutationExitCode = $LASTEXITCODE
    }
    finally {
        $ErrorActionPreference = $previousErrorPreference
    }
    if ($auditMutationExitCode -eq 0 -or ($auditMutationProbe -join "`n") -notmatch 'audit is append-only') { throw 'Audit append-only trigger did not fail closed.' }
    $global:LASTEXITCODE = 0

    $ErrorActionPreference = 'Continue'
    try {
        $tenantBindingProbe = & $psql '--host' '127.0.0.1' '--port' "$Port" '--username' 'postgres' '--dbname' $restoredDatabase '--set' 'ON_ERROR_STOP=1' '--command' "UPDATE file_objects SET tenant_id = 'bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb' WHERE file_id = '10000000-0000-4000-8000-000000000001';" 2>&1
        $tenantBindingExitCode = $LASTEXITCODE
    }
    finally {
        $ErrorActionPreference = $previousErrorPreference
    }
    if ($tenantBindingExitCode -eq 0 -or ($tenantBindingProbe -join "`n") -notmatch 'foreign key constraint') { throw 'Tenant id/reference binding did not fail closed.' }
    $global:LASTEXITCODE = 0

    $corruptDump = Join-Path $backupRoot 'postgres.corrupt.dump'
    Copy-Item -LiteralPath $dumpPath -Destination $corruptDump
    $bytes = [IO.File]::ReadAllBytes($corruptDump)
    $bytes[[Math]::Floor($bytes.Length / 2)] = $bytes[[Math]::Floor($bytes.Length / 2)] -bxor 0xFF
    [IO.File]::WriteAllBytes($corruptDump, $bytes)
    if ((Get-FileHash $corruptDump -Algorithm SHA256).Hash.ToLowerInvariant() -eq $dumpHash) { throw 'Corrupted dump was not detected by its manifest hash.' }

    $rto = ([DateTimeOffset]::UtcNow - $restoreStarted).TotalMinutes
    $backupCaptureWindow = ($cutoff - $drillStarted).TotalMinutes
    if ($rto -gt 120) { throw "The local restore exceeded the hypothetical RTO. RTO=$rto" }

    [ordered]@{
        status = 'PASS'
        records = $rowCount
        objects = $inventory.Count
        auditEntries = $auditCount
        postgisSrid = 4326
        legalHoldBlockedPurge = $true
        orphanDetected = $orphanCount
        corruptionDetected = $corruptCount
        dumpCorruptionDetected = $true
        metadataSnapshotVerified = $true
        auditSnapshotVerified = $true
        auditChainVerified = $true
        auditAppendOnly = $true
        tenantBindingEnforced = $true
        backupCaptureWindowMinutes = [Math]::Round($backupCaptureWindow, 4)
        rpoTargetMinutes = 15
        rpoStatus = 'UNPROVEN_WITHOUT_MANAGED_PITR'
        rtoMinutesObserved = [Math]::Round($rto, 4)
        dumpSha256 = $dumpHash
    } | ConvertTo-Json
}
finally {
    $pgCtl = Join-Path $postgresRoot 'bin\pg_ctl.exe'
    if ($serverStarted -and (Test-Path -LiteralPath $pgCtl)) {
        & $pgCtl stop --wait --timeout 30 --mode fast --pgdata $dataRoot
        if ($LASTEXITCODE -ne 0) { throw 'Failed to stop isolated PostgreSQL.' }
        $serverStarted = $false
    }
    if (Test-Path -LiteralPath $pgCtl) {
        & $pgCtl status --pgdata $dataRoot *> $null
        if ($LASTEXITCODE -eq 0) { throw 'Teardown validation failed: PostgreSQL still running.' }
    }
    Assert-PortAvailable -RequestedPort $Port
    if (-not $KeepRuntime) {
        foreach ($path in @($dataRoot, $sourceObjects, $backupObjects, $restoredObjects)) { Remove-ValidatedDirectory -Path $path }
    }
    $lockStream.Dispose()
    $global:LASTEXITCODE = 0
}
