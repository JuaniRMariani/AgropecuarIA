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

    private static ClaimsPrincipal PrincipalWithAuthTime(DateTimeOffset authenticatedAtUtc) =>
        PrincipalWithAuthTime(
            authenticatedAtUtc.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture));

    private static ClaimsPrincipal PrincipalWithAuthTime(string authenticatedAt) =>
        new(new ClaimsIdentity(
            [new Claim("auth_time", authenticatedAt)],
            "oidc"));

    private static void AssertIdentityNotVerified(Action action)
    {
        IdentityOperationException error = Assert.ThrowsExactly<IdentityOperationException>(action);
        Assert.AreEqual("identity.not_verified", error.Code);
    }
}
