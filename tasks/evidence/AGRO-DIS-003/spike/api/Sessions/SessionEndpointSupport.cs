using AgropecuarIA.IdentitySpike.Api.Common;

namespace AgropecuarIA.IdentitySpike.Api.Sessions;

internal static class SessionEndpointSupport
{
    internal static SessionResolution RequireAuthenticated(
        HttpContext context,
        SessionContextService contextService,
        out IResult? failure)
    {
        var resolution = contextService.Resolve(context);
        failure = resolution.Kind switch
        {
            SessionResolutionKind.SignedOut or SessionResolutionKind.Revoked =>
                ProblemResults.NotAuthenticated(context),
            SessionResolutionKind.NoActiveMembership => ProblemResults.NoActiveMembership(context),
            _ => null
        };
        return resolution;
    }

    internal static SessionResolution RequireActive(
        HttpContext context,
        SessionContextService contextService,
        out IResult? failure)
    {
        var resolution = RequireAuthenticated(context, contextService, out failure);
        if (failure is null && resolution.Kind == SessionResolutionKind.SelectionRequired)
        {
            failure = ProblemResults.OrganizationSelectionRequired(context);
        }

        return resolution;
    }

    internal static SessionResolution RequireStepUp(
        HttpContext context,
        SessionContextService contextService,
        out IResult? failure)
    {
        var resolution = RequireActive(context, contextService, out failure);
        if (failure is null && !contextService.HasValidStepUp(resolution.Session!))
        {
            failure = ProblemResults.StepUpRequired(context);
        }

        return resolution;
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
