using System.Net;
using System.Text.Json;
using AgropecuarIA.Identity.Delivery;
using AgropecuarIA.Identity.Domain;
using AgropecuarIA.Identity.Tests.Infrastructure;
using Npgsql;

namespace AgropecuarIA.Identity.Tests;

[TestClass]
[DoNotParallelize]
public sealed class StepUpApiIntegrationTests
{
    [TestMethod]
    public async Task DevelopmentStepUpRotatesCookieAndRejectsOldSessionAndReplay()
    {
        await using var scenario = await IdentityApiScenario.CreateAsync();
        using var browser = scenario.CreateBrowser();
        string antiforgery = await IdentityApiTestActions.SignInAsync(browser, "email-owner");
        string oldSessionToken = browser.Cookies[IdentityAuthenticationDefaults.SessionCookieName];
        var oldCookies = browser.Cookies.ToDictionary(
            cookie => cookie.Key,
            cookie => cookie.Value,
            StringComparer.Ordinal);

        Guid attemptId = await StartStepUpAsync(browser, antiforgery);
        using (HttpResponseMessage complete = await browser.PostWithoutBodyAsync(
            $"/api/development/identity/step-up-attempts/{attemptId:D}/complete",
            antiforgery))
        {
            Assert.AreEqual(HttpStatusCode.OK, complete.StatusCode);
            using JsonDocument session = await JsonDocument.ParseAsync(
                await complete.Content.ReadAsStreamAsync());
            JsonElement assurance = session.RootElement.GetProperty("authentication");
            Assert.AreEqual("strong", assurance.GetProperty("level").GetString());
            Assert.AreEqual(
                StepUpPurposes.ManageAuthenticationMethods,
                assurance.GetProperty("purpose").GetString());
            Assert.AreNotEqual(JsonValueKind.Null, assurance.GetProperty("strongAuthenticatedAtUtc").ValueKind);
            Assert.AreNotEqual(JsonValueKind.Null, assurance.GetProperty("expiresAtUtc").ValueKind);
        }

        Assert.AreNotEqual(
            oldSessionToken,
            browser.Cookies[IdentityAuthenticationDefaults.SessionCookieName]);
        using (var oldBrowser = scenario.CreateBrowser(oldCookies))
        using (HttpResponseMessage staleSession = await oldBrowser.GetAsync("/api/identity/session"))
        {
            Assert.AreEqual(HttpStatusCode.Unauthorized, staleSession.StatusCode);
        }

        string rotatedAntiforgery = await browser.GetAntiforgeryTokenAsync();
        using HttpResponseMessage replay = await browser.PostWithoutBodyAsync(
            $"/api/development/identity/step-up-attempts/{attemptId:D}/complete",
            rotatedAntiforgery);
        Assert.AreEqual(HttpStatusCode.Conflict, replay.StatusCode);
        using JsonDocument problem = await JsonDocument.ParseAsync(
            await replay.Content.ReadAsStreamAsync());
        Assert.AreEqual(
            "identity.step_up_attempt_conflict",
            problem.RootElement.GetProperty("code").GetString());
    }

    [TestMethod]
    public async Task StartRequiresAntiforgeryAndRejectsUnknownPurpose()
    {
        await using var scenario = await IdentityApiScenario.CreateAsync();
        using var browser = scenario.CreateBrowser();
        string antiforgery = await IdentityApiTestActions.SignInAsync(browser, "email-owner");

        using (HttpResponseMessage missingAntiforgery = await browser.PostAsync(
            "/api/identity/step-up-attempts",
            new Dictionary<string, string>
            {
                ["purpose"] = StepUpPurposes.ManageAuthenticationMethods,
            }))
        {
            Assert.AreEqual(HttpStatusCode.BadRequest, missingAntiforgery.StatusCode);
        }

        using HttpResponseMessage invalidPurpose = await browser.PostAsync(
            "/api/identity/step-up-attempts",
            new Dictionary<string, string> { ["purpose"] = "arbitrary_action" },
            antiforgery);
        Assert.AreEqual(HttpStatusCode.BadRequest, invalidPurpose.StatusCode);
        using JsonDocument problem = await JsonDocument.ParseAsync(
            await invalidPurpose.Content.ReadAsStreamAsync());
        Assert.AreEqual(
            "identity.invalid_step_up_purpose",
            problem.RootElement.GetProperty("code").GetString());
    }

    [TestMethod]
    public async Task StepUpStartsAreRateLimitedPerSession()
    {
        await using var scenario = await IdentityApiScenario.CreateAsync();
        using var browser = scenario.CreateBrowser();
        string antiforgery = await IdentityApiTestActions.SignInAsync(browser, "email-owner");

        var statuses = new List<HttpStatusCode>();
        for (int request = 0; request < 6; request++)
        {
            using HttpResponseMessage response = await browser.PostAsync(
                "/api/identity/step-up-attempts",
                new Dictionary<string, string>
                {
                    ["purpose"] = StepUpPurposes.ManageAuthenticationMethods,
                },
                antiforgery);
            statuses.Add(response.StatusCode);
        }

        CollectionAssert.AreEqual(
            new[]
            {
                HttpStatusCode.Created,
                HttpStatusCode.Created,
                HttpStatusCode.Created,
                HttpStatusCode.Created,
                HttpStatusCode.Created,
                HttpStatusCode.TooManyRequests,
            },
            statuses);
    }

    [TestMethod]
    public async Task ConcurrentCallbacksConsumeTheAttemptExactlyOnce()
    {
        await using var scenario = await IdentityApiScenario.CreateAsync();
        using var browser = scenario.CreateBrowser();
        string antiforgery = await IdentityApiTestActions.SignInAsync(browser, "email-owner");
        Guid attemptId = await StartStepUpAsync(browser, antiforgery);
        var cookies = browser.Cookies.ToDictionary(
            cookie => cookie.Key,
            cookie => cookie.Value,
            StringComparer.Ordinal);
        using var firstBrowser = scenario.CreateBrowser(cookies);
        using var secondBrowser = scenario.CreateBrowser(cookies);

        Task<HttpResponseMessage> first = firstBrowser.PostWithoutBodyAsync(
            $"/api/development/identity/step-up-attempts/{attemptId:D}/complete",
            antiforgery);
        Task<HttpResponseMessage> second = secondBrowser.PostWithoutBodyAsync(
            $"/api/development/identity/step-up-attempts/{attemptId:D}/complete",
            antiforgery);
        HttpResponseMessage[] responses = await Task.WhenAll(first, second);
        try
        {
            HttpStatusCode[] statuses = responses
                .Select(response => response.StatusCode)
                .ToArray();

            Assert.AreEqual(
                1,
                statuses.Count(status => status == HttpStatusCode.OK),
                "Exactly one callback must consume the attempt.");

            HttpStatusCode rejectedStatus = statuses.Single(status => status != HttpStatusCode.OK);
            Assert.IsTrue(
                rejectedStatus is HttpStatusCode.Unauthorized or HttpStatusCode.Conflict,
                $"The losing callback must fail closed, but returned {(int)rejectedStatus}.");
        }
        finally
        {
            foreach (HttpResponseMessage response in responses)
            {
                response.Dispose();
            }
        }
    }

    [TestMethod]
    public async Task AttemptCannotBeCompletedFromAnotherSessionOrUser()
    {
        await using var scenario = await IdentityApiScenario.CreateAsync();
        using var initiatingBrowser = scenario.CreateBrowser();
        string initiatingAntiforgery = await IdentityApiTestActions.SignInAsync(
            initiatingBrowser,
            "email-owner");
        Guid attemptId = await StartStepUpAsync(initiatingBrowser, initiatingAntiforgery);

        using var sameUserDifferentSession = scenario.CreateBrowser();
        string sameUserAntiforgery = await IdentityApiTestActions.SignInAsync(
            sameUserDifferentSession,
            "email-owner");
        using (HttpResponseMessage wrongSession = await sameUserDifferentSession.PostWithoutBodyAsync(
            $"/api/development/identity/step-up-attempts/{attemptId:D}/complete",
            sameUserAntiforgery))
        {
            await AssertStepUpConflictAsync(wrongSession);
        }

        using var differentUser = scenario.CreateBrowser();
        string differentUserAntiforgery = await IdentityApiTestActions.SignInAsync(
            differentUser,
            "identity-owned-by-another-user");
        using HttpResponseMessage wrongUser = await differentUser.PostWithoutBodyAsync(
            $"/api/development/identity/step-up-attempts/{attemptId:D}/complete",
            differentUserAntiforgery);
        await AssertStepUpConflictAsync(wrongUser);
    }

    [TestMethod]
    public async Task RevokedInitiatingSessionCannotCompleteAttempt()
    {
        await using var scenario = await IdentityApiScenario.CreateAsync();
        using var browser = scenario.CreateBrowser();
        string antiforgery = await IdentityApiTestActions.SignInAsync(browser, "email-owner");
        Guid attemptId = await StartStepUpAsync(browser, antiforgery);
        var stolenCookies = browser.Cookies.ToDictionary(
            cookie => cookie.Key,
            cookie => cookie.Value,
            StringComparer.Ordinal);
        using var staleBrowser = scenario.CreateBrowser(stolenCookies);

        using (HttpResponseMessage revoke = await browser.PostWithoutBodyAsync(
            "/api/identity/session/revoke",
            antiforgery))
        {
            Assert.AreEqual(HttpStatusCode.NoContent, revoke.StatusCode);
        }

        using HttpResponseMessage completion = await staleBrowser.PostWithoutBodyAsync(
            $"/api/development/identity/step-up-attempts/{attemptId:D}/complete",
            antiforgery);
        Assert.AreEqual(HttpStatusCode.Unauthorized, completion.StatusCode);
    }

    [TestMethod]
    public async Task ExpiredAttemptIsRejectedByCompleteEndpoint()
    {
        await using var scenario = await IdentityApiScenario.CreateAsync();
        using var browser = scenario.CreateBrowser();
        string antiforgery = await IdentityApiTestActions.SignInAsync(browser, "email-owner");
        Guid attemptId = await StartStepUpAsync(browser, antiforgery);

        await using (var connection = new NpgsqlConnection(scenario.ConnectionString))
        {
            await connection.OpenAsync();
            await using var expire = new NpgsqlCommand(
                """
                UPDATE identity.step_up_attempts
                SET "StartedAtUtc" = now() - interval '2 minutes',
                    "ExpiresAtUtc" = now() - interval '1 minute'
                WHERE "Id" = @attemptId
                """,
                connection);
            expire.Parameters.AddWithValue("attemptId", attemptId);
            Assert.AreEqual(1, await expire.ExecuteNonQueryAsync());
        }

        using HttpResponseMessage completion = await browser.PostWithoutBodyAsync(
            $"/api/development/identity/step-up-attempts/{attemptId:D}/complete",
            antiforgery);
        await AssertStepUpConflictAsync(completion);
    }

    [TestMethod]
    public async Task CompleteRequiresAntiforgeryToken()
    {
        await using var scenario = await IdentityApiScenario.CreateAsync();
        using var browser = scenario.CreateBrowser();
        string antiforgery = await IdentityApiTestActions.SignInAsync(browser, "email-owner");
        Guid attemptId = await StartStepUpAsync(browser, antiforgery);

        using HttpResponseMessage completion = await browser.PostAsync(
            $"/api/development/identity/step-up-attempts/{attemptId:D}/complete",
            new Dictionary<string, string>());
        Assert.AreEqual(HttpStatusCode.BadRequest, completion.StatusCode);
    }

    private static async Task<Guid> StartStepUpAsync(
        BrowserSession browser,
        string antiforgery)
    {
        using HttpResponseMessage response = await browser.PostAsync(
            "/api/identity/step-up-attempts",
            new Dictionary<string, string>
            {
                ["purpose"] = StepUpPurposes.ManageAuthenticationMethods,
            },
            antiforgery);
        Assert.AreEqual(HttpStatusCode.Created, response.StatusCode);
        using JsonDocument payload = await JsonDocument.ParseAsync(
            await response.Content.ReadAsStreamAsync());
        Assert.AreEqual(
            $"/api/identity/step-up/{payload.RootElement.GetProperty("attemptId").GetGuid():D}",
            payload.RootElement.GetProperty("authorizationUrl").GetString());
        return payload.RootElement.GetProperty("attemptId").GetGuid();
    }

    private static async Task AssertStepUpConflictAsync(HttpResponseMessage response)
    {
        Assert.AreEqual(HttpStatusCode.Conflict, response.StatusCode);
        using JsonDocument problem = await JsonDocument.ParseAsync(
            await response.Content.ReadAsStreamAsync());
        Assert.AreEqual(
            "identity.step_up_attempt_conflict",
            problem.RootElement.GetProperty("code").GetString());
    }
}
