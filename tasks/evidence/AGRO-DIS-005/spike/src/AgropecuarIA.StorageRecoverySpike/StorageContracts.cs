using System.Collections.ObjectModel;

namespace AgropecuarIA.StorageRecoverySpike;

public enum FileState
{
    PendingUpload,
    Uploaded,
    Scanning,
    Available,
    Quarantined,
    Rejected,
    ScanFailed,
    Purging,
    PurgeUncertain,
    Deleted,
}

public enum FileClassification
{
    Public,
    Internal,
    Confidential,
    FiscalPersonal,
    Secret,
}

public enum ScanVerdict
{
    Clean,
    Threat,
    Unsupported,
    AccessDenied,
    Failed,
}

public sealed record ActorContext(string TenantId, string ActorId);

public sealed record OperatorContext(string OperatorId);

public sealed record LinkedResource(string Type, Guid Id);

public sealed record UploadIntent(
    Guid FileId,
    int Version,
    string UploadToken,
    DateTimeOffset ExpiresAt,
    long MaxBytes,
    IReadOnlyCollection<string> AllowedMediaTypes);

public sealed record DownloadGrant(
    Guid FileId,
    int Version,
    string DownloadToken,
    DateTimeOffset ExpiresAt,
    string DisplayName,
    string MediaType);

public sealed record ScanResult(
    Guid FileId,
    int Version,
    long Sequence,
    string Sha256,
    ScanVerdict Verdict,
    string ScannerRef,
    DateTimeOffset ScannedAt);

public sealed record StoredFileSnapshot(
    Guid FileId,
    int Version,
    string TenantRef,
    string DisplayName,
    string DeclaredMediaType,
    string? DetectedMediaType,
    long SizeBytes,
    string? Sha256,
    FileState State,
    FileClassification Classification,
    LinkedResource LinkedResource,
    bool LegalHold,
    DateTimeOffset CreatedAt);

public sealed record AuditEvent(
    long Sequence,
    DateTimeOffset OccurredAt,
    string TenantRef,
    Guid FileId,
    string Action,
    string ActorRef);

public sealed record ReconciliationReport(
    IReadOnlyCollection<string> OrphanObjectKeys,
    IReadOnlyCollection<Guid> MissingFileIds,
    IReadOnlyCollection<Guid> PurgeUncertainFileIds)
{
    public static ReconciliationReport Create(
        IEnumerable<string> orphans,
        IEnumerable<Guid> missing,
        IEnumerable<Guid> purgeUncertain) =>
        new(new ReadOnlyCollection<string>(orphans.Order(StringComparer.Ordinal).ToArray()),
            new ReadOnlyCollection<Guid>(missing.Order().ToArray()),
            new ReadOnlyCollection<Guid>(purgeUncertain.Order().ToArray()));
}

public sealed class ResourceDeniedException : Exception
{
    public ResourceDeniedException() : base("The resource is unavailable for this operation.")
    {
    }
}

public sealed class StorageConflictException(string message) : Exception(message);

public sealed class ScannerUnavailableException(string message) : Exception(message);

public sealed class StorageValidationException(string code, string message) : Exception(message)
{
    public string Code { get; } = code;
}
