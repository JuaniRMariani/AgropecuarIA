namespace AgropecuarIA.IdentitySpike.Api.Sessions;

internal sealed class SessionContextService(
    SessionStore sessionStore,
    IMembershipDiscoveryRepository membershipDiscoveryRepository,
    TimeProvider timeProvider)
{
    internal async Task<SessionResolution> ResolveAsync(
        HttpContext context,
        CancellationToken cancellationToken)
    {
        if (!context.Request.Cookies.TryGetValue(SessionStore.CookieName, out var rawSessionId) ||
            !Guid.TryParse(rawSessionId, out var sessionId))
        {
            return new(SessionResolutionKind.SignedOut, null, [], null, null);
        }

        var session = sessionStore.Find(sessionId);
        if (session is null)
        {
            return new(SessionResolutionKind.SignedOut, null, [], null, null);
        }

        if (session.RevokedAt is not null || session.ExpiresAt <= timeProvider.GetUtcNow())
        {
            return new(
                SessionResolutionKind.Revoked,
                session,
                [],
                null,
                NormalizeRevocationReason(session.RevocationReason));
        }

        IReadOnlyList<OrganizationMembership> memberships;
        try
        {
            memberships = await membershipDiscoveryRepository.ListActiveForActorAsync(
                session.UserId,
                cancellationToken);
        }
        catch (MembershipDiscoveryLimitExceededException)
        {
            return new(
                SessionResolutionKind.MembershipLimitExceeded,
                session,
                [],
                null,
                null);
        }
        if (memberships.Count == 0)
        {
            return new(SessionResolutionKind.NoActiveMembership, session, memberships, null, null);
        }

        if (session.SelectedOrganizationId is null)
        {
            if (memberships.Count == 1)
            {
                return new(SessionResolutionKind.Active, session, memberships, memberships[0], null);
            }

            return new(SessionResolutionKind.SelectionRequired, session, memberships, null, null);
        }

        var membership = memberships.SingleOrDefault(
            item => item.OrganizationId == session.SelectedOrganizationId.Value);

        return membership is null
            ? new(
                SessionResolutionKind.Revoked,
                session,
                memberships,
                null,
                "membership_revoked")
            : new(SessionResolutionKind.Active, session, memberships, membership, null);
    }

    internal bool HasValidStepUp(SessionRecord session) =>
        session.StepUpExpiresAt is DateTimeOffset expiresAt && expiresAt > timeProvider.GetUtcNow();

    internal static ActiveSessionResponse ToActiveResponse(
        SessionRecord session,
        OrganizationMembership membership) => new(
            "active",
            ToActorResponse(session),
            ToOrganizationResponse(membership),
            membership.Permissions,
            $"membership-v{membership.SecurityVersion}",
            ToSessionResponse(session));

    internal static SelectionRequiredResponse ToSelectionRequiredResponse(
        SessionRecord session,
        IReadOnlyList<OrganizationMembership> memberships) => new(
            "selection_required",
            ToActorResponse(session),
            memberships.Select(ToOrganizationResponse).ToArray(),
            ToSessionResponse(session));

    private static ActorResponse ToActorResponse(SessionRecord session) => new(
        "human",
        session.UserId,
        session.Assurance,
        session.Methods,
        session.AuthenticatedAt);

    private static OrganizationResponse ToOrganizationResponse(OrganizationMembership membership) => new(
        membership.OrganizationId,
        membership.MembershipId,
        membership.DisplayName,
        membership.Status);

    private static SessionResponse ToSessionResponse(SessionRecord session) => new(
        session.SessionId,
        session.ExpiresAt,
        session.StepUpExpiresAt);

    private static string NormalizeRevocationReason(string? reason) => reason switch
    {
        "membership_revoked" => "membership_revoked",
        "security_version_changed" => "security_version_changed",
        _ => "session_revoked"
    };
}
