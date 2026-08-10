using AgropecuarIA.IdentitySpike.Api.Auditing;
using AgropecuarIA.IdentitySpike.Api.Common;

namespace AgropecuarIA.IdentitySpike.Api.Sessions;

internal static class SessionEndpoints
{
    internal static void Map(RouteGroupBuilder api)
    {
        api.MapGet("/session", GetSession);
        api.MapPost("/session/switch-organization", SwitchOrganization)
            .AddEndpointFilter<AntiforgeryEndpointFilter>();
        api.MapPost("/session/revoke", RevokeSession)
            .AddEndpointFilter<AntiforgeryEndpointFilter>();
    }

    private static async Task<IResult> GetSession(
        HttpContext context,
        SessionContextService contextService,
        CancellationToken cancellationToken)
    {
        SessionResolution resolution = await contextService.ResolveAsync(context, cancellationToken);
        return resolution.Kind switch
        {
            SessionResolutionKind.SignedOut => TypedResults.Ok(new SignedOutResponse("signed_out")),
            SessionResolutionKind.Revoked => TypedResults.Ok(
                new RevokedSessionResponse("revoked", resolution.RevocationReason ?? "session_revoked")),
            SessionResolutionKind.NoActiveMembership => ProblemResults.NoActiveMembership(context),
            SessionResolutionKind.SelectionRequired => TypedResults.Ok(
                SessionContextService.ToSelectionRequiredResponse(
                    resolution.Session!,
                    resolution.Memberships)),
            SessionResolutionKind.Active => TypedResults.Ok(
                SessionContextService.ToActiveResponse(
                    resolution.Session!,
                    resolution.ActiveMembership!)),
            SessionResolutionKind.MembershipLimitExceeded =>
                ProblemResults.MembershipLimitExceeded(context),
            _ => throw new InvalidOperationException("Unknown session resolution.")
        };
    }

    private static async Task<IResult> SwitchOrganization(
        SwitchOrganizationRequest request,
        HttpContext context,
        SessionContextService contextService,
        SessionStore sessionStore,
        AuditEventRepository auditRepository,
        CancellationToken cancellationToken)
    {
        SessionRequirementResult requirement = await SessionEndpointSupport.RequireAuthenticatedAsync(
            context,
            contextService,
            cancellationToken);
        if (requirement.Failure is not null)
        {
            return requirement.Failure;
        }

        SessionResolution resolution = requirement.Resolution;
        OrganizationMembership? membership = resolution.Memberships.SingleOrDefault(
            candidate => candidate.OrganizationId == request.OrganizationId);
        if (membership is null)
        {
            await auditRepository.RecordAsync(
                IdentityAuditEvent.Denied(
                    "AccessDenied",
                    resolution.Session!.UserId,
                    resolution.Session.SelectedOrganizationId,
                    CorrelationIdAccessor.Get(context),
                    "organization_not_available"),
                cancellationToken);
            return ProblemResults.NeutralNotFound(context);
        }

        var replacement = sessionStore.SwitchOrganization(
            resolution.Session!.SessionId,
            membership.OrganizationId);
        if (replacement is null)
        {
            return ProblemResults.NotAuthenticated(context);
        }

        await auditRepository.RecordAsync(
            IdentityAuditEvent.Succeeded(
                "OrganizationContextChanged",
                replacement.UserId,
                membership.OrganizationId,
                CorrelationIdAccessor.Get(context)),
            cancellationToken);
        SessionEndpointSupport.SetSessionCookie(context, replacement);
        return TypedResults.Ok(SessionContextService.ToActiveResponse(replacement, membership));
    }

    private static async Task<IResult> RevokeSession(
        HttpContext context,
        SessionContextService contextService,
        SessionStore sessionStore,
        AuditEventRepository auditRepository,
        CancellationToken cancellationToken)
    {
        SessionRequirementResult requirement = await SessionEndpointSupport.RequireAuthenticatedAsync(
            context,
            contextService,
            cancellationToken);
        if (requirement.Failure is not null)
        {
            return requirement.Failure;
        }

        SessionResolution resolution = requirement.Resolution;
        var revoked = sessionStore.Revoke(resolution.Session!.SessionId, "session_revoked");
        if (revoked is null)
        {
            return ProblemResults.NotAuthenticated(context);
        }

        await auditRepository.RecordAsync(
            IdentityAuditEvent.Succeeded(
                "SessionRevoked",
                revoked.UserId,
                revoked.SelectedOrganizationId,
                CorrelationIdAccessor.Get(context)),
            cancellationToken);
        SessionEndpointSupport.DeleteSessionCookie(context);
        return TypedResults.NoContent();
    }

    private sealed record SwitchOrganizationRequest(Guid OrganizationId);
}
