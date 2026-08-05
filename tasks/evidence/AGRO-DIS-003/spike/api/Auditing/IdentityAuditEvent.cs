namespace AgropecuarIA.IdentitySpike.Api.Auditing;

internal sealed record IdentityAuditEvent(
    Guid EventId,
    string EventType,
    DateTimeOffset OccurredAt,
    Guid? ActorId,
    Guid? OrganizationId,
    Guid CorrelationId,
    string Outcome,
    string? ReasonCode)
{
    internal static IdentityAuditEvent Succeeded(
        string eventType,
        Guid? actorId,
        Guid? organizationId,
        Guid correlationId) => new(
            Guid.NewGuid(),
            eventType,
            DateTimeOffset.UtcNow,
            actorId,
            organizationId,
            correlationId,
            "succeeded",
            null);

    internal static IdentityAuditEvent Accepted(
        string eventType,
        Guid? actorId,
        Guid? organizationId,
        Guid correlationId,
        string? reasonCode = null) => new(
            Guid.NewGuid(),
            eventType,
            DateTimeOffset.UtcNow,
            actorId,
            organizationId,
            correlationId,
            "accepted",
            reasonCode);

    internal static IdentityAuditEvent Denied(
        string eventType,
        Guid? actorId,
        Guid? organizationId,
        Guid correlationId,
        string reasonCode) => new(
            Guid.NewGuid(),
            eventType,
            DateTimeOffset.UtcNow,
            actorId,
            organizationId,
            correlationId,
            "denied",
            reasonCode);
}
