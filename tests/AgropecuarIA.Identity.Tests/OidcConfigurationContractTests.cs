using AgropecuarIA.Api;
using AgropecuarIA.Identity.Application;
using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace AgropecuarIA.Identity.Tests;

[TestClass]
public sealed class OidcConfigurationContractTests
{
    [TestMethod]
    public void OidcUsesCodePkceQueryAndUnmappedStandardClaims()
    {
        var options = new OpenIdConnectOptions();

        IdentityEndpoints.ConfigureOidc(options);

        Assert.AreEqual(OpenIdConnectResponseType.Code, options.ResponseType);
        Assert.AreEqual(OpenIdConnectResponseMode.Query, options.ResponseMode);
        Assert.IsTrue(options.UsePkce);
        Assert.IsFalse(options.MapInboundClaims);
        Assert.IsFalse(options.SaveTokens);
    }

    [TestMethod]
    public void ReauthenticationChallengeRequestsMaxAgeAndValidatesFreshAuthTime()
    {
        DateTimeOffset issuedAtUtc = new(2026, 8, 10, 12, 0, 0, TimeSpan.Zero);
        var properties = new AuthenticationProperties();
        var message = new OpenIdConnectMessage();
        OidcReauthentication.PrepareChallenge(properties, issuedAtUtc);

        OidcReauthentication.ApplyChallenge(message, properties);
        DateTimeOffset authenticatedAtUtc = issuedAtUtc.AddSeconds(1);
        ClaimsPrincipal principal = PrincipalWithAuthTime(authenticatedAtUtc);

        Assert.AreEqual("0", message.MaxAge);
        Assert.AreEqual(
            authenticatedAtUtc,
            OidcReauthentication.ValidateCallback(
                principal,
                properties,
                issuedAtUtc.AddSeconds(2)));
    }

    [TestMethod]
    public void ReauthenticationRejectsMissingStaleAndFutureAuthTime()
    {
        DateTimeOffset issuedAtUtc = new(2026, 8, 10, 12, 0, 0, TimeSpan.Zero);
        var properties = new AuthenticationProperties();
        OidcReauthentication.PrepareChallenge(properties, issuedAtUtc);

        AssertIdentityNotVerified(() => OidcReauthentication.ValidateCallback(
            new ClaimsPrincipal(new ClaimsIdentity()),
            properties,
            issuedAtUtc));
        AssertIdentityNotVerified(() => OidcReauthentication.ValidateCallback(
            PrincipalWithAuthTime("not-a-timestamp"),
            properties,
            issuedAtUtc));
        AssertIdentityNotVerified(() => OidcReauthentication.ValidateCallback(
            PrincipalWithAuthTime(long.MaxValue.ToString(CultureInfo.InvariantCulture)),
            properties,
            issuedAtUtc));
        AssertIdentityNotVerified(() => OidcReauthentication.ValidateCallback(
            PrincipalWithAuthTime(issuedAtUtc.AddMinutes(-2)),
            properties,
            issuedAtUtc));
        AssertIdentityNotVerified(() => OidcReauthentication.ValidateCallback(
            PrincipalWithAuthTime(issuedAtUtc.AddMinutes(2)),
            properties,
            issuedAtUtc));
        AssertIdentityNotVerified(() => OidcReauthentication.ValidateCallback(
            PrincipalWithAuthTime(issuedAtUtc),
            properties: null,
            issuedAtUtc));
    }

    [TestMethod]
    public void StrongAuthenticationChallengeRequiresMfaAndStoresOnlyValidatedTokenProof()
    {
        DateTimeOffset issuedAtUtc = new(2026, 8, 10, 12, 0, 0, TimeSpan.Zero);
        var properties = new AuthenticationProperties();
        OidcReauthentication.PrepareChallenge(
            properties,
            issuedAtUtc,
            requireStrongAuthentication: true);
        var message = new OpenIdConnectMessage();

        OidcReauthentication.ApplyChallenge(message, properties);
        ClaimsPrincipal principal = PrincipalWithClaims(
            issuedAtUtc.AddSeconds(1),
            OidcReauthentication.MfaAuthenticationContext,
            "mfa");
        OidcValidatedAuthentication proof = OidcReauthentication.ValidateToken(
            principal,
            properties,
            issuedAtUtc.AddSeconds(2));

        Assert.AreEqual("0", message.MaxAge);
        Assert.AreEqual(OidcReauthentication.MfaAuthenticationContext, message.AcrValues);
        Assert.IsTrue(proof.IsStrongAuthentication);
        Assert.AreEqual("https://idp.example.test/", proof.Issuer);
        Assert.AreEqual("auth0|owner", proof.Subject);
        Assert.AreEqual(proof, OidcReauthentication.ReadValidatedToken(properties));
        Assert.IsFalse(properties.Items.Values.Any(value =>
            string.Equals(value, "mfa", StringComparison.OrdinalIgnoreCase)));
    }

    [TestMethod]
    public void StrongAuthenticationRejectsMissingOrUntrustedMfaClaims()
    {
        DateTimeOffset issuedAtUtc = new(2026, 8, 10, 12, 0, 0, TimeSpan.Zero);

        AssertStrongProofRejected(issuedAtUtc, acr: null, amr: "mfa");
        AssertStrongProofRejected(issuedAtUtc, acr: OidcReauthentication.MfaAuthenticationContext, amr: null);
        AssertStrongProofRejected(issuedAtUtc, acr: OidcReauthentication.MfaAuthenticationContext, amr: "pwd");
        AssertStrongProofRejected(issuedAtUtc, acr: "urn:untrusted", amr: "mfa");
    }

    private static ClaimsPrincipal PrincipalWithAuthTime(DateTimeOffset authenticatedAtUtc) =>
        PrincipalWithAuthTime(
            authenticatedAtUtc.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture));

    private static ClaimsPrincipal PrincipalWithAuthTime(string authenticatedAt) =>
        new(new ClaimsIdentity(
            [new Claim("auth_time", authenticatedAt)],
            "oidc"));

    private static ClaimsPrincipal PrincipalWithClaims(
        DateTimeOffset authenticatedAtUtc,
        string? acr,
        string? amr)
    {
        var claims = new List<Claim>
        {
            new("auth_time", authenticatedAtUtc.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture)),
            new("iss", "https://idp.example.test/"),
            new("sub", "auth0|owner"),
        };
        if (acr is not null)
        {
            claims.Add(new Claim("acr", acr));
        }
        if (amr is not null)
        {
            claims.Add(new Claim("amr", amr));
        }

        return new ClaimsPrincipal(new ClaimsIdentity(claims, "oidc"));
    }

    private static void AssertStrongProofRejected(
        DateTimeOffset issuedAtUtc,
        string? acr,
        string? amr)
    {
        var properties = new AuthenticationProperties();
        OidcReauthentication.PrepareChallenge(
            properties,
            issuedAtUtc,
            requireStrongAuthentication: true);
        ClaimsPrincipal principal = PrincipalWithClaims(issuedAtUtc.AddSeconds(1), acr, amr);
        AssertIdentityNotVerified(() => OidcReauthentication.ValidateToken(
            principal,
            properties,
            issuedAtUtc.AddSeconds(2)));
    }

    private static void AssertIdentityNotVerified(Action action)
    {
        IdentityOperationException error = Assert.ThrowsExactly<IdentityOperationException>(action);
        Assert.AreEqual("identity.not_verified", error.Code);
    }
}
