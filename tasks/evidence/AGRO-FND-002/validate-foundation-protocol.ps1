[CmdletBinding()]
param(
    [switch] $SelfTest
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$evidenceRoot = $PSScriptRoot
$repoRoot = (Resolve-Path -LiteralPath (Join-Path $evidenceRoot '..\..\..')).Path
$protocolPath = Join-Path $evidenceRoot 'foundation-protocol.json'

function Read-Protocol {
    param([string] $Path)

    $raw = Get-Content -LiteralPath $Path -Raw -Encoding UTF8
    return $raw | ConvertFrom-Json
}

function Copy-Protocol {
    param([object] $Protocol)

    return ($Protocol | ConvertTo-Json -Depth 100) | ConvertFrom-Json
}

function Add-Error {
    param(
        [System.Collections.Generic.List[string]] $Errors,
        [bool] $Condition,
        [string] $Message
    )

    if (-not $Condition) {
        $Errors.Add($Message)
    }
}

function Test-ExactSequence {
    param(
        [System.Collections.Generic.List[string]] $Errors,
        [object[]] $Actual,
        [string[]] $Expected,
        [string] $Label
    )

    $actualValues = @($Actual | ForEach-Object { [string] $_ })
    if ($actualValues.Count -ne $Expected.Count -or ($actualValues -join '|') -cne ($Expected -join '|')) {
        $Errors.Add("$Label must be exactly: $($Expected -join ', ').")
    }
}

function Test-ExactProperties {
    param(
        [System.Collections.Generic.List[string]] $Errors,
        [object] $Value,
        [string[]] $Expected,
        [string] $Label
    )

    $actualNames = @($Value.PSObject.Properties.Name | Sort-Object)
    $expectedNames = @($Expected | Sort-Object)
    if ($actualNames.Count -ne $expectedNames.Count -or ($actualNames -join '|') -cne ($expectedNames -join '|')) {
        $Errors.Add("$Label properties must be exactly: $($Expected -join ', ').")
    }
}

function Get-BacklogStatus {
    param(
        [string] $Backlog,
        [string] $TaskId
    )

    $escapedTaskId = [regex]::Escape($TaskId)
    $matches = [regex]::Matches($Backlog, "(?m)^\|\s*\[$escapedTaskId\]\([^\r\n]+\)\s*\|[^\r\n]+$")
    if ($matches.Count -ne 1) {
        return $null
    }

    $cells = @($matches[0].Value.Split('|') | ForEach-Object { $_.Trim() })
    if ($cells.Count -lt 6) {
        return $null
    }

    return $cells[4]
}

function Test-RepositoryReferences {
    param(
        [object] $Protocol,
        [System.Collections.Generic.List[string]] $Errors
    )

    $requiredReferences = @(
        'tasks/evidence/AGRO-FND-002/README.md',
        'tasks/backlog/00-index.md',
        'tasks/backlog/EPIC-01-fundacion-arquitectura.md',
        'tasks/backlog/EPIC-02-identidad-tenancy-autorizacion.md',
        'tasks/implementation-plan.md',
        'tasks/decisions-and-gaps.md',
        'tasks/todo .md',
        'docs/adr/ADR-009-limites-modulares-y-compatibilidad.md',
        'docs/07-seguridad-y-privacidad.md',
        'tasks/evidence/AGRO-FND-002/idempotency-and-delivery-policy.md',
        'tasks/evidence/AGRO-FND-002/audit-retention-and-threats.md'
    )
    Test-ExactSequence -Errors $Errors -Actual @($Protocol.references) -Expected $requiredReferences -Label 'references'

    foreach ($reference in @($Protocol.references)) {
        $resolved = Join-Path $repoRoot ([string] $reference).Replace('/', [IO.Path]::DirectorySeparatorChar)
        if (-not (Test-Path -LiteralPath $resolved -PathType Leaf)) {
            $Errors.Add("Referenced file does not exist: $reference.")
        }
    }

    $backlogPath = Join-Path $repoRoot 'tasks\backlog\00-index.md'
    if (Test-Path -LiteralPath $backlogPath -PathType Leaf) {
        $backlog = Get-Content -LiteralPath $backlogPath -Raw -Encoding UTF8
        $taskStatus = Get-BacklogStatus -Backlog $backlog -TaskId 'AGRO-FND-002'
        if ($taskStatus -notin @('Propuesto', 'Ready', 'En curso')) {
            $Errors.Add("AGRO-FND-002 backlog state must be Propuesto, Ready or En curso while the protocol gate is in progress; found '$taskStatus'.")
        }
        $consumerStatus = Get-BacklogStatus -Backlog $backlog -TaskId 'AGRO-ID-003'
        if ($consumerStatus -ne 'En curso') {
            $Errors.Add("AGRO-ID-003 must be En curso while its first consumer implementation is active; found '$consumerStatus'.")
        }
    }

    $epicFoundationPath = Join-Path $repoRoot 'tasks\backlog\EPIC-01-fundacion-arquitectura.md'
    if (Test-Path -LiteralPath $epicFoundationPath -PathType Leaf) {
        $content = Get-Content -LiteralPath $epicFoundationPath -Raw -Encoding UTF8
        if (-not $content.Contains('## AGRO-FND-002')) {
            $Errors.Add('EPIC-01 no longer contains the exact AGRO-FND-002 task heading.')
        }
        foreach ($required in @('mismo request+mismo key = mismo efecto', 'payload distinto = conflicto', 'commit negocio+outbox')) {
            if (-not $content.Contains($required)) {
                $Errors.Add("EPIC-01 is missing required FND-002 invariant: $required.")
            }
        }
    }

    $epicIdentityPath = Join-Path $repoRoot 'tasks\backlog\EPIC-02-identidad-tenancy-autorizacion.md'
    if (Test-Path -LiteralPath $epicIdentityPath -PathType Leaf) {
        $content = Get-Content -LiteralPath $epicIdentityPath -Raw -Encoding UTF8
        if (-not $content.Contains('## AGRO-ID-003')) {
            $Errors.Add('EPIC-02 no longer contains the nominated AGRO-ID-003 first consumer.')
        }
        if (-not $content.Contains('ID-001, FND-002 y matriz de roles')) {
            $Errors.Add('EPIC-02 no longer preserves the AGRO-ID-003 dependency on FND-002.')
        }
    }

    $decisionsPath = Join-Path $repoRoot 'tasks\decisions-and-gaps.md'
    if (Test-Path -LiteralPath $decisionsPath -PathType Leaf) {
        $content = Get-Content -LiteralPath $decisionsPath -Raw -Encoding UTF8
        foreach ($required in @('ADR-PEND-007', 'Aceptada para desarrollo R1; runtime pendiente', 'debe probar la primera')) {
            if (-not $content.Contains($required)) {
                $Errors.Add("decisions-and-gaps.md is missing required sequencing evidence: $required.")
            }
        }
    }

    $todoPath = Join-Path $repoRoot 'tasks\todo .md'
    if (Test-Path -LiteralPath $todoPath -PathType Leaf) {
        $content = Get-Content -LiteralPath $todoPath -Raw -Encoding UTF8
        foreach ($required in @('AGRO-FND-002 protocolo idempotente', 'CreateOrganization', 'No crear `src/**`')) {
            if (-not $content.Contains($required)) {
                $Errors.Add("Iteration 23 is missing required scope evidence: $required.")
            }
        }
    }

    $readmePath = Join-Path $evidenceRoot 'README.md'
    if (Test-Path -LiteralPath $readmePath -PathType Leaf) {
        $content = Get-Content -LiteralPath $readmePath -Raw -Encoding UTF8
        foreach ($required in @('AGRO-ID-003', 'CreateOrganization', 'permanece `En curso`', 'primer producer real', 'dispatcher, inbox')) {
            if (-not $content.Contains($required)) {
                $Errors.Add("README.md is missing required sequencing or scope evidence: $required.")
            }
        }
    }

    $foundationPolicyPath = Join-Path $evidenceRoot 'idempotency-and-delivery-policy.md'
    if (Test-Path -LiteralPath $foundationPolicyPath -PathType Leaf) {
        $content = Get-Content -LiteralPath $foundationPolicyPath -Raw -Encoding UTF8
        foreach ($required in @(
            "scope_kind='tenant'",
            "scope_kind='platform'",
            'HMAC-SHA-256(key_version_secret, domain_separator || raw_key)',
            'reconciliation_required',
            'Nunca retry ciego.',
            'lease_owner',
            'lease_until_utc',
            'fence_token',
            'stale_owner_zero_effect',
            'stable_ledger_identity',
            'multi_version_lookup_aliases',
            'alias_identity_split',
            'delivery_poisoned',
            'N y N-1',
            'La primera migraci',
            'rollback de binario',
            'CreateOrganization',
            'at-least-once',
            'exactly-once')) {
            if (-not $content.Contains($required)) {
                $Errors.Add("idempotency-and-delivery-policy.md is missing required protocol evidence: $required.")
            }
        }
    }

    $auditPolicyPath = Join-Path $evidenceRoot 'audit-retention-and-threats.md'
    if (Test-Path -LiteralPath $auditPolicyPath -PathType Leaf) {
        $content = Get-Content -LiteralPath $auditPolicyPath -Raw -Encoding UTF8
        foreach ($required in @(
            'AGRO-ID-003/CreateOrganization',
            'protocolo discriminado `platform | tenant`',
            'namespace platform constante',
            '(scope_kind, scope_id, operation, idempotency_key_digest)',
            'El mismo valor de key bajo tenant B es independiente',
            'sin oracle ni lectura cross-tenant',
            '(tenant, operation, idempotency_key)',
            'contractVersion` participa en la huella/binding',
            'no ejecuta auto-purge',
            'journal local o la outbox, el hecho y el ledger se revierten')) {
            if (-not $content.Contains($required)) {
                $Errors.Add("audit-retention-and-threats.md is missing required protocol evidence: $required.")
            }
        }
        if ($content.Contains('tenant distintos produce conflicto')) {
            $Errors.Add('audit-retention-and-threats.md must not claim a cross-tenant conflict oracle.')
        }
    }
}

function Test-Protocol {
    param(
        [object] $Protocol,
        [switch] $CheckRepository
    )

    $errors = [System.Collections.Generic.List[string]]::new()

    Test-ExactProperties $errors $Protocol @('schemaVersion', 'protocolVersion', 'task', 'runtimeImplemented', 'scope', 'idempotencyKey', 'fingerprint', 'authorization', 'ledger', 'transaction', 'concurrency', 'delivery', 'retention', 'firstConsumer', 'completion', 'compatibility', 'sources', 'references') 'protocol'
    Test-ExactProperties $errors $Protocol.task @('id', 'statusBeforeContractGate', 'statusAfterContractGate', 'statusUntilFirstConsumerEvidence') 'task'
    Test-ExactProperties $errors $Protocol.scope @('discriminator', 'supportedKinds', 'tenant', 'platformBootstrap', 'boundContext', 'actorInUniqueness', 'crossTenantLedgerLookup', 'crossTenantConflictResponse') 'scope'
    Test-ExactProperties $errors $Protocol.scope.tenant @('kind', 'uniqueBy', 'tenantDerivedServerSide') 'scope.tenant'
    Test-ExactProperties $errors $Protocol.scope.platformBootstrap @('kind', 'allowedOperation', 'namespace', 'namespaceSource', 'uniqueBy', 'syntheticTenantAllowed') 'scope.platformBootstrap'
    Test-ExactProperties $errors $Protocol.idempotencyKey @('clientGenerated', 'opaque', 'minLength', 'maxLength', 'characterSet', 'validationPattern', 'derivedFromDomainIdentifiers', 'persistence', 'digest', 'logged', 'metricLabel') 'idempotencyKey'
    Test-ExactProperties $errors $Protocol.idempotencyKey.digest @('algorithm', 'keyVersionPersisted', 'secretOutsideDatabase', 'domainSeparated', 'rotationRetainsVerificationMaterial', 'lookupStrategy', 'aliasUniqueBy', 'oneAliasPerLedgerVersionBy', 'lookupAllVerificationVersionsBeforeClaim', 'writerCreatesAliasesForAcceptedVersions', 'activationRequiresNMinus1AliasIntersectionProof', 'historicalBackfillRequired', 'lazyAliasBackfillAllowed', 'multipleAliasLedgerCollisionCode', 'multipleAliasLedgerCollisionResult', 'rotationCannotCreateNewLogicalIdentity', 'unverifiableVersionResult') 'idempotencyKey.digest'
    Test-ExactProperties $errors $Protocol.fingerprint @('algorithm', 'canonicalization', 'inputs', 'rawPayloadPersisted', 'rawPayloadLogged', 'metricLabel') 'fingerprint'
    Test-ExactProperties $errors $Protocol.authorization @('beforeLedgerLookup', 'beforeReplay', 'revalidatesActorResourceAndAuthVersion', 'unauthorizedReplayRevealsExistence') 'authorization'
    Test-ExactProperties $errors $Protocol.ledger @('states', 'sameFingerprint', 'differentFingerprint', 'inProgress', 'responseExpired') 'ledger'
    Test-ExactProperties $errors $Protocol.ledger.differentFingerprint @('httpStatus', 'code') 'ledger.differentFingerprint'
    Test-ExactProperties $errors $Protocol.ledger.inProgress @('httpStatus', 'code', 'retryAfterRequired') 'ledger.inProgress'
    Test-ExactProperties $errors $Protocol.ledger.responseExpired @('reexecuteBusinessEffect', 'result', 'effectMarkerRetained') 'ledger.responseExpired'
    Test-ExactProperties $errors $Protocol.transaction @('database', 'atomicParticipants', 'localJournalFailure', 'outboxFailure', 'centralAuditProjection', 'commitUnknown') 'transaction'
    Test-ExactProperties $errors $Protocol.transaction.commitUnknown @('result', 'blindRetry', 'verificationConnection', 'verificationTargets', 'unresolvedResponse', 'reexecutionRequiresProvenAbsence') 'transaction.commitUnknown'
    Test-ExactProperties $errors $Protocol.concurrency @('claimArbiter', 'leaseFields', 'clock', 'expiredLeaseProvesRollback', 'recoveryRequiresFreshConnection', 'recoveryReauthorizes', 'recoveryRechecks', 'configuration', 'normativeDefaults', 'fencing') 'concurrency'
    Test-ExactProperties $errors $Protocol.concurrency.fencing @('token', 'terminalTransactionVerifies', 'compareAndSetRequired', 'staleOwnerBusinessEffect') 'concurrency.fencing'
    Test-ExactProperties $errors $Protocol.delivery @('guarantee', 'exactlyOnceTransport', 'inboxUniqueBy', 'ordering', 'globalOrdering', 'gapHandling', 'duplicateHandling', 'retry') 'delivery'
    Test-ExactProperties $errors $Protocol.delivery.retry @('bounded', 'classification', 'backoff', 'terminalState', 'reusesLedgerFailedTerminal', 'manualReconciliation') 'delivery.retry'
    Test-ExactProperties $errors $Protocol.retention @('localDevelopmentAutomaticPurge', 'legalRetentionDays', 'externalLegalGate', 'responseExpiryDoesNotAuthorizeReexecution', 'legalHoldPolicyImplemented', 'productionPolicyApproved') 'retention'
    Test-ExactProperties $errors $Protocol.firstConsumer @('taskId', 'capability', 'status', 'implemented', 'ownsProductRlsMigration', 'spikePromotionAllowed') 'firstConsumer'
    Test-ExactProperties $errors $Protocol.completion @('parentRemainsInProgress', 'requiresRealTenantConsumerEvidence', 'contractOnlyDoesNotSatisfyDefinitionOfDone') 'completion'
    Test-ExactProperties $errors $Protocol.compatibility @('migrationStrategy', 'coexistence', 'readersTolerateExistingRows', 'newFields', 'operationEnabledOnlyOnSupportingNodes', 'canonicalizationVersionPersisted', 'applicationBinaryRollback', 'rollForward', 'proof') 'compatibility'

    Add-Error $errors ($Protocol.schemaVersion -ceq '1.0') 'schemaVersion must be 1.0.'
    Add-Error $errors ($Protocol.protocolVersion -ceq '1.0.0') 'protocolVersion must be 1.0.0.'
    Add-Error $errors ($Protocol.task.id -ceq 'AGRO-FND-002') 'task.id must be AGRO-FND-002.'
    Add-Error $errors ($Protocol.task.statusBeforeContractGate -ceq 'Propuesto') 'task.statusBeforeContractGate must be Propuesto.'
    Add-Error $errors ($Protocol.task.statusAfterContractGate -ceq 'En curso') 'task.statusAfterContractGate must be En curso.'
    Add-Error $errors ($Protocol.task.statusUntilFirstConsumerEvidence -ceq 'En curso') 'The parent task must remain En curso until first-consumer evidence exists.'
    Add-Error $errors ($Protocol.runtimeImplemented -eq $false) 'runtimeImplemented must remain false for this contract-only increment.'

    Add-Error $errors ($Protocol.scope.discriminator -ceq 'scopeKind') 'scope.discriminator must be scopeKind.'
    Test-ExactSequence $errors @($Protocol.scope.supportedKinds) @('tenant', 'platform') 'scope.supportedKinds'
    Add-Error $errors ($Protocol.scope.tenant.kind -ceq 'tenant') 'scope.tenant.kind must be tenant.'
    Test-ExactSequence $errors @($Protocol.scope.tenant.uniqueBy) @('tenantId', 'operation', 'idempotencyKey') 'scope.tenant.uniqueBy'
    Add-Error $errors ($Protocol.scope.tenant.tenantDerivedServerSide -eq $true) 'Tenant must be derived server-side.'
    Add-Error $errors ($Protocol.scope.platformBootstrap.kind -ceq 'platform') 'scope.platformBootstrap.kind must be platform.'
    Add-Error $errors ($Protocol.scope.platformBootstrap.allowedOperation -ceq 'CreateOrganization') 'The platform bootstrap scope must be limited to CreateOrganization.'
    Add-Error $errors ($Protocol.scope.platformBootstrap.namespace -ceq 'organization-bootstrap') 'The platform bootstrap namespace must be explicit and stable.'
    Add-Error $errors ($Protocol.scope.platformBootstrap.namespaceSource -ceq 'server_constant') 'The platform bootstrap namespace must be a server constant.'
    Test-ExactSequence $errors @($Protocol.scope.platformBootstrap.uniqueBy) @('platformNamespace', 'operation', 'idempotencyKey') 'scope.platformBootstrap.uniqueBy'
    Add-Error $errors ($Protocol.scope.platformBootstrap.syntheticTenantAllowed -eq $false) 'Platform bootstrap must not use a synthetic tenant.'
    Test-ExactSequence $errors @($Protocol.scope.boundContext) @('actorId', 'resourceId', 'authVersion', 'requestFingerprint') 'scope.boundContext'
    Add-Error $errors ($Protocol.scope.actorInUniqueness -eq $false) 'Actor must be bound to the record but excluded from uniqueness.'
    Add-Error $errors ($Protocol.scope.crossTenantLedgerLookup -eq $false) 'A request must never look up another tenant ledger partition.'
    Add-Error $errors ($Protocol.scope.crossTenantConflictResponse -eq $false) 'The protocol must not create a cross-tenant conflict oracle.'

    Add-Error $errors ($Protocol.idempotencyKey.clientGenerated -eq $true) 'Idempotency key must be client-generated.'
    Add-Error $errors ($Protocol.idempotencyKey.opaque -eq $true) 'Idempotency key must be opaque.'
    Add-Error $errors ($Protocol.idempotencyKey.minLength -eq 16) 'Idempotency key minimum length must be 16.'
    Add-Error $errors ($Protocol.idempotencyKey.maxLength -eq 128) 'Idempotency key maximum length must be 128.'
    Add-Error $errors ($Protocol.idempotencyKey.characterSet -ceq 'visible-ascii') 'Idempotency key must use visible ASCII.'
    Add-Error $errors ($Protocol.idempotencyKey.validationPattern -ceq '^[\x21-\x7E]{16,128}$') 'Idempotency key validation pattern must enforce 16-128 visible ASCII characters.'
    Add-Error $errors ($Protocol.idempotencyKey.derivedFromDomainIdentifiers -eq $false) 'Idempotency key must not derive from tenant, actor, resource or another domain identifier.'
    Add-Error $errors ($Protocol.idempotencyKey.persistence -ceq 'keyed_digest_only') 'Only a keyed digest of the idempotency key may be persisted.'
    Add-Error $errors ($Protocol.idempotencyKey.digest.algorithm -ceq 'hmac-sha256') 'Idempotency key persistence must use HMAC-SHA-256.'
    Add-Error $errors ($Protocol.idempotencyKey.digest.keyVersionPersisted -eq $true) 'The HMAC key version must be persisted for rotation.'
    Add-Error $errors ($Protocol.idempotencyKey.digest.secretOutsideDatabase -eq $true) 'HMAC key material must remain outside PostgreSQL.'
    Add-Error $errors ($Protocol.idempotencyKey.digest.domainSeparated -eq $true) 'The HMAC input must use domain separation.'
    Add-Error $errors ($Protocol.idempotencyKey.digest.rotationRetainsVerificationMaterial -eq $true) 'Rotation must retain verification material for the approved horizon.'
    Add-Error $errors ($Protocol.idempotencyKey.digest.lookupStrategy -ceq 'multi_version_aliases') 'HMAC rotation must use multi-version aliases for stable logical lookup.'
    Test-ExactSequence $errors @($Protocol.idempotencyKey.digest.aliasUniqueBy) @('scopeKind', 'scopeId', 'operation', 'keyVersion', 'keyDigest') 'idempotencyKey.digest.aliasUniqueBy'
    Test-ExactSequence $errors @($Protocol.idempotencyKey.digest.oneAliasPerLedgerVersionBy) @('ledgerId', 'keyVersion') 'idempotencyKey.digest.oneAliasPerLedgerVersionBy'
    Add-Error $errors ($Protocol.idempotencyKey.digest.lookupAllVerificationVersionsBeforeClaim -eq $true) 'A claim must look up every retained verification version first.'
    Add-Error $errors ($Protocol.idempotencyKey.digest.writerCreatesAliasesForAcceptedVersions -eq $true) 'Writers must create aliases for all accepted HMAC versions.'
    Add-Error $errors ($Protocol.idempotencyKey.digest.activationRequiresNMinus1AliasIntersectionProof -eq $true) 'HMAC activation requires proven N/N-1 alias intersection.'
    Add-Error $errors ($Protocol.idempotencyKey.digest.historicalBackfillRequired -eq $false) 'Historical rows must remain discoverable without impossible raw-key backfill.'
    Add-Error $errors ($Protocol.idempotencyKey.digest.lazyAliasBackfillAllowed -eq $true) 'A discovered historical ledger must support transactional lazy alias backfill.'
    Add-Error $errors ($Protocol.idempotencyKey.digest.multipleAliasLedgerCollisionCode -ceq 'alias_identity_split') 'Multiple ledger identities must use the explicit alias_identity_split reason.'
    Add-Error $errors ($Protocol.idempotencyKey.digest.multipleAliasLedgerCollisionResult -ceq 'reconciliation_required') 'Aliases resolving to multiple ledgers must fail closed into reconciliation.'
    Add-Error $errors ($Protocol.idempotencyKey.digest.rotationCannotCreateNewLogicalIdentity -eq $true) 'HMAC rotation must not create a second logical key identity.'
    Add-Error $errors ($Protocol.idempotencyKey.digest.unverifiableVersionResult -ceq 'reconciliation_required') 'An unverifiable HMAC version must require reconciliation, not become a new key.'
    Add-Error $errors ($Protocol.idempotencyKey.logged -eq $false) 'Idempotency key must not be logged.'
    Add-Error $errors ($Protocol.idempotencyKey.metricLabel -eq $false) 'Idempotency key must not be a metric label.'

    Add-Error $errors ($Protocol.fingerprint.algorithm -ceq 'sha256') 'Fingerprint algorithm must be sha256.'
    Add-Error $errors ($Protocol.fingerprint.canonicalization -ceq 'operation-versioned-deterministic') 'Fingerprint canonicalization must be operation-versioned and deterministic.'
    Test-ExactSequence $errors @($Protocol.fingerprint.inputs) @('httpMethod', 'routeTemplate', 'contractVersion', 'normalizedPayload') 'fingerprint.inputs'
    Add-Error $errors ($Protocol.fingerprint.rawPayloadPersisted -eq $false) 'Raw payload must not be persisted.'
    Add-Error $errors ($Protocol.fingerprint.rawPayloadLogged -eq $false) 'Raw payload must not be logged.'
    Add-Error $errors ($Protocol.fingerprint.metricLabel -eq $false) 'Fingerprint must not be a metric label.'

    Add-Error $errors ($Protocol.authorization.beforeLedgerLookup -eq $true) 'Authorization must occur before ledger lookup.'
    Add-Error $errors ($Protocol.authorization.beforeReplay -eq $true) 'Authorization must be revalidated before replay.'
    Add-Error $errors ($Protocol.authorization.revalidatesActorResourceAndAuthVersion -eq $true) 'Replay must revalidate actor, resource and authorization version.'
    Add-Error $errors ($Protocol.authorization.unauthorizedReplayRevealsExistence -eq $false) 'Unauthorized replay must not reveal resource or ledger existence.'

    Test-ExactSequence $errors @($Protocol.ledger.states) @('in_progress', 'succeeded', 'failed_terminal', 'response_expired') 'ledger.states'
    Add-Error $errors ($Protocol.ledger.sameFingerprint -ceq 'semantic_result_or_authorized_reconstruction') 'Same key and fingerprint must return only a semantic result or an authorized reconstruction.'
    Add-Error $errors ($Protocol.ledger.differentFingerprint.httpStatus -eq 409) 'A reused key with a different fingerprint must return 409.'
    Add-Error $errors ($Protocol.ledger.differentFingerprint.code -ceq 'idempotency.key_reused') 'Fingerprint mismatch must use the canonical conflict code.'
    Add-Error $errors ($Protocol.ledger.inProgress.httpStatus -eq 409) 'An in-progress duplicate must return 409.'
    Add-Error $errors ($Protocol.ledger.inProgress.code -ceq 'idempotency.in_progress') 'An in-progress duplicate must use the canonical code.'
    Add-Error $errors ($Protocol.ledger.inProgress.retryAfterRequired -eq $true) 'An in-progress response must include Retry-After.'
    Add-Error $errors ($Protocol.ledger.responseExpired.reexecuteBusinessEffect -eq $false) 'Expired response data must never authorize re-execution.'
    Add-Error $errors ($Protocol.ledger.responseExpired.result -ceq 'reconcile_or_lookup_resource') 'Expired response data must require reconciliation or resource lookup.'
    Add-Error $errors ($Protocol.ledger.responseExpired.effectMarkerRetained -eq $true) 'Expired response data must retain the effect marker.'

    Add-Error $errors ($Protocol.transaction.database -ceq 'postgresql') 'The atomic unit must use PostgreSQL.'
    Test-ExactSequence $errors @($Protocol.transaction.atomicParticipants) @('businessEffect', 'idempotencyLedger', 'localJournal', 'outbox') 'transaction.atomicParticipants'
    Add-Error $errors ($Protocol.transaction.localJournalFailure -ceq 'rollback_transaction') 'Local journal failure must roll back the transaction.'
    Add-Error $errors ($Protocol.transaction.outboxFailure -ceq 'rollback_transaction') 'Outbox failure must roll back the transaction.'
    Add-Error $errors ($Protocol.transaction.centralAuditProjection -ceq 'eventual_at_least_once') 'Central audit must remain an eventual at-least-once projection.'
    Add-Error $errors ($Protocol.transaction.commitUnknown.result -ceq 'reconciliation_required') 'An unknown commit result must require reconciliation.'
    Add-Error $errors ($Protocol.transaction.commitUnknown.blindRetry -eq $false) 'An unknown commit must never be retried blindly.'
    Add-Error $errors ($Protocol.transaction.commitUnknown.verificationConnection -ceq 'new') 'Unknown commit verification must use a new connection.'
    Test-ExactSequence $errors @($Protocol.transaction.commitUnknown.verificationTargets) @('idempotencyLedger', 'businessEffect', 'outbox') 'transaction.commitUnknown.verificationTargets'
    Add-Error $errors ($Protocol.transaction.commitUnknown.unresolvedResponse -ceq 'service_unavailable') 'An unresolved commit must fail unavailable without another effect.'
    Add-Error $errors ($Protocol.transaction.commitUnknown.reexecutionRequiresProvenAbsence -eq $true) 'Re-execution requires proven absence of the original effect.'

    Add-Error $errors ($Protocol.concurrency.claimArbiter -ceq 'postgresql_unique_constraint') 'The PostgreSQL unique constraint must arbitrate claims.'
    Test-ExactSequence $errors @($Protocol.concurrency.leaseFields) @('startedAtUtc', 'leaseOwner', 'leaseUntilUtc', 'attempt', 'concurrencyVersion') 'concurrency.leaseFields'
    Add-Error $errors ($Protocol.concurrency.clock -ceq 'postgresql') 'Lease time must come from PostgreSQL.'
    Add-Error $errors ($Protocol.concurrency.expiredLeaseProvesRollback -eq $false) 'An expired lease must not be treated as proof of rollback.'
    Add-Error $errors ($Protocol.concurrency.recoveryRequiresFreshConnection -eq $true) 'Lease recovery must use a fresh connection.'
    Add-Error $errors ($Protocol.concurrency.recoveryReauthorizes -eq $true) 'Lease recovery must reauthorize before reclaim.'
    Test-ExactSequence $errors @($Protocol.concurrency.recoveryRechecks) @('idempotencyLedger', 'businessInvariant', 'outbox') 'concurrency.recoveryRechecks'
    Add-Error $errors ($Protocol.concurrency.configuration -ceq 'runtime_validated') 'Lease configuration must be runtime validated.'
    Add-Error $errors ($Protocol.concurrency.normativeDefaults -eq $false) 'The contract must not invent normative lease defaults.'
    Add-Error $errors ($Protocol.concurrency.fencing.token -ceq 'monotonic') 'Lease recovery must issue a monotonic fencing token.'
    Test-ExactSequence $errors @($Protocol.concurrency.fencing.terminalTransactionVerifies) @('leaseOwner', 'fenceToken') 'concurrency.fencing.terminalTransactionVerifies'
    Add-Error $errors ($Protocol.concurrency.fencing.compareAndSetRequired -eq $true) 'The terminal transaction must use compare-and-set on owner and fence.'
    Add-Error $errors ($Protocol.concurrency.fencing.staleOwnerBusinessEffect -ceq 'none') 'A stale lease owner must produce zero business effect.'

    Add-Error $errors ($Protocol.delivery.guarantee -ceq 'at_least_once') 'Delivery guarantee must be at-least-once.'
    Add-Error $errors ($Protocol.delivery.exactlyOnceTransport -eq $false) 'The protocol must not claim exactly-once transport.'
    Test-ExactSequence $errors @($Protocol.delivery.inboxUniqueBy) @('consumer', 'eventId') 'delivery.inboxUniqueBy'
    Add-Error $errors ($Protocol.delivery.ordering -ceq 'aggregate_stream_only') 'Ordering must be limited to an aggregate stream.'
    Add-Error $errors ($Protocol.delivery.globalOrdering -eq $false) 'The protocol must not claim global ordering.'
    Add-Error $errors ($Protocol.delivery.gapHandling -ceq 'poison_quarantine') 'Aggregate version gaps must enter poison quarantine.'
    Add-Error $errors ($Protocol.delivery.duplicateHandling -ceq 'acknowledge_without_effect') 'Duplicate delivery must be acknowledged without another effect.'
    Add-Error $errors ($Protocol.delivery.retry.bounded -eq $true) 'Retries must be bounded.'
    Add-Error $errors ($Protocol.delivery.retry.classification -ceq 'explicit_retryable_or_terminal') 'Retry classification must distinguish retryable and terminal failures.'
    Add-Error $errors ($Protocol.delivery.retry.backoff -ceq 'configured_with_jitter') 'Retry backoff must be configured with jitter.'
    Add-Error $errors ($Protocol.delivery.retry.terminalState -ceq 'delivery_poisoned') 'Exhausted delivery retries must use delivery_poisoned.'
    Add-Error $errors ($Protocol.delivery.retry.reusesLedgerFailedTerminal -eq $false) 'Delivery poison must remain separate from ledger failed_terminal.'
    Add-Error $errors ($Protocol.delivery.retry.manualReconciliation -eq $true) 'Poison handling must support explicit reconciliation.'

    Add-Error $errors ($Protocol.retention.localDevelopmentAutomaticPurge -eq $false) 'Local development must not auto-purge protocol evidence.'
    Add-Error $errors ($null -eq $Protocol.retention.legalRetentionDays) 'The protocol must not invent legal retention days.'
    Test-ExactSequence $errors @($Protocol.retention.externalLegalGate) @('Q-060', 'VAL-LEG') 'retention.externalLegalGate'
    Add-Error $errors ($Protocol.retention.responseExpiryDoesNotAuthorizeReexecution -eq $true) 'Response expiry must not authorize re-execution.'
    Add-Error $errors ($Protocol.retention.legalHoldPolicyImplemented -eq $false) 'The contract-only increment must not claim implemented legal hold.'
    Add-Error $errors ($Protocol.retention.productionPolicyApproved -eq $false) 'Production retention policy must remain unapproved.'

    Add-Error $errors ($Protocol.firstConsumer.taskId -ceq 'AGRO-ID-003') 'The nominated first consumer must be AGRO-ID-003.'
    Add-Error $errors ($Protocol.firstConsumer.capability -ceq 'CreateOrganization') 'The nominated first capability must be CreateOrganization.'
    Add-Error $errors ($Protocol.firstConsumer.status -ceq 'integrated_local') 'The first consumer must reflect its locally integrated implementation state.'
    Add-Error $errors ($Protocol.firstConsumer.implemented -eq $true) 'The first consumer must retain its locally verified runtime evidence.'
    Add-Error $errors ($Protocol.firstConsumer.ownsProductRlsMigration -eq $true) 'AGRO-ID-003 must own its product RLS migration.'
    Add-Error $errors ($Protocol.firstConsumer.spikePromotionAllowed -eq $false) 'The disposable spike must not be promoted into runtime.'

    Add-Error $errors ($Protocol.completion.parentRemainsInProgress -eq $true) 'AGRO-FND-002 must remain En curso.'
    Add-Error $errors ($Protocol.completion.requiresRealTenantConsumerEvidence -eq $true) 'Completion must require evidence from a real tenant consumer.'
    Add-Error $errors ($Protocol.completion.contractOnlyDoesNotSatisfyDefinitionOfDone -eq $true) 'Contract-only evidence must not satisfy the task Definition of Done.'

    Add-Error $errors ($Protocol.compatibility.migrationStrategy -ceq 'expand') 'The first migration step must be expand.'
    Add-Error $errors ($Protocol.compatibility.coexistence -ceq 'N/N-1') 'Compatibility must explicitly cover N/N-1 coexistence.'
    Add-Error $errors ($Protocol.compatibility.readersTolerateExistingRows -eq $true) 'N and N-1 readers must tolerate existing rows.'
    Add-Error $errors ($Protocol.compatibility.newFields -ceq 'nullable_or_compatible_default_until_backfill') 'New fields must remain nullable or use a compatible default until backfill.'
    Add-Error $errors ($Protocol.compatibility.operationEnabledOnlyOnSupportingNodes -eq $true) 'The operation must be enabled only on nodes supporting the protocol.'
    Add-Error $errors ($Protocol.compatibility.canonicalizationVersionPersisted -eq $true) 'Canonicalization version must be persisted across rollout.'
    Add-Error $errors ($Protocol.compatibility.applicationBinaryRollback -eq $true) 'The rollout must preserve application binary rollback.'
    Add-Error $errors ($Protocol.compatibility.rollForward -eq $true) 'The rollout must preserve roll-forward.'
    Add-Error $errors ($Protocol.compatibility.proof -ceq 'no_duplicate_effect_and_no_event_loss') 'Compatibility tests must prove no duplicate effect and no event loss.'

    Test-ExactSequence $errors @($Protocol.sources) @(
        'https://datatracker.ietf.org/doc/draft-ietf-httpapi-idempotency-key-header/07/',
        'https://www.rfc-editor.org/rfc/rfc9651.html',
        'https://www.rfc-editor.org/rfc/rfc8785.html',
        'https://www.rfc-editor.org/rfc/rfc9110.html#name-retry-after',
        'https://learn.microsoft.com/ef/core/saving/transactions',
        'https://learn.microsoft.com/ef/core/miscellaneous/connection-resiliency#transaction-commit-failure-and-the-idempotency-issue',
        'https://www.postgresql.org/docs/current/sql-select.html',
        'https://www.postgresql.org/docs/current/transaction-iso.html',
        'https://www.postgresql.org/docs/current/sql-insert.html'
    ) 'sources'

    if ($CheckRepository) {
        Test-RepositoryReferences -Protocol $Protocol -Errors $errors
    }

    return $errors
}

function Invoke-MutationTests {
    param([object] $Protocol)

    $baseErrors = @(Test-Protocol -Protocol $Protocol)
    if ($baseErrors.Count -gt 0) {
        throw "Canonical protocol is invalid before mutation tests: $($baseErrors -join ' | ')."
    }

    $cases = @(
        @{ Name = 'historical-body-replay-claim'; Mutate = { param($p) $p.ledger.sameFingerprint = 'replay_historical_body' } },
        @{ Name = 'wrong-pre-gate-status'; Mutate = { param($p) $p.task.statusBeforeContractGate = 'En curso' } },
        @{ Name = 'fake-runtime'; Mutate = { param($p) $p.runtimeImplemented = $true } },
        @{ Name = 'actor-added-to-uniqueness'; Mutate = { param($p) $p.scope.tenant.uniqueBy = @('tenantId', 'operation', 'actorId', 'idempotencyKey') } },
        @{ Name = 'tenant-removed-from-uniqueness'; Mutate = { param($p) $p.scope.tenant.uniqueBy = @('operation', 'idempotencyKey') } },
        @{ Name = 'scope-discriminator-removed'; Mutate = { param($p) $p.scope.discriminator = '' } },
        @{ Name = 'synthetic-tenant-for-bootstrap'; Mutate = { param($p) $p.scope.platformBootstrap.syntheticTenantAllowed = $true } },
        @{ Name = 'cross-tenant-ledger-lookup'; Mutate = { param($p) $p.scope.crossTenantLedgerLookup = $true } },
        @{ Name = 'cross-tenant-conflict-oracle'; Mutate = { param($p) $p.scope.crossTenantConflictResponse = $true } },
        @{ Name = 'key-not-opaque'; Mutate = { param($p) $p.idempotencyKey.opaque = $false } },
        @{ Name = 'key-outside-length-contract'; Mutate = { param($p) $p.idempotencyKey.maxLength = 512 } },
        @{ Name = 'unkeyed-key-digest'; Mutate = { param($p) $p.idempotencyKey.digest.algorithm = 'sha256' } },
        @{ Name = 'key-version-not-persisted'; Mutate = { param($p) $p.idempotencyKey.digest.keyVersionPersisted = $false } },
        @{ Name = 'rotation-bypasses-logical-identity'; Mutate = { param($p) $p.idempotencyKey.digest.rotationCannotCreateNewLogicalIdentity = $false } },
        @{ Name = 'rotation-without-N-minus-1-alias-proof'; Mutate = { param($p) $p.idempotencyKey.digest.activationRequiresNMinus1AliasIntersectionProof = $false } },
        @{ Name = 'rotation-requires-impossible-global-backfill'; Mutate = { param($p) $p.idempotencyKey.digest.historicalBackfillRequired = $true } },
        @{ Name = 'multiple-alias-ledgers-not-reconciled'; Mutate = { param($p) $p.idempotencyKey.digest.multipleAliasLedgerCollisionResult = 'pick_first' } },
        @{ Name = 'raw-payload-persisted'; Mutate = { param($p) $p.fingerprint.rawPayloadPersisted = $true } },
        @{ Name = 'non-canonical-fingerprint'; Mutate = { param($p) $p.fingerprint.canonicalization = 'raw-json' } },
        @{ Name = 'authorization-after-lookup'; Mutate = { param($p) $p.authorization.beforeLedgerLookup = $false } },
        @{ Name = 'authorization-not-rechecked-before-replay'; Mutate = { param($p) $p.authorization.beforeReplay = $false } },
        @{ Name = 'unauthorized-replay-leaks-existence'; Mutate = { param($p) $p.authorization.unauthorizedReplayRevealsExistence = $true } },
        @{ Name = 'missing-ledger-state'; Mutate = { param($p) $p.ledger.states = @('in_progress', 'succeeded', 'failed_terminal') } },
        @{ Name = 'fingerprint-mismatch-not-conflict'; Mutate = { param($p) $p.ledger.differentFingerprint.httpStatus = 200 } },
        @{ Name = 'inflight-not-conflict'; Mutate = { param($p) $p.ledger.inProgress.httpStatus = 202 } },
        @{ Name = 'expired-response-reexecutes'; Mutate = { param($p) $p.ledger.responseExpired.reexecuteBusinessEffect = $true } },
        @{ Name = 'journal-outside-atomic-unit'; Mutate = { param($p) $p.transaction.atomicParticipants = @('businessEffect', 'idempotencyLedger', 'outbox') } },
        @{ Name = 'blind-retry-after-commit-unknown'; Mutate = { param($p) $p.transaction.commitUnknown.blindRetry = $true } },
        @{ Name = 'expired-lease-assumed-rollback'; Mutate = { param($p) $p.concurrency.expiredLeaseProvesRollback = $true } },
        @{ Name = 'non-monotonic-fence'; Mutate = { param($p) $p.concurrency.fencing.token = 'random' } },
        @{ Name = 'stale-owner-can-write'; Mutate = { param($p) $p.concurrency.fencing.staleOwnerBusinessEffect = 'allowed' } },
        @{ Name = 'exactly-once-transport-claim'; Mutate = { param($p) $p.delivery.exactlyOnceTransport = $true } },
        @{ Name = 'wrong-inbox-identity'; Mutate = { param($p) $p.delivery.inboxUniqueBy = @('tenantId', 'eventId') } },
        @{ Name = 'global-order-claim'; Mutate = { param($p) $p.delivery.globalOrdering = $true } },
        @{ Name = 'unbounded-retry'; Mutate = { param($p) $p.delivery.retry.bounded = $false } },
        @{ Name = 'delivery-poison-reuses-ledger-terminal'; Mutate = { param($p) $p.delivery.retry.reusesLedgerFailedTerminal = $true } },
        @{ Name = 'invented-legal-days'; Mutate = { param($p) $p.retention.legalRetentionDays = 365 } },
        @{ Name = 'local-auto-purge-enabled'; Mutate = { param($p) $p.retention.localDevelopmentAutomaticPurge = $true } },
        @{ Name = 'false-production-retention-approval'; Mutate = { param($p) $p.retention.productionPolicyApproved = $true } },
        @{ Name = 'consumer-implementation-lost'; Mutate = { param($p) $p.firstConsumer.implemented = $false } },
        @{ Name = 'consumer-regressed-to-future'; Mutate = { param($p) $p.firstConsumer.status = 'future' } },
        @{ Name = 'wrong-first-consumer'; Mutate = { param($p) $p.firstConsumer.taskId = 'AGRO-CAT-001' } },
        @{ Name = 'spike-promotion'; Mutate = { param($p) $p.firstConsumer.spikePromotionAllowed = $true } },
        @{ Name = 'no-N-minus-1-coexistence'; Mutate = { param($p) $p.compatibility.coexistence = 'N-only' } },
        @{ Name = 'no-application-rollback'; Mutate = { param($p) $p.compatibility.applicationBinaryRollback = $false } }
    )

    $failed = [System.Collections.Generic.List[string]]::new()
    foreach ($case in $cases) {
        $candidate = Copy-Protocol $Protocol
        & $case.Mutate $candidate
        $errors = @(Test-Protocol -Protocol $candidate)
        $passed = $errors.Count -gt 0
        Write-Host ("SELFTEST {0}: {1}" -f $case.Name, $(if ($passed) { 'PASS' } else { 'FAIL' }))
        if (-not $passed) {
            $failed.Add([string] $case.Name)
        }
    }

    if ($failed.Count -gt 0) {
        throw "Mutation tests failed to detect invalid protocols: $($failed -join ', ')."
    }

    return $cases.Count
}

$protocol = Read-Protocol -Path $protocolPath
if ($SelfTest) {
    $mutationCount = Invoke-MutationTests -Protocol $protocol
    Write-Output "SELFTEST PASS: $mutationCount/$mutationCount mutations rejected."
}

$validationErrors = @(Test-Protocol -Protocol $protocol -CheckRepository)
if ($validationErrors.Count -gt 0) {
    foreach ($validationError in $validationErrors) {
        Write-Host "VALIDATION ERROR: $validationError"
    }
    throw "Foundation protocol validation failed with $($validationErrors.Count) error(s)."
}

Write-Output 'VALIDATION PASS: protocol 1.0.0; discriminated tenant|platform scope; 4 ledger states; runtimeImplemented=false; AGRO-ID-003/CreateOrganization integrated locally; parent remains En curso.'
