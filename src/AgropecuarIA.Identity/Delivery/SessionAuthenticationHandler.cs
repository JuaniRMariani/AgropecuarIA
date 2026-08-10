using System.Globalization;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;
using AgropecuarIA.Identity.Application;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AgropecuarIA.Identity.Delivery;

public static class IdentityAuthenticationDefaults
{
    public const string SessionScheme = "AgropecuarIA.Session";
    public const string ExternalScheme = "AgropecuarIA.External";
    public const string SessionCookieName = "__Host-agro-session";
    public const string SessionIdClaim = "agro:session_id";
    public const string AuthenticationAssuranceVerifiedClaim =
        "agro:authentication_assurance_verified";
    public const string StrongAuthenticatedAtClaim = "agro:strong_authenticated_at";
    public const string StrongAuthenticationPurposeClaim = "agro:strong_authentication_purpose";
}

public sealed class SessionAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    IdentityApplicationService identityService)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    private static readonly JsonSerializerOptions ProblemJsonOptions = new(JsonSerializerDefaults.Web);

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Cookies.TryGetValue(IdentityAuthenticationDefaults.SessionCookieName, out string? token))
        {
            return AuthenticateResult.NoResult();
        }

        AuthenticatedSession? session = await identityService.AuthenticateAsync(token, Context.RequestAborted);
        if (session is null)
        {
            return AuthenticateResult.Fail("The session is missing, expired, or revoked.");
        }

        List<Claim> claims =
        [
            new(ClaimTypes.NameIdentifier, session.UserId.ToString("D")),
            new(IdentityAuthenticationDefaults.SessionIdClaim, session.SessionId.ToString("D")),
            new("auth_time", session.AuthenticatedAtUtc.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture)),
            new(
                IdentityAuthenticationDefaults.AuthenticationAssuranceVerifiedClaim,
                session.IsAuthenticationAssuranceVerified.ToString(CultureInfo.InvariantCulture)),
        ];
        if (session.StrongAuthenticatedAtUtc is not null)
        {
            claims.Add(new Claim(
                IdentityAuthenticationDefaults.StrongAuthenticatedAtClaim,
                session.StrongAuthenticatedAtUtc.Value.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture)));
        }

        if (session.StrongAuthenticationPurpose is not null)
        {
            claims.Add(new Claim(
                IdentityAuthenticationDefaults.StrongAuthenticationPurposeClaim,
                session.StrongAuthenticationPurpose));
        }
        ClaimsIdentity identity = new(claims, Scheme.Name);
        AuthenticationTicket ticket = new(new ClaimsPrincipal(identity), Scheme.Name);
        return AuthenticateResult.Success(ticket);
    }

    protected override Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        Response.StatusCode = StatusCodes.Status401Unauthorized;
        Response.ContentType = "application/problem+json";
        return Response.WriteAsJsonAsync(
            new
            {
                type = "https://agropecuaria.local/problems/identity.session_required",
                title = "A valid session is required.",
                status = StatusCodes.Status401Unauthorized,
                code = "identity.session_required",
                correlationId = Context.TraceIdentifier,
            },
            ProblemJsonOptions,
            "application/problem+json",
            Context.RequestAborted);
    }
}

public static class AuthenticatedSessionClaims
{
    public static AuthenticatedSession Read(ClaimsPrincipal principal)
    {
        string? userIdValue = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        string? sessionIdValue = principal.FindFirstValue(IdentityAuthenticationDefaults.SessionIdClaim);
        string? authenticatedAtValue = principal.FindFirstValue("auth_time");
        string? assuranceVerifiedValue = principal.FindFirstValue(
            IdentityAuthenticationDefaults.AuthenticationAssuranceVerifiedClaim);
        string? strongAuthenticatedAtValue = principal.FindFirstValue(
            IdentityAuthenticationDefaults.StrongAuthenticatedAtClaim);
        string? strongAuthenticationPurpose = principal.FindFirstValue(
            IdentityAuthenticationDefaults.StrongAuthenticationPurposeClaim);

        if (!Guid.TryParse(userIdValue, out Guid userId) ||
            !Guid.TryParse(sessionIdValue, out Guid sessionId) ||
            !long.TryParse(authenticatedAtValue, CultureInfo.InvariantCulture, out long authenticatedAtUnix))
        {
            throw IdentityErrors.SessionRequired();
        }

        return new AuthenticatedSession(
            sessionId,
            userId,
            DateTimeOffset.FromUnixTimeSeconds(authenticatedAtUnix),
            bool.TryParse(assuranceVerifiedValue, out bool assuranceVerified) && assuranceVerified,
            long.TryParse(
                strongAuthenticatedAtValue,
                CultureInfo.InvariantCulture,
                out long strongAuthenticatedAtUnix)
                ? DateTimeOffset.FromUnixTimeSeconds(strongAuthenticatedAtUnix)
                : null,
            strongAuthenticationPurpose);
    }
}
