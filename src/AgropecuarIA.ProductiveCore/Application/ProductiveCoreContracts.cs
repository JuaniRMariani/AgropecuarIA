using Microsoft.AspNetCore.Http;

namespace AgropecuarIA.ProductiveCore.Application;

public sealed record CreateFieldCommand(Guid OrganizationId, string DisplayName, string IdempotencyKey);

public sealed record StartProductionCycleRequest(
    string CatalogCode,
    string CatalogDisplayName,
    string Purpose,
    string System,
    string SupportLevel,
    DateTimeOffset StartDateUtc);

public sealed record RecordProductionEventRequest(
    string EventType,
    DateTimeOffset EffectiveDateUtc,
    decimal? Quantity,
    string? Unit,
    string? Notes,
    string? Origin);

public sealed record RenameFieldDraftCommand(
    Guid OrganizationId,
    Guid FieldId,
    string DisplayName,
    Guid ExpectedVersion,
    string IdempotencyKey);

public sealed record ConfigureFieldGeometryCommand(
    Guid OrganizationId,
    Guid FieldId,
    string BoundaryGeoJson,
    decimal DeclaredAreaHectares,
    decimal CalculatedAreaHectares,
    double CentroidLatitude,
    double CentroidLongitude,
    string? OfficialProvinceCode,
    string? OfficialDepartmentCode,
    Guid ExpectedVersion);

public sealed record ConfigureFieldGeometryRequest(
    string BoundaryGeoJson,
    decimal DeclaredAreaHectares,
    decimal CalculatedAreaHectares,
    double CentroidLatitude,
    double CentroidLongitude,
    string? OfficialProvinceCode,
    string? OfficialDepartmentCode);

public sealed record ConfiguredFieldGeometryResult(
    Guid FieldId,
    Guid OrganizationId,
    string DisplayName,
    string Type,
    string Status,
    string SpatialStatus,
    decimal? DeclaredAreaHectares,
    decimal? CalculatedAreaHectares,
    double? CentroidLatitude,
    double? CentroidLongitude,
    string? BoundaryGeoJson,
    string? OfficialProvinceCode,
    string? OfficialDepartmentCode,
    DateTimeOffset CreatedAtUtc,
    long Revision,
    Guid Version);

public sealed record ManagementUnitResult(
    Guid FieldId,
    Guid OrganizationId,
    string DisplayName,
    string Type,
    string Status,
    string SpatialStatus,
    DateTimeOffset CreatedAtUtc,
    Guid Version);

public sealed record CreatedManagementUnitResult(
    Guid FieldId,
    Guid OrganizationId,
    string DisplayName,
    string Type,
    string Status,
    string SpatialStatus,
    DateTimeOffset CreatedAtUtc,
    Guid Version,
    bool IsReplay);

public sealed record RenamedManagementUnitResult(
    Guid FieldId,
    Guid OrganizationId,
    string DisplayName,
    string Type,
    string Status,
    string SpatialStatus,
    DateTimeOffset CreatedAtUtc,
    long Revision,
    Guid Version,
    bool IsReplay);

public sealed record ProductiveRequestContext
{
    public ProductiveRequestContext(
        string correlationId,
        Guid actorUserId,
        Guid sessionId,
        Guid organizationId)
    {
        if (string.IsNullOrWhiteSpace(correlationId) || correlationId.Length > 128)
        {
            throw new ArgumentException(
                "Correlation ID must contain between 1 and 128 characters.",
                nameof(correlationId));
        }

        if (actorUserId == Guid.Empty || sessionId == Guid.Empty || organizationId == Guid.Empty)
        {
            throw new ArgumentException("Actor, session, and organization IDs are required.");
        }

        CorrelationId = correlationId;
        ActorUserId = actorUserId;
        SessionId = sessionId;
        OrganizationId = organizationId;
    }

    public string CorrelationId { get; }

    public Guid ActorUserId { get; }

    public Guid SessionId { get; }

    public Guid OrganizationId { get; }
}

public sealed class ProductiveCoreOperationException : Exception
{
    public ProductiveCoreOperationException(
        string code,
        int statusCode,
        string title,
        bool retryable = false,
        int? retryAfterSeconds = null)
        : base(title)
    {
        Code = code;
        StatusCode = statusCode;
        Title = title;
        Retryable = retryable;
        RetryAfterSeconds = retryAfterSeconds;
    }

    public string Code { get; }

    public int StatusCode { get; }

    public string Title { get; }

    public bool Retryable { get; }

    public int? RetryAfterSeconds { get; }
}

public static class ProductiveCoreErrors
{
    public static ProductiveCoreOperationException InvalidFieldDisplayName() =>
        new(
            "productive_core.invalid_field_display_name",
            StatusCodes.Status400BadRequest,
            "The field display name is invalid.");

    public static ProductiveCoreOperationException InvalidIdempotencyKey() =>
        new(
            "productive_core.invalid_idempotency_key",
            StatusCodes.Status400BadRequest,
            "The idempotency key is invalid.");

    public static ProductiveCoreOperationException InvalidFieldVersion() =>
        new(
            "productive_core.invalid_field_version",
            StatusCodes.Status400BadRequest,
            "A valid strong field version is required.");

    public static ProductiveCoreOperationException FieldDisplayNameUnchanged() =>
        new(
            "productive_core.field_display_name_unchanged",
            StatusCodes.Status400BadRequest,
            "The field display name must change.");

    public static ProductiveCoreOperationException FieldNotAvailable() =>
        new(
            "productive_core.field_not_available",
            StatusCodes.Status404NotFound,
            "The field is not available.");

    public static ProductiveCoreOperationException ManagementUnitUnavailable() =>
        new(
            "productive_core.management_unit_unavailable",
            StatusCodes.Status503ServiceUnavailable,
            "Management units are unavailable.",
            retryable: true,
            retryAfterSeconds: 1);

    public static ProductiveCoreOperationException ManagementUnitCapacityReached() =>
        new(
            "productive_core.management_unit_capacity_reached",
            StatusCodes.Status409Conflict,
            "The organization has reached the current field capacity.");

    public static ProductiveCoreOperationException FieldVersionStale() =>
        new(
            "productive_core.field_version_stale",
            StatusCodes.Status412PreconditionFailed,
            "The field changed before this request was applied.");

    public static ProductiveCoreOperationException IdempotencyKeyReused() =>
        new(
            "idempotency.key_reused",
            StatusCodes.Status409Conflict,
            "The idempotency key is already bound to another request.");

    public static ProductiveCoreOperationException IdempotencyInProgress() =>
        new(
            "idempotency.in_progress",
            StatusCodes.Status409Conflict,
            "The idempotent operation is still in progress.",
            retryable: true,
            retryAfterSeconds: 1);

    public static ProductiveCoreOperationException IdempotencyFailedTerminal() =>
        new(
            "idempotency.failed_terminal",
            StatusCodes.Status409Conflict,
            "The idempotent operation failed terminally.");

    public static ProductiveCoreOperationException ReconciliationRequired() =>
        new(
            "idempotency.reconciliation_required",
            StatusCodes.Status503ServiceUnavailable,
            "The result could not be determined safely. Retry with the same idempotency key.",
            retryable: true,
            retryAfterSeconds: 1);
}
public sealed record ArchiveFieldDraftCommand(
    Guid OrganizationId,
    Guid FieldId,
    Guid ExpectedVersion,
    string IdempotencyKey);

public sealed record ArchivedManagementUnitResult(
    Guid FieldId,
    Guid OrganizationId,
    string DisplayName,
    string Type,
    string Status,
    string SpatialStatus,
    DateTimeOffset CreatedAtUtc,
    long Revision,
    Guid Version,
    bool IsReplay);
