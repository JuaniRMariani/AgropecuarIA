using System.Net;
using System.Text.Json;
using AgropecuarIA.Identity.Tests.Infrastructure;
using AgropecuarIA.Weather.Application;
using Npgsql;

namespace AgropecuarIA.Identity.Tests;

[TestClass]
[DoNotParallelize]
public sealed class WeatherAccessApiIntegrationTests
{
    [TestMethod]
    public async Task WeatherOwnerCanPersistRainAndOtherTenantCannotReadOrWrite()
    {
        await using IdentityApiScenario scenario = await CreateScenarioAsync();
        using BrowserSession owner = scenario.CreateBrowser();
        string csrf = await IdentityApiTestActions.SignInAsync(owner, "email-owner");
        Guid organizationId = await IdentityApiTestActions.CreateOrganizationAsync(
            owner, "Weather owner", "weather-owner-0001", csrf);
        Guid fieldId = await CreateFieldAsync(owner, organizationId, csrf);
        string path = $"/api/organizations/{organizationId:D}/fields/{fieldId:D}/weather";

        using HttpResponseMessage missingCsrf = await owner.PostAsync(path + "/rain", Rain());
        Assert.AreEqual(HttpStatusCode.BadRequest, missingCsrf.StatusCode);
        using HttpResponseMessage created = await owner.PostAsync(path + "/rain", Rain(), csrf);
        Assert.AreEqual(HttpStatusCode.Created, created.StatusCode);
        Assert.IsTrue(created.Headers.CacheControl?.NoStore);
        using HttpResponseMessage listed = await owner.GetAsync(path + "/rain");
        Assert.AreEqual(HttpStatusCode.OK, listed.StatusCode);
        using JsonDocument rows = await ReadAsync(listed);
        Assert.AreEqual(1, rows.RootElement.GetArrayLength());
        Assert.AreEqual(12.5m, rows.RootElement[0].GetProperty("amountMillimeters").GetDecimal());

        using BrowserSession stranger = scenario.CreateBrowser();
        string strangerCsrf = await IdentityApiTestActions.SignInAsync(stranger, "google-owner");
        foreach (string suffix in new[]
        {
            "/rain", "/forecast?latitude=-34&longitude=-58", "/alerts?latitude=-34&longitude=-58",
            "/suitability?activityType=siembra&windSpeedKmh=2&temperatureCelsius=20&precipitationProbability=0&precipitationMm=0&relativeHumidity=50",
        })
        {
            using HttpResponseMessage denied = await stranger.GetAsync(path + suffix);
            Assert.AreEqual(HttpStatusCode.NotFound, denied.StatusCode, suffix);
            Assert.IsTrue(denied.Headers.CacheControl?.NoStore);
        }
        using HttpResponseMessage deniedWrite = await stranger.PostAsync(path + "/rain", Rain(), strangerCsrf);
        Assert.AreEqual(HttpStatusCode.NotFound, deniedWrite.StatusCode);
        using HttpResponseMessage deniedOrg = await stranger.GetAsync($"/api/organizations/{organizationId:D}/weather/rules");
        Assert.AreEqual(HttpStatusCode.NotFound, deniedOrg.StatusCode);
        using HttpResponseMessage unchanged = await owner.GetAsync(path + "/rain");
        using JsonDocument after = await ReadAsync(unchanged);
        Assert.AreEqual(1, after.RootElement.GetArrayLength());
    }

    [TestMethod]
    public async Task WeatherRulesValidateFieldOwnershipAndUnconfiguredSuitabilityAbstains()
    {
        await using IdentityApiScenario scenario = await CreateScenarioAsync();
        using BrowserSession owner = scenario.CreateBrowser();
        string csrf = await IdentityApiTestActions.SignInAsync(owner, "email-owner");
        Guid org = await IdentityApiTestActions.CreateOrganizationAsync(owner, "Rules owner", "weather-rules-0001", csrf);
        Guid field = await CreateFieldAsync(owner, org, csrf);
        string rulesPath = $"/api/organizations/{org:D}/weather/rules";
        var rule = new { fieldId = field, activityType = "siembra", ruleName = "Synthetic fixture", maxWindSpeedKmh = 15m };
        using HttpResponseMessage missingCsrf = await owner.PostAsync(rulesPath, rule);
        Assert.AreEqual(HttpStatusCode.BadRequest, missingCsrf.StatusCode);
        using HttpResponseMessage foreignField = await owner.PostAsync(rulesPath,
            new { fieldId = Guid.NewGuid(), activityType = "siembra", ruleName = "Unowned", maxWindSpeedKmh = 15m }, csrf);
        Assert.AreEqual(HttpStatusCode.NotFound, foreignField.StatusCode);
        using HttpResponseMessage unknownField = await owner.GetAsync(rulesPath + $"?fieldId={Guid.NewGuid():D}");
        Assert.AreEqual(HttpStatusCode.NotFound, unknownField.StatusCode);

        string suitability = $"/api/organizations/{org:D}/fields/{field:D}/weather/suitability?activityType=siembra&windSpeedKmh=2&temperatureCelsius=20&precipitationProbability=0&precipitationMm=0&relativeHumidity=50";
        using HttpResponseMessage unconfigured = await owner.GetAsync(suitability);
        Assert.AreEqual(HttpStatusCode.OK, unconfigured.StatusCode);
        using JsonDocument result = await ReadAsync(unconfigured);
        Assert.AreEqual("insufficient_data", result.RootElement[0].GetProperty("status").GetString());
        Assert.IsFalse(result.RootElement[0].GetProperty("isSuitable").GetBoolean());
        Assert.AreEqual("weather.activity.rule_unconfigured", result.RootElement[0].GetProperty("reasonCode").GetString());

        using HttpResponseMessage created = await owner.PostAsync(rulesPath, rule, csrf);
        Assert.AreEqual(HttpStatusCode.Created, created.StatusCode);
        using HttpResponseMessage listed = await owner.GetAsync(rulesPath + $"?fieldId={field:D}");
        Assert.AreEqual(HttpStatusCode.OK, listed.StatusCode);
        using JsonDocument rules = await ReadAsync(listed);
        Assert.AreEqual(1, rules.RootElement.GetArrayLength());
    }

    [TestMethod]
    public async Task CatalogAndAlertMutationPoliciesDenyOrdinarySessionsByDefault()
    {
        await using IdentityApiScenario scenario = await CreateScenarioAsync();
        using BrowserSession browser = scenario.CreateBrowser();
        using HttpResponseMessage anonymousCatalog = await browser.GetAsync("/api/catalog/items");
        Assert.AreEqual(HttpStatusCode.Unauthorized, anonymousCatalog.StatusCode);
        string csrf = await IdentityApiTestActions.SignInAsync(browser, "email-owner");
        foreach (string path in new[] { "/api/catalog/ingest", "/api/catalog/publish", $"/api/catalog/rollback/{Guid.NewGuid():D}", "/api/weather/alerts/ingest" })
        {
            using HttpResponseMessage denied = await browser.PostAsync(path, new { }, csrf);
            Assert.AreEqual(HttpStatusCode.Forbidden, denied.StatusCode, path);
            Assert.IsTrue(denied.Headers.CacheControl?.NoStore);
        }
        using HttpResponseMessage items = await browser.GetAsync("/api/catalog/items");
        Assert.AreEqual(HttpStatusCode.OK, items.StatusCode);
    }

    [TestMethod]
    public async Task WeatherStorageForcesTenantIsolationAndDeniesUnscopedOrForgedSessions()
    {
        await using IdentityApiScenario scenario = await CreateScenarioAsync();
        using BrowserSession browser = scenario.CreateBrowser();
        string csrf = await IdentityApiTestActions.SignInAsync(browser, "email-owner");
        Guid org = await IdentityApiTestActions.CreateOrganizationAsync(browser, "Storage owner", "weather-storage-0001", csrf);
        Guid field = await CreateFieldAsync(browser, org, csrf);
        using HttpResponseMessage created = await browser.PostAsync(
            $"/api/organizations/{org:D}/fields/{field:D}/weather/rain", Rain(), csrf);
        Assert.AreEqual(HttpStatusCode.Created, created.StatusCode);

        await using NpgsqlConnection connection = new(scenario.ConnectionString);
        await connection.OpenAsync();
        await using (NpgsqlCommand flags = connection.CreateCommand())
        {
            flags.CommandText = """
                SELECT count(*) FROM pg_class c JOIN pg_namespace n ON n.oid = c.relnamespace
                WHERE n.nspname = 'weather' AND c.relname IN ('observed_rains', 'activity_rules')
                  AND c.relrowsecurity AND c.relforcerowsecurity
                """;
            Assert.AreEqual(2L, await flags.ExecuteScalarAsync());
        }
        await using NpgsqlTransaction transaction = await connection.BeginTransactionAsync();
        await using NpgsqlCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "SET LOCAL ROLE agro_weather_app";
        await command.ExecuteNonQueryAsync();
        command.CommandText = "SELECT count(*) FROM weather.observed_rains";
        Assert.AreEqual(0L, await command.ExecuteScalarAsync());
        command.CommandText = """
            SELECT set_config('app.current_scope_kind', 'tenant', true),
                   set_config('app.current_organization_id', @org, true),
                   set_config('app.current_actor_id', @actor, true),
                   set_config('app.current_session_id', @session, true),
                   set_config('app.current_authorization_version', @version, true)
            """;
        command.Parameters.AddWithValue("org", org.ToString("D"));
        command.Parameters.AddWithValue("actor", Guid.NewGuid().ToString("D"));
        command.Parameters.AddWithValue("session", Guid.NewGuid().ToString("D"));
        command.Parameters.AddWithValue("version", Guid.NewGuid().ToString("D"));
        await command.ExecuteNonQueryAsync();
        command.CommandText = "SELECT count(*) FROM weather.observed_rains";
        Assert.AreEqual(0L, await command.ExecuteScalarAsync());
        command.CommandText = "SELECT has_table_privilege(current_user, 'weather.weather_alerts', 'INSERT')";
        Assert.AreEqual(false, await command.ExecuteScalarAsync());
        command.CommandText = "SELECT has_table_privilege(current_user, 'weather.observed_rains', 'UPDATE')";
        Assert.AreEqual(false, await command.ExecuteScalarAsync());
    }

    [TestMethod]
    public async Task ConfiguredAlertEditorCanCreateAndCancelButStillRequiresCsrf()
    {
        await using IdentityApiScenario scenario = await CreateScenarioAsync();
        using BrowserSession original = scenario.CreateBrowser();
        _ = await IdentityApiTestActions.SignInAsync(original, "email-owner");
        Guid actor;
        await using (NpgsqlConnection connection = new(scenario.ConnectionString))
        {
            await connection.OpenAsync();
            await using NpgsqlCommand command = connection.CreateCommand();
            command.CommandText = "SELECT \"Id\" FROM identity.users";
            actor = (Guid)(await command.ExecuteScalarAsync())!;
        }
        var cookies = new Dictionary<string, string>(original.Cookies);
        scenario.RestartInTest(new Dictionary<string, string?>
        {
            ["Weather:AlertIngestionActorUserIds:0"] = actor.ToString("D"),
        });
        using BrowserSession editor = scenario.CreateBrowser(cookies);
        string csrf = await editor.GetAntiforgeryTokenAsync();
        DateTimeOffset now = DateTimeOffset.UtcNow;
        IngestCapAlertCommand alert = new("synthetic-alert", "synthetic-only", now, "actual", "Synthetic weather",
            "yellow", "observed", "Synthetic headline", "Synthetic description", null, "Synthetic area",
            "[[0,0],[0,1],[1,1],[0,0]]", 0, 1, 0, 1, now.AddMinutes(-1), now.AddHours(1));
        using HttpResponseMessage noCsrf = await editor.PostAsync("/api/weather/alerts/ingest", alert);
        Assert.AreEqual(HttpStatusCode.BadRequest, noCsrf.StatusCode);
        using HttpResponseMessage created = await editor.PostAsync("/api/weather/alerts/ingest", alert, csrf);
        Assert.AreEqual(HttpStatusCode.Created, created.StatusCode);
        using HttpResponseMessage canceled = await editor.PostAsync("/api/weather/alerts/ingest", alert with { Status = "cancel" }, csrf);
        Assert.AreEqual(HttpStatusCode.Created, canceled.StatusCode);
        using JsonDocument result = await ReadAsync(canceled);
        Assert.AreEqual("cancel", result.RootElement.GetProperty("status").GetString());
        await using NpgsqlConnection verification = new(scenario.ConnectionString);
        await verification.OpenAsync();
        await using NpgsqlCommand probe = verification.CreateCommand();
        probe.CommandText = "SELECT \"Status\" FROM weather.weather_alerts";
        Assert.AreEqual("cancel", await probe.ExecuteScalarAsync());
    }

    [TestMethod]
    public async Task RainRectificationRequiresExistingObservationInSameOrganizationAndField()
    {
        await using IdentityApiScenario scenario = await CreateScenarioAsync();
        using BrowserSession owner = scenario.CreateBrowser();
        string csrf = await IdentityApiTestActions.SignInAsync(owner, "email-owner");
        Guid org = await IdentityApiTestActions.CreateOrganizationAsync(owner, "Correction owner", "rain-correction-0001", csrf);
        Guid field = await CreateFieldAsync(owner, org, csrf);
        Guid otherOrg = await IdentityApiTestActions.CreateOrganizationAsync(owner, "Other correction owner", "rain-correction-0002", csrf);
        Guid otherField = await CreateFieldAsync(owner, otherOrg, csrf);
        string path = $"/api/organizations/{org:D}/fields/{field:D}/weather/rain";
        using HttpResponseMessage created = await owner.PostAsync(path, Rain(), csrf);
        Assert.AreEqual(HttpStatusCode.Created, created.StatusCode);
        using JsonDocument source = await ReadAsync(created);
        Guid sourceId = source.RootElement.GetProperty("id").GetGuid();
        using HttpResponseMessage unknown = await owner.PostAsync(path,
            new { observedDateUtc = DateTimeOffset.UtcNow.AddDays(-1), amountMillimeters = 10m, rectifiedFromId = Guid.NewGuid() }, csrf);
        Assert.AreEqual(HttpStatusCode.BadRequest, unknown.StatusCode);
        using HttpResponseMessage wrongScope = await owner.PostAsync(
            $"/api/organizations/{otherOrg:D}/fields/{otherField:D}/weather/rain",
            new { observedDateUtc = DateTimeOffset.UtcNow.AddDays(-1), amountMillimeters = 10m, rectifiedFromId = sourceId }, csrf);
        Assert.AreEqual(HttpStatusCode.BadRequest, wrongScope.StatusCode);
        using HttpResponseMessage rectified = await owner.PostAsync(path,
            new { observedDateUtc = DateTimeOffset.UtcNow.AddDays(-1), amountMillimeters = 10m, rectifiedFromId = sourceId }, csrf);
        Assert.AreEqual(HttpStatusCode.Created, rectified.StatusCode);
        using HttpResponseMessage listed = await owner.GetAsync(path);
        using JsonDocument rows = await ReadAsync(listed);
        Assert.AreEqual(2, rows.RootElement.GetArrayLength());
    }

    private static Task<IdentityApiScenario> CreateScenarioAsync() => IdentityApiScenario.CreateAsync(
        configuration: new Dictionary<string, string?>
        {
            ["ProductiveCore:ApplyMigrations"] = "true",
            ["Catalog:ApplyMigrations"] = "true",
            ["Weather:ApplyMigrations"] = "true",
            ["ProductiveCore:ManagementUnitCreation:Enabled"] = "true",
            ["ProductiveCore:ManagementUnitCreation:CurrentKeyVersion"] = "test-v1",
            ["ProductiveCore:ManagementUnitCreation:HmacKeys:test-v1"] = "dGVzdC1vbmx5LWlkZW1wb3RlbmN5LWhtYWMta2V5LTMyaW4=",
        });

    private static async Task<Guid> CreateFieldAsync(BrowserSession browser, Guid organization, string csrf)
    {
        using HttpResponseMessage response = await browser.PostWithIdempotencyKeyAsync(
            $"/api/organizations/{organization:D}/fields", new { displayName = "Synthetic field" }, csrf, "weather-field-synthetic-request-0001");
        Assert.AreEqual(HttpStatusCode.Created, response.StatusCode, await response.Content.ReadAsStringAsync());
        using JsonDocument json = await ReadAsync(response);
        return json.RootElement.GetProperty("fieldId").GetGuid();
    }

    private static object Rain() => new { observedDateUtc = DateTimeOffset.UtcNow.AddDays(-1), amountMillimeters = 12.5m };

    private static async Task<JsonDocument> ReadAsync(HttpResponseMessage response) =>
        await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
}
