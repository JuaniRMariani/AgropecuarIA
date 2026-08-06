using System.Net;
using AgropecuarIA.Identity.Tests.Infrastructure;

namespace AgropecuarIA.Identity.Tests;

[TestClass]
[DoNotParallelize]
public sealed class DevelopmentIdentityEndpointSecurityTests
{
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
}
