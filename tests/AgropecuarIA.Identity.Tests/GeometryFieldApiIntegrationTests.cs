using System.Net;
using System.Text.Json;
using AgropecuarIA.Identity.Tests.Infrastructure;

namespace AgropecuarIA.Identity.Tests;

[TestClass]
[DoNotParallelize]
public sealed class GeometryFieldApiIntegrationTests
{
    private const string Boundary = """{"type":"Polygon","coordinates":[[[-61,-35],[-60.99,-35],[-60.99,-34.99],[-61,-34.99],[-61,-35]]]}""";

    [TestMethod]
    public async Task GeometryRequiresSessionCsrfOwnerAndServerCalculatedFacts()
    {
        await using IdentityApiScenario scenario = await IdentityApiScenario.CreateAsync(configuration: new Dictionary<string, string?>
        {
            ["ProductiveCore:ApplyMigrations"] = "true",
            ["ProductiveCore:ManagementUnitCreation:Enabled"] = "true",
            ["ProductiveCore:ManagementUnitCreation:CurrentKeyVersion"] = "test-v1",
            ["ProductiveCore:ManagementUnitCreation:HmacKeys:test-v1"] = "dGVzdC1vbmx5LWlkZW1wb3RlbmN5LWhtYWMta2V5LTMyaW4=",
        });
        using BrowserSession owner = scenario.CreateBrowser();
        string csrf = await IdentityApiTestActions.SignInAsync(owner, "email-owner");
        Guid organization = await IdentityApiTestActions.CreateOrganizationAsync(owner, "Geometry owner", "geometry-owner-org-000001", csrf);
        using HttpResponseMessage create = await owner.PostWithIdempotencyKeyAsync($"/api/organizations/{organization:D}/fields",
            new { displayName = "Synthetic geometry field" }, csrf, new string('g', 32));
        Assert.AreEqual(HttpStatusCode.Created, create.StatusCode, await create.Content.ReadAsStringAsync());
        using JsonDocument fieldJson = await ReadAsync(create);
        Guid field = fieldJson.RootElement.GetProperty("fieldId").GetGuid();
        string ifMatch = $"\"{fieldJson.RootElement.GetProperty("version").GetGuid():D}\"";
        string path = $"/api/organizations/{organization:D}/fields/{field:D}/geometry";
        var body = new { boundaryGeoJson = Boundary, declaredAreaHectares = 7.1234m };

        using BrowserSession anonymous = scenario.CreateBrowser();
        using HttpResponseMessage missingSession = await anonymous.PostWithIfMatchAsync(path, body, null, ifMatch);
        Assert.AreEqual(HttpStatusCode.Unauthorized, missingSession.StatusCode);
        using HttpResponseMessage anonymousRead = await anonymous.GetAsync(path);
        Assert.AreEqual(HttpStatusCode.Unauthorized, anonymousRead.StatusCode);
        using HttpResponseMessage missingCsrf = await owner.PostWithIfMatchAsync(path, body, null, ifMatch);
        Assert.AreEqual(HttpStatusCode.BadRequest, missingCsrf.StatusCode);
        using HttpResponseMessage missingVersion = await owner.PostWithIfMatchAsync(path, body, csrf, null);
        Assert.AreEqual(HttpStatusCode.BadRequest, missingVersion.StatusCode);
        using HttpResponseMessage weakVersion = await owner.PostWithIfMatchAsync(path, body, csrf, "W/" + ifMatch);
        Assert.AreEqual(HttpStatusCode.BadRequest, weakVersion.StatusCode);
        using HttpResponseMessage unconfigured = await owner.GetAsync(path);
        Assert.AreEqual(HttpStatusCode.NotFound, unconfigured.StatusCode);
        using HttpResponseMessage spoofed = await owner.PostWithIfMatchAsync(path,
            new { boundaryGeoJson = Boundary, declaredAreaHectares = 7m, calculatedAreaHectares = 999999m, centroidLatitude = 0, officialProvinceCode = "06" }, csrf, ifMatch);
        Assert.AreEqual(HttpStatusCode.BadRequest, spoofed.StatusCode);
        using HttpResponseMessage invalid = await owner.PostWithIfMatchAsync(path,
            new { boundaryGeoJson = "{\"type\":\"Point\",\"coordinates\":[0,0]}", declaredAreaHectares = 7m }, csrf, ifMatch);
        Assert.AreEqual(HttpStatusCode.BadRequest, invalid.StatusCode);
        using HttpResponseMessage oversized = await owner.PostWithIfMatchAsync(path,
            new { boundaryGeoJson = Boundary.PadRight((1024 * 1024) + 1), declaredAreaHectares = 7m }, csrf, ifMatch);
        Assert.AreEqual(HttpStatusCode.RequestEntityTooLarge, oversized.StatusCode);

        using BrowserSession stranger = scenario.CreateBrowser();
        string strangerCsrf = await IdentityApiTestActions.SignInAsync(stranger, "google-owner");
        using HttpResponseMessage foreign = await stranger.PostWithIfMatchAsync(path, body, strangerCsrf, ifMatch);
        Assert.AreEqual(HttpStatusCode.NotFound, foreign.StatusCode);
        using HttpResponseMessage stale = await owner.PostWithIfMatchAsync(path, body, csrf, $"\"{Guid.NewGuid():D}\"");
        Assert.AreEqual(HttpStatusCode.PreconditionFailed, stale.StatusCode);
        using HttpResponseMessage configured = await owner.PostWithIfMatchAsync(path, body, csrf, ifMatch);
        Assert.AreEqual(HttpStatusCode.OK, configured.StatusCode, await configured.Content.ReadAsStringAsync());
        Assert.IsTrue(configured.Headers.CacheControl?.NoStore);
        using JsonDocument configuredBody = await ReadAsync(configured);
        JsonElement result = configuredBody.RootElement;
        Assert.AreEqual("configured", result.GetProperty("spatialStatus").GetString());
        Assert.AreEqual(7.1234m, result.GetProperty("declaredAreaHectares").GetDecimal());
        Assert.IsTrue(result.GetProperty("calculatedAreaHectares").GetDecimal() > 90m);
        Assert.AreEqual(JsonValueKind.Null, result.GetProperty("officialProvinceCode").ValueKind);
        Assert.AreEqual(JsonValueKind.Null, result.GetProperty("officialDepartmentCode").ValueKind);
        Guid snapshot = result.GetProperty("geometryVersionId").GetGuid();
        string newTag = $"\"{result.GetProperty("version").GetGuid():D}\"";
        Assert.AreEqual(newTag, configured.Headers.ETag?.Tag);
        using HttpResponseMessage current = await owner.GetAsync(path);
        Assert.AreEqual(HttpStatusCode.OK, current.StatusCode);
        Assert.IsTrue(current.Headers.CacheControl?.NoStore);
        Assert.AreEqual(newTag, current.Headers.ETag?.Tag);
        using JsonDocument currentBody = await ReadAsync(current);
        Assert.AreEqual(snapshot, currentBody.RootElement.GetProperty("geometryVersionId").GetGuid());
        using HttpResponseMessage reconfigure = await owner.PostWithIfMatchAsync(path, body, csrf, newTag);
        Assert.AreEqual(HttpStatusCode.Conflict, reconfigure.StatusCode);
        using HttpResponseMessage oldVersion = await owner.PostWithIfMatchAsync(path, body, csrf, ifMatch);
        Assert.AreEqual(HttpStatusCode.PreconditionFailed, oldVersion.StatusCode);
        using HttpResponseMessage foreignRead = await stranger.GetAsync(path);
        Assert.AreEqual(HttpStatusCode.NotFound, foreignRead.StatusCode);
    }

    private static async Task<JsonDocument> ReadAsync(HttpResponseMessage response) =>
        await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
}
