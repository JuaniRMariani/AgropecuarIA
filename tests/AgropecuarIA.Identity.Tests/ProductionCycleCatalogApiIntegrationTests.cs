using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using AgropecuarIA.Catalog.Application;
using AgropecuarIA.Catalog.Infrastructure;
using AgropecuarIA.Identity.Tests.Infrastructure;
using AgropecuarIA.ProductiveCore.Application;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace AgropecuarIA.Identity.Tests;

[TestClass]
[DoNotParallelize]
public sealed class ProductionCycleCatalogApiIntegrationTests
{
    private static readonly DateTimeOffset StartDate = new(2026, 8, 1, 0, 0, 0, TimeSpan.Zero);
    private static readonly CatalogEditorialContext FixtureEditor = new(Guid.Parse("bc814899-f242-433a-9bc9-be166dc4f73e"),
        Guid.Parse("7e038397-d6e6-4f77-8a17-a5bd779fe1bc"), "synthetic-cycle-catalog-fixture");

    [TestMethod]
    public async Task CycleStartRequiresSessionCsrfStrictPayloadAndOwnerBeforeCatalogResolution()
    {
        var observation = new ResolutionObservation();
        await using IdentityApiScenario scenario = await CreateScenarioAsync(observation);
        using BrowserSession owner = scenario.CreateBrowser();
        (string csrf, Guid organization, Guid field, Guid fieldVersion) = await CreateOwnerFieldAsync(owner);
        string path = CyclePath(organization, field);
        var request = new { catalogCode = "MAIZ", purpose = "grano", system = "secano", startDateUtc = StartDate };
        using BrowserSession anonymous = scenario.CreateBrowser();
        using HttpResponseMessage noSession = await anonymous.PostAsync(path, request);
        Assert.AreEqual(HttpStatusCode.Unauthorized, noSession.StatusCode);
        using HttpResponseMessage noCsrf = await owner.PostAsync(path, request);
        Assert.AreEqual(HttpStatusCode.BadRequest, noCsrf.StatusCode);
        using HttpResponseMessage spoof = await owner.PostAsync(path, new
        {
            request.catalogCode,
            request.purpose,
            request.system,
            request.startDateUtc,
            catalogDisplayName = "Caller forged name",
            supportLevel = "ESPECIALIZADA_VALIDADA",
        }, csrf);
        Assert.AreEqual(HttpStatusCode.BadRequest, spoof.StatusCode);
        using HttpResponseMessage unknown = await owner.PostAsync(path, new
        {
            request.catalogCode,
            request.purpose,
            request.system,
            request.startDateUtc,
            unexpected = true,
        }, csrf);
        Assert.AreEqual(HttpStatusCode.BadRequest, unknown.StatusCode);
        using BrowserSession stranger = scenario.CreateBrowser();
        string strangerCsrf = await IdentityApiTestActions.SignInAsync(stranger, "google-owner");
        using HttpResponseMessage foreign = await stranger.PostAsync(path, request, strangerCsrf);
        await AssertProblemAsync(foreign, HttpStatusCode.NotFound, "productive_core.field_not_available");
        Guid otherOrganization = await IdentityApiTestActions.CreateOrganizationAsync(stranger, "Other cycle owner", "cycle-other-organization-0001", strangerCsrf);
        using HttpResponseMessage foreignField = await stranger.PostAsync(CyclePath(otherOrganization, field), request, strangerCsrf);
        await AssertProblemAsync(foreignField, HttpStatusCode.NotFound, "productive_core.field_not_available");
        Assert.AreEqual(0, observation.Calls);
        using HttpResponseMessage absent = await owner.PostAsync(path, request, csrf);
        await AssertProblemAsync(absent, HttpStatusCode.Conflict, "productive_core.catalog_not_published");
        using HttpResponseMessage absentStale = await owner.PostAsync(path, new
        {
            catalogCode = "MISSING",
            request.purpose,
            request.system,
            request.startDateUtc,
            expectedCatalogVersionId = Guid.NewGuid(),
        }, csrf);
        await AssertProblemAsync(absentStale, HttpStatusCode.Conflict, "productive_core.catalog_version_stale");
        Assert.AreEqual(0L, await CountCyclesAsync(scenario));
        CatalogPublishResult published = await PublishFixtureAsync(scenario, "v1", "Canonical maize");
        using HttpResponseMessage created = await owner.PostAsync(path, request, csrf);
        Assert.AreEqual(HttpStatusCode.Created, created.StatusCode, await created.Content.ReadAsStringAsync());
        ProductionCycleDto cycle = (await created.Content.ReadFromJsonAsync<ProductionCycleDto>())!;
        Assert.AreEqual("Canonical maize", cycle.CatalogDisplayName);
        Assert.AreEqual("resolved_publication", cycle.CatalogReferenceStatus);
        Assert.AreEqual(published.VersionId, cycle.CatalogSnapshot!.VersionId);
        Assert.AreEqual("verified_snapshot", cycle.CatalogSnapshot.ProvenanceStatus);
        Assert.AreEqual("FLUJO_GENERICO", cycle.SupportLevel);
        Assert.AreEqual("FLUJO_GENERICO", cycle.EffectiveSupportLevel);
        Assert.IsEmpty(cycle.Capabilities);
        Assert.HasCount(3, cycle.AbsentCapabilities);
        int resolvedCalls = observation.Calls;
        using HttpResponseMessage archived = await owner.PostWithConcurrencyAsync(
            $"/api/organizations/{organization:D}/fields/{field:D}/archive", csrf, $"\"{fieldVersion:D}\"", new string('r', 32));
        Assert.AreEqual(HttpStatusCode.OK, archived.StatusCode, await archived.Content.ReadAsStringAsync());
        using HttpResponseMessage noNewCycle = await owner.PostAsync(path, request, csrf);
        await AssertProblemAsync(noNewCycle, HttpStatusCode.Conflict, "productive_core.field_archived");
        Assert.AreEqual(resolvedCalls, observation.Calls);
        Assert.AreEqual(1L, await CountCyclesAsync(scenario));
        using HttpResponseMessage ownerHistory = await owner.GetAsync(path);
        Assert.AreEqual(HttpStatusCode.OK, ownerHistory.StatusCode);
        using HttpResponseMessage foreignHistory = await stranger.GetAsync($"/api/organizations/{organization:D}/cycles/{cycle.Id:D}/timeline");
        await AssertProblemAsync(foreignHistory, HttpStatusCode.NotFound, "productive_core.field_not_available");
    }

    [TestMethod]
    public async Task CycleCatalogErrorsHaveNoEffectsAndHistoricalReadsDoNotConsultUnavailableCatalog()
    {
        var observation = new ResolutionObservation();
        await using IdentityApiScenario scenario = await CreateScenarioAsync(observation);
        using BrowserSession owner = scenario.CreateBrowser();
        (string csrf, Guid organization, Guid field, _) = await CreateOwnerFieldAsync(owner);
        CatalogPublishResult first = await PublishFixtureAsync(scenario, "v1", "Canonical maize");
        string path = CyclePath(organization, field);
        using HttpResponseMessage missing = await owner.PostAsync(path, new
        {
            catalogCode = "MISSING",
            purpose = "grano",
            system = "secano",
            startDateUtc = StartDate,
            expectedCatalogVersionId = first.VersionId,
        }, csrf);
        await AssertProblemAsync(missing, HttpStatusCode.NotFound, "productive_core.catalog_item_not_found");
        using HttpResponseMessage stale = await owner.PostAsync(path, new
        {
            catalogCode = "MISSING",
            purpose = "grano",
            system = "secano",
            startDateUtc = StartDate,
            expectedCatalogVersionId = Guid.NewGuid(),
        }, csrf);
        await AssertProblemAsync(stale, HttpStatusCode.Conflict, "productive_core.catalog_version_stale");
        Assert.AreEqual(0L, await CountCyclesAsync(scenario));
        var request = new { catalogCode = "MAIZ", purpose = "grano", system = "secano", startDateUtc = StartDate };
        using HttpResponseMessage created = await owner.PostAsync(path, request, csrf);
        Assert.AreEqual(HttpStatusCode.Created, created.StatusCode, await created.Content.ReadAsStringAsync());
        ProductionCycleDto cycle = (await created.Content.ReadFromJsonAsync<ProductionCycleDto>())!;
        await SqlAsync(scenario, "ALTER TABLE catalog.catalog_published_versions RENAME TO unavailable_versions");
        try
        {
            using HttpResponseMessage unavailable = await owner.PostAsync(path, request, csrf);
            await AssertProblemAsync(unavailable, HttpStatusCode.ServiceUnavailable, "productive_core.catalog_unavailable");
            Assert.DoesNotContain("catalog_published_versions", await unavailable.Content.ReadAsStringAsync());
            int calls = observation.Calls;
            using HttpResponseMessage history = await owner.GetAsync($"/api/organizations/{organization:D}/cycles/{cycle.Id:D}/timeline");
            Assert.AreEqual(HttpStatusCode.OK, history.StatusCode, await history.Content.ReadAsStringAsync());
            ProductionTimelineResult timeline = (await history.Content.ReadFromJsonAsync<ProductionTimelineResult>())!;
            Assert.AreEqual(cycle.CatalogSnapshot, timeline.Cycle.CatalogSnapshot);
            using HttpResponseMessage list = await owner.GetAsync(path);
            Assert.AreEqual(HttpStatusCode.OK, list.StatusCode);
            Assert.AreEqual(calls, observation.Calls);
            Assert.AreEqual(1L, await CountCyclesAsync(scenario));
        }
        finally { await SqlAsync(scenario, "ALTER TABLE catalog.unavailable_versions RENAME TO catalog_published_versions"); }
        await SqlAsync(scenario, """
            CREATE FUNCTION productive_core.cycle_test_fault() RETURNS trigger LANGUAGE plpgsql AS $$ BEGIN RAISE EXCEPTION 'PRIVATE_CYCLE_SQL_FAILURE'; END $$;
            CREATE TRIGGER cycle_test_fault BEFORE INSERT ON productive_core.production_cycles FOR EACH ROW EXECUTE FUNCTION productive_core.cycle_test_fault();
            """);
        int callsBeforeFailure = observation.Calls;
        using HttpResponseMessage failedWrite = await owner.PostAsync(path, request, csrf);
        await AssertProblemAsync(failedWrite, HttpStatusCode.ServiceUnavailable, "productive_core.cycle_unavailable");
        Assert.DoesNotContain("PRIVATE_CYCLE_SQL_FAILURE", await failedWrite.Content.ReadAsStringAsync());
        Assert.AreEqual(callsBeforeFailure + 1, observation.Calls);
        Assert.AreEqual(1L, await CountCyclesAsync(scenario));
    }

    [TestMethod]
    public async Task CycleRetainsObservedPublicationWhenNewCatalogCommitsAfterResolutionBeforeCycleCommit()
    {
        var observation = new ResolutionObservation { PauseNextResolution = true };
        await using IdentityApiScenario scenario = await CreateScenarioAsync(observation);
        using BrowserSession owner = scenario.CreateBrowser();
        (string csrf, Guid organization, Guid field, _) = await CreateOwnerFieldAsync(owner);
        CatalogPublishResult first = await PublishFixtureAsync(scenario, "before", "Before publication");
        var request = new { catalogCode = "MAIZ", purpose = "grano", system = "secano", startDateUtc = StartDate, expectedCatalogVersionId = first.VersionId };
        Task<HttpResponseMessage> pending = owner.PostAsync(CyclePath(organization, field), request, csrf);
        CatalogPublishResult second;
        try
        {
            await observation.Resolved.Task.WaitAsync(TimeSpan.FromSeconds(20));
            second = await PublishFixtureAsync(scenario, "after", "After publication");
        }
        finally { observation.Continue.TrySetResult(); }
        using HttpResponseMessage created = await pending.WaitAsync(TimeSpan.FromSeconds(20));
        Assert.AreEqual(HttpStatusCode.Created, created.StatusCode, await created.Content.ReadAsStringAsync());
        ProductionCycleDto cycle = (await created.Content.ReadFromJsonAsync<ProductionCycleDto>())!;
        Assert.AreEqual(first.VersionId, cycle.CatalogSnapshot!.VersionId);
        Assert.AreEqual("Before publication", cycle.CatalogDisplayName);
        Assert.AreNotEqual(second.VersionId, cycle.CatalogSnapshot.VersionId);
        using HttpResponseMessage stale = await owner.PostAsync(CyclePath(organization, field), request, csrf);
        await AssertProblemAsync(stale, HttpStatusCode.Conflict, "productive_core.catalog_version_stale");
        Assert.AreEqual(1L, await CountCyclesAsync(scenario));
    }

    [TestMethod]
    public async Task LegacyPublicationProvidesRealReferenceWithoutSourceProofOrEffectiveSpecialization()
    {
        await using IdentityApiScenario scenario = await CreateScenarioAsync(applyCatalogMigrations: false);
        await using CatalogDbContext catalog = CatalogDatabase(scenario);
        await catalog.GetService<IMigrator>().MigrateAsync("20260904192627_AddPublishedCatalog");
        Guid version = Guid.NewGuid();
        Guid item = Guid.NewGuid();
        await SqlAsync(scenario, """
            INSERT INTO catalog.catalog_published_versions ("Id","VersionTag","IsActive","PublishedBy","ItemsCount","PublishedAtUtc")
                VALUES (@version,'legacy-publication',true,'legacy',1,now());
            INSERT INTO catalog.catalog_published_items ("Id","VersionId","Code","NormalizedCode","DisplayName","NormalizedDisplayName",
                "Jurisdiction","SupportLevel","Category","Synonyms","IsActive","CreatedAtUtc")
                VALUES (@item,@version,'MAIZ','maiz','Legacy maize','legacy maize','AR','ESPECIALIZADA_VALIDADA','OTROS','[]'::jsonb,true,now());
            """, ("version", version), ("item", item));
        await catalog.Database.MigrateAsync();
        using BrowserSession owner = scenario.CreateBrowser();
        (string csrf, Guid organization, Guid field, _) = await CreateOwnerFieldAsync(owner);
        using HttpResponseMessage created = await owner.PostAsync(CyclePath(organization, field), new
        {
            catalogCode = "MAIZ",
            purpose = "grano",
            system = "secano",
            startDateUtc = StartDate,
            expectedCatalogVersionId = version,
        }, csrf);
        Assert.AreEqual(HttpStatusCode.Created, created.StatusCode, await created.Content.ReadAsStringAsync());
        ProductionCycleDto cycle = (await created.Content.ReadFromJsonAsync<ProductionCycleDto>())!;
        Assert.AreEqual("resolved_publication", cycle.CatalogReferenceStatus);
        Assert.AreEqual(version, cycle.CatalogSnapshot!.VersionId);
        Assert.AreEqual(item, cycle.CatalogSnapshot.ItemId);
        Assert.AreEqual("ESPECIALIZADA_VALIDADA", cycle.CatalogSnapshot.DeclaredCatalogSupportLevel);
        Assert.AreEqual("legacy_unavailable", cycle.CatalogSnapshot.ProvenanceStatus);
        Assert.IsNull(cycle.CatalogSnapshot.SourceSnapshotId);
        Assert.IsNull(cycle.CatalogSnapshot.SourceId);
        Assert.IsNull(cycle.CatalogSnapshot.SourceHash);
        Assert.IsNull(cycle.CatalogSnapshot.SourceIngestedAtUtc);
        Assert.AreEqual("FLUJO_GENERICO", cycle.SupportLevel);
        Assert.AreEqual("FLUJO_GENERICO", cycle.EffectiveSupportLevel);
        Assert.IsEmpty(cycle.Capabilities);
    }

    private static Task<IdentityApiScenario> CreateScenarioAsync(ResolutionObservation? observation = null, bool applyCatalogMigrations = true) =>
        IdentityApiScenario.CreateAsync(configuration: new Dictionary<string, string?>
        {
            ["Catalog:ApplyMigrations"] = applyCatalogMigrations.ToString(),
            ["ProductiveCore:ApplyMigrations"] = "true",
            ["ProductiveCore:ManagementUnitCreation:Enabled"] = "true",
            ["ProductiveCore:ManagementUnitCreation:CurrentKeyVersion"] = "test-v1",
            ["ProductiveCore:ManagementUnitCreation:HmacKeys:test-v1"] = "dGVzdC1vbmx5LWlkZW1wb3RlbmN5LWhtYWMta2V5LTMyaW4=",
            ["ProductiveCore:ManagementUnitRename:Enabled"] = "true",
            ["ProductiveCore:ManagementUnitRename:CurrentKeyVersion"] = "test-v1",
            ["ProductiveCore:ManagementUnitRename:HmacKeys:test-v1"] = "dGVzdC1vbmx5LWlkZW1wb3Rlbm5pbmctaG1hYy1rZXktMzg=",
        }, configureServices: observation is null ? null : (services, _) =>
        {
            ServiceDescriptor original = services.Single(x => x.ServiceType == typeof(IProductionCatalogResolver));
            services.Remove(original);
            services.AddScoped<IProductionCatalogResolver>(provider => new ObservedResolver(
                (IProductionCatalogResolver)ActivatorUtilities.CreateInstance(provider, original.ImplementationType!), observation));
        });

    private static async Task<(string Csrf, Guid Organization, Guid Field, Guid Version)> CreateOwnerFieldAsync(BrowserSession owner)
    {
        string csrf = await IdentityApiTestActions.SignInAsync(owner, "email-owner");
        Guid organization = await IdentityApiTestActions.CreateOrganizationAsync(owner, "Cycle catalog owner", "cycle-catalog-owner-000001", csrf);
        using HttpResponseMessage response = await owner.PostWithIdempotencyKeyAsync($"/api/organizations/{organization:D}/fields",
            new { displayName = "Synthetic cycle field" }, csrf, new string('c', 32));
        Assert.AreEqual(HttpStatusCode.Created, response.StatusCode, await response.Content.ReadAsStringAsync());
        using JsonDocument field = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return (csrf, organization, field.RootElement.GetProperty("fieldId").GetGuid(), field.RootElement.GetProperty("version").GetGuid());
    }

    private static CatalogDbContext CatalogDatabase(IdentityApiScenario scenario) => new(new DbContextOptionsBuilder<CatalogDbContext>()
        .UseNpgsql(scenario.ConnectionString, options => options.MigrationsHistoryTable("__EFMigrationsHistory", "catalog")).Options);

    private static async Task<CatalogPublishResult> PublishFixtureAsync(IdentityApiScenario scenario, string tag, string name)
    {
        await using CatalogDbContext database = CatalogDatabase(scenario);
        string payload = JsonSerializer.Serialize(new[] { new { code = "MAIZ", displayName = name } });
        await new CatalogIngestionApplicationService(database).IngestAsync(new("synthetic-cycle-fixture", Convert.ToBase64String(Encoding.UTF8.GetBytes(payload))), FixtureEditor, CancellationToken.None);
        CatalogEditorialDiffResult diff = await new CatalogDiffApplicationService(database).GenerateDiffAsync(CancellationToken.None);
        return await new CatalogPublicationApplicationService(database).PublishAsync(new(tag, diff.CandidateHash), FixtureEditor, CancellationToken.None);
    }

    private static string CyclePath(Guid organization, Guid field) => $"/api/organizations/{organization:D}/fields/{field:D}/cycles";

    private static async Task AssertProblemAsync(HttpResponseMessage response, HttpStatusCode status, string code)
    {
        Assert.AreEqual(status, response.StatusCode, await response.Content.ReadAsStringAsync());
        using JsonDocument problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.AreEqual(code, problem.RootElement.GetProperty("code").GetString());
    }

    private static async Task SqlAsync(IdentityApiScenario scenario, string sql, params (string Name, object Value)[] parameters)
    {
        await using var connection = new NpgsqlConnection(scenario.ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(sql, connection);
        foreach (var parameter in parameters) command.Parameters.AddWithValue(parameter.Name, parameter.Value);
        await command.ExecuteNonQueryAsync();
    }

    private static async Task<long> CountCyclesAsync(IdentityApiScenario scenario)
    {
        await using var connection = new NpgsqlConnection(scenario.ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand("SELECT count(*) FROM productive_core.production_cycles", connection);
        return (long)(await command.ExecuteScalarAsync())!;
    }

    private sealed class ResolutionObservation
    {
        public int Calls;
        public bool PauseNextResolution;
        public TaskCompletionSource Resolved { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Continue { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    }

    private sealed class ObservedResolver(IProductionCatalogResolver inner, ResolutionObservation observation) : IProductionCatalogResolver
    {
        public async Task<ProductionCatalogResolution> ResolveActiveAsync(string catalogCode, Guid? expectedCatalogVersionId, CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref observation.Calls);
            ProductionCatalogResolution result = await inner.ResolveActiveAsync(catalogCode, expectedCatalogVersionId, cancellationToken);
            if (result.Status == ProductionCatalogResolutionStatus.Resolved && observation.PauseNextResolution)
            {
                observation.PauseNextResolution = false;
                observation.Resolved.TrySetResult();
                await observation.Continue.Task.WaitAsync(cancellationToken);
            }
            return result;
        }
    }
}
