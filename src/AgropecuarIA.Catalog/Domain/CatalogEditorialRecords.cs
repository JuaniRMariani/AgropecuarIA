using System.Text.Json;

namespace AgropecuarIA.Catalog.Domain;

public sealed record CatalogIntegrationEventDefinition(string Type, int MajorVersion, string SchemaVersion,
    string Source, string Scope, string AggregateType, string PayloadSchemaPath);

public static class CatalogIntegrationEvents
{
    public static CatalogIntegrationEventDefinition ProductCatalogPublished { get; } = new(
        "ProductCatalogPublished", 1, "1.0.0", "national-catalog", "platform", "NationalCatalogRelease",
        "tasks/evidence/AGRO-FND-001/contracts/product-catalog-published.v1.schema.json");
    public static CatalogIntegrationEventDefinition ProductCatalogRolledBack { get; } = new(
        "ProductCatalogRolledBack", 1, "1.0.0", "national-catalog", "platform", "NationalCatalogRelease",
        "tasks/evidence/AGRO-FND-001/contracts/product-catalog-rolled-back.v1.schema.json");
    public static IReadOnlyList<CatalogIntegrationEventDefinition> All { get; } =
        Array.AsReadOnly(new[] { ProductCatalogPublished, ProductCatalogRolledBack });
}

public sealed record ProductCatalogPublishedPayload(Guid VersionId, string VersionTag, Guid? PreviousActiveVersionId,
    int ItemsCount, string CandidateHash, IReadOnlyList<Guid> SourceSnapshotIds, DateTimeOffset PublishedAtUtc);

public sealed record ProductCatalogRolledBackPayload(Guid VersionId, Guid? PreviousActiveVersionId, DateTimeOffset RolledBackAtUtc);

public sealed class CatalogPublishedSource
{
    private CatalogPublishedSource() { }
    public CatalogPublishedSource(Guid versionId, Guid sourceSnapshotId)
    {
        VersionId = versionId;
        SourceSnapshotId = sourceSnapshotId;
    }

    public Guid VersionId { get; private set; }
    public Guid SourceSnapshotId { get; private set; }
}

public sealed class CatalogEditorialAudit
{
    private CatalogEditorialAudit() { }
    public CatalogEditorialAudit(Guid id, string action, Guid actorUserId, Guid sessionId, string correlationId,
        Guid? versionId, Guid? sourceSnapshotId, DateTimeOffset occurredAtUtc)
    {
        Id = id;
        Action = action;
        ActorUserId = actorUserId;
        SessionId = sessionId;
        CorrelationId = correlationId;
        VersionId = versionId;
        SourceSnapshotId = sourceSnapshotId;
        OccurredAtUtc = occurredAtUtc;
    }

    public Guid Id { get; private set; }
    public string Action { get; private set; } = string.Empty;
    public Guid ActorUserId { get; private set; }
    public Guid SessionId { get; private set; }
    public string CorrelationId { get; private set; } = string.Empty;
    public Guid? VersionId { get; private set; }
    public Guid? SourceSnapshotId { get; private set; }
    public DateTimeOffset OccurredAtUtc { get; private set; }
}

/// <summary>Durable local publication facts only. No transport dispatcher is implemented.</summary>
public sealed class CatalogOutboxMessage
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private CatalogOutboxMessage() { }
    public static CatalogOutboxMessage Create<T>(CatalogIntegrationEventDefinition definition, Guid versionId,
        Guid auditId, Guid actorUserId, string correlationId, DateTimeOffset occurredAtUtc, T payload) => new()
        {
            Id = Guid.NewGuid(),
            EventType = definition.Type,
            SchemaVersion = definition.SchemaVersion,
            Source = definition.Source,
            Scope = definition.Scope,
            AggregateType = definition.AggregateType,
            AggregateId = versionId,
            AuditId = auditId,
            ActorUserId = actorUserId,
            CorrelationId = correlationId,
            OccurredAtUtc = occurredAtUtc,
            PayloadJson = JsonSerializer.Serialize(payload, JsonOptions),
        };

    public Guid Id { get; private set; }
    public string EventType { get; private set; } = string.Empty;
    public string SchemaVersion { get; private set; } = string.Empty;
    public string Source { get; private set; } = string.Empty;
    public string Scope { get; private set; } = string.Empty;
    public string AggregateType { get; private set; } = string.Empty;
    public Guid AggregateId { get; private set; }
    public Guid AuditId { get; private set; }
    public Guid ActorUserId { get; private set; }
    public string CorrelationId { get; private set; } = string.Empty;
    public DateTimeOffset OccurredAtUtc { get; private set; }
    public string PayloadJson { get; private set; } = string.Empty;
}
