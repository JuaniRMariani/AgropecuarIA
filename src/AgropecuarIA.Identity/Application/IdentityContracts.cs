using AgropecuarIA.Identity.Domain;

namespace AgropecuarIA.Identity.Application;

public sealed record IdentitySessionResult(
    Guid UserId,
    string DisplayName,
    IReadOnlyList<LinkedIdentityResult> Identities,
    IReadOnlyList<MembershipResult> Memberships,
    AuthenticationAssuranceResult AuthenticationAssurance);

public sealed record AuthenticationAssuranceResult(
    string Level,
    DateTimeOffset AuthenticatedAtUtc,
    string? Purpose,
    DateTimeOffset? StrongAuthenticatedAtUtc,
    DateTimeOffset? ExpiresAtUtc);

public sealed record LinkedIdentityResult(
    Guid IdentityId,
    string Connection,
    string Label,
    DateTimeOffset VerifiedAtUtc);

public sealed record MembershipResult(
    Guid OrganizationId,
    string OrganizationName,
    string Role);

public sealed record CreateOrganizationCommand(string DisplayName, string IdempotencyKey);

public sealed record CreatedOrganizationResult(
    Guid OrganizationId,
    string DisplayName,
    string OrganizationStatus,
    Guid MembershipId,
    string MembershipRole,
    string MembershipStatus,
    long AuthorizationVersion);

public sealed record IssuedSession(Guid SessionId, Guid UserId, string Token, DateTimeOffset ExpiresAtUtc);

public sealed record AuthenticatedSession(
    Guid SessionId,
    Guid UserId,
    DateTimeOffset AuthenticatedAtUtc,
    bool IsAuthenticationAssuranceVerified,
    DateTimeOffset? StrongAuthenticatedAtUtc = null,
    string? StrongAuthenticationPurpose = null);

public sealed record StartedLinkAttempt(
    Guid AttemptId,
    string Connection,
    DateTimeOffset ExpiresAtUtc,
    string AuthorizationUrl);

public sealed record StartedStepUpAttempt(
    Guid AttemptId,
    string Purpose,
    DateTimeOffset ExpiresAtUtc,
    string AuthorizationUrl);

public sealed record StepUpAttemptValidation(
    Guid AttemptId,
    string Purpose,
    DateTimeOffset ExpiresAtUtc);

public sealed record VerifiedStepUpProof(
    string Issuer,
    string Subject,
    DateTimeOffset AuthenticatedAtUtc,
    bool IsStrongAuthentication);

public sealed record IdentityRequestContext
{
    private IdentityRequestContext(string correlationId, Guid? actorId, RequestScope scope)
    {
        if (string.IsNullOrWhiteSpace(correlationId) || correlationId.Length > 128)
        {
            throw new ArgumentException(
                "Correlation ID must contain between 1 and 128 characters.",
                nameof(correlationId));
        }

        if (actorId == Guid.Empty)
        {
            throw new ArgumentException("Actor ID cannot be empty.", nameof(actorId));
        }

        CorrelationId = correlationId;
        ActorId = actorId;
        Scope = scope ?? throw new ArgumentNullException(nameof(scope));
    }

    public string CorrelationId { get; }

    public Guid? ActorId { get; }

    public RequestScope Scope { get; }

    public static IdentityRequestContext ForPlatform(string correlationId, Guid? actorId = null) =>
        new(correlationId, actorId, RequestScope.Platform);

    public static IdentityRequestContext ForTenant(
        string correlationId,
        Guid actorId,
        Guid tenantId)
    {
        if (actorId == Guid.Empty)
        {
            throw new ArgumentException("Actor ID is required for tenant scope.", nameof(actorId));
        }

        return new IdentityRequestContext(
            correlationId,
            actorId,
            RequestScope.ForTenant(tenantId));
    }

    public void RequirePlatform()
    {
        if (Scope is not RequestScope.PlatformRequestScope)
        {
            throw new InvalidOperationException("The identity operation requires platform scope.");
        }
    }

    public void RequirePlatformActor(Guid expectedActorId)
    {
        RequirePlatform();
        if (expectedActorId == Guid.Empty || ActorId != expectedActorId)
        {
            throw new InvalidOperationException(
                "The identity operation requires the authenticated platform actor.");
        }
    }
}

public sealed class IdentityOperationException : Exception
{
    public IdentityOperationException(
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

public static class IdentityErrors
{
    public static IdentityOperationException InvalidConnection() =>
        new("identity.invalid_connection", 400, "The requested identity connection is invalid.");

    public static IdentityOperationException IdentityNotVerified() =>
        new("identity.not_verified", 409, "The identity could not be verified.");

    public static IdentityOperationException SessionRequired() =>
        new("identity.session_required", 401, "A valid session is required.");

    public static IdentityOperationException RecentAuthenticationRequired() =>
        new("identity.reauthentication_required", 403, "Recent authentication is required.");

    public static IdentityOperationException LinkAttemptConflict() =>
        new("identity.link_attempt_conflict", 409, "The link attempt is invalid, expired, consumed, or incomplete.");

    public static IdentityOperationException IdentityConflict() =>
        new("identity.conflict", 409, "The identity cannot be linked to this account.");

    public static IdentityOperationException LastIdentity() =>
        new("identity.last_identity", 409, "The last sign-in identity cannot be removed.");

    public static IdentityOperationException ProviderUnavailable() =>
        new("identity.provider_unavailable", 503, "The identity provider is unavailable.");

    public static IdentityOperationException InvalidStepUpPurpose() =>
        new("identity.invalid_step_up_purpose", 400, "The requested step-up purpose is invalid.");

    public static IdentityOperationException StepUpAttemptConflict() =>
        new("identity.step_up_attempt_conflict", 409, "The step-up attempt is invalid, expired, consumed, or does not belong to this session.");

    public static IdentityOperationException StrongAuthenticationRequired() =>
        new("identity.strong_authentication_required", 403, "Strong authentication is required.");

    public static IdentityOperationException InvalidOrganizationDisplayName() =>
        new("identity.invalid_organization_display_name", 400, "The organization display name is invalid.");

    public static IdentityOperationException InvalidIdempotencyKey() =>
        new("identity.invalid_idempotency_key", 400, "The idempotency key is invalid.");

    public static IdentityOperationException OrganizationCreationUnavailable() =>
        new("identity.organization_creation_unavailable", 503, "Organization creation is unavailable.");

    public static IdentityOperationException IdempotencyKeyReused() =>
        new("idempotency.key_reused", 409, "The idempotency key is already bound to another request.");

    public static IdentityOperationException IdempotencyInProgress() =>
        new(
            "idempotency.in_progress",
            409,
            "The idempotent operation is still in progress.",
            retryable: true,
            retryAfterSeconds: 1);

    public static IdentityOperationException IdempotencyFailedTerminal() =>
        new("idempotency.failed_terminal", 409, "The idempotent operation failed terminally.");

    public static IdentityOperationException ReconciliationRequired() =>
        new(
            "idempotency.reconciliation_required",
            503,
            "The result could not be determined safely. Retry with the same idempotency key.",
            retryable: true,
            retryAfterSeconds: 1);
}
