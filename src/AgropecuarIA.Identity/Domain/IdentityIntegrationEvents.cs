namespace AgropecuarIA.Identity.Domain;

public enum IdentityIntegrationEventKind
{
    IdentityLinked = 1,
    IdentityStepUpCompleted = 2,
    OrganizationCreated = 3,
    OrganizationOwnerInvited = 4,
    OrganizationOwnerInvitationAccepted = 5,
    OrganizationOwnerInvitationRevoked = 6,
}

public sealed record IdentityIntegrationEventEnvelope(
    Guid EventId,
    RequestScope Scope,
    DateTimeOffset OccurredAtUtc,
    DateTimeOffset? EffectiveAtUtc,
    DateTimeOffset RecordedAtUtc,
    Guid ActorId,
    string CorrelationId,
    Guid? CausationId,
    Guid AggregateId,
    long AggregateVersion);

public sealed record IdentityLinkedIntegrationEventPayload(
    Guid UserId,
    Guid IdentityId,
    string Connection,
    DateTimeOffset LinkedAtUtc);

public sealed record IdentityStepUpCompletedIntegrationEventPayload(
    Guid UserId,
    Guid PreviousSessionId,
    Guid SessionId,
    string Purpose,
    DateTimeOffset CompletedAtUtc);

public sealed record OrganizationCreatedIntegrationEventPayload(
    Guid OrganizationId,
    Guid OwnerMembershipId,
    DateTimeOffset CreatedAtUtc);

public sealed record OrganizationOwnerInvitedIntegrationEventPayload(
    Guid OrganizationId,
    Guid InvitationId,
    DateTimeOffset ExpiresAtUtc,
    DateTimeOffset InvitedAtUtc);

public sealed record OrganizationOwnerInvitationAcceptedIntegrationEventPayload(
    Guid OrganizationId,
    Guid InvitationId,
    Guid MembershipId,
    DateTimeOffset AcceptedAtUtc);

public sealed record OrganizationOwnerInvitationRevokedIntegrationEventPayload(
    Guid OrganizationId,
    Guid InvitationId,
    DateTimeOffset RevokedAtUtc);

public sealed class IdentityIntegrationEventDefinition
{
    internal IdentityIntegrationEventDefinition(
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

        if (majorVersion <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(majorVersion),
                "The event contract major version must be positive.");
        }

        if (!Version.TryParse(schemaVersion, out Version? parsedVersion) ||
            parsedVersion.Major != majorVersion)
        {
            throw new ArgumentException(
                "The event schema version must be semantic and match its major version.",
                nameof(schemaVersion));
        }

        if (scope is not "platform" and not "tenant")
        {
            throw new ArgumentException("The event scope must be explicit.", nameof(scope));
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

public static class IdentityIntegrationEvents
{
    private const string Source = "identity-tenancy";
    private const string PlatformScope = "platform";
    private const string SchemaVersion = "1.0.0";

    public static IdentityIntegrationEventDefinition IdentityLinked { get; } = new(
        "IdentityLinked",
        1,
        SchemaVersion,
        Source,
        PlatformScope,
        nameof(PlatformUser),
        "tasks/evidence/AGRO-FND-001/contracts/identity-linked.v1.schema.json");

    public static IdentityIntegrationEventDefinition IdentityStepUpCompleted { get; } = new(
        "IdentityStepUpCompleted",
        1,
        SchemaVersion,
        Source,
        PlatformScope,
        nameof(PlatformUser),
        "tasks/evidence/AGRO-FND-001/contracts/identity-step-up-completed.v1.schema.json");

    public static IdentityIntegrationEventDefinition OrganizationCreated { get; } = new(
        "OrganizationCreated",
        1,
        SchemaVersion,
        Source,
        PlatformScope,
        "OrganizationDirectoryEntry",
        "tasks/evidence/AGRO-FND-001/contracts/organization-created.v1.schema.json");

    public static IdentityIntegrationEventDefinition OrganizationOwnerInvited { get; } = new(
        "OrganizationOwnerInvited",
        1,
        SchemaVersion,
        Source,
        "tenant",
        "OrganizationOwnerInvitation",
        "tasks/evidence/AGRO-FND-001/contracts/organization-owner-invited.v1.schema.json");

    public static IdentityIntegrationEventDefinition OrganizationOwnerInvitationAccepted { get; } = new(
        "OrganizationOwnerInvitationAccepted",
        1,
        SchemaVersion,
        Source,
        "tenant",
        "OrganizationOwnerInvitation",
        "tasks/evidence/AGRO-FND-001/contracts/organization-owner-invitation-accepted.v1.schema.json");

    public static IdentityIntegrationEventDefinition OrganizationOwnerInvitationRevoked { get; } = new(
        "OrganizationOwnerInvitationRevoked",
        1,
        SchemaVersion,
        Source,
        "tenant",
        "OrganizationOwnerInvitation",
        "tasks/evidence/AGRO-FND-001/contracts/organization-owner-invitation-revoked.v1.schema.json");

    public static IReadOnlyList<IdentityIntegrationEventDefinition> All { get; } =
        Array.AsReadOnly(
            Enum.GetValues<IdentityIntegrationEventKind>()
                .Select(GetRequired)
                .ToArray());

    public static IdentityIntegrationEventDefinition GetRequired(IdentityIntegrationEventKind kind) =>
        kind switch
        {
            IdentityIntegrationEventKind.IdentityLinked => IdentityLinked,
            IdentityIntegrationEventKind.IdentityStepUpCompleted => IdentityStepUpCompleted,
            IdentityIntegrationEventKind.OrganizationCreated => OrganizationCreated,
            IdentityIntegrationEventKind.OrganizationOwnerInvited => OrganizationOwnerInvited,
            IdentityIntegrationEventKind.OrganizationOwnerInvitationAccepted => OrganizationOwnerInvitationAccepted,
            IdentityIntegrationEventKind.OrganizationOwnerInvitationRevoked => OrganizationOwnerInvitationRevoked,
            _ => throw new ArgumentOutOfRangeException(
                nameof(kind),
                kind,
                "The identity integration event kind is not registered."),
        };
}
