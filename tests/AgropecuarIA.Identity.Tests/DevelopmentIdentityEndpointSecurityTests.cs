using System.Net;
using AgropecuarIA.Identity.Tests.Infrastructure;

namespace AgropecuarIA.Identity.Tests;

[TestClass]
[DoNotParallelize]
public sealed class DevelopmentIdentityEndpointSecurityTests
{
    private const string FixtureProfileCookie = "__Host-agro.fixture-profile";

    [TestMethod]
    public async Task CandidateVerificationRequiresAnAuthenticatedSession()
    {
        await using var scenario = await IdentityApiScenario.CreateAsync();
        using var owner = scenario.CreateBrowser();
        var ownerAntiforgery = await IdentityApiTestActions.SignInAsync(owner, "email-owner");
        var attemptId = await IdentityApiTestActions.StartLinkAsync(owner, "google", ownerAntiforgery);

        using var anonymousBrowser = scenario.CreateBrowser();
        var anonymousAntiforgery = await anonymousBrowser.GetAntiforgeryTokenAsync();
        using var verification = await anonymousBrowser.PostAsync(
            $"/api/development/identity/link-attempts/{attemptId:D}/verify",
            new Dictionary<string, string> { ["fixture"] = "google-owner" },
            anonymousAntiforgery);

        Assert.AreEqual(HttpStatusCode.Unauthorized, verification.StatusCode);
    }

    [TestMethod]
    public async Task CandidateVerificationIsBoundToTheInitiatingUserAndSession()
    {
        await using var scenario = await IdentityApiScenario.CreateAsync();
        using var owner = scenario.CreateBrowser();
        var ownerAntiforgery = await IdentityApiTestActions.SignInAsync(owner, "email-owner");
        var attemptId = await IdentityApiTestActions.StartLinkAsync(owner, "google", ownerAntiforgery);

        using var differentUser = scenario.CreateBrowser();
        var differentUserAntiforgery = await IdentityApiTestActions.SignInAsync(
            differentUser,
            "identity-owned-by-another-user");
        using (var verification = await differentUser.PostAsync(
            $"/api/development/identity/link-attempts/{attemptId:D}/verify",
            new Dictionary<string, string> { ["fixture"] = "google-owner" },
            differentUserAntiforgery))
        {
            Assert.AreEqual(HttpStatusCode.Conflict, verification.StatusCode);
        }

        using var completion = await owner.PostWithoutBodyAsync(
            $"/api/identity/link-attempts/{attemptId:D}/complete",
            ownerAntiforgery);
        Assert.AreEqual(HttpStatusCode.Conflict, completion.StatusCode);
    }

    [TestMethod]
    public async Task SyntheticProfilesAreDistinctStableBoundedAndProtectedPerBrowser()
    {
        var configuration = new Dictionary<string, string?>
        {
            ["Identity:DevelopmentProvider:SyntheticProfileCount"] = "4",
        };
        await using var scenario = await IdentityApiScenario.CreateAsync(configuration: configuration);
        BrowserSession[] browsers = Enumerable.Range(0, 4)
            .Select(_ => scenario.CreateBrowser())
            .ToArray();
        try
        {
            await Task.WhenAll(
                browsers.Select(browser =>
                    IdentityApiTestActions.SignInAsync(browser, "email-owner")));
            Guid[] firstUserIds = await ReadUserIdsAsync(browsers);
            Assert.HasCount(4, firstUserIds.Distinct());

            foreach (BrowserSession browser in browsers)
            {
                string profileCookie = browser.SetCookieHeaders.Single(header =>
                    header.StartsWith($"{FixtureProfileCookie}=", StringComparison.Ordinal));
                StringAssert.Contains(profileCookie, "; secure", StringComparison.OrdinalIgnoreCase);
                StringAssert.Contains(profileCookie, "; httponly", StringComparison.OrdinalIgnoreCase);
                StringAssert.Contains(profileCookie, "; path=/", StringComparison.OrdinalIgnoreCase);
                Assert.IsFalse(profileCookie.Contains("domain=", StringComparison.OrdinalIgnoreCase));
            }

            await Task.WhenAll(
                browsers.Select(browser =>
                    IdentityApiTestActions.SignInAsync(browser, "email-owner")));
            CollectionAssert.AreEqual(firstUserIds, await ReadUserIdsAsync(browsers));

            using BrowserSession explicitProfileA = scenario.CreateBrowser();
            using BrowserSession explicitProfileB = scenario.CreateBrowser();
            await Task.WhenAll(
                IdentityApiTestActions.SignInAsync(explicitProfileA, "email-owner-1"),
                IdentityApiTestActions.SignInAsync(explicitProfileB, "email-owner-1"));
            Guid[] convergedUserIds = await ReadUserIdsAsync([explicitProfileA, explicitProfileB]);
            Assert.AreEqual(convergedUserIds[0], convergedUserIds[1]);
            CollectionAssert.Contains(firstUserIds, convergedUserIds[0]);

            using BrowserSession tampered = scenario.CreateBrowser(
                new Dictionary<string, string>
                {
                    [FixtureProfileCookie] = "4",
                });
            await IdentityApiTestActions.SignInAsync(tampered, "email-owner");
            using var tamperedSession = await IdentityApiTestActions.GetSessionAsync(tampered);
            CollectionAssert.Contains(
                firstUserIds,
                tamperedSession.RootElement.GetProperty("userId").GetGuid(),
                "An unprotected caller-provided slot must be ignored without creating a fifth identity.");

            using BrowserSession invalid = scenario.CreateBrowser();
            string antiforgery = await invalid.GetAntiforgeryTokenAsync();
            using HttpResponseMessage invalidFixture = await invalid.PostAsync(
                "/api/development/identity/sign-in",
                new Dictionary<string, string> { ["fixture"] = "email-owner-5" },
                antiforgery);
            Assert.AreEqual(HttpStatusCode.BadRequest, invalidFixture.StatusCode);
        }
        finally
        {
            foreach (BrowserSession browser in browsers)
            {
                browser.Dispose();
            }
        }
    }

    [TestMethod]
    public async Task SyntheticProfileCountOutsideAllowListFailsAtStartup()
    {
        var configuration = new Dictionary<string, string?>
        {
            ["Identity:DevelopmentProvider:SyntheticProfileCount"] = "5",
        };

        await Assert.ThrowsExactlyAsync<Microsoft.Extensions.Options.OptionsValidationException>(() =>
            IdentityApiScenario.CreateAsync(configuration: configuration));
    }

    private static async Task<Guid[]> ReadUserIdsAsync(IEnumerable<BrowserSession> browsers)
    {
        var userIds = new List<Guid>();
        foreach (BrowserSession browser in browsers)
        {
            using var session = await IdentityApiTestActions.GetSessionAsync(browser);
            userIds.Add(session.RootElement.GetProperty("userId").GetGuid());
        }

        return userIds.ToArray();
    }
}
