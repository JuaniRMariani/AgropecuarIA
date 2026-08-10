using System.Globalization;
using System.Security.Claims;
using AgropecuarIA.Identity.Application;
using Microsoft.AspNetCore.Authentication;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace AgropecuarIA.Api;

public static class OidcReauthentication
{
    public const string RequestIssuedAtProperty = "agro:oidc_request_issued_at";

    private static readonly TimeSpan ClockSkew = TimeSpan.FromMinutes(1);

    public static void PrepareChallenge(
        AuthenticationProperties properties,
        DateTimeOffset issuedAtUtc)
    {
        ArgumentNullException.ThrowIfNull(properties);
        properties.Items[RequestIssuedAtProperty] = issuedAtUtc
            .ToUnixTimeSeconds()
            .ToString(CultureInfo.InvariantCulture);
    }

    public static void ApplyChallenge(
        OpenIdConnectMessage message,
        AuthenticationProperties properties)
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(properties);
        if (!properties.Items.ContainsKey(RequestIssuedAtProperty))
        {
            throw IdentityErrors.IdentityNotVerified();
        }

        message.MaxAge = "0";
    }

    public static DateTimeOffset ValidateCallback(
        ClaimsPrincipal principal,
        AuthenticationProperties? properties,
        DateTimeOffset nowUtc)
    {
        ArgumentNullException.ThrowIfNull(principal);
        string? issuedAtValue = null;
        properties?.Items.TryGetValue(RequestIssuedAtProperty, out issuedAtValue);
        string? authenticatedAtValue = principal.FindFirstValue("auth_time");

        if (!long.TryParse(
                issuedAtValue,
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out long issuedAtUnix) ||
            !long.TryParse(
                authenticatedAtValue,
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out long authenticatedAtUnix))
        {
            throw IdentityErrors.IdentityNotVerified();
        }

        DateTimeOffset issuedAtUtc;
        DateTimeOffset authenticatedAtUtc;
        try
        {
            issuedAtUtc = DateTimeOffset.FromUnixTimeSeconds(issuedAtUnix);
            authenticatedAtUtc = DateTimeOffset.FromUnixTimeSeconds(authenticatedAtUnix);
        }
        catch (ArgumentOutOfRangeException)
        {
            throw IdentityErrors.IdentityNotVerified();
        }

        if (issuedAtUtc > nowUtc.Add(ClockSkew) ||
            authenticatedAtUtc > nowUtc.Add(ClockSkew) ||
            authenticatedAtUtc < issuedAtUtc.Subtract(ClockSkew))
        {
            throw IdentityErrors.IdentityNotVerified();
        }

        return authenticatedAtUtc;
    }
}
