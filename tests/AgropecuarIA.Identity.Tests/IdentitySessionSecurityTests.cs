using System.Net;
using System.Collections.Concurrent;
using System.Diagnostics.Metrics;
using System.Security.Cryptography;
using AgropecuarIA.Identity.Delivery;
using AgropecuarIA.Identity.Tests.Infrastructure;
using Npgsql;
using System.Text.Json;

namespace AgropecuarIA.Identity.Tests;

[TestClass]
[DoNotParallelize]
public sealed class IdentitySessionSecurityTests
{
    [TestMethod]
    public async Task StateChangesRejectMissingAntiforgeryToken()
    {
        await using var scenario = await IdentityApiScenario.CreateAsync();
        using var browser = scenario.CreateBrowser();

        using (var signIn = await browser.PostAsync(
            "/api/development/identity/sign-in",
            new Dictionary<string, string> { ["fixture"] = "email-owner" }))
        {
            Assert.AreEqual(HttpStatusCode.BadRequest, signIn.StatusCode);
        }

        using var session = await browser.GetAsync("/api/identity/session");
        Assert.AreEqual(HttpStatusCode.Unauthorized, session.StatusCode);
        Assert.AreEqual("application/problem+json", session.Content.Headers.ContentType?.MediaType);
        using var problem = await JsonDocument.ParseAsync(await session.Content.ReadAsStreamAsync());
        Assert.AreEqual(
            "identity.session_required",
            problem.RootElement.GetProperty("code").GetString());
    }

    [TestMethod]
    public async Task AuthenticatedStateChangesAlsoRejectMissingAntiforgeryToken()
    {
        await using var scenario = await IdentityApiScenario.CreateAsync();
        using var browser = scenario.CreateBrowser();
        await IdentityApiTestActions.SignInAsync(browser, "email-owner");

        using (var link = await browser.PostAsync(
            "/api/identity/link-attempts",
            new Dictionary<string, string> { ["connection"] = "google" }))
        {
            Assert.AreEqual(HttpStatusCode.BadRequest, link.StatusCode);
        }

        using (var revoke = await browser.PostAsync(
            "/api/identity/session/revoke",
            new Dictionary<string, string>()))
        {
            Assert.AreEqual(HttpStatusCode.BadRequest, revoke.StatusCode);
        }

        using var session = await browser.GetAsync("/api/identity/session");
        Assert.AreEqual(HttpStatusCode.OK, session.StatusCode);
    }

    [TestMethod]
    public async Task SessionCookieUsesSecureBrowserProtections()
    {
        await using var scenario = await IdentityApiScenario.CreateAsync();
        using var browser = scenario.CreateBrowser();
        await IdentityApiTestActions.SignInAsync(browser, "email-owner");

        var sessionCookie = browser.SetCookieHeaders.Single(header =>
            header.StartsWith("__Host-agro-session=", StringComparison.Ordinal));
        StringAssert.Contains(sessionCookie, "; secure", StringComparison.OrdinalIgnoreCase);
        StringAssert.Contains(sessionCookie, "; httponly", StringComparison.OrdinalIgnoreCase);
        StringAssert.Contains(sessionCookie, "; samesite=", StringComparison.OrdinalIgnoreCase);
        StringAssert.Contains(sessionCookie, "; path=/", StringComparison.OrdinalIgnoreCase);
        Assert.IsFalse(
            sessionCookie.Contains("domain=", StringComparison.OrdinalIgnoreCase),
            "A __Host- cookie must not include Domain.");
    }

    [TestMethod]
    public async Task SessionResponsesForbidCachingAndSessionTrafficIsRateLimited()
    {
        await using var scenario = await IdentityApiScenario.CreateAsync();
        using var browser = scenario.CreateBrowser();
        await IdentityApiTestActions.SignInAsync(browser, "email-owner");

        using (var session = await browser.GetAsync("/api/identity/session"))
        {
            Assert.AreEqual(HttpStatusCode.OK, session.StatusCode);
            Assert.IsTrue(session.Headers.CacheControl?.NoStore);
            Assert.IsTrue(session.Headers.Pragma.Any(value =>
                value.Name.Equals("no-cache", StringComparison.OrdinalIgnoreCase)));
        }

        var statuses = new List<HttpStatusCode>();
        for (int request = 0; request < 35; request++)
        {
            using var response = await browser.GetAsync("/api/identity/session");
            statuses.Add(response.StatusCode);
            if (response.StatusCode == HttpStatusCode.TooManyRequests)
            {
                await AssertRateLimitProblemAsync(response);
            }
        }

        Assert.IsTrue(statuses.Contains(HttpStatusCode.TooManyRequests));
    }

    [TestMethod]
    public async Task InvalidOpaqueSessionIsRateLimitedBeforeAuthenticationWork()
    {
        await using var scenario = await IdentityApiScenario.CreateAsync();
        var invalidSession = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        using var browser = scenario.CreateBrowser(new Dictionary<string, string>
        {
            [IdentityAuthenticationDefaults.SessionCookieName] = invalidSession,
        });

        var statuses = new List<HttpStatusCode>();
        for (int request = 0; request < 35; request++)
        {
            using var response = await browser.GetAsync("/api/identity/session");
            statuses.Add(response.StatusCode);
            Assert.IsTrue(response.Headers.CacheControl?.NoStore);
            if (response.StatusCode == HttpStatusCode.TooManyRequests)
            {
                await AssertRateLimitProblemAsync(response);
            }
        }

        Assert.IsTrue(statuses.Contains(HttpStatusCode.Unauthorized));
        Assert.IsTrue(statuses.Contains(HttpStatusCode.TooManyRequests));
    }

    [TestMethod]
    public async Task SensitiveIdentityMutationWithStaleAuthenticationReturnsForbiddenProblem()
    {
        await using var scenario = await IdentityApiScenario.CreateAsync();
        using var browser = scenario.CreateBrowser();
        var antiforgeryToken = await IdentityApiTestActions.SignInAsync(browser, "email-owner");
        using var session = await IdentityApiTestActions.GetSessionAsync(browser);
        var userId = session.RootElement.GetProperty("userId").GetGuid();

        await using (var connection = new NpgsqlConnection(scenario.ConnectionString))
        {
            await connection.OpenAsync();
            await using var command = new NpgsqlCommand(
                """
                UPDATE identity.sessions
                SET "AuthenticatedAtUtc" = now() - interval '2 days'
                WHERE "UserId" = @userId
                """,
                connection);
            command.Parameters.AddWithValue("userId", userId);
            Assert.AreEqual(1, await command.ExecuteNonQueryAsync());
        }

        using var response = await browser.PostAsync(
            "/api/identity/link-attempts",
            new Dictionary<string, string> { ["connection"] = "google" },
            antiforgeryToken);

        Assert.AreEqual(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.AreEqual("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        using var problem = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        AssertClosedProblemContract(problem.RootElement);
        Assert.AreEqual(
            "identity.reauthentication_required",
            problem.RootElement.GetProperty("code").GetString());
    }

    [TestMethod]
    public async Task RevokedCookieCannotBeReusedByAStolenSession()
    {
        await using var scenario = await IdentityApiScenario.CreateAsync();
        using var browser = scenario.CreateBrowser();
        var antiforgeryToken = await IdentityApiTestActions.SignInAsync(browser, "email-owner");
        var stolenCookies = browser.Cookies.ToDictionary(
            cookie => cookie.Key,
            cookie => cookie.Value,
            StringComparer.Ordinal);
        using var stolenBrowser = scenario.CreateBrowser(stolenCookies);

        using (var beforeRevocation = await stolenBrowser.GetAsync("/api/identity/session"))
        {
            Assert.AreEqual(HttpStatusCode.OK, beforeRevocation.StatusCode);
        }

        using (var revocation = await browser.PostWithoutBodyAsync(
            "/api/identity/session/revoke",
            antiforgeryToken))
        {
            Assert.AreEqual(HttpStatusCode.NoContent, revocation.StatusCode);
        }

        using var replay = await stolenBrowser.GetAsync("/api/identity/session");
        Assert.AreEqual(HttpStatusCode.Unauthorized, replay.StatusCode);
    }

    [TestMethod]
    public async Task ProductionFailsStartupClosedWithoutOidcConfiguration()
    {
        await Assert.ThrowsExactlyAsync<Microsoft.Extensions.Options.OptionsValidationException>(
            () => IdentityApiScenario.CreateAsync(environment: "Production"));
    }

    [TestMethod]
    public async Task ProductionWithOidcConfiguredHasNoSyntheticEndpoints()
    {
        var oidcConfiguration = new Dictionary<string, string?>
        {
            ["Identity:Oidc:Authority"] = "https://identity.invalid",
            ["Identity:Oidc:ClientId"] = "test-client",
            ["Identity:Oidc:ClientSecret"] = "test-secret-not-a-real-credential",
            ["Identity:Oidc:GoogleEnabled"] = "false",
        };
        await using var scenario = await IdentityApiScenario.CreateAsync(
            environment: "Production",
            configuration: oidcConfiguration);
        using var browser = scenario.CreateBrowser();

        using (var capabilities = await browser.GetAsync("/api/identity/capabilities"))
        {
            Assert.AreEqual(HttpStatusCode.OK, capabilities.StatusCode);
            var payload = await capabilities.Content.ReadAsStringAsync();
            StringAssert.Contains(payload, "\"oidcConfigured\":true", StringComparison.Ordinal);
            StringAssert.Contains(payload, "\"developmentProviderEnabled\":false", StringComparison.Ordinal);
            StringAssert.Contains(
                payload,
                "\"id\":\"google\",\"label\":\"Google\",\"available\":false",
                StringComparison.Ordinal);
        }

        using (var disabledGoogle = await browser.GetAsync("/api/identity/login/google"))
        {
            Assert.AreEqual(HttpStatusCode.ServiceUnavailable, disabledGoogle.StatusCode);
        }

        using (var signIn = await browser.PostAsync(
            "/api/development/identity/sign-in",
            new Dictionary<string, string> { ["fixture"] = "email-owner" }))
        {
            Assert.AreEqual(HttpStatusCode.NotFound, signIn.StatusCode);
        }

        using (var verify = await browser.PostAsync(
            $"/api/development/identity/link-attempts/{Guid.NewGuid():D}/verify",
            new Dictionary<string, string> { ["fixture"] = "google-owner" }))
        {
            Assert.AreEqual(HttpStatusCode.NotFound, verify.StatusCode);
        }
    }

    [TestMethod]
    public async Task DisabledConnectionCannotStartALinkAttempt()
    {
        var configuration = new Dictionary<string, string?>
        {
            ["Identity:Oidc:GoogleEnabled"] = "false",
        };
        await using var scenario = await IdentityApiScenario.CreateAsync(configuration: configuration);
        using var browser = scenario.CreateBrowser();
        var antiforgery = await IdentityApiTestActions.SignInAsync(browser, "email-owner");

        using var response = await browser.PostAsync(
            "/api/identity/link-attempts",
            new Dictionary<string, string> { ["connection"] = "google" },
            antiforgery);

        Assert.AreEqual(HttpStatusCode.ServiceUnavailable, response.StatusCode);
    }

    [TestMethod]
    public async Task LogsDoNotContainTokensCookiesOrIdentityLabels()
    {
        var telemetryMeasurements = new ConcurrentQueue<IReadOnlyDictionary<string, object?>>();
        using var meterListener = new MeterListener();
        meterListener.InstrumentPublished = (instrument, listener) =>
        {
            if (instrument.Meter.Name == "AgropecuarIA.Identity")
            {
                listener.EnableMeasurementEvents(instrument);
            }
        };
        meterListener.SetMeasurementEventCallback<long>((_, _, tags, _) =>
        {
            var measurementTags = new Dictionary<string, object?>(StringComparer.Ordinal);
            foreach (var tag in tags)
            {
                measurementTags[tag.Key] = tag.Value;
            }

            telemetryMeasurements.Enqueue(measurementTags);
        });
        meterListener.Start();

        await using var scenario = await IdentityApiScenario.CreateAsync();
        using var browser = scenario.CreateBrowser();
        var antiforgeryToken = await IdentityApiTestActions.SignInAsync(browser, "email-owner");
        using var session = await IdentityApiTestActions.GetSessionAsync(browser);
        var sensitiveValues = new List<string> { antiforgeryToken };
        sensitiveValues.AddRange(browser.Cookies.Values);
        sensitiveValues.Add(session.RootElement.GetProperty("displayName").GetString() ?? string.Empty);
        sensitiveValues.AddRange(
            session.RootElement.GetProperty("identities")
                .EnumerateArray()
                .Select(identity => identity.GetProperty("label").GetString() ?? string.Empty));

        using (var revocation = await browser.PostWithoutBodyAsync(
            "/api/identity/session/revoke",
            antiforgeryToken))
        {
            Assert.AreEqual(HttpStatusCode.NoContent, revocation.StatusCode);
        }

        var logs = string.Join(Environment.NewLine, scenario.Logs.Entries);
        var metrics = string.Join(
            Environment.NewLine,
            telemetryMeasurements.SelectMany(tags => tags.Select(tag => $"{tag.Key}={tag.Value}")));
        foreach (var sensitiveValue in sensitiveValues.Where(value => value.Length >= 4))
        {
            Assert.IsFalse(
                logs.Contains(sensitiveValue, StringComparison.Ordinal),
                $"A sensitive value was written to logs: {sensitiveValue[..Math.Min(3, sensitiveValue.Length)]}***");
            Assert.IsFalse(
                metrics.Contains(sensitiveValue, StringComparison.Ordinal),
                "A sensitive value was attached to an identity metric.");
        }

        Assert.IsNotEmpty(telemetryMeasurements, "Identity operations must emit diagnostic metrics.");
        foreach (IReadOnlyDictionary<string, object?> measurement in telemetryMeasurements)
        {
            Assert.AreEqual("1.0.0", measurement["contract.version"]);
            Assert.AreEqual("identity-api", measurement["contract.consumer"]);
        }
    }

    private static async Task AssertRateLimitProblemAsync(HttpResponseMessage response)
    {
        Assert.AreEqual("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        using var problem = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        AssertClosedProblemContract(problem.RootElement);
        Assert.AreEqual("request.rate_limited", problem.RootElement.GetProperty("code").GetString());
        Assert.AreEqual(429, problem.RootElement.GetProperty("status").GetInt32());
    }

    private static void AssertClosedProblemContract(JsonElement problem)
    {
        string[] allowedProperties = ["type", "title", "status", "code", "correlationId"];
        string[] actualProperties = problem.EnumerateObject()
            .Select(property => property.Name)
            .Order(StringComparer.Ordinal)
            .ToArray();

        CollectionAssert.AreEquivalent(
            allowedProperties,
            actualProperties,
            $"Unexpected Problem Details fields: {string.Join(", ", actualProperties)}");
    }
}
