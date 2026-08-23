using System.Text.Json;

namespace AgropecuarIA.Identity.Domain;

public static class IdentityConnections
{
    public const string Email = "email";
    public const string Google = "google";

    public static bool IsSupported(string value) =>
        value is Email or Google;
}

public static class StepUpPurposes
{
    public const string ManageAuthenticationMethods = "manage_authentication_methods";
    public const string ManageOrganizationOwners = "manage_organization_owners";
    public const string ManageSessions = "manage_sessions";

    public static bool IsSupported(string value) =>
        value is ManageAuthenticationMethods or ManageOrganizationOwners or ManageSessions;
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

    public long Version { get; private set; }

    public long NextVersion()
    {
        Version = checked(Version + 1);
        return Version;
    }
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
        DateTimeOffset expiresAtUtc,
        bool isAuthenticationAssuranceVerified,
        DateTimeOffset? strongAuthenticatedAtUtc = null,
        string? strongAuthenticationPurpose = null)
    {
        if ((strongAuthenticatedAtUtc is null) != (strongAuthenticationPurpose is null) ||
            (strongAuthenticationPurpose is not null &&
                !StepUpPurposes.IsSupported(strongAuthenticationPurpose)))
        {
            throw new ArgumentException(
                "Strong authentication time and a supported purpose must be provided together.");
        }

        if (strongAuthenticatedAtUtc > expiresAtUtc)
        {
            throw new ArgumentException(
                "Strong authentication cannot outlive the session.",
                nameof(strongAuthenticatedAtUtc));
        }

        Id = id;
        UserId = userId;
        TokenHash = tokenHash;
        AuthenticatedAtUtc = authenticatedAtUtc;
        ExpiresAtUtc = expiresAtUtc;
        IsAuthenticationAssuranceVerified = isAuthenticationAssuranceVerified;
        StrongAuthenticatedAtUtc = strongAuthenticatedAtUtc;
        StrongAuthenticationPurpose = strongAuthenticationPurpose;
    }

    public Guid Id { get; private set; }

    public Guid UserId { get; private set; }

    public byte[] TokenHash { get; private set; } = [];

    public DateTimeOffset AuthenticatedAtUtc { get; private set; }

    public DateTimeOffset ExpiresAtUtc { get; private set; }

    public bool IsAuthenticationAssuranceVerified { get; private set; }

    public DateTimeOffset? StrongAuthenticatedAtUtc { get; private set; }

    public string? StrongAuthenticationPurpose { get; private set; }

    public DateTimeOffset? RevokedAtUtc { get; private set; }

    public Guid Version { get; private set; } = Guid.NewGuid();

    public bool Revoke(DateTimeOffset revokedAtUtc)
    {
        if (RevokedAtUtc is not null)
        {
            return false;
        }

        RevokedAtUtc = revokedAtUtc;
        Version = Guid.NewGuid();
        return true;
    }
}

public sealed class StepUpAttempt
{
    private StepUpAttempt()
    {
    }

    public StepUpAttempt(
        Guid id,
        Guid userId,
        Guid initiatingSessionId,
        string purpose,
        DateTimeOffset startedAtUtc,
        DateTimeOffset expiresAtUtc)
    {
        if (id == Guid.Empty || userId == Guid.Empty || initiatingSessionId == Guid.Empty)
        {
            throw new ArgumentException("Step-up attempt, user, and session IDs are required.");
        }

        if (!StepUpPurposes.IsSupported(purpose))
        {
            throw new ArgumentException("The step-up purpose is not supported.", nameof(purpose));
        }

        if (expiresAtUtc <= startedAtUtc)
        {
            throw new ArgumentException("Step-up expiry must follow its start time.", nameof(expiresAtUtc));
        }

        Id = id;
        UserId = userId;
        InitiatingSessionId = initiatingSessionId;
        Purpose = purpose;
        StartedAtUtc = startedAtUtc;
        ExpiresAtUtc = expiresAtUtc;
    }

    public Guid Id { get; private set; }

    public Guid UserId { get; private set; }

    public Guid InitiatingSessionId { get; private set; }

    public string Purpose { get; private set; } = string.Empty;

    public DateTimeOffset StartedAtUtc { get; private set; }

    public DateTimeOffset ExpiresAtUtc { get; private set; }

    public DateTimeOffset? ConsumedAtUtc { get; private set; }

    public Guid Version { get; private set; } = Guid.NewGuid();

    public void Consume(DateTimeOffset consumedAtUtc)
    {
        if (ConsumedAtUtc is not null)
        {
            throw new InvalidOperationException("The step-up attempt has already been consumed.");
        }

        if (consumedAtUtc < StartedAtUtc)
        {
            throw new ArgumentException("Consumption cannot precede the attempt.", nameof(consumedAtUtc));
        }

        if (consumedAtUtc >= ExpiresAtUtc)
        {
            throw new ArgumentException("An expired step-up attempt cannot be consumed.", nameof(consumedAtUtc));
        }

        ConsumedAtUtc = consumedAtUtc;
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

public sealed class IdentitySecurityJournalEntry
{
    private IdentitySecurityJournalEntry()
    {
    }

    public IdentitySecurityJournalEntry(
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
    private static readonly JsonSerializerOptions PayloadSerializerOptions =
        new(JsonSerializerDefaults.Web);

    private IdentityOutboxMessage()
    {
    }

    private IdentityOutboxMessage(
        IdentityIntegrationEventKind kind,
        IdentityIntegrationEventEnvelope envelope,
        string payload)
    {
        IdentityIntegrationEventDefinition definition = IdentityIntegrationEvents.GetRequired(kind);
        ArgumentNullException.ThrowIfNull(envelope);
        ArgumentException.ThrowIfNullOrWhiteSpace(envelope.CorrelationId);
        ArgumentException.ThrowIfNullOrWhiteSpace(payload);
        ArgumentNullException.ThrowIfNull(envelope.Scope);
        if (envelope.EventId == Guid.Empty || envelope.ActorId == Guid.Empty || envelope.AggregateId == Guid.Empty)
        {
            throw new ArgumentException("Event, actor, and aggregate IDs are required.");
        }

        if (envelope.AggregateVersion <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(envelope),
                "The aggregate version must be positive.");
        }

        if (!string.Equals(definition.Scope, envelope.Scope.Kind, StringComparison.Ordinal))
        {
            throw new ArgumentException(
                $"Event '{definition.Type}' requires '{definition.Scope}' scope.",
                nameof(envelope));
        }

        if (envelope.RecordedAtUtc < envelope.OccurredAtUtc)
        {
            throw new ArgumentException(
                "Recorded time cannot precede occurred time.",
                nameof(envelope));
        }

        EventId = envelope.EventId;
        Type = definition.Type;
        Version = definition.MajorVersion;
        SchemaVersion = definition.SchemaVersion;
        Source = definition.Source;
        ScopeKind = envelope.Scope.Kind;
        TenantId = envelope.Scope.TenantId;
        OccurredAtUtc = envelope.OccurredAtUtc;
        EffectiveAtUtc = envelope.EffectiveAtUtc;
        RecordedAtUtc = envelope.RecordedAtUtc;
        ActorId = envelope.ActorId;
        CorrelationId = envelope.CorrelationId;
        CausationId = envelope.CausationId;
        AggregateType = definition.AggregateType;
        AggregateId = envelope.AggregateId;
        AggregateVersion = envelope.AggregateVersion;
        Payload = payload;
    }

    public static IdentityOutboxMessage CreateIdentityLinked(
        IdentityIntegrationEventEnvelope envelope,
        IdentityLinkedIntegrationEventPayload payload)
    {
        ArgumentNullException.ThrowIfNull(payload);
        ValidateUserPayload(envelope, payload.UserId, payload.LinkedAtUtc);
        if (payload.IdentityId == Guid.Empty)
        {
            throw new ArgumentException("The linked identity ID is required.", nameof(payload));
        }

        if (!IdentityConnections.IsSupported(payload.Connection))
        {
            throw new ArgumentException("The identity connection is not supported.", nameof(payload));
        }

        return new IdentityOutboxMessage(
            IdentityIntegrationEventKind.IdentityLinked,
            envelope,
            JsonSerializer.Serialize(payload, PayloadSerializerOptions));
    }

    public static IdentityOutboxMessage CreateIdentityStepUpCompleted(
        IdentityIntegrationEventEnvelope envelope,
        IdentityStepUpCompletedIntegrationEventPayload payload)
    {
        ArgumentNullException.ThrowIfNull(payload);
        ValidateUserPayload(envelope, payload.UserId, payload.CompletedAtUtc);
        if (payload.PreviousSessionId == Guid.Empty || payload.SessionId == Guid.Empty)
        {
            throw new ArgumentException("The previous and rotated session IDs are required.", nameof(payload));
        }

        if (payload.PreviousSessionId == payload.SessionId)
        {
            throw new ArgumentException("The rotated session must be new.", nameof(payload));
        }

        if (!StepUpPurposes.IsSupported(payload.Purpose))
        {
            throw new ArgumentException("The step-up purpose is not supported.", nameof(payload));
        }

        return new IdentityOutboxMessage(
            IdentityIntegrationEventKind.IdentityStepUpCompleted,
            envelope,
            JsonSerializer.Serialize(payload, PayloadSerializerOptions));
    }

    public static IdentityOutboxMessage CreateOrganizationCreated(
        IdentityIntegrationEventEnvelope envelope,
        OrganizationCreatedIntegrationEventPayload payload)
    {
        ArgumentNullException.ThrowIfNull(payload);
        ArgumentNullException.ThrowIfNull(envelope);
        if (payload.OrganizationId == Guid.Empty || payload.OwnerMembershipId == Guid.Empty)
        {
            throw new ArgumentException(
                "The organization and owner membership IDs are required.",
                nameof(payload));
        }

        if (payload.OrganizationId != envelope.AggregateId ||
            payload.CreatedAtUtc != envelope.OccurredAtUtc)
        {
            throw new ArgumentException(
                "The organization payload must match its event envelope.",
                nameof(payload));
        }

        return new IdentityOutboxMessage(
            IdentityIntegrationEventKind.OrganizationCreated,
            envelope,
            JsonSerializer.Serialize(payload, PayloadSerializerOptions));
    }

    public static IdentityOutboxMessage CreateOrganizationOwnerInvited(
        IdentityIntegrationEventEnvelope envelope,
        OrganizationOwnerInvitedIntegrationEventPayload payload)
    {
        ValidateOwnerInvitationEnvelope(
            envelope,
            payload.OrganizationId,
            payload.InvitationId,
            payload.InvitedAtUtc,
            expectedAggregateVersion: 1);
        if (payload.ExpiresAtUtc <= payload.InvitedAtUtc)
        {
            throw new ArgumentException("The invitation must expire after it is created.", nameof(payload));
        }

        return new IdentityOutboxMessage(
            IdentityIntegrationEventKind.OrganizationOwnerInvited,
            envelope,
            JsonSerializer.Serialize(payload, PayloadSerializerOptions));
    }

    public static IdentityOutboxMessage CreateOrganizationOwnerInvitationAccepted(
        IdentityIntegrationEventEnvelope envelope,
        OrganizationOwnerInvitationAcceptedIntegrationEventPayload payload)
    {
        ValidateOwnerInvitationEnvelope(
            envelope,
            payload.OrganizationId,
            payload.InvitationId,
            payload.AcceptedAtUtc,
            expectedAggregateVersion: 2);
        if (payload.MembershipId == Guid.Empty)
        {
            throw new ArgumentException("The accepted membership ID is required.", nameof(payload));
        }

        return new IdentityOutboxMessage(
            IdentityIntegrationEventKind.OrganizationOwnerInvitationAccepted,
            envelope,
            JsonSerializer.Serialize(payload, PayloadSerializerOptions));
    }

    public static IdentityOutboxMessage CreateOrganizationOwnerInvitationRevoked(
        IdentityIntegrationEventEnvelope envelope,
        OrganizationOwnerInvitationRevokedIntegrationEventPayload payload)
    {
        ValidateOwnerInvitationEnvelope(
            envelope,
            payload.OrganizationId,
            payload.InvitationId,
            payload.RevokedAtUtc,
            expectedAggregateVersion: 2);
        return new IdentityOutboxMessage(
            IdentityIntegrationEventKind.OrganizationOwnerInvitationRevoked,
            envelope,
            JsonSerializer.Serialize(payload, PayloadSerializerOptions));
    }

    public static IdentityOutboxMessage CreateOrganizationOwnerMembershipRemoved(
        IdentityIntegrationEventEnvelope envelope,
        OrganizationOwnerMembershipRemovedIntegrationEventPayload payload)
    {
        ArgumentNullException.ThrowIfNull(envelope);
        ArgumentNullException.ThrowIfNull(payload);
        if (payload.OrganizationId == Guid.Empty ||
            payload.MembershipId == Guid.Empty ||
            payload.AuthorizationVersion < 2 ||
            payload.RevokedInvitationCount < 0 ||
            envelope.Scope is not RequestScope.TenantRequestScope tenantScope ||
            tenantScope.TenantId != payload.OrganizationId ||
            envelope.AggregateId != payload.MembershipId ||
            envelope.OccurredAtUtc != payload.RemovedAtUtc ||
            envelope.AggregateVersion != payload.AuthorizationVersion)
        {
            throw new ArgumentException(
                "The removed owner membership payload must match its tenant event envelope.",
                nameof(payload));
        }

        return new IdentityOutboxMessage(
            IdentityIntegrationEventKind.OrganizationOwnerMembershipRemoved,
            envelope,
            JsonSerializer.Serialize(payload, PayloadSerializerOptions));
    }

    private static void ValidateOwnerInvitationEnvelope(
        IdentityIntegrationEventEnvelope envelope,
        Guid organizationId,
        Guid invitationId,
        DateTimeOffset occurredAtUtc,
        long expectedAggregateVersion)
    {
        ArgumentNullException.ThrowIfNull(envelope);
        if (organizationId == Guid.Empty || invitationId == Guid.Empty ||
            envelope.Scope is not RequestScope.TenantRequestScope tenantScope ||
            tenantScope.TenantId != organizationId ||
            envelope.AggregateId != invitationId ||
            envelope.OccurredAtUtc != occurredAtUtc ||
            envelope.AggregateVersion != expectedAggregateVersion)
        {
            throw new ArgumentException(
                "The owner invitation payload must match its tenant event envelope.",
                nameof(envelope));
        }
    }

    private static void ValidateUserPayload(
        IdentityIntegrationEventEnvelope envelope,
        Guid userId,
        DateTimeOffset eventAtUtc)
    {
        ArgumentNullException.ThrowIfNull(envelope);
        if (userId == Guid.Empty || userId != envelope.AggregateId || userId != envelope.ActorId)
        {
            throw new ArgumentException(
                "The payload user must match the event actor and aggregate.",
                nameof(userId));
        }

        if (eventAtUtc != envelope.OccurredAtUtc)
        {
            throw new ArgumentException(
                "The payload occurrence time must match the event envelope.",
                nameof(eventAtUtc));
        }
    }

    public Guid EventId { get; private set; }

    public string Type { get; private set; } = string.Empty;

    public int Version { get; private set; }

    public string SchemaVersion { get; private set; } = string.Empty;

    public string Source { get; private set; } = string.Empty;

    public string ScopeKind { get; private set; } = string.Empty;

    public Guid? TenantId { get; private set; }

    public DateTimeOffset OccurredAtUtc { get; private set; }

    public DateTimeOffset? EffectiveAtUtc { get; private set; }

    public DateTimeOffset RecordedAtUtc { get; private set; }

    public Guid ActorId { get; private set; }

    public string CorrelationId { get; private set; } = string.Empty;

    public Guid? CausationId { get; private set; }

    public string AggregateType { get; private set; } = string.Empty;

    public Guid AggregateId { get; private set; }

    public long AggregateVersion { get; private set; }

    public string Payload { get; private set; } = string.Empty;

    public DateTimeOffset? DispatchedAtUtc { get; private set; }
}

public sealed record VerifiedExternalIdentity(
    string Connection,
    string Issuer,
    string Subject,
    string Label,
    string DisplayName,
    DateTimeOffset VerifiedAtUtc,
    DateTimeOffset AuthenticatedAtUtc);

public sealed class UserTotpCredential
{
    private UserTotpCredential() { }

    public UserTotpCredential(Guid userId, string protectedSecret)
    {
        UserId = userId;
        ProtectedSecret = protectedSecret;
        CreatedAtUtc = DateTimeOffset.UtcNow;
    }

    public Guid UserId { get; private set; }
    public string ProtectedSecret { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; private set; }
}

public sealed class UserPasskeyCredential
{
    private UserPasskeyCredential() { }

    public UserPasskeyCredential(
        Guid userId,
        byte[] credentialId,
        byte[] publicKey,
        uint signCount,
        Guid aaguid)
    {
        UserId = userId;
        CredentialId = credentialId;
        PublicKey = publicKey;
        SignCount = signCount;
        Aaguid = aaguid;
        CreatedAtUtc = DateTimeOffset.UtcNow;
    }

    public Guid UserId { get; private set; }
    public byte[] CredentialId { get; private set; } = [];
    public byte[] PublicKey { get; private set; } = [];
    public uint SignCount { get; private set; }
    public Guid Aaguid { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }

    public void UpdateSignCount(uint signCount)
    {
        SignCount = signCount;
    }
}

public sealed class UserRecoveryCode
{
    private UserRecoveryCode() { }

    public UserRecoveryCode(Guid userId, string codeHash)
    {
        UserId = userId;
        CodeHash = codeHash;
        CreatedAtUtc = DateTimeOffset.UtcNow;
    }

    public Guid UserId { get; private set; }
    public string CodeHash { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset? UsedAtUtc { get; private set; }

    public void MarkAsUsed(DateTimeOffset timestamp)
    {
        UsedAtUtc = timestamp;
    }
}
