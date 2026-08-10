namespace AgropecuarIA.IdentitySpike.Api.Sessions;

internal sealed record OrganizationMembership(
    Guid MembershipId,
    Guid OrganizationId,
    string DisplayName,
    string Status,
    IReadOnlyList<string> Permissions,
    int SecurityVersion);

internal sealed record SessionRecord(
    Guid SessionId,
    Guid UserId,
    string Assurance,
    IReadOnlyList<string> Methods,
    DateTimeOffset AuthenticatedAt,
    DateTimeOffset ExpiresAt,
    DateTimeOffset? StepUpExpiresAt,
    Guid? SelectedOrganizationId,
    DateTimeOffset? RevokedAt,
    string? RevocationReason);

internal sealed record ActorResponse(
    string Kind,
    Guid UserId,
    string Assurance,
    IReadOnlyList<string> Methods,
    DateTimeOffset AuthenticatedAt);

internal sealed record OrganizationResponse(
    Guid OrganizationId,
    Guid MembershipId,
    string DisplayName,
    string MembershipStatus);

internal sealed record SessionResponse(Guid SessionId, DateTimeOffset ExpiresAt, DateTimeOffset? StepUpExpiresAt);

internal sealed record SignedOutResponse(string Kind);

internal sealed record SelectionRequiredResponse(
    string Kind,
    ActorResponse Actor,
    IReadOnlyList<OrganizationResponse> Organizations,
    SessionResponse Session);

internal sealed record ActiveSessionResponse(
    string Kind,
    ActorResponse Actor,
    OrganizationResponse Tenant,
    IReadOnlyList<string> Permissions,
    string AuthorizationVersion,
    SessionResponse Session);

internal sealed record RevokedSessionResponse(string Kind, string Reason);

internal enum SessionResolutionKind
{
    SignedOut,
    Revoked,
    NoActiveMembership,
    SelectionRequired,
    Active,
    MembershipLimitExceeded
}

internal sealed record SessionResolution(
    SessionResolutionKind Kind,
    SessionRecord? Session,
    IReadOnlyList<OrganizationMembership> Memberships,
    OrganizationMembership? ActiveMembership,
    string? RevocationReason);
