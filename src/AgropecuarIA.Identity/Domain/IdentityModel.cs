namespace AgropecuarIA.Identity.Domain;

public static class IdentityConnections
{
    public const string Email = "email";
    public const string Google = "google";

    public static bool IsSupported(string value) =>
        value is Email or Google;
}

public sealed class PlatformUser
{
    private PlatformUser()
    {
    }

    public PlatformUser(Guid id, string displayName, DateTimeOffset createdAtUtc)
    {
        Id = id;
        DisplayName = displayName;
        CreatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; private set; }

    public string DisplayName { get; private set; } = string.Empty;

    public DateTimeOffset CreatedAtUtc { get; private set; }
}

public sealed class ExternalIdentity
{
    private ExternalIdentity()
    {
    }

    public ExternalIdentity(
        Guid id,
        Guid userId,
        string connection,
        string issuer,
        string subject,
        string label,
        DateTimeOffset verifiedAtUtc)
    {
        Id = id;
        UserId = userId;
        Connection = connection;
        Issuer = issuer;
        Subject = subject;
        Label = label;
        VerifiedAtUtc = verifiedAtUtc;
    }

    public Guid Id { get; private set; }

    public Guid UserId { get; private set; }

    public string Connection { get; private set; } = string.Empty;

    public string Issuer { get; private set; } = string.Empty;

    public string Subject { get; private set; } = string.Empty;

    public string Label { get; private set; } = string.Empty;

    public DateTimeOffset VerifiedAtUtc { get; private set; }
}

public sealed class OrganizationMembership
{
    private OrganizationMembership()
    {
    }

    public OrganizationMembership(Guid userId, Guid organizationId, string organizationName, string role)
    {
        UserId = userId;
        OrganizationId = organizationId;
        OrganizationName = organizationName;
        Role = role;
    }

    public Guid UserId { get; private set; }

    public Guid OrganizationId { get; private set; }

    public string OrganizationName { get; private set; } = string.Empty;

    public string Role { get; private set; } = string.Empty;
}

public sealed class UserSession
{
    private UserSession()
    {
    }

    public UserSession(
        Guid id,
        Guid userId,
        byte[] tokenHash,
        DateTimeOffset authenticatedAtUtc,
        DateTimeOffset expiresAtUtc)
    {
        Id = id;
        UserId = userId;
        TokenHash = tokenHash;
        AuthenticatedAtUtc = authenticatedAtUtc;
        ExpiresAtUtc = expiresAtUtc;
    }

    public Guid Id { get; private set; }

    public Guid UserId { get; private set; }

    public byte[] TokenHash { get; private set; } = [];

    public DateTimeOffset AuthenticatedAtUtc { get; private set; }

    public DateTimeOffset ExpiresAtUtc { get; private set; }

    public DateTimeOffset? RevokedAtUtc { get; private set; }

    public Guid Version { get; private set; } = Guid.NewGuid();

    public void Revoke(DateTimeOffset revokedAtUtc)
    {
        RevokedAtUtc ??= revokedAtUtc;
        Version = Guid.NewGuid();
    }
}

public sealed class LinkAttempt
{
    private LinkAttempt()
    {
    }

    public LinkAttempt(
        Guid id,
        Guid userId,
        Guid initiatingSessionId,
        string connection,
        DateTimeOffset expiresAtUtc)
    {
        Id = id;
        UserId = userId;
        InitiatingSessionId = initiatingSessionId;
        Connection = connection;
        ExpiresAtUtc = expiresAtUtc;
    }

    public Guid Id { get; private set; }

    public Guid UserId { get; private set; }

    public Guid InitiatingSessionId { get; private set; }

    public string Connection { get; private set; } = string.Empty;

    public DateTimeOffset ExpiresAtUtc { get; private set; }

    public string? CandidateIssuer { get; private set; }

    public string? CandidateSubject { get; private set; }

    public string? CandidateLabel { get; private set; }

    public DateTimeOffset? CandidateVerifiedAtUtc { get; private set; }

    public DateTimeOffset? ConsumedAtUtc { get; private set; }

    public Guid Version { get; private set; } = Guid.NewGuid();

    public bool HasCandidateProof => CandidateIssuer is not null && CandidateSubject is not null;

    public void AttachCandidateProof(VerifiedExternalIdentity candidate)
    {
        CandidateIssuer = candidate.Issuer;
        CandidateSubject = candidate.Subject;
        CandidateLabel = candidate.Label;
        CandidateVerifiedAtUtc = candidate.VerifiedAtUtc;
        Version = Guid.NewGuid();
    }

    public void Consume(DateTimeOffset consumedAtUtc)
    {
        ConsumedAtUtc = consumedAtUtc;
        Version = Guid.NewGuid();
    }
}

public sealed class IdentityAuditEvent
{
    private IdentityAuditEvent()
    {
    }

    public IdentityAuditEvent(
        Guid id,
        Guid? userId,
        Guid? sessionId,
        string action,
        string outcome,
        string? connection,
        string correlationId,
        DateTimeOffset occurredAtUtc)
    {
        Id = id;
        UserId = userId;
        SessionId = sessionId;
        Action = action;
        Outcome = outcome;
        Connection = connection;
        CorrelationId = correlationId;
        OccurredAtUtc = occurredAtUtc;
    }

    public Guid Id { get; private set; }

    public Guid? UserId { get; private set; }

    public Guid? SessionId { get; private set; }

    public string Action { get; private set; } = string.Empty;

    public string Outcome { get; private set; } = string.Empty;

    public string? Connection { get; private set; }

    public string CorrelationId { get; private set; } = string.Empty;

    public DateTimeOffset OccurredAtUtc { get; private set; }
}

public sealed class IdentityOutboxMessage
{
    private IdentityOutboxMessage()
    {
    }

    public IdentityOutboxMessage(
        Guid eventId,
        string type,
        int version,
        DateTimeOffset occurredAtUtc,
        Guid aggregateId,
        string payload)
    {
        EventId = eventId;
        Type = type;
        Version = version;
        OccurredAtUtc = occurredAtUtc;
        AggregateId = aggregateId;
        Payload = payload;
    }

    public Guid EventId { get; private set; }

    public string Type { get; private set; } = string.Empty;

    public int Version { get; private set; }

    public DateTimeOffset OccurredAtUtc { get; private set; }

    public Guid AggregateId { get; private set; }

    public string Payload { get; private set; } = string.Empty;

    public DateTimeOffset? DispatchedAtUtc { get; private set; }
}

public sealed record VerifiedExternalIdentity(
    string Connection,
    string Issuer,
    string Subject,
    string Label,
    string DisplayName,
    DateTimeOffset VerifiedAtUtc);
