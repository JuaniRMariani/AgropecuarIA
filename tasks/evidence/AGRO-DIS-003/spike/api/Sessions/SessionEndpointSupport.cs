using AgropecuarIA.IdentitySpike.Api.Common;

namespace AgropecuarIA.IdentitySpike.Api.Sessions;

internal static class SessionEndpointSupport
{
    internal static async Task<SessionRequirementResult> RequireAuthenticatedAsync(
        HttpContext context,
        SessionContextService contextService,
        CancellationToken cancellationToken)
    {
        SessionResolution resolution = await contextService.ResolveAsync(context, cancellationToken);
        IResult? failure = resolution.Kind switch
        {
            SessionResolutionKind.SignedOut or SessionResolutionKind.Revoked =>
                ProblemResults.NotAuthenticated(context),
            SessionResolutionKind.NoActiveMembership => ProblemResults.NoActiveMembership(context),
            SessionResolutionKind.MembershipLimitExceeded =>
                ProblemResults.MembershipLimitExceeded(context),
            _ => null
        };
        return new SessionRequirementResult(resolution, failure);
    }

    internal static async Task<SessionRequirementResult> RequireActiveAsync(
        HttpContext context,
        SessionContextService contextService,
        CancellationToken cancellationToken)
    {
        SessionRequirementResult requirement = await RequireAuthenticatedAsync(
            context,
            contextService,
            cancellationToken);
        if (requirement.Failure is null &&
            requirement.Resolution.Kind == SessionResolutionKind.SelectionRequired)
        {
            return requirement with
            {
                Failure = ProblemResults.OrganizationSelectionRequired(context)
            };
        }

        return requirement;
    }

    internal static async Task<SessionRequirementResult> RequireStepUpAsync(
        HttpContext context,
        SessionContextService contextService,
        CancellationToken cancellationToken)
    {
        SessionRequirementResult requirement = await RequireActiveAsync(
            context,
            contextService,
            cancellationToken);
        if (requirement.Failure is null &&
            !contextService.HasValidStepUp(requirement.Resolution.Session!))
        {
            return requirement with { Failure = ProblemResults.StepUpRequired(context) };
        }

        return requirement;
    }

    internal static void SetSessionCookie(HttpContext context, SessionRecord session)
    {
        context.Response.Cookies.Append(
            SessionStore.CookieName,
            session.SessionId.ToString("D"),
            new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Lax,
                Path = "/",
                Expires = session.ExpiresAt,
                IsEssential = true
            });
    }

    internal static void DeleteSessionCookie(HttpContext context) =>
        context.Response.Cookies.Delete(
            SessionStore.CookieName,
            new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Lax,
                Path = "/",
                IsEssential = true
            });
}

internal sealed record SessionRequirementResult(SessionResolution Resolution, IResult? Failure);
