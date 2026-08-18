using System.Text.Json;
using System.Text.Json.Serialization;

namespace AgropecuarIA.ProductiveCore.Domain;

public sealed class ProductiveCoreIntegrationEventDefinition
{
    internal ProductiveCoreIntegrationEventDefinition(
        string type,
        int majorVersion,
        string schemaVersion,
        string source,
        string scope,
        string aggregateType,
        string payloadSchemaPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(type);
        ArgumentException.ThrowIfNullOrWhiteSpace(schemaVersion);
        ArgumentException.ThrowIfNullOrWhiteSpace(source);
        ArgumentException.ThrowIfNullOrWhiteSpace(scope);
        ArgumentException.ThrowIfNullOrWhiteSpace(aggregateType);
        ArgumentException.ThrowIfNullOrWhiteSpace(payloadSchemaPath);
        if (majorVersion <= 0 ||
            !Version.TryParse(schemaVersion, out Version? parsedVersion) ||
            parsedVersion.Major != majorVersion)
        {
            throw new ArgumentException("The event version is invalid.", nameof(schemaVersion));
        }

        if (scope is not "tenant")
        {
            throw new ArgumentException("Productive Core events must be tenant scoped.", nameof(scope));
        }

        if (Path.IsPathRooted(payloadSchemaPath) || payloadSchemaPath.Contains('\\'))
        {
            throw new ArgumentException(
                "The payload schema path must be repository-relative and use forward slashes.",
                nameof(payloadSchemaPath));
        }

        Type = type;
        MajorVersion = majorVersion;
        SchemaVersion = schemaVersion;
        Source = source;
        Scope = scope;
        AggregateType = aggregateType;
        PayloadSchemaPath = payloadSchemaPath;
    }

    public string Type { get; }

    public int MajorVersion { get; }

    public string SchemaVersion { get; }

    public string Source { get; }

    public string Scope { get; }

    public string AggregateType { get; }

    public string PayloadSchemaPath { get; }
}

public static class ProductiveCoreIntegrationEvents
{
    public static ProductiveCoreIntegrationEventDefinition ManagementUnitCreated { get; } = new(
        "ManagementUnitCreated",
        1,
        "1.0.0",
        "productive-core",
        "tenant",
        nameof(ManagementUnit),
        "tasks/evidence/AGRO-FND-001/contracts/management-unit-created.v1.schema.json");

    public static ProductiveCoreIntegrationEventDefinition ManagementUnitDisplayNameChanged { get; } = new(
        "ManagementUnitDisplayNameChanged",
        1,
        "1.0.0",
        "productive-core",
        "tenant",
        nameof(ManagementUnit),
        "tasks/evidence/AGRO-FND-001/contracts/management-unit-display-name-changed.v1.schema.json");

    public static IReadOnlyList<ProductiveCoreIntegrationEventDefinition> All { get; } =
        Array.AsReadOnly(new[] { ManagementUnitCreated, ManagementUnitDisplayNameChanged });
}

public sealed class ProductiveJournalEntry
{
    private ProductiveJournalEntry()
    {
    }

    public ProductiveJournalEntry(
        Guid id,
        Guid organizationId,
        Guid actorUserId,
        Guid sessionId,
        string correlationId,
        DateTimeOffset occurredAtUtc)
        : this(
            id,
            organizationId,
            actorUserId,
            sessionId,
            "management_unit_created",
            correlationId,
            occurredAtUtc)
    {
    }

    private ProductiveJournalEntry(
        Guid id,
        Guid organizationId,
        Guid actorUserId,
        Guid sessionId,
        string action,
        string correlationId,
        DateTimeOffset occurredAtUtc)
    {
        if (id == Guid.Empty || organizationId == Guid.Empty || actorUserId == Guid.Empty || sessionId == Guid.Empty)
        {
            throw new ArgumentException("Journal, tenant, actor, and session IDs are required.");
        }

        if (action is not ("management_unit_created" or "management_unit_display_name_changed"))
        {
            throw new ArgumentException("The Productive Core journal action is invalid.", nameof(action));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(correlationId);
        if (correlationId.Length > 128)
        {
            throw new ArgumentException("Correlation ID cannot exceed 128 characters.", nameof(correlationId));
        }

        Id = id;
        OrganizationId = organizationId;
        ActorUserId = actorUserId;
        SessionId = sessionId;
        Action = action;
        Outcome = "succeeded";
        CorrelationId = correlationId;
        OccurredAtUtc = occurredAtUtc;
    }

    public static ProductiveJournalEntry CreateManagementUnitDisplayNameChanged(
        Guid id,
        Guid organizationId,
        Guid actorUserId,
        Guid sessionId,
        string correlationId,
        DateTimeOffset occurredAtUtc) =>
        new(
            id,
            organizationId,
            actorUserId,
            sessionId,
            "management_unit_display_name_changed",
            correlationId,
            occurredAtUtc);

    public Guid Id { get; private set; }

    public Guid OrganizationId { get; private set; }

    public Guid ActorUserId { get; private set; }

    public Guid SessionId { get; private set; }

    public string Action { get; private set; } = string.Empty;

    public string Outcome { get; private set; } = string.Empty;

    public string CorrelationId { get; private set; } = string.Empty;

    public DateTimeOffset OccurredAtUtc { get; private set; }
}

public sealed record ManagementUnitCreatedIntegrationEventPayload(
    Guid OrganizationId,
    Guid ManagementUnitId,
    string UnitType,
    string Status,
    DateTimeOffset CreatedAtUtc);

public sealed record ManagementUnitDisplayNameChangedIntegrationEventPayload(
    Guid OrganizationId,
    Guid ManagementUnitId,
    long Revision,
    DateTimeOffset ChangedAtUtc);

public sealed class ProductiveOutboxMessage
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private ProductiveOutboxMessage()
    {
    }

    private ProductiveOutboxMessage(
        Guid id,
        Guid organizationId,
        Guid aggregateId,
        ProductiveCoreIntegrationEventDefinition definition,
        long aggregateVersion,
        string correlationId,
        DateTimeOffset occurredAtUtc,
        string payloadJson)
    {
        Id = id;
        OrganizationId = organizationId;
        EventType = definition.Type;
        SchemaVersion = definition.SchemaVersion;
        Source = definition.Source;
        Scope = definition.Scope;
        AggregateType = definition.AggregateType;
        AggregateId = aggregateId;
        AggregateVersion = aggregateVersion;
        CorrelationId = correlationId;
        OccurredAtUtc = occurredAtUtc;
        AvailableAtUtc = occurredAtUtc;
        PayloadJson = payloadJson;
    }

    public Guid Id { get; private set; }

    public Guid OrganizationId { get; private set; }

    public string EventType { get; private set; } = string.Empty;

    public string SchemaVersion { get; private set; } = string.Empty;

    public string Source { get; private set; } = string.Empty;

    public string Scope { get; private set; } = string.Empty;

    public string AggregateType { get; private set; } = string.Empty;

    public Guid AggregateId { get; private set; }

    public long AggregateVersion { get; private set; }

    public string CorrelationId { get; private set; } = string.Empty;

    public DateTimeOffset OccurredAtUtc { get; private set; }

    public DateTimeOffset AvailableAtUtc { get; private set; }

    public string PayloadJson { get; private set; } = string.Empty;

    public static ProductiveOutboxMessage CreateManagementUnitCreated(
        Guid id,
        string correlationId,
        ManagementUnitCreatedIntegrationEventPayload payload)
    {
        ArgumentNullException.ThrowIfNull(payload);
        if (id == Guid.Empty || payload.OrganizationId == Guid.Empty || payload.ManagementUnitId == Guid.Empty)
        {
            throw new ArgumentException("Event, organization, and management unit IDs are required.");
        }

        if (payload.UnitType != ManagementUnitTypes.Field || payload.Status != ManagementUnitStatuses.Draft)
        {
            throw new ArgumentException("The management unit event has an unsupported type or status.", nameof(payload));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(correlationId);
        return new ProductiveOutboxMessage(
            id,
            payload.OrganizationId,
            payload.ManagementUnitId,
            ProductiveCoreIntegrationEvents.ManagementUnitCreated,
            1,
            correlationId,
            payload.CreatedAtUtc,
            JsonSerializer.Serialize(payload, SerializerOptions));
    }

    public static ProductiveOutboxMessage CreateManagementUnitDisplayNameChanged(
        Guid id,
        string correlationId,
        ManagementUnitDisplayNameChangedIntegrationEventPayload payload)
    {
        ArgumentNullException.ThrowIfNull(payload);
        if (id == Guid.Empty || payload.OrganizationId == Guid.Empty ||
            payload.ManagementUnitId == Guid.Empty || payload.Revision < 2)
        {
            throw new ArgumentException(
                "Event, organization, management unit, and revision are required.");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(correlationId);
        return new ProductiveOutboxMessage(
            id,
            payload.OrganizationId,
            payload.ManagementUnitId,
            ProductiveCoreIntegrationEvents.ManagementUnitDisplayNameChanged,
            payload.Revision,
            correlationId,
            payload.ChangedAtUtc,
            JsonSerializer.Serialize(payload, SerializerOptions));
    }
}
