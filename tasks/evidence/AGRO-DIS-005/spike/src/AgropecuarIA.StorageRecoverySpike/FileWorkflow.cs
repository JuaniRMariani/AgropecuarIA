using System.Collections.ObjectModel;
using System.Security.Cryptography;
using System.Text;

namespace AgropecuarIA.StorageRecoverySpike;

public sealed class FileWorkflow
{
    public const long MaximumBytes = 10 * 1024 * 1024;
    public static readonly IReadOnlyCollection<string> AllowedMediaTypes =
        Array.AsReadOnly(["application/pdf", "image/jpeg", "image/png"]);

    private readonly IObjectStore objectStore;
    private readonly IClock clock;
    private readonly ISafeTelemetry telemetry;
    private readonly IResourceAuthorizer resourceAuthorizer;
    private readonly IOperationsAuthorizer operationsAuthorizer;
    private readonly TokenProtector tokenProtector;
    private readonly byte[] tenantReferenceSalt;
    private readonly Dictionary<Guid, FileRecord> files = [];
    private readonly Dictionary<Guid, UploadRegistration> uploadRegistrations = [];
    private readonly List<AuditEvent> auditEvents = [];
    private readonly object gate = new();

    public FileWorkflow(
        IObjectStore objectStore,
        IClock clock,
        ISafeTelemetry telemetry,
        IResourceAuthorizer resourceAuthorizer,
        IOperationsAuthorizer operationsAuthorizer,
        ReadOnlySpan<byte> signingSecret,
        ReadOnlySpan<byte> tenantReferenceSalt)
    {
        this.objectStore = objectStore;
        this.clock = clock;
        this.telemetry = telemetry;
        this.resourceAuthorizer = resourceAuthorizer;
        this.operationsAuthorizer = operationsAuthorizer;
        tokenProtector = new TokenProtector(signingSecret);
        if (tenantReferenceSalt.Length < 16)
        {
            throw new ArgumentException("Tenant reference salt must be at least 128 bits.", nameof(tenantReferenceSalt));
        }

        this.tenantReferenceSalt = tenantReferenceSalt.ToArray();
    }

    public IReadOnlyList<AuditEvent> GetAuditEvents(OperatorContext operatorContext)
    {
        AuthorizeOperator(operatorContext, "audit_read");
        lock (gate)
        {
            return new ReadOnlyCollection<AuditEvent>(auditEvents.ToArray());
        }
    }

    public UploadIntent CreateUploadIntent(
        ActorContext actor,
        string displayName,
        string declaredMediaType,
        FileClassification classification,
        LinkedResource linkedResource,
        TimeSpan? lifetime = null)
    {
        ValidateActor(actor);
        ArgumentNullException.ThrowIfNull(linkedResource);
        if (string.IsNullOrWhiteSpace(linkedResource.Type) || linkedResource.Id == Guid.Empty)
        {
            throw new StorageValidationException("invalid_resource", "A valid linked resource is required.");
        }

        if (!Enum.IsDefined(classification))
        {
            throw new StorageValidationException("invalid_classification", "The file classification is invalid.");
        }

        if (classification == FileClassification.Secret)
        {
            throw new StorageValidationException("secret_not_allowed", "Secrets cannot be stored as file objects.");
        }

        if (!resourceAuthorizer.IsAllowed(actor, linkedResource, "upload"))
        {
            throw new ResourceDeniedException();
        }

        var safeDisplayName = ValidateDisplayName(displayName);
        if (!AllowedMediaTypes.Contains(declaredMediaType, StringComparer.Ordinal))
        {
            throw new StorageValidationException("media_type_not_allowed", "The declared media type is not allowed.");
        }

        var ttl = lifetime ?? TimeSpan.FromMinutes(10);
        if (ttl <= TimeSpan.Zero || ttl > TimeSpan.FromMinutes(15))
        {
            throw new StorageValidationException("invalid_expiry", "The upload intent lifetime must be positive and at most 15 minutes.");
        }

        var fileId = Guid.NewGuid();
        const int version = 1;
        var tenantRef = DeriveTenantRef(actor.TenantId);
        var expiresAt = clock.UtcNow.Add(ttl);
        var payload = NewPayload("upload", fileId, version, tenantRef, expiresAt);
        var token = tokenProtector.Protect(payload);
        var record = new FileRecord(
            fileId,
            version,
            tenantRef,
            BuildObjectKey(tenantRef, fileId, version),
            safeDisplayName,
            declaredMediaType,
            classification,
            linkedResource,
            clock.UtcNow);
        lock (gate)
        {
            files.Add(fileId, record);
            uploadRegistrations.Add(fileId, new UploadRegistration(TokenProtector.HashToken(token)));
            AppendAudit(record, actor, "upload_intent_created");
        }
        telemetry.Record("storage.upload_intent.created", tenantRef, fileId);

        return new UploadIntent(fileId, version, token, expiresAt, MaximumBytes, AllowedMediaTypes);
    }

    public async Task<StoredFileSnapshot> CompleteUploadAsync(
        ActorContext actor,
        string uploadToken,
        ReadOnlyMemory<byte> content,
        CancellationToken cancellationToken = default)
    {
        var (payload, record) = AuthorizeToken(actor, uploadToken, "upload", "upload");
        UploadRegistration registration;
        lock (gate)
        {
            if (!uploadRegistrations.TryGetValue(record.FileId, out registration!)
                || registration.Consumed
                || !CryptographicOperations.FixedTimeEquals(
                    Convert.FromHexString(registration.TokenHash),
                    Convert.FromHexString(TokenProtector.HashToken(uploadToken))))
            {
                throw new ResourceDeniedException();
            }
        }

        if (payload.ExpiresUnixSeconds <= clock.UtcNow.ToUnixTimeSeconds())
        {
            throw new ResourceDeniedException();
        }

        if (content.IsEmpty || content.Length > MaximumBytes)
        {
            throw new StorageValidationException("invalid_size", $"The object must contain between 1 and {MaximumBytes} bytes.");
        }

        var detectedMediaType = DetectMediaType(content.Span);
        if (detectedMediaType is null || !string.Equals(detectedMediaType, record.DeclaredMediaType, StringComparison.Ordinal))
        {
            throw new StorageValidationException("media_type_mismatch", "The declared media type does not match the file signature.");
        }

        var hash = Convert.ToHexStringLower(SHA256.HashData(content.Span));
        try
        {
            await objectStore.CreateAsync(record.ObjectKey, content, cancellationToken);
        }
        catch (IOException)
        {
            throw new StorageConflictException("The immutable object version already exists.");
        }

        lock (gate)
        {
            registration.Consumed = true;
            record.MarkUploaded(detectedMediaType, content.Length, hash);
            AppendAudit(record, actor, "upload_completed_quarantined");
        }
        telemetry.Record("storage.upload.completed", record.TenantRef, record.FileId, ("bytes", content.Length));
        return record.Snapshot();
    }

    public async Task<ScanResult> ScanAsync(
        ActorContext actor,
        Guid fileId,
        IMalwareScanner scanner,
        string scannerRef,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(scanner);
        var validatedScannerRef = ValidateScannerRef(scannerRef);
        cancellationToken.ThrowIfCancellationRequested();
        BeginScan(actor, fileId);
        var record = AuthorizeRecord(actor, fileId, "scan");
        var sha256 = record.Sha256 ?? throw new StorageConflictException("The immutable object hash is missing.");

        ScanVerdict verdict;
        try
        {
            var content = await objectStore.ReadAsync(record.ObjectKey, cancellationToken);
            verdict = await scanner.ScanAsync(content, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            ApplyScanResult(actor, CreateScanResult(record, sha256, ScanVerdict.Failed, validatedScannerRef));
            throw;
        }
        catch (IOException)
        {
            verdict = ScanVerdict.Failed;
        }
        catch (ScannerUnavailableException)
        {
            verdict = ScanVerdict.Failed;
        }
        catch
        {
            ApplyScanResult(actor, CreateScanResult(record, sha256, ScanVerdict.Failed, validatedScannerRef));
            throw;
        }

        if (!Enum.IsDefined(verdict))
        {
            verdict = ScanVerdict.Failed;
        }

        var result = CreateScanResult(record, sha256, verdict, validatedScannerRef);
        ApplyScanResult(actor, result);
        return result;
    }

    public StoredFileSnapshot BeginScan(ActorContext actor, Guid fileId)
    {
        var record = AuthorizeRecord(actor, fileId, "scan");
        StoredFileSnapshot snapshot;
        lock (gate)
        {
            record.BeginScan();
            AppendAudit(record, actor, "scan_started");
            snapshot = record.Snapshot();
        }
        telemetry.Record("storage.scan.started", record.TenantRef, record.FileId);
        return snapshot;
    }

    public StoredFileSnapshot ApplyScanResult(ActorContext actor, ScanResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        ValidateScannerRef(result.ScannerRef);
        if (result.Sequence < 1)
        {
            throw new StorageValidationException("invalid_scan_result", "The scan result is invalid.");
        }

        if (!Enum.IsDefined(result.Verdict))
        {
            result = result with { Verdict = ScanVerdict.Failed };
        }

        var record = AuthorizeRecord(actor, result.FileId, "scan");
        StoredFileSnapshot snapshot;
        lock (gate)
        {
            if (result.Version != record.Version
                || record.Sha256 is null
                || !string.Equals(result.Sha256, record.Sha256, StringComparison.Ordinal))
            {
                throw new StorageConflictException("The scan result does not match the immutable object version and hash.");
            }

            if (record.TryGetScan(result.Sequence, out var previous))
            {
                if (previous == result)
                {
                    return record.Snapshot();
                }

                throw new StorageConflictException("A different result already exists for this scan sequence.");
            }

            if (record.State != FileState.Scanning || result.Sequence != record.LastScanSequence + 1)
            {
                throw new StorageConflictException("The scan result is out of order or the scan is already terminal.");
            }

            record.ApplyScan(result);
            AppendAudit(record, actor, $"scan_{result.Verdict.ToString().ToLowerInvariant()}");
            snapshot = record.Snapshot();
        }

        telemetry.Record(
            "storage.scan.completed",
            record.TenantRef,
            record.FileId,
            ("sequence", result.Sequence),
            ("verdict", result.Verdict.ToString().ToLowerInvariant()));
        return snapshot;
    }

    public DownloadGrant CreateDownloadGrant(
        ActorContext actor,
        Guid fileId,
        TimeSpan? lifetime = null)
    {
        var record = AuthorizeRecord(actor, fileId, "download");
        var ttl = lifetime ?? TimeSpan.FromMinutes(2);
        if (ttl <= TimeSpan.Zero || ttl > TimeSpan.FromMinutes(5))
        {
            throw new StorageValidationException("invalid_expiry", "The download grant lifetime must be positive and at most 5 minutes.");
        }

        DownloadGrant grant;
        lock (gate)
        {
            if (record.State != FileState.Available || record.DetectedMediaType is null)
            {
                throw new ResourceDeniedException();
            }

            var expiresAt = clock.UtcNow.Add(ttl);
            var token = tokenProtector.Protect(NewPayload("download", fileId, record.Version, record.TenantRef, expiresAt));
            AppendAudit(record, actor, "download_grant_created");
            grant = new DownloadGrant(record.FileId, record.Version, token, expiresAt, record.DisplayName, record.DetectedMediaType);
        }
        telemetry.Record("storage.download_grant.created", record.TenantRef, record.FileId);
        return grant;
    }

    public async Task<byte[]> DownloadAsync(
        ActorContext actor,
        string downloadToken,
        CancellationToken cancellationToken = default)
    {
        var (payload, record) = AuthorizeToken(actor, downloadToken, "download", "download");
        lock (gate)
        {
            if (payload.ExpiresUnixSeconds <= clock.UtcNow.ToUnixTimeSeconds())
            {
                throw new ResourceDeniedException();
            }

            record.BeginDownload();
        }

        try
        {
            byte[] content;
            try
            {
                content = await objectStore.ReadAsync(record.ObjectKey, cancellationToken);
            }
            catch (IOException)
            {
                telemetry.Record("storage.download.read_failed", record.TenantRef, record.FileId);
                throw new ResourceDeniedException();
            }
            catch (UnauthorizedAccessException)
            {
                telemetry.Record("storage.download.read_failed", record.TenantRef, record.FileId);
                throw new ResourceDeniedException();
            }

            var actualHash = Convert.ToHexStringLower(SHA256.HashData(content));
            if (!string.Equals(actualHash, record.Sha256, StringComparison.Ordinal))
            {
                telemetry.Record("storage.download.integrity_failed", record.TenantRef, record.FileId);
                throw new ResourceDeniedException();
            }

            AppendAudit(record, actor, "download_completed");
            telemetry.Record("storage.download.completed", record.TenantRef, record.FileId, ("bytes", content.Length));
            return content;
        }
        finally
        {
            lock (gate)
            {
                record.EndDownload();
            }
        }
    }

    public StoredFileSnapshot SetLegalHold(ActorContext actor, Guid fileId, bool enabled)
    {
        var action = enabled ? "legal_hold_apply" : "legal_hold_release";
        var record = AuthorizeRecord(actor, fileId, action);
        StoredFileSnapshot snapshot;
        lock (gate)
        {
            record.SetLegalHold(enabled);
            AppendAudit(record, actor, enabled ? "legal_hold_applied" : "legal_hold_released");
            snapshot = record.Snapshot();
        }
        telemetry.Record("storage.legal_hold.changed", record.TenantRef, record.FileId, ("enabled", enabled));
        return snapshot;
    }

    public async Task PurgeAsync(ActorContext actor, Guid fileId, CancellationToken cancellationToken = default)
    {
        var record = AuthorizeRecord(actor, fileId, "purge");
        lock (gate)
        {
            if (!record.BeginPurge())
            {
                return;
            }

            AppendAudit(record, actor, "purge_started");
        }

        try
        {
            await objectStore.DeleteAsync(record.ObjectKey, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            MarkPurgeUncertain(record, actor);
            throw;
        }
        catch (IOException)
        {
            MarkPurgeUncertain(record, actor);
            throw new StorageConflictException("The object could not be purged.");
        }
        catch (Exception)
        {
            MarkPurgeUncertain(record, actor);
            throw new StorageConflictException("The object could not be purged.");
        }

        lock (gate)
        {
            record.CompletePurge();
            AppendAudit(record, actor, "file_purged");
        }
        telemetry.Record("storage.file.purged", record.TenantRef, record.FileId);
    }

    public StoredFileSnapshot Get(ActorContext actor, Guid fileId)
    {
        var record = AuthorizeRecord(actor, fileId, "read");
        lock (gate)
        {
            return record.Snapshot();
        }
    }

    public async Task<ReconciliationReport> ReconcileAsync(
        OperatorContext operatorContext,
        CancellationToken cancellationToken = default)
    {
        AuthorizeOperator(operatorContext, "reconcile");
        Dictionary<string, FileRecord> knownObjects;
        lock (gate)
        {
            knownObjects = files.Values
                .Where(static file => file.State != FileState.PendingUpload && file.State != FileState.Deleted)
                .ToDictionary(static file => file.ObjectKey, StringComparer.Ordinal);
        }
        var orphanKeys = new List<string>();
        await foreach (var key in objectStore.ListKeysAsync(cancellationToken))
        {
            if (!knownObjects.Remove(key))
            {
                orphanKeys.Add(key);
            }
        }

        IReadOnlyCollection<Guid> uncertain;
        lock (gate)
        {
            uncertain = files.Values
                .Where(static file => file.State == FileState.PurgeUncertain)
                .Select(static file => file.FileId)
                .ToArray();
        }

        return ReconciliationReport.Create(
            orphanKeys,
            knownObjects.Values.Select(static file => file.FileId),
            uncertain);
    }

    public async Task<StoredFileSnapshot> ResolvePurgeUncertainAsync(
        OperatorContext operatorContext,
        Guid fileId,
        CancellationToken cancellationToken = default)
    {
        AuthorizeOperator(operatorContext, "reconcile");
        FileRecord record;
        lock (gate)
        {
            if (!files.TryGetValue(fileId, out record!) || record.State != FileState.PurgeUncertain)
            {
                throw new ResourceDeniedException();
            }
        }

        bool objectExists;
        try
        {
            objectExists = await objectStore.ExistsAsync(record.ObjectKey, cancellationToken);
        }
        catch (IOException)
        {
            throw new StorageConflictException("The uncertain purge could not be reconciled.");
        }
        catch (UnauthorizedAccessException)
        {
            throw new StorageConflictException("The uncertain purge could not be reconciled.");
        }

        StoredFileSnapshot snapshot;
        lock (gate)
        {
            record.ResolvePurgeUncertain(objectExists);
            AppendOperatorAudit(record, operatorContext, objectExists ? "purge_reconciled_present" : "purge_reconciled_deleted");
            snapshot = record.Snapshot();
        }
        telemetry.Record("storage.file.purge_reconciled", record.TenantRef, record.FileId, ("object_exists", objectExists));
        return snapshot;
    }

    public static string DetectMediaType(ReadOnlySpan<byte> content)
    {
        if (content.StartsWith("%PDF-"u8))
        {
            return "application/pdf";
        }

        if (content.Length >= 3 && content[0] == 0xFF && content[1] == 0xD8 && content[2] == 0xFF)
        {
            return "image/jpeg";
        }

        if (content.Length >= 8
            && content[0] == 0x89
            && content[1] == 0x50
            && content[2] == 0x4E
            && content[3] == 0x47
            && content[4] == 0x0D
            && content[5] == 0x0A
            && content[6] == 0x1A
            && content[7] == 0x0A)
        {
            return "image/png";
        }

        return string.Empty;
    }

    private (GrantPayload Payload, FileRecord Record) AuthorizeToken(ActorContext actor, string token, string purpose, string action)
    {
        ValidateActor(actor);
        lock (gate)
        {
            if (!tokenProtector.TryUnprotect(token, out var payload)
                || payload is null
                || !string.Equals(payload.Purpose, purpose, StringComparison.Ordinal)
                || !files.TryGetValue(payload.FileId, out var record)
                || payload.Version != record.Version
                || !string.Equals(payload.TenantRef, record.TenantRef, StringComparison.Ordinal)
                || !string.Equals(DeriveTenantRef(actor.TenantId), record.TenantRef, StringComparison.Ordinal)
                || !resourceAuthorizer.IsAllowed(actor, record.LinkedResource, action))
            {
                throw new ResourceDeniedException();
            }

            return (payload, record);
        }
    }

    private FileRecord AuthorizeRecord(ActorContext actor, Guid fileId, string action)
    {
        ValidateActor(actor);
        lock (gate)
        {
            if (!files.TryGetValue(fileId, out var record)
                || !string.Equals(DeriveTenantRef(actor.TenantId), record.TenantRef, StringComparison.Ordinal)
                || !resourceAuthorizer.IsAllowed(actor, record.LinkedResource, action))
            {
                throw new ResourceDeniedException();
            }

            return record;
        }
    }

    private void AuthorizeOperator(OperatorContext operatorContext, string scope)
    {
        ArgumentNullException.ThrowIfNull(operatorContext);
        ArgumentException.ThrowIfNullOrWhiteSpace(operatorContext.OperatorId);
        if (!operationsAuthorizer.IsAllowed(operatorContext, scope))
        {
            throw new ResourceDeniedException();
        }
    }

    private ScanResult CreateScanResult(FileRecord record, string sha256, ScanVerdict verdict, string scannerRef)
    {
        lock (gate)
        {
            return new ScanResult(
                record.FileId,
                record.Version,
                record.LastScanSequence + 1,
                sha256,
                verdict,
                scannerRef,
                clock.UtcNow);
        }
    }

    private void MarkPurgeUncertain(FileRecord record, ActorContext actor)
    {
        lock (gate)
        {
            record.MarkPurgeUncertain();
            AppendAudit(record, actor, "purge_uncertain");
        }
        telemetry.Record("storage.file.purge_uncertain", record.TenantRef, record.FileId);
    }

    private string DeriveTenantRef(string tenantId)
    {
        var tenantBytes = Encoding.UTF8.GetBytes(tenantId);
        var material = new byte[tenantReferenceSalt.Length + tenantBytes.Length];
        tenantReferenceSalt.CopyTo(material, 0);
        tenantBytes.CopyTo(material, tenantReferenceSalt.Length);
        return Convert.ToHexStringLower(SHA256.HashData(material))[..16];
    }

    private static string BuildObjectKey(string tenantRef, Guid fileId, int version) =>
        $"tenants/{tenantRef}/quarantine/{fileId:N}/v{version}";

    private static GrantPayload NewPayload(string purpose, Guid fileId, int version, string tenantRef, DateTimeOffset expiresAt) =>
        new(purpose, fileId, version, tenantRef, expiresAt.ToUnixTimeSeconds(), Convert.ToHexStringLower(RandomNumberGenerator.GetBytes(16)));

    private void AppendAudit(FileRecord record, ActorContext actor, string action)
    {
        var actorRef = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(actor.ActorId)))[..16];
        lock (gate)
        {
            auditEvents.Add(new AuditEvent(auditEvents.Count + 1, clock.UtcNow, record.TenantRef, record.FileId, action, actorRef));
        }
    }

    private void AppendOperatorAudit(FileRecord record, OperatorContext operatorContext, string action)
    {
        var operatorRef = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(operatorContext.OperatorId)))[..16];
        lock (gate)
        {
            auditEvents.Add(new AuditEvent(auditEvents.Count + 1, clock.UtcNow, record.TenantRef, record.FileId, action, operatorRef));
        }
    }

    private static void ValidateActor(ActorContext actor)
    {
        ArgumentNullException.ThrowIfNull(actor);
        ArgumentException.ThrowIfNullOrWhiteSpace(actor.TenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(actor.ActorId);
    }

    private static string ValidateDisplayName(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        var name = Path.GetFileName(value.Trim());
        if (name.Length is < 1 or > 180 || name.Any(char.IsControl))
        {
            throw new StorageValidationException("invalid_name", "The display name is invalid.");
        }

        return name;
    }

    private static string ValidateScannerRef(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        if (value.Length > 80)
        {
            throw new StorageValidationException("invalid_scanner", "The scanner reference is too long.");
        }

        return value;
    }

    private sealed class UploadRegistration(string tokenHash)
    {
        public string TokenHash { get; } = tokenHash;

        public bool Consumed { get; set; }
    }

    private sealed class FileRecord(
        Guid fileId,
        int version,
        string tenantRef,
        string objectKey,
        string displayName,
        string declaredMediaType,
        FileClassification classification,
        LinkedResource linkedResource,
        DateTimeOffset createdAt)
    {
        private readonly Dictionary<long, ScanResult> scans = [];
        private FileState? stateBeforePurge;
        private int activeDownloads;

        public Guid FileId { get; } = fileId;
        public int Version { get; } = version;
        public string TenantRef { get; } = tenantRef;
        public string ObjectKey { get; } = objectKey;
        public string DisplayName { get; } = displayName;
        public string DeclaredMediaType { get; } = declaredMediaType;
        public string? DetectedMediaType { get; private set; }
        public long SizeBytes { get; private set; }
        public string? Sha256 { get; private set; }
        public FileState State { get; private set; } = FileState.PendingUpload;
        public FileClassification Classification { get; } = classification;
        public LinkedResource LinkedResource { get; } = linkedResource;
        public bool LegalHold { get; private set; }
        public DateTimeOffset CreatedAt { get; } = createdAt;
        public long LastScanSequence { get; private set; }

        public void MarkUploaded(string detectedMediaType, long sizeBytes, string sha256)
        {
            if (State != FileState.PendingUpload)
            {
                throw new StorageConflictException("The upload is already complete.");
            }

            DetectedMediaType = detectedMediaType;
            SizeBytes = sizeBytes;
            Sha256 = sha256;
            State = FileState.Uploaded;
        }

        public void BeginScan()
        {
            if ((State != FileState.Uploaded && State != FileState.ScanFailed) || Sha256 is null)
            {
                throw new StorageConflictException("The file is not ready to begin scanning.");
            }

            State = FileState.Scanning;
        }

        public bool TryGetScan(long sequence, out ScanResult result) => scans.TryGetValue(sequence, out result!);

        public void ApplyScan(ScanResult result)
        {
            scans.Add(result.Sequence, result);
            LastScanSequence = result.Sequence;
            State = result.Verdict switch
            {
                ScanVerdict.Clean => FileState.Available,
                ScanVerdict.Threat => FileState.Quarantined,
                ScanVerdict.Unsupported => FileState.Rejected,
                ScanVerdict.AccessDenied or ScanVerdict.Failed => FileState.ScanFailed,
                _ => throw new ArgumentOutOfRangeException(nameof(result)),
            };
        }

        public void SetLegalHold(bool enabled)
        {
            if (State is FileState.Purging or FileState.PurgeUncertain or FileState.Deleted)
            {
                throw new StorageConflictException("Legal hold cannot change while purge state is unresolved or deleted.");
            }

            LegalHold = enabled;
        }

        public void BeginDownload()
        {
            if (State != FileState.Available)
            {
                throw new ResourceDeniedException();
            }

            checked
            {
                activeDownloads++;
            }
        }

        public void EndDownload()
        {
            if (activeDownloads <= 0)
            {
                throw new StorageConflictException("No download lease is active.");
            }

            activeDownloads--;
        }

        public bool BeginPurge()
        {
            if (LegalHold)
            {
                throw new StorageConflictException("Legal hold prevents purge.");
            }

            if (State == FileState.Deleted)
            {
                return false;
            }

            if (State is not (FileState.Uploaded
                or FileState.Available
                or FileState.Quarantined
                or FileState.Rejected
                or FileState.ScanFailed))
            {
                throw new StorageConflictException("The file cannot be purged from its current state.");
            }

            if (activeDownloads > 0)
            {
                throw new StorageConflictException("The file has an active download.");
            }

            stateBeforePurge = State;
            State = FileState.Purging;
            return true;
        }

        public void MarkPurgeUncertain()
        {
            if (State != FileState.Purging || stateBeforePurge is null)
            {
                throw new StorageConflictException("No purge transition is active.");
            }

            State = FileState.PurgeUncertain;
        }

        public void ResolvePurgeUncertain(bool objectExists)
        {
            if (State != FileState.PurgeUncertain || stateBeforePurge is null)
            {
                throw new StorageConflictException("No uncertain purge is active.");
            }

            State = objectExists ? stateBeforePurge.Value : FileState.Deleted;
            stateBeforePurge = null;
        }

        public void CompletePurge()
        {
            if (State != FileState.Purging)
            {
                throw new StorageConflictException("No purge transition is active.");
            }

            State = FileState.Deleted;
            stateBeforePurge = null;
        }

        public StoredFileSnapshot Snapshot() =>
            new(FileId, Version, TenantRef, DisplayName, DeclaredMediaType, DetectedMediaType, SizeBytes,
                Sha256, State, Classification, LinkedResource, LegalHold, CreatedAt);
    }
}
