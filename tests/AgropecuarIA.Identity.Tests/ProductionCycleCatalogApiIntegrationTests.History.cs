using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json.Nodes;
using AgropecuarIA.Identity.Tests.Infrastructure;
using AgropecuarIA.ProductiveCore.Application;
using Microsoft.AspNetCore.WebUtilities;

namespace AgropecuarIA.Identity.Tests;

public sealed partial class ProductionCycleCatalogApiIntegrationTests
{
    [TestMethod]
    public async Task HistoryPageHttpIsBoundedContextualPrivateAndReadableAfterArchive()
    {
        var observation = new ResolutionObservation();
        // This bounded input matrix exceeds 30 requests; only this fixture gets a
        // larger active rate limit. Production defaults and limiter tests stay unchanged.
        await using IdentityApiScenario scenario = await CreateScenarioAsync(observation, productiveRateLimit: 100);
        using BrowserSession owner = scenario.CreateBrowser();
        (string csrf, Guid organization, Guid field, Guid fieldVersion) = await CreateOwnerFieldAsync(owner);
        await PublishFixtureAsync(scenario, "history", "History fixture");
        var created = new List<ProductionCycleDto>();
        for (int index = 0; index < 3; index++)
        {
            using HttpResponseMessage response = await owner.PostAsync(CyclePath(organization, field), new
            {
                catalogCode = "MAIZ", purpose = "grano", system = "secano", startDateUtc = StartDate,
            }, csrf);
            Assert.AreEqual(HttpStatusCode.Created, response.StatusCode);
            created.Add((await response.Content.ReadFromJsonAsync<ProductionCycleDto>())!);
        }
        Guid cycle = created[0].Id;
        for (int index = 0; index < 3; index++)
        {
            using HttpResponseMessage response = await owner.PostAsync($"/api/organizations/{organization:D}/cycles/{cycle:D}/events", new
            {
                eventType = "observacion", effectiveDateUtc = StartDate, notes = "Synthetic page fixture",
            }, csrf);
            Assert.AreEqual(HttpStatusCode.Created, response.StatusCode);
        }
        string listPath = CyclePath(organization, field) + "/page";
        string timelinePath = $"/api/organizations/{organization:D}/cycles/{cycle:D}/timeline/page";
        int catalogCalls = observation.Calls;
        using HttpResponseMessage responsePage = await owner.GetAsync(listPath + "?limit=2");
        Assert.AreEqual(HttpStatusCode.OK, responsePage.StatusCode);
        Assert.IsTrue(responsePage.Headers.CacheControl!.NoStore);
        Assert.IsTrue(responsePage.Headers.CacheControl.Private);
        ProductionCyclePage page = (await responsePage.Content.ReadFromJsonAsync<ProductionCyclePage>())!;
        Assert.HasCount(2, page.Items);
        Assert.IsTrue(page.HasMore);
        using HttpResponseMessage responseTail = await owner.GetAsync(listPath + "?limit=2&cursor=" + page.NextCursor);
        ProductionCyclePage tail = (await responseTail.Content.ReadFromJsonAsync<ProductionCyclePage>())!;
        Assert.HasCount(1, tail.Items);
        Assert.IsFalse(tail.HasMore);
        Assert.IsNull(tail.NextCursor);
        CollectionAssert.AreEquivalent(created.Select(item => item.Id).ToArray(), page.Items.Concat(tail.Items).Select(item => item.Id).ToArray());

        foreach ((string property, JsonNode value) in new Dictionary<string, JsonNode>
        {
            ["Version"] = JsonValue.Create(2)!, ["Kind"] = JsonValue.Create("unknown")!,
            ["OrganizationId"] = JsonValue.Create(Guid.NewGuid().ToString("D"))!,
            ["Id"] = JsonValue.Create(Guid.Empty.ToString("D"))!,
            ["RecordedAtUtc"] = JsonValue.Create("2026-08-01T00:00:00.0000001Z")!,
            ["unexpected"] = JsonValue.Create(true)!,
        })
        {
            JsonObject token = JsonNode.Parse(WebEncoders.Base64UrlDecode(page.NextCursor!))!.AsObject();
            token[property] = value;
            string changed = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token.ToJsonString()));
            using HttpResponseMessage rejected = await owner.GetAsync(listPath + "?cursor=" + changed);
            await AssertProblemAsync(rejected, HttpStatusCode.BadRequest, "productive_core.invalid_history_query");
        }

        using HttpResponseMessage emptyFieldResponse = await owner.PostWithIdempotencyKeyAsync(
            $"/api/organizations/{organization:D}/fields", new { displayName = "Empty history field" }, csrf, new string('h', 32));
        Assert.AreEqual(HttpStatusCode.Created, emptyFieldResponse.StatusCode);
        JsonObject emptyField = (await emptyFieldResponse.Content.ReadFromJsonAsync<JsonObject>())!;
        Guid emptyFieldId = emptyField["fieldId"]!.GetValue<Guid>();
        string emptyPath = CyclePath(organization, emptyFieldId) + "/page";
        using HttpResponseMessage emptyResponse = await owner.GetAsync(emptyPath);
        ProductionCyclePage emptyPage = (await emptyResponse.Content.ReadFromJsonAsync<ProductionCyclePage>())!;
        Assert.IsEmpty(emptyPage.Items);
        Assert.IsFalse(emptyPage.HasMore);
        Assert.IsNull(emptyPage.NextCursor);
        using HttpResponseMessage crossedField = await owner.GetAsync(emptyPath + "?cursor=" + page.NextCursor);
        await AssertProblemAsync(crossedField, HttpStatusCode.BadRequest, "productive_core.invalid_history_query");

        foreach (string query in new[] { "limit=0", "limit=101", "limit=-1", "limit=bad", "cursor=", "cursor=%2B", "cursor=" + new string('a', 513), "cursor=bnVsbA" })
        {
            using HttpResponseMessage invalid = await owner.GetAsync(listPath + "?" + query);
            Assert.AreEqual(HttpStatusCode.BadRequest, invalid.StatusCode, query);
        }
        using HttpResponseMessage crossedKind = await owner.GetAsync(timelinePath + "?cursor=" + page.NextCursor);
        await AssertProblemAsync(crossedKind, HttpStatusCode.BadRequest, "productive_core.invalid_history_query");
        using HttpResponseMessage eventResponse = await owner.GetAsync(timelinePath + "?limit=2");
        ProductionTimelinePage eventPage = (await eventResponse.Content.ReadFromJsonAsync<ProductionTimelinePage>())!;
        Assert.HasCount(2, eventPage.Events);
        Assert.IsTrue(eventPage.HasMore);
        Assert.AreEqual(created[0].CatalogSnapshot, eventPage.Cycle.CatalogSnapshot);
        using HttpResponseMessage crossedCycle = await owner.GetAsync($"/api/organizations/{organization:D}/cycles/{created[1].Id:D}/timeline/page?cursor=" + eventPage.NextCursor);
        await AssertProblemAsync(crossedCycle, HttpStatusCode.BadRequest, "productive_core.invalid_history_query");
        using HttpResponseMessage eventTailResponse = await owner.GetAsync(timelinePath + "?limit=2&cursor=" + eventPage.NextCursor);
        ProductionTimelinePage eventTail = (await eventTailResponse.Content.ReadFromJsonAsync<ProductionTimelinePage>())!;
        Assert.HasCount(1, eventTail.Events);
        Assert.IsFalse(eventTail.HasMore);
        Assert.IsNull(eventTail.NextCursor);

        using BrowserSession anonymous = scenario.CreateBrowser();
        using HttpResponseMessage unauthenticated = await anonymous.GetAsync(listPath);
        Assert.AreEqual(HttpStatusCode.Unauthorized, unauthenticated.StatusCode);
        using BrowserSession other = scenario.CreateBrowser();
        await IdentityApiTestActions.SignInAsync(other, "google-owner");
        using HttpResponseMessage denied = await other.GetAsync(listPath + "?cursor=" + page.NextCursor);
        await AssertProblemAsync(denied, HttpStatusCode.NotFound, "productive_core.field_not_available");
        using HttpResponseMessage deniedEvents = await other.GetAsync(timelinePath);
        await AssertProblemAsync(deniedEvents, HttpStatusCode.NotFound, "productive_core.field_not_available");
        using HttpResponseMessage missingField = await owner.GetAsync(CyclePath(organization, Guid.NewGuid()) + "/page?cursor=" + page.NextCursor);
        await AssertProblemAsync(missingField, HttpStatusCode.NotFound, "productive_core.field_not_available");
        using HttpResponseMessage missingCycle = await owner.GetAsync($"/api/organizations/{organization:D}/cycles/{Guid.NewGuid():D}/timeline/page");
        await AssertProblemAsync(missingCycle, HttpStatusCode.NotFound, "productive_core.field_not_available");

        using HttpResponseMessage archive = await owner.PostWithConcurrencyAsync(
            $"/api/organizations/{organization:D}/fields/{field:D}/archive", csrf, $"\"{fieldVersion:D}\"", new string('z', 32));
        Assert.AreEqual(HttpStatusCode.OK, archive.StatusCode);
        using HttpResponseMessage archivedHistory = await owner.GetAsync(listPath);
        Assert.AreEqual(HttpStatusCode.OK, archivedHistory.StatusCode);
        using HttpResponseMessage archivedEvents = await owner.GetAsync(timelinePath);
        Assert.AreEqual(HttpStatusCode.OK, archivedEvents.StatusCode);
        Assert.AreEqual(catalogCalls, observation.Calls);
        Assert.AreEqual(3L, await CountCyclesAsync(scenario));
        using BrowserSession retainedSession = scenario.CreateBrowser(owner.Cookies.ToDictionary(
            cookie => cookie.Key, cookie => cookie.Value, StringComparer.Ordinal));
        using HttpResponseMessage beforeRevoke = await retainedSession.GetAsync(listPath + "?cursor=" + page.NextCursor);
        Assert.AreEqual(HttpStatusCode.OK, beforeRevoke.StatusCode);
        using HttpResponseMessage revoke = await owner.PostWithoutBodyAsync("/api/identity/session/revoke", csrf);
        Assert.AreEqual(HttpStatusCode.NoContent, revoke.StatusCode);
        using HttpResponseMessage revokedContinuation = await retainedSession.GetAsync(listPath + "?cursor=" + page.NextCursor);
        Assert.AreEqual(HttpStatusCode.Unauthorized, revokedContinuation.StatusCode);
    }
}
