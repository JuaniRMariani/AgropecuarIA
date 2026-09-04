using System.Net;
using System.Text.Json;
using AgropecuarIA.Identity.Tests.Infrastructure;

namespace AgropecuarIA.Identity.Tests;

[TestClass]
[DoNotParallelize]
public sealed class ArchiveFieldApiIntegrationTests
{
    [TestMethod]
    public async Task ArchiveRequiresSessionCsrfOwnerAndStrongConcurrencyHeaders()
    {
        await using IdentityApiScenario scenario = await CreateScenarioAsync();
        using BrowserSession owner = scenario.CreateBrowser();
        string csrf = await IdentityApiTestActions.SignInAsync(owner, "email-owner");
        Guid organization = await IdentityApiTestActions.CreateOrganizationAsync(owner, "Archive owner", "archive-org-owner-0001", csrf);
        (Guid field, Guid version) = await CreateFieldAsync(owner, organization, csrf);
        string path = $"/api/organizations/{organization:D}/fields/{field:D}/archive";
        string ifMatch = $"\"{version:D}\"";
        string key = new('a', 32);

        using BrowserSession anonymous = scenario.CreateBrowser();
        using HttpResponseMessage missingSession = await anonymous.PostWithConcurrencyAsync(path, null, ifMatch, key);
        Assert.AreEqual(HttpStatusCode.Unauthorized, missingSession.StatusCode);
        using HttpResponseMessage missingCsrf = await owner.PostWithConcurrencyAsync(path, null, ifMatch, key);
        Assert.AreEqual(HttpStatusCode.BadRequest, missingCsrf.StatusCode);
        using HttpResponseMessage missingVersion = await owner.PostWithConcurrencyAsync(path, csrf, null, key);
        Assert.AreEqual(HttpStatusCode.BadRequest, missingVersion.StatusCode);
        using HttpResponseMessage weakVersion = await owner.PostWithConcurrencyAsync(path, csrf, "W/" + ifMatch, key);
        Assert.AreEqual(HttpStatusCode.BadRequest, weakVersion.StatusCode);
        using HttpResponseMessage missingKey = await owner.PostWithConcurrencyAsync(path, csrf, ifMatch, null);
        Assert.AreEqual(HttpStatusCode.BadRequest, missingKey.StatusCode);

        using BrowserSession stranger = scenario.CreateBrowser();
        string strangerCsrf = await IdentityApiTestActions.SignInAsync(stranger, "google-owner");
        using HttpResponseMessage foreign = await stranger.PostWithConcurrencyAsync(path, strangerCsrf, ifMatch, key);
        Assert.AreEqual(HttpStatusCode.NotFound, foreign.StatusCode);
        Assert.IsTrue(foreign.Headers.CacheControl?.NoStore);

        using HttpResponseMessage stale = await owner.PostWithConcurrencyAsync(path, csrf, $"\"{Guid.NewGuid():D}\"", key);
        Assert.AreEqual(HttpStatusCode.PreconditionFailed, stale.StatusCode);
        using HttpResponseMessage archived = await owner.PostWithConcurrencyAsync(path, csrf, ifMatch, key);
        Assert.AreEqual(HttpStatusCode.OK, archived.StatusCode, await archived.Content.ReadAsStringAsync());
        Assert.IsTrue(archived.Headers.CacheControl?.NoStore);
        using JsonDocument result = await ReadAsync(archived);
        Assert.AreEqual("archived", result.RootElement.GetProperty("status").GetString());
        Assert.IsFalse(result.RootElement.GetProperty("isReplay").GetBoolean());
        string resultVersion = result.RootElement.GetProperty("version").GetString()!;
        Assert.AreEqual($"\"{resultVersion}\"", archived.Headers.ETag?.Tag);

        using HttpResponseMessage replay = await owner.PostWithConcurrencyAsync(path, csrf, ifMatch, key);
        Assert.AreEqual(HttpStatusCode.OK, replay.StatusCode);
        using JsonDocument replayBody = await ReadAsync(replay);
        Assert.IsTrue(replayBody.RootElement.GetProperty("isReplay").GetBoolean());
        Assert.AreEqual(resultVersion, replayBody.RootElement.GetProperty("version").GetString());
    }

    private static Task<IdentityApiScenario> CreateScenarioAsync() => IdentityApiScenario.CreateAsync(
        configuration: new Dictionary<string, string?>
        {
            ["ProductiveCore:ApplyMigrations"] = "true",
            ["ProductiveCore:ManagementUnitCreation:Enabled"] = "true",
            ["ProductiveCore:ManagementUnitCreation:CurrentKeyVersion"] = "test-v1",
            ["ProductiveCore:ManagementUnitCreation:HmacKeys:test-v1"] = "dGVzdC1vbmx5LWlkZW1wb3RlbmN5LWhtYWMta2V5LTMyaW4=",
            ["ProductiveCore:ManagementUnitRename:Enabled"] = "true",
            ["ProductiveCore:ManagementUnitRename:CurrentKeyVersion"] = "test-v1",
            ["ProductiveCore:ManagementUnitRename:HmacKeys:test-v1"] = "dGVzdC1vbmx5LWlkZW1wb3Rlbm5pbmctaG1hYy1rZXktMzg=",
        });

    private static async Task<(Guid FieldId, Guid Version)> CreateFieldAsync(BrowserSession browser, Guid organization, string csrf)
    {
        using HttpResponseMessage response = await browser.PostWithIdempotencyKeyAsync(
            $"/api/organizations/{organization:D}/fields", new { displayName = "Synthetic archive field" },
            csrf, "archive-create-synthetic-field-0001");
        Assert.AreEqual(HttpStatusCode.Created, response.StatusCode, await response.Content.ReadAsStringAsync());
        using JsonDocument json = await ReadAsync(response);
        return (json.RootElement.GetProperty("fieldId").GetGuid(), json.RootElement.GetProperty("version").GetGuid());
    }

    private static async Task<JsonDocument> ReadAsync(HttpResponseMessage response) =>
        await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
}
