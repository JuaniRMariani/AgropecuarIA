using System.Text;
using System.Text.Json;
using AgropecuarIA.Catalog.Application;
using AgropecuarIA.Catalog.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;

namespace AgropecuarIA.Catalog.Tests;

[TestClass]
[DoNotParallelize]
public sealed class CatalogIntegrityDatabaseTests
{
    [TestMethod]
    [DataRow("multiple_active")]
    [DataRow("normalized_duplicate")]
    [DataRow("orphan")]
    public async Task MigrationRejectsAmbiguousHistoryWithoutSelectingOrDeletingWinner(string corruption)
    {
        await using CatalogDatabaseScenario scenario = await CatalogDatabaseScenario.CreateAsync("20260904192627_AddPublishedCatalog");
        Guid first = Guid.NewGuid();
        Guid second = Guid.NewGuid();
        await scenario.ExecuteAsync("""
            INSERT INTO catalog.catalog_published_versions ("Id","VersionTag","IsActive","PublishedBy","ItemsCount","PublishedAtUtc")
            VALUES (@first,'legacy1',true,'legacy-editor',0,now()),(@second,'legacy2',@otherActive,'legacy-editor',0,now());
            """, ("first", first), ("second", second), ("otherActive", corruption == "multiple_active"));
        if (corruption is "normalized_duplicate" or "orphan")
        {
            await InsertLegacyItemAsync(scenario, corruption == "orphan" ? Guid.NewGuid() : first, "MAIZ", "maiz");
            if (corruption == "normalized_duplicate") await InsertLegacyItemAsync(scenario, first, "MAÍZ", "maiz");
        }
        await using CatalogDbContext context = scenario.OpenContext();
        PostgresException error = await Assert.ThrowsExactlyAsync<PostgresException>(() => context.Database.MigrateAsync());
        Assert.Contains("explicit reviewed repair", error.MessageText);
        await using var connection = new NpgsqlConnection(scenario.ConnectionString);
        await connection.OpenAsync();
        await using var count = new NpgsqlCommand("SELECT count(*) FROM catalog.catalog_published_versions", connection);
        Assert.AreEqual(2L, (long)(await count.ExecuteScalarAsync())!);
        await using var migrations = new NpgsqlCommand("SELECT count(*) FROM catalog.\"__EFMigrationsHistory\" WHERE \"MigrationId\"='20260904202003_SecureCatalogPublication'", connection);
        Assert.AreEqual(0L, (long)(await migrations.ExecuteScalarAsync())!);
    }

    [TestMethod]
    public async Task HistoricalVersionsWithoutProvenanceRemainReadableAndUnverified()
    {
        await using CatalogDatabaseScenario scenario = await CatalogDatabaseScenario.CreateAsync("20260904192627_AddPublishedCatalog");
        Guid version = Guid.NewGuid();
        await scenario.ExecuteAsync("""
            INSERT INTO catalog.catalog_published_versions ("Id","VersionTag","IsActive","PublishedBy","ItemsCount","PublishedAtUtc")
            VALUES (@id,'legacy',true,'unverified-legacy-editor',1,now());
            """, ("id", version));
        await InsertLegacyItemAsync(scenario, version, "MAIZ", "maiz");
        await using CatalogDbContext context = scenario.OpenContext();
        await context.Database.MigrateAsync();
        CatalogPublishedItemDto item = (await scenario.SearchAsync()).Items.Single();
        Assert.AreEqual("legacy_unavailable", item.ProvenanceStatus);
        Assert.IsNull(item.SourceSnapshotId);
        Assert.IsNull(item.SourceId);
        Assert.IsNull(item.SourceHash);
        Assert.IsNull(item.SourceIngestedAtUtc);
        Assert.HasCount(0, item.Capabilities);
        Assert.AreEqual(version, item.VersionId);
    }

    [TestMethod]
    public async Task HistoryTablesAreAppendOnlyAndOnlyActivationMayChange()
    {
        await using CatalogDatabaseScenario scenario = await CatalogDatabaseScenario.CreateAsync();
        await scenario.IngestAsync("fixture", "[{\"code\":\"X\",\"displayName\":\"Name\"}]");
        await scenario.PublishAsync("v1");
        long[] before = await scenario.CountsAsync();
        foreach (string table in new[] { "catalog_source_snapshots", "catalog_staging_entries", "catalog_published_items", "catalog_published_sources", "catalog_editorial_audits", "catalog_outbox_messages" })
        {
            string key = table == "catalog_published_sources" ? "VersionId" : "Id";
            await Assert.ThrowsExactlyAsync<PostgresException>(() => scenario.ExecuteAsync($"UPDATE catalog.{table} SET \"{key}\"=\"{key}\""));
            await Assert.ThrowsExactlyAsync<PostgresException>(() => scenario.ExecuteAsync($"DELETE FROM catalog.{table}"));
        }
        await Assert.ThrowsExactlyAsync<PostgresException>(() => scenario.ExecuteAsync("UPDATE catalog.catalog_published_versions SET \"VersionTag\"='rewritten'"));
        await Assert.ThrowsExactlyAsync<PostgresException>(() => scenario.ExecuteAsync("DELETE FROM catalog.catalog_published_versions"));
        CollectionAssert.AreEqual(before, await scenario.CountsAsync());
        await using CatalogDbContext context = scenario.OpenContext();
        Assert.IsFalse(context.Database.HasPendingModelChanges());
    }

    [TestMethod]
    public async Task CommittedSourcesAndReleasesRejectLateChildInsertionEvenDuringActivation()
    {
        await using CatalogDatabaseScenario scenario = await CatalogDatabaseScenario.CreateAsync();
        await scenario.IngestAsync("fixture", "[{\"code\":\"X\",\"displayName\":\"Name\"}]");
        CatalogPublishResult first = await scenario.PublishAsync("v1");
        await scenario.IngestAsync("empty", "[]");
        long[] before = await scenario.CountsAsync();
        PostgresException staging = await Assert.ThrowsExactlyAsync<PostgresException>(() => scenario.ExecuteAsync("""
            INSERT INTO catalog.catalog_staging_entries
                ("Id","SourceId","SourceHash","Code","DisplayName","Jurisdiction","CreatedAtUtc","Category","NormalizedCode","SourceSnapshotId","Synonyms")
            SELECT @id,"SourceId","SourceHash",'LATE','Late','AR',now(),'OTROS','late',"SourceSnapshotId",'[]'::jsonb
            FROM catalog.catalog_staging_entries LIMIT 1
            """, ("id", Guid.NewGuid())));
        Assert.Contains("complete immutable source", staging.MessageText);
        PostgresException item = await Assert.ThrowsExactlyAsync<PostgresException>(() => scenario.ExecuteAsync("""
            INSERT INTO catalog.catalog_published_items
                ("Id","VersionId","Code","NormalizedCode","DisplayName","NormalizedDisplayName","Jurisdiction","SupportLevel","Category","Synonyms","IsActive","CreatedAtUtc","NormalizedSynonyms","SourceSnapshotId")
            SELECT @id,"VersionId",'LATE','late','Late','late','AR','FLUJO_GENERICO','OTROS','[]'::jsonb,true,now(),ARRAY[]::text[],"SourceSnapshotId"
            FROM catalog.catalog_published_items LIMIT 1
            """, ("id", Guid.NewGuid())));
        Assert.Contains("parent creation transaction", item.MessageText);
        PostgresException manifest = await Assert.ThrowsExactlyAsync<PostgresException>(() => scenario.ExecuteAsync("""
            UPDATE catalog.catalog_published_versions SET "IsActive"=false WHERE "Id"=@version;
            UPDATE catalog.catalog_published_versions SET "IsActive"=true WHERE "Id"=@version;
            INSERT INTO catalog.catalog_published_sources ("VersionId","SourceSnapshotId")
                SELECT @version,"Id" FROM catalog.catalog_source_snapshots WHERE "SourceId"='empty';
            """, ("version", first.VersionId)));
        Assert.Contains("parent creation transaction", manifest.MessageText);
        CollectionAssert.AreEqual(before, await scenario.CountsAsync());
    }

    [TestMethod]
    public async Task DirectActivationRequiresFreshAuditOutboxAndCannotLeaveNoActiveRelease()
    {
        await using CatalogDatabaseScenario scenario = await CatalogDatabaseScenario.CreateAsync();
        await scenario.IngestAsync("fixture", "[{\"code\":\"X\",\"displayName\":\"Synthetic\"}]");
        CatalogPublishResult first = await scenario.PublishAsync("v1");
        CatalogPublishResult second = await scenario.PublishAsync("v2");
        long[] before = await scenario.CountsAsync();
        PostgresException noActive = await Assert.ThrowsExactlyAsync<PostgresException>(() => scenario.ExecuteAsync(
            "UPDATE catalog.catalog_published_versions SET \"IsActive\"=false WHERE \"IsActive\""));
        Assert.Contains("new matching transaction audit", noActive.MessageText);
        PostgresException unaudited = await Assert.ThrowsExactlyAsync<PostgresException>(() => scenario.ExecuteAsync("""
            UPDATE catalog.catalog_published_versions SET "IsActive"=false WHERE "IsActive";
            UPDATE catalog.catalog_published_versions SET "IsActive"=true WHERE "Id"=@target;
            """, ("target", first.VersionId)));
        Assert.Contains("new matching transaction audit", unaudited.MessageText);
        foreach (bool correctPrevious in new[] { false, true })
        {
            PostgresException wrongPrevious = await Assert.ThrowsExactlyAsync<PostgresException>(() => scenario.ExecuteAsync("""
            UPDATE catalog.catalog_published_versions SET "IsActive"=false WHERE "IsActive";
            UPDATE catalog.catalog_published_versions SET "IsActive"=true WHERE "Id"=@target;
            INSERT INTO catalog.catalog_editorial_audits
                ("Id","Action","ActorUserId","SessionId","CorrelationId","VersionId","SourceSnapshotId","OccurredAtUtc")
                SELECT @audit,"Action","ActorUserId","SessionId","CorrelationId","VersionId","SourceSnapshotId","OccurredAtUtc"
                FROM catalog.catalog_editorial_audits WHERE "VersionId"=@target AND "Action"='catalog_published';
            INSERT INTO catalog.catalog_outbox_messages
                ("Id","EventType","SchemaVersion","Source","Scope","AggregateType","AggregateId","AuditId","ActorUserId","CorrelationId","OccurredAtUtc","PayloadJson")
                SELECT @event,"EventType","SchemaVersion","Source","Scope","AggregateType","AggregateId",@audit,"ActorUserId","CorrelationId","OccurredAtUtc",
                    CASE WHEN @correctPrevious THEN jsonb_set("PayloadJson",'{previousActiveVersionId}',to_jsonb(@previous::text)) ELSE "PayloadJson" END
                FROM catalog.catalog_outbox_messages WHERE "AggregateId"=@target AND "EventType"='ProductCatalogPublished';
            """, ("target", first.VersionId), ("audit", Guid.NewGuid()), ("event", Guid.NewGuid()), ("correctPrevious", correctPrevious), ("previous", second.VersionId)));
            Assert.Contains("new matching transaction audit", wrongPrevious.MessageText);
        }
        await scenario.ExecuteAsync("UPDATE catalog.catalog_published_versions SET \"IsActive\"=\"IsActive\"");
        CollectionAssert.AreEqual(before, await scenario.CountsAsync());
    }

    [TestMethod]
    public async Task DisposableMigrationRollbackAndForwardPreserveLegacySchemaAvailability()
    {
        await using CatalogDatabaseScenario scenario = await CatalogDatabaseScenario.CreateAsync();
        await using CatalogDbContext context = scenario.OpenContext();
        await context.GetService<IMigrator>().MigrateAsync("20260904192627_AddPublishedCatalog");
        await context.Database.MigrateAsync();
        Assert.IsFalse(context.Database.HasPendingModelChanges());
        await scenario.IngestAsync("fixture", "[]");
        Assert.HasCount(1, (await scenario.DiffAsync()).SelectedSnapshots);
    }

    [TestMethod]
    public async Task Base64Utf8PayloadAndRowLimitsRejectWithoutSuccessfulSnapshots()
    {
        await using CatalogDatabaseScenario scenario = await CatalogDatabaseScenario.CreateAsync();
        await using CatalogDbContext context = scenario.OpenContext();
        var ingestion = new CatalogIngestionApplicationService(context);
        foreach (string payload in new[] { "%%%", Convert.ToBase64String(new byte[] { 0xff, 0xfe }) })
        {
            CatalogOperationException invalid = await Assert.ThrowsExactlyAsync<CatalogOperationException>(() =>
                ingestion.IngestAsync(new("fixture", payload), CatalogDatabaseScenario.Editor, CancellationToken.None));
            Assert.AreEqual("catalog.invalid_source", invalid.Code);
        }
        string maximum = "[]".PadRight(1024 * 1024);
        Assert.IsTrue(await scenario.IngestAsync("maximum-payload", maximum));
        long[] before = await scenario.CountsAsync();
        Assert.AreEqual("catalog.source_too_large", (await Assert.ThrowsExactlyAsync<CatalogOperationException>(() => scenario.IngestAsync("too-large", maximum + " "))).Code);
        string rows = JsonSerializer.Serialize(Enumerable.Range(0, 10001).Select(i => new { code = $"C{i:D5}", displayName = "Synthetic" }));
        Assert.AreEqual("catalog.source_too_large", (await Assert.ThrowsExactlyAsync<CatalogOperationException>(() => scenario.IngestAsync("too-many-rows", rows))).Code);
        CollectionAssert.AreEqual(before, await scenario.CountsAsync());
        string acceptedRows = JsonSerializer.Serialize(Enumerable.Range(0, 10000).Select(i => new { code = $"C{i:D5}", displayName = "Synthetic" }));
        Assert.IsTrue(await scenario.IngestAsync("maximum-rows", acceptedRows));
        CatalogEditorialDiffResult diff = await scenario.DiffAsync();
        Assert.AreEqual(10000, diff.TotalStaged);
        Assert.AreEqual(2, diff.SelectedSnapshots.Count);
    }

    [TestMethod]
    public async Task SearchAndVersionsBoundsAndMissingHistoricalVersionAreExplicit()
    {
        await using CatalogDatabaseScenario scenario = await CatalogDatabaseScenario.CreateAsync();
        Assert.IsNull((await scenario.SearchAsync()).VersionId);
        Assert.AreEqual("catalog.version_not_found", (await Assert.ThrowsExactlyAsync<CatalogOperationException>(() => scenario.SearchAsync(new(VersionId: Guid.NewGuid())))).Code);
        Assert.AreEqual("catalog.invalid_request", (await Assert.ThrowsExactlyAsync<CatalogOperationException>(() => scenario.SearchAsync(new(Query: new string('x', 257))))).Code);
        Assert.AreEqual("catalog.invalid_request", (await Assert.ThrowsExactlyAsync<CatalogOperationException>(() => scenario.SearchAsync(new(Limit: 101)))).Code);
        await using CatalogDbContext context = scenario.OpenContext();
        var search = new CatalogSearchApplicationService(context);
        Assert.AreEqual("catalog.invalid_request", (await Assert.ThrowsExactlyAsync<CatalogOperationException>(() => search.ListVersionsAsync(101, 0, CancellationToken.None))).Code);
        Assert.AreEqual("catalog.invalid_request", (await Assert.ThrowsExactlyAsync<CatalogOperationException>(() => search.ListVersionsAsync(20, 10001, CancellationToken.None))).Code);
    }

    private static Task InsertLegacyItemAsync(CatalogDatabaseScenario scenario, Guid version, string code, string normalized) => scenario.ExecuteAsync("""
        INSERT INTO catalog.catalog_published_items ("Id","VersionId","Code","NormalizedCode","DisplayName","NormalizedDisplayName",
            "Jurisdiction","SupportLevel","Category","Synonyms","IsActive","CreatedAtUtc")
        VALUES (@id,@version,@code,@normalized,'Legacy maize','legacy maize','AR','ESPECIALIZADA_VALIDADA','OTROS','[]'::jsonb,true,now())
        """, ("id", Guid.NewGuid()), ("version", version), ("code", code), ("normalized", normalized));
}
