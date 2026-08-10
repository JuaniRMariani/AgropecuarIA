using System.Globalization;
using System.Security.Claims;
using System.Text.Json;
using AgropecuarIA.Identity.Application;
using Microsoft.AspNetCore.Authentication;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace AgropecuarIA.Api;

public static class OidcReauthentication
{
    public const string RequestIssuedAtProperty = "agro:oidc_request_issued_at";
    public const string StrongAuthenticationProperty = "agro:oidc_strong_authentication";
    public const string ValidatedAuthenticatedAtProperty = "agro:validated_authenticated_at";
    public const string ValidatedIssuerProperty = "agro:validated_issuer";
    public const string ValidatedSubjectProperty = "agro:validated_subject";
    public const string ValidatedStrongAuthenticationProperty = "agro:validated_strong_authentication";
    public const string MfaAuthenticationContext =
        "http://schemas.openid.net/pape/policies/2007/06/multi-factor";

    private static readonly TimeSpan ClockSkew = TimeSpan.FromMinutes(1);

    public static void PrepareChallenge(
        AuthenticationProperties properties,
        DateTimeOffset issuedAtUtc,
        bool requireStrongAuthentication = false)
    {
        ArgumentNullException.ThrowIfNull(properties);
        properties.Items[RequestIssuedAtProperty] = issuedAtUtc
            .ToUnixTimeSeconds()
            .ToString(CultureInfo.InvariantCulture);
        if (requireStrongAuthentication)
        {
            properties.Items[StrongAuthenticationProperty] = bool.TrueString;
        }
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
        if (RequiresStrongAuthentication(properties))
        {
            message.AcrValues = MfaAuthenticationContext;
        }
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

    public static OidcValidatedAuthentication ValidateToken(
        ClaimsPrincipal tokenPrincipal,
        AuthenticationProperties properties,
        DateTimeOffset nowUtc)
    {
        ArgumentNullException.ThrowIfNull(tokenPrincipal);
        ArgumentNullException.ThrowIfNull(properties);

        DateTimeOffset authenticatedAtUtc = ValidateCallback(tokenPrincipal, properties, nowUtc);
        string? issuer = tokenPrincipal.FindFirstValue("iss");
        string? subject = tokenPrincipal.FindFirstValue("sub");
        if (string.IsNullOrWhiteSpace(issuer) || string.IsNullOrWhiteSpace(subject))
        {
            throw IdentityErrors.IdentityNotVerified();
        }

        bool requiresStrongAuthentication = RequiresStrongAuthentication(properties);
        bool isStrongAuthentication =
            requiresStrongAuthentication && HasStrongAuthentication(tokenPrincipal);
        if (requiresStrongAuthentication && !isStrongAuthentication)
        {
            throw IdentityErrors.IdentityNotVerified();
        }

        properties.Items[ValidatedAuthenticatedAtProperty] = authenticatedAtUtc
            .ToUnixTimeSeconds()
            .ToString(CultureInfo.InvariantCulture);
        properties.Items[ValidatedIssuerProperty] = issuer;
        properties.Items[ValidatedSubjectProperty] = subject;
        properties.Items[ValidatedStrongAuthenticationProperty] =
            isStrongAuthentication.ToString(CultureInfo.InvariantCulture);

        return new OidcValidatedAuthentication(
            issuer,
            subject,
            authenticatedAtUtc,
            isStrongAuthentication);
    }

    public static OidcValidatedAuthentication ReadValidatedToken(
        AuthenticationProperties? properties)
    {
        string? authenticatedAtValue = null;
        string? issuer = null;
        string? subject = null;
        string? strongValue = null;
        properties?.Items.TryGetValue(ValidatedAuthenticatedAtProperty, out authenticatedAtValue);
        properties?.Items.TryGetValue(ValidatedIssuerProperty, out issuer);
        properties?.Items.TryGetValue(ValidatedSubjectProperty, out subject);
        properties?.Items.TryGetValue(ValidatedStrongAuthenticationProperty, out strongValue);

        if (!long.TryParse(
                authenticatedAtValue,
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out long authenticatedAtUnix) ||
            string.IsNullOrWhiteSpace(issuer) ||
            string.IsNullOrWhiteSpace(subject) ||
            !bool.TryParse(strongValue, out bool isStrongAuthentication))
        {
            throw IdentityErrors.IdentityNotVerified();
        }

        try
        {
            return new OidcValidatedAuthentication(
                issuer,
                subject,
                DateTimeOffset.FromUnixTimeSeconds(authenticatedAtUnix),
                isStrongAuthentication);
        }
        catch (ArgumentOutOfRangeException)
        {
            throw IdentityErrors.IdentityNotVerified();
        }
    }

    private static bool RequiresStrongAuthentication(AuthenticationProperties properties) =>
        properties.Items.TryGetValue(StrongAuthenticationProperty, out string? value) &&
        bool.TryParse(value, out bool required) &&
        required;

    private static bool HasStrongAuthentication(ClaimsPrincipal principal)
    {
        if (!string.Equals(
                principal.FindFirstValue("acr"),
                MfaAuthenticationContext,
                StringComparison.Ordinal))
        {
            return false;
        }

        foreach (Claim claim in principal.FindAll("amr"))
        {
            if (string.Equals(claim.Value, "mfa", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (ContainsMfaJsonValue(claim.Value))
            {
                return true;
            }
        }

        return false;
    }

    private static bool ContainsMfaJsonValue(string value)
    {
        if (value.Length == 0 || value[0] != '[')
        {
            return false;
        }

        try
        {
            using JsonDocument document = JsonDocument.Parse(value);
            return document.RootElement.ValueKind == JsonValueKind.Array &&
                document.RootElement.EnumerateArray().Any(item =>
                    item.ValueKind == JsonValueKind.String &&
                    string.Equals(item.GetString(), "mfa", StringComparison.OrdinalIgnoreCase));
        }
        catch (JsonException)
        {
            return false;
        }
    }
}

public sealed record OidcValidatedAuthentication(
    string Issuer,
    string Subject,
    DateTimeOffset AuthenticatedAtUtc,
    bool IsStrongAuthentication);
