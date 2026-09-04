using System.Text;
using AgropecuarIA.Catalog.Application;
using AgropecuarIA.Catalog.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;

namespace AgropecuarIA.Catalog.Tests;

[TestClass]
[DoNotParallelize]
public sealed class CatalogEditorialDatabaseTests
{
    private const string FirstSource = """[{"code":"MAIZ","displayName":"Maíz","category":"AGRICULTURA","synonyms":["Choclo","Zea mays"]},{"code":"SOJA","displayName":"Soja"}]""";
    private const string UpdatedSource = """[{"code":"MAIZ","displayName":"Maíz actualizado","category":"AGRICULTURA","synonyms":["Choclo","Zea mays"]},{"code":"TRIGO","displayName":"Trigo"}]""";

    [TestMethod]
    public async Task LatestCompleteSourceDiffAndHistoryRetainProvenanceWithoutFalseSpecialization()
    {
        await using CatalogDatabaseScenario scenario = await CatalogDatabaseScenario.CreateAsync();
        Assert.IsTrue(await scenario.IngestAsync("fixture", FirstSource));
        CatalogEditorialDiffResult originalDiff = await scenario.DiffAsync();
        Assert.AreEqual(2, originalDiff.Added);
        Assert.AreEqual(originalDiff.CandidateHash, (await scenario.DiffAsync()).CandidateHash);
        CatalogPublishResult first = await scenario.PublishAsync("v1", originalDiff.CandidateHash);
        Assert.IsFalse(await scenario.IngestAsync("FIXTURE", FirstSource));
        Assert.IsTrue(await scenario.IngestAsync("fixture", UpdatedSource));
        CatalogEditorialDiffResult update = await scenario.DiffAsync();
        Assert.AreEqual(2, update.TotalStaged);
        Assert.AreEqual(1, update.Added);
        Assert.AreEqual(1, update.Modified);
        Assert.AreEqual(1, update.Removed);
        Assert.AreEqual(0, update.Conflicts);
        Assert.HasCount(1, update.SelectedSnapshots);
        CatalogPublishResult second = await scenario.PublishAsync("v2", update.CandidateHash);
        CatalogSearchResult current = await scenario.SearchAsync(new(Query: "CHÓCLO"));
        CatalogPublishedItemDto item = current.Items.Single();
        Assert.AreEqual("Maíz actualizado", item.DisplayName);
        Assert.AreEqual("FLUJO_GENERICO", item.SupportLevel);
        Assert.AreEqual("verified_snapshot", item.ProvenanceStatus);
        Assert.AreEqual(update.SelectedSnapshots.Single().SnapshotId, item.SourceSnapshotId);
        Assert.AreEqual(update.SelectedSnapshots.Single().ContentHash, item.SourceHash);
        Assert.HasCount(0, item.Capabilities);
        Assert.HasCount(3, item.AbsentCapabilities);
        Assert.AreEqual(second.VersionId, item.ActiveVersionId);
        CatalogSearchResult history = await scenario.SearchAsync(new(VersionId: first.VersionId));
        Assert.IsTrue(history.IsHistorical);
        Assert.AreEqual("Maíz", history.Items.Single(x => x.Code == "MAIZ").DisplayName);
        Assert.AreEqual(originalDiff.SelectedSnapshots.Single().SnapshotId, history.Items.Single(x => x.Code == "MAIZ").SourceSnapshotId);
        Assert.IsTrue(await scenario.RollbackAsync(first.VersionId));
        Assert.AreEqual(first.VersionId, (await scenario.SearchAsync()).VersionId);
        CollectionAssert.AreEqual(new long[] { 2, 4, 2, 4, 2, 5, 3, 1 }, await scenario.CountsAsync());
        await using CatalogDbContext context = scenario.OpenContext();
        CatalogVersionsResult versions = await new CatalogSearchApplicationService(context).ListVersionsAsync(1, 0, CancellationToken.None);
        Assert.AreEqual(2, versions.TotalCount);
        Assert.IsTrue(versions.HasMore);
        Assert.AreEqual(first.VersionId, versions.ActiveVersionId);
        Assert.AreEqual(second.VersionId, versions.Versions.Single().Id);
        CatalogPublishedItemDto? detail = await new CatalogSearchApplicationService(context).GetItemByCodeAsync("maíz", CancellationToken.None, first.VersionId);
        Assert.AreEqual("Maíz", detail!.DisplayName);
        Assert.AreEqual(first.PublishedAtUtc, (await context.CatalogPublishedVersions.SingleAsync(v => v.Id == first.VersionId)).PublishedAtUtc);
    }

    [TestMethod]
    [DataRow("not-json")]
    [DataRow("null")]
    [DataRow("{}")]
    [DataRow("[null]")]
    [DataRow("[{\"code\":\"X\",\"displayName\":\"\\uD800\"}]")]
    [DataRow("[{\"code\":\"OK\",\"displayName\":\"Valid\"},{\"code\":\"BAD\"}]")]
    [DataRow("[{\"code\":\"X\",\"displayName\":\"Name\",\"supportLevel\":\"ESPECIALIZADA_VALIDADA\"}]")]
    [DataRow("[{\"code\":\"X\",\"Code\":\"Y\",\"displayName\":\"Name\"}]")]
    [DataRow("[{\"code\":\"X\",\"displayName\":\"Name\",\"jurisdiction\":null}]")]
    [DataRow("[{\"code\":\"X\",\"displayName\":\"Name\",\"synonyms\":[\"Maíz\",\"MAIZ\"]}]")]
    [DataRow("[{\"code\":\"MAIZ\",\"displayName\":\"Name\"},{\"code\":\"MAÍZ\",\"displayName\":\"Other\"}]")]
    [DataRow("[{\"code\":\"MAIZ\",\"displayName\":\"Name\"},{\"code\":\"OTHER\",\"displayName\":\"Other\",\"synonyms\":[\"MAÍZ\"]}]")]
    public async Task InvalidUpdatedSourceIsRejectedWhollyAndDoesNotReplaceLastCompleteSnapshot(string corrupt)
    {
        await using CatalogDatabaseScenario scenario = await CatalogDatabaseScenario.CreateAsync();
        await scenario.IngestAsync("fixture", FirstSource);
        CatalogEditorialDiffResult original = await scenario.DiffAsync();
        long[] before = await scenario.CountsAsync();
        CatalogOperationException rejected = await Assert.ThrowsExactlyAsync<CatalogOperationException>(() => scenario.IngestAsync("fixture", corrupt));
        Assert.AreEqual("catalog.invalid_source", rejected.Code);
        CollectionAssert.AreEqual(before, await scenario.CountsAsync());
        Assert.AreEqual(original.CandidateHash, (await scenario.DiffAsync()).CandidateHash);
    }

    [TestMethod]
    public async Task EmptyCompleteSnapshotRepresentsRemovalWithoutPublishingEmptyCatalog()
    {
        await using CatalogDatabaseScenario scenario = await CatalogDatabaseScenario.CreateAsync();
        await scenario.IngestAsync("fixture", FirstSource);
        CatalogPublishResult first = await scenario.PublishAsync("v1");
        await scenario.IngestAsync("fixture", "[]");
        CatalogEditorialDiffResult empty = await scenario.DiffAsync();
        Assert.AreEqual(2, empty.Removed);
        Assert.AreEqual(0, empty.TotalStaged);
        Assert.AreEqual(0, empty.SelectedSnapshots.Single().EntryCount);
        CatalogOperationException rejected = await Assert.ThrowsExactlyAsync<CatalogOperationException>(() => scenario.PublishAsync("empty", empty.CandidateHash));
        Assert.AreEqual("catalog.empty_candidate", rejected.Code);
        Assert.AreEqual(first.VersionId, (await scenario.SearchAsync()).VersionId);
    }

    [TestMethod]
    [DataRow("[{\"code\":\"MAÍZ\",\"displayName\":\"Maíz\"}]")]
    [DataRow("[{\"code\":\"OTHER\",\"displayName\":\"Other\",\"synonyms\":[\"chóclo\"]}]")]
    public async Task CrossSourceNormalizedCodesAndAliasesAreExplicitConflicts(string other)
    {
        await using CatalogDatabaseScenario scenario = await CatalogDatabaseScenario.CreateAsync();
        await scenario.IngestAsync("first", FirstSource);
        await scenario.IngestAsync("second", other);
        CatalogEditorialDiffResult diff = await scenario.DiffAsync();
        Assert.IsTrue(diff.Conflicts > 0);
        Assert.IsTrue(diff.ConflictDetails.Count > 0);
        CatalogOperationException error = await Assert.ThrowsExactlyAsync<CatalogOperationException>(() => scenario.PublishAsync("conflict", diff.CandidateHash));
        Assert.AreEqual("catalog.candidate_conflict", error.Code);
        Assert.IsNull((await scenario.SearchAsync()).VersionId);
    }

    [TestMethod]
    public async Task CandidateHashBindsBothReviewedSourcesAndActiveVersion()
    {
        await using CatalogDatabaseScenario scenario = await CatalogDatabaseScenario.CreateAsync();
        await scenario.IngestAsync("fixture", FirstSource);
        string old = (await scenario.DiffAsync()).CandidateHash;
        await scenario.IngestAsync("fixture", UpdatedSource);
        Assert.AreEqual("catalog.candidate_stale", (await Assert.ThrowsExactlyAsync<CatalogOperationException>(() => scenario.PublishAsync("stale-source", old))).Code);
        string reviewed = (await scenario.DiffAsync()).CandidateHash;
        await scenario.PublishAsync("winner", reviewed);
        Assert.AreEqual("catalog.candidate_stale", (await Assert.ThrowsExactlyAsync<CatalogOperationException>(() => scenario.PublishAsync("stale-base", reviewed))).Code);
    }

    [TestMethod]
    public async Task PublicationAndRollbackRacesKeepOneActiveVersionAndNoLostHistory()
    {
        await using CatalogDatabaseScenario scenario = await CatalogDatabaseScenario.CreateAsync();
        await scenario.IngestAsync("fixture", FirstSource);
        string hash = (await scenario.DiffAsync()).CandidateHash;
        async Task<int> PublishAsync(string tag, string candidate)
        {
            try { await scenario.PublishAsync(tag, candidate); return 200; }
            catch (CatalogOperationException error) { return error.StatusCode; }
        }
        int[] race = await Task.WhenAll(PublishAsync("a", hash), PublishAsync("b", hash));
        Assert.AreEqual(1, race.Count(status => status == 200));
        Assert.AreEqual(1, race.Count(status => status == 409));
        Guid first = (await scenario.SearchAsync()).VersionId!.Value;
        await scenario.PublishAsync("second");
        string secondHash = (await scenario.DiffAsync()).CandidateHash;
        Task<int> publish = PublishAsync("third", secondHash);
        Task<bool> rollback = scenario.RollbackAsync(first);
        await Task.WhenAll(publish, rollback);
        Assert.IsTrue(await rollback);
        Assert.IsTrue(await publish is 200 or 409);
        Assert.AreEqual(1L, (await scenario.CountsAsync())[^1]);
        Assert.AreEqual(2, (await scenario.SearchAsync(new(VersionId: first))).TotalCount);
    }

    [TestMethod]
    [DataRow("catalog_published_versions")]
    [DataRow("catalog_published_items")]
    [DataRow("catalog_published_sources")]
    [DataRow("catalog_editorial_audits")]
    [DataRow("catalog_outbox_messages")]
    public async Task PublicationRollsBackEverySinkIncludingPriorActiveDeactivation(string table)
    {
        RequirePublicationTable(table);
        await using CatalogDatabaseScenario scenario = await CatalogDatabaseScenario.CreateAsync();
        await scenario.IngestAsync("fixture", FirstSource);
        Guid first = (await scenario.PublishAsync("first")).VersionId;
        await scenario.IngestAsync("fixture", UpdatedSource);
        string hash = (await scenario.DiffAsync()).CandidateHash;
        long[] before = await scenario.CountsAsync();
        await AddFaultAsync(scenario, table, "INSERT");
        CatalogOperationException error = await Assert.ThrowsExactlyAsync<CatalogOperationException>(() => scenario.PublishAsync("failed", hash));
        Assert.AreEqual("catalog.unavailable", error.Code);
        CollectionAssert.AreEqual(before, await scenario.CountsAsync());
        Assert.AreEqual(first, (await scenario.SearchAsync()).VersionId);
    }

    [TestMethod]
    [DataRow("catalog_published_versions")]
    [DataRow("catalog_editorial_audits")]
    [DataRow("catalog_outbox_messages")]
    public async Task RollbackPreservesActiveVersionWhenAnySinkFails(string table)
    {
        RequirePublicationTable(table);
        await using CatalogDatabaseScenario scenario = await CatalogDatabaseScenario.CreateAsync();
        await scenario.IngestAsync("fixture", FirstSource);
        Guid first = (await scenario.PublishAsync("first")).VersionId;
        Guid second = (await scenario.PublishAsync("second")).VersionId;
        long[] before = await scenario.CountsAsync();
        await AddFaultAsync(scenario, table, table == "catalog_published_versions" ? "UPDATE" : "INSERT");
        CatalogOperationException error = await Assert.ThrowsExactlyAsync<CatalogOperationException>(() => scenario.RollbackAsync(first));
        Assert.AreEqual("catalog.unavailable", error.Code);
        CollectionAssert.AreEqual(before, await scenario.CountsAsync());
        Assert.AreEqual(second, (await scenario.SearchAsync()).VersionId);
    }

    [TestMethod]
    [DataRow("catalog_source_snapshots")]
    [DataRow("catalog_staging_entries")]
    [DataRow("catalog_editorial_audits")]
    public async Task IngestionRollsBackEverySinkWithoutCreatingSuccessfulSnapshot(string table)
    {
        if (table is not ("catalog_source_snapshots" or "catalog_staging_entries" or "catalog_editorial_audits")) throw new ArgumentOutOfRangeException(nameof(table));
        await using CatalogDatabaseScenario scenario = await CatalogDatabaseScenario.CreateAsync();
        long[] before = await scenario.CountsAsync();
        await AddFaultAsync(scenario, table, "INSERT");
        Assert.AreEqual("catalog.unavailable", (await Assert.ThrowsExactlyAsync<CatalogOperationException>(() => scenario.IngestAsync("fixture", FirstSource))).Code);
        CollectionAssert.AreEqual(before, await scenario.CountsAsync());
    }

    [TestMethod]
    [DataRow("SOURCE")]
    [DataRow(" source ")]
    [DataRow("\tSOURCE\u00a0")]
    public async Task LegacySnapshotsRemainUnverifiedAndCanonicalReingestionCannotBypassReview(string source)
    {
        await using CatalogDatabaseScenario scenario = await CatalogDatabaseScenario.CreateAsync("20260904192627_AddPublishedCatalog");
        byte[] content = Encoding.UTF8.GetBytes(FirstSource);
        await scenario.ExecuteAsync("INSERT INTO catalog.catalog_source_snapshots (\"Id\",\"SourceId\",\"ContentHash\",\"CreatedAtUtc\") VALUES (@id,@source,@hash,@now)",
            ("id", Guid.NewGuid()), ("source", source), ("hash", System.Security.Cryptography.SHA256.HashData(content)), ("now", DateTimeOffset.UtcNow));
        await using CatalogDbContext context = scenario.OpenContext();
        await context.Database.MigrateAsync();
        Assert.AreEqual("catalog.legacy_snapshot_unverified", (await Assert.ThrowsExactlyAsync<CatalogOperationException>(() => scenario.IngestAsync("source", FirstSource))).Code);
        Assert.HasCount(0, (await scenario.DiffAsync()).SelectedSnapshots);
        Assert.AreEqual(1L, (await scenario.CountsAsync())[0]);
        Assert.IsFalse((await context.CatalogSourceSnapshots.SingleAsync()).IsComplete);
    }

    private static void RequirePublicationTable(string table)
    {
        if (table is not ("catalog_published_versions" or "catalog_published_items" or "catalog_published_sources" or "catalog_editorial_audits" or "catalog_outbox_messages"))
            throw new ArgumentOutOfRangeException(nameof(table));
    }

    private static Task AddFaultAsync(CatalogDatabaseScenario scenario, string table, string operation) => scenario.ExecuteAsync($"""
        CREATE FUNCTION catalog.test_sink_fault() RETURNS trigger LANGUAGE plpgsql AS $fault$ BEGIN RAISE EXCEPTION 'synthetic sink fault'; END $fault$;
        CREATE TRIGGER test_sink_fault BEFORE {operation} ON catalog.{table} FOR EACH ROW EXECUTE FUNCTION catalog.test_sink_fault();
        """);
}
