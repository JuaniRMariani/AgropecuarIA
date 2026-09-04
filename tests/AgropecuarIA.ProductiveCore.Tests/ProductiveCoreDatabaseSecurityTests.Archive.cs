using System.Text.Json;
using AgropecuarIA.ProductiveCore.Application;
using AgropecuarIA.ProductiveCore.Domain;
using AgropecuarIA.ProductiveCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Npgsql;

namespace AgropecuarIA.ProductiveCore.Tests;

public sealed partial class ProductiveCoreDatabaseSecurityTests
{
    [TestMethod]
    public async Task ArchiveOwnerWritesCorrectEventAndReplaysWithoutDuplicates()
    {
        await using DatabaseScenario scenario = await DatabaseScenario.CreateAsync();
        Guid fieldId = Guid.NewGuid();
        Guid version = Guid.NewGuid();
        await InsertFieldWithVersionAsync(scenario, fieldId, version, "Preserved archive name");
        ProductiveCoreArchiveApplicationService service = CreateArchiveService(scenario);
        ArchiveFieldDraftCommand command = ArchiveCommand(scenario, fieldId, version);
        ProductiveRequestContext context = ArchiveContext(scenario);

        ArchivedManagementUnitResult archived = await service.ArchiveFieldDraftAsync(command, context, CancellationToken.None);
        ArchivedManagementUnitResult replay = await service.ArchiveFieldDraftAsync(command, context, CancellationToken.None);
        Assert.IsFalse(archived.IsReplay);
        Assert.IsTrue(replay.IsReplay);
        Assert.AreEqual(archived.Version, replay.Version);
        Assert.AreEqual(2L, archived.Revision);
        Assert.AreEqual("archived", archived.Status);
        Assert.AreEqual("Preserved archive name", archived.DisplayName);

        await using var admin = new NpgsqlConnection(scenario.ConnectionString);
        await admin.OpenAsync();
        Assert.IsTrue(await ScalarBooleanAsync(admin, """
            SELECT (SELECT count(*) = 2 AND bool_and(c.relrowsecurity AND c.relforcerowsecurity)
                    FROM pg_class c JOIN pg_namespace n ON n.oid = c.relnamespace
                    WHERE n.nspname = 'productive_core'
                      AND c.relname IN ('management_unit_archive_ledgers', 'management_unit_archive_key_aliases'))
                AND NOT has_table_privilege('agro_productive_app', 'productive_core.management_unit_archive_ledgers', 'UPDATE')
                AND NOT has_table_privilege('agro_productive_app', 'productive_core.management_unit_archive_key_aliases', 'DELETE')
            """));
        Assert.IsTrue(await ScalarBooleanAsync(admin, """
            SELECT (SELECT count(*) = 1 FROM productive_core.management_units WHERE "Status" = 'archived' AND "Revision" = 2)
                AND (SELECT count(*) = 1 FROM productive_core.management_unit_archive_ledgers WHERE "Operation" = 'archive_field' AND "State" = 'succeeded')
                AND (SELECT count(*) = 1 FROM productive_core.management_unit_archive_key_aliases WHERE "Operation" = 'archive_field')
                AND (SELECT count(*) = 1 FROM productive_core.journal_entries WHERE "Action" = 'management_unit_archived')
                AND (SELECT count(*) = 1 FROM productive_core.outbox_messages WHERE "EventType" = 'ManagementUnitArchived' AND "AggregateVersion" = 2)
            """));
        await using var payloadCommand = new NpgsqlCommand("SELECT \"PayloadJson\"::text FROM productive_core.outbox_messages", admin);
        using JsonDocument payload = JsonDocument.Parse((string)(await payloadCommand.ExecuteScalarAsync())!);
        Assert.AreEqual(5, payload.RootElement.EnumerateObject().Count());
        Assert.AreEqual(scenario.FirstOrganizationId, payload.RootElement.GetProperty("organizationId").GetGuid());
        Assert.AreEqual(fieldId, payload.RootElement.GetProperty("managementUnitId").GetGuid());
        Assert.AreEqual("archived", payload.RootElement.GetProperty("status").GetString());
        Assert.IsTrue(payload.RootElement.TryGetProperty("archivedAtUtc", out _));
        Assert.IsFalse(payload.RootElement.TryGetProperty("displayName", out _));
    }

    [TestMethod]
    public async Task ArchiveRejectsForeignOrganizationAndField()
    {
        await using DatabaseScenario scenario = await DatabaseScenario.CreateAsync();
        Guid fieldId = Guid.NewGuid();
        Guid version = Guid.NewGuid();
        await InsertFieldWithVersionAsync(scenario, fieldId, version, "Private field");
        ProductiveCoreArchiveApplicationService service = CreateArchiveService(scenario);
        ProductiveRequestContext foreignActor = new("foreign-archive", scenario.SecondActorId,
            scenario.SecondSessionId, scenario.FirstOrganizationId);
        ProductiveCoreOperationException denied = await Assert.ThrowsExactlyAsync<ProductiveCoreOperationException>(() =>
            service.ArchiveFieldDraftAsync(ArchiveCommand(scenario, fieldId, version), foreignActor, CancellationToken.None));
        Assert.AreEqual("productive_core.field_not_available", denied.Code);

        ArchiveFieldDraftCommand foreignField = new(scenario.SecondOrganizationId, fieldId, version, new string('b', 32));
        ProductiveCoreOperationException hidden = await Assert.ThrowsExactlyAsync<ProductiveCoreOperationException>(() =>
            service.ArchiveFieldDraftAsync(foreignField,
                new ProductiveRequestContext("foreign-field", scenario.SecondActorId, scenario.SecondSessionId,
                    scenario.SecondOrganizationId), CancellationToken.None));
        Assert.AreEqual("productive_core.field_not_available", hidden.Code);

        await service.ArchiveFieldDraftAsync(ArchiveCommand(scenario, fieldId, version), ArchiveContext(scenario), CancellationToken.None);
        await using var foreignConnection = new NpgsqlConnection(scenario.RuntimeConnectionString);
        await foreignConnection.OpenAsync();
        await using NpgsqlTransaction transaction = await BeginAuthorizedAsync(foreignConnection,
            scenario.SecondActorId, scenario.SecondOrganizationId, scenario.SecondSessionId, scenario.SecondAuthorizationVersion);
        Assert.AreEqual(0L, await ScalarInt64Async(foreignConnection, transaction,
            "SELECT count(*) FROM productive_core.management_unit_archive_ledgers"));
        Assert.AreEqual(0L, await ScalarInt64Async(foreignConnection, transaction,
            "SELECT count(*) FROM productive_core.management_unit_archive_key_aliases"));
        await transaction.RollbackAsync();
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public async Task ArchiveConcurrencyUsesOneTransition(bool sameIdempotencyKey)
    {
        await using DatabaseScenario scenario = await DatabaseScenario.CreateAsync();
        Guid fieldId = Guid.NewGuid();
        Guid version = Guid.NewGuid();
        await InsertFieldWithVersionAsync(scenario, fieldId, version, "Concurrent archive");
        ProductiveCoreArchiveApplicationService service = CreateArchiveService(scenario);
        ArchiveFieldDraftCommand first = ArchiveCommand(scenario, fieldId, version);
        ArchiveFieldDraftCommand second = sameIdempotencyKey ? first : first with { IdempotencyKey = new string('b', 32) };

        ArchiveAttempt[] attempts = await Task.WhenAll(
            AttemptArchiveAsync(service, first, ArchiveContext(scenario)),
            AttemptArchiveAsync(service, second, ArchiveContext(scenario)));
        if (sameIdempotencyKey)
        {
            Assert.IsTrue(attempts.All(attempt => attempt.Result is not null));
            Assert.AreEqual(1, attempts.Count(attempt => attempt.Result?.IsReplay == true));
            Assert.AreEqual(attempts[0].Result!.Version, attempts[1].Result!.Version);
        }
        else
        {
            Assert.AreEqual(1, attempts.Count(attempt => attempt.Result is not null));
            Assert.AreEqual("productive_core.field_version_stale", attempts.Single(attempt => attempt.Result is null).Error);
        }

        await using var admin = new NpgsqlConnection(scenario.ConnectionString);
        await admin.OpenAsync();
        Assert.IsTrue(await ScalarBooleanAsync(admin, """
            SELECT (SELECT count(*) = 1 FROM productive_core.management_units WHERE "Status" = 'archived' AND "Revision" = 2)
                AND (SELECT count(*) = 1 FROM productive_core.management_unit_archive_ledgers)
                AND (SELECT count(*) = 1 FROM productive_core.journal_entries)
                AND (SELECT count(*) = 1 FROM productive_core.outbox_messages)
            """));
    }

    [TestMethod]
    [DataRow("management_units")]
    [DataRow("management_unit_archive_ledgers")]
    [DataRow("management_unit_archive_key_aliases")]
    [DataRow("journal_entries")]
    [DataRow("outbox_messages")]
    public async Task ArchiveRollsBackEverySinkFailure(string failingTable)
    {
        if (failingTable is not ("management_units" or "management_unit_archive_ledgers" or
            "management_unit_archive_key_aliases" or "journal_entries" or "outbox_messages"))
            throw new ArgumentOutOfRangeException(nameof(failingTable));

        await using DatabaseScenario scenario = await DatabaseScenario.CreateAsync();
        Guid fieldId = Guid.NewGuid();
        Guid version = Guid.NewGuid();
        await InsertFieldWithVersionAsync(scenario, fieldId, version, "Archive rollback");
        string triggerEvent = failingTable == "management_units" ? "UPDATE" : "INSERT";
        await ExecuteAsync(scenario.ConnectionString, $"""
            CREATE FUNCTION productive_core.test_archive_fault() RETURNS trigger LANGUAGE plpgsql AS $fault$
            BEGIN RAISE EXCEPTION 'archive sink fault'; END
            $fault$;
            CREATE TRIGGER test_archive_fault BEFORE {triggerEvent} ON productive_core.{failingTable}
                FOR EACH ROW EXECUTE FUNCTION productive_core.test_archive_fault();
            """);
        ProductiveCoreOperationException error = await Assert.ThrowsExactlyAsync<ProductiveCoreOperationException>(() =>
            CreateArchiveService(scenario).ArchiveFieldDraftAsync(
                ArchiveCommand(scenario, fieldId, version), ArchiveContext(scenario), CancellationToken.None));
        Assert.AreEqual("productive_core.management_unit_unavailable", error.Code);

        await using var admin = new NpgsqlConnection(scenario.ConnectionString);
        await admin.OpenAsync();
        Assert.IsTrue(await ScalarBooleanAsync(admin, $"""
            SELECT (SELECT count(*) = 1 FROM productive_core.management_units
                    WHERE "Status" = 'draft' AND "Revision" = 1 AND "Version" = '{version:D}'::uuid)
                AND (SELECT count(*) = 0 FROM productive_core.management_unit_archive_ledgers)
                AND (SELECT count(*) = 0 FROM productive_core.management_unit_archive_key_aliases)
                AND (SELECT count(*) = 0 FROM productive_core.journal_entries)
                AND (SELECT count(*) = 0 FROM productive_core.outbox_messages)
            """));
    }

    [TestMethod]
    public async Task ArchiveRetentionCoverageTracksArchiveLedgers()
    {
        await using DatabaseScenario scenario = await DatabaseScenario.CreateAsync();
        Guid fieldId = Guid.NewGuid();
        Guid version = Guid.NewGuid();
        await InsertFieldWithVersionAsync(scenario, fieldId, version, "Archive key rotation");
        ArchiveFieldDraftCommand command = ArchiveCommand(scenario, fieldId, version);
        ProductiveRequestContext context = ArchiveContext(scenario);
        ArchivedManagementUnitResult original = await CreateArchiveService(scenario, "v1")
            .ArchiveFieldDraftAsync(command, context, CancellationToken.None);
        ProductiveCoreOperationException missingKey = await Assert.ThrowsExactlyAsync<ProductiveCoreOperationException>(() =>
            CreateArchiveService(scenario, "v2").ArchiveFieldDraftAsync(command, context, CancellationToken.None));
        Assert.AreEqual("productive_core.management_unit_unavailable", missingKey.Code);
        ArchivedManagementUnitResult overlap = await CreateArchiveService(scenario, "v1", "v2")
            .ArchiveFieldDraftAsync(command, context, CancellationToken.None);
        Assert.IsTrue(overlap.IsReplay);
        ArchivedManagementUnitResult retired = await CreateArchiveService(scenario, "v2")
            .ArchiveFieldDraftAsync(command, context, CancellationToken.None);
        Assert.IsTrue(retired.IsReplay);
        Assert.AreEqual(original.Version, retired.Version);
    }

    private static ArchiveFieldDraftCommand ArchiveCommand(DatabaseScenario scenario, Guid fieldId, Guid version) =>
        new(scenario.FirstOrganizationId, fieldId, version, new string('a', 32));

    private static ProductiveRequestContext ArchiveContext(DatabaseScenario scenario) =>
        new("archive-test", scenario.FirstActorId, scenario.FirstSessionId, scenario.FirstOrganizationId);

    private static ProductiveCoreArchiveApplicationService CreateArchiveService(DatabaseScenario scenario, params string[] keyVersions)
    {
        if (keyVersions.Length == 0) keyVersions = ["v1"];
        ServiceProvider metrics = new ServiceCollection().AddMetrics().BuildServiceProvider();
        return new ProductiveCoreArchiveApplicationService(
            new PostgresProductiveCoreUnitOfWorkFactory(new TestProductiveDbContextFactory(scenario.RuntimeConnectionString)),
            new ProductiveCoreTelemetry(metrics.GetRequiredService<System.Diagnostics.Metrics.IMeterFactory>()),
            TimeProvider.System,
            Options.Create(new ManagementUnitRenameOptions
            {
                Enabled = true,
                CurrentKeyVersion = keyVersions[^1],
                HmacKeys = keyVersions.ToDictionary(version => version,
                    version => Convert.ToBase64String(RenameTestKey(version)), StringComparer.Ordinal),
            }));
    }

    private sealed record ArchiveAttempt(ArchivedManagementUnitResult? Result, string? Error);

    private static async Task<ArchiveAttempt> AttemptArchiveAsync(ProductiveCoreArchiveApplicationService service,
        ArchiveFieldDraftCommand command, ProductiveRequestContext context)
    {
        try
        {
            return new ArchiveAttempt(await service.ArchiveFieldDraftAsync(command, context, CancellationToken.None), null);
        }
        catch (ProductiveCoreOperationException exception)
        {
            return new ArchiveAttempt(null, exception.Code);
        }
    }
}
