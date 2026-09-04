using AgropecuarIA.ProductiveCore.Application;
using AgropecuarIA.ProductiveCore.Domain;
using AgropecuarIA.ProductiveCore.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;

namespace AgropecuarIA.ProductiveCore.Tests;

public sealed partial class ProductiveCoreDatabaseSecurityTests
{
    [TestMethod]
    public async Task CycleCatalogReferenceIsMandatoryForNewRowsAndImmutableThroughClose()
    {
        await using DatabaseScenario scenario = await DatabaseScenario.CreateAsync();
        Guid fieldId = Guid.NewGuid();
        await InsertFieldWithVersionAsync(scenario, fieldId, Guid.NewGuid(), "Catalog reference field");
        ProductionCycleApplicationService service = CreateCycleService(scenario);
        ProductionCycleDto created = await service.StartCycleAsync(CycleCommand(scenario.FirstOrganizationId, fieldId), CycleContext(scenario), CancellationToken.None);
        Assert.AreEqual(ProductionCatalogReferenceStatuses.ResolvedPublication, created.CatalogReferenceStatus);
        Assert.AreEqual(TestCatalogSnapshot(), created.CatalogSnapshot);
        Assert.AreEqual("FLUJO_GENERICO", created.EffectiveSupportLevel);
        Assert.IsEmpty(created.Capabilities);
        foreach (string mutation in new[]
        {
            "\"CatalogVersionId\"=gen_random_uuid()", "\"CatalogDisplayName\"='spoofed'", "\"SupportLevel\"='ESPECIALIZADA_VALIDADA'",
            "\"CatalogReferenceStatus\"='legacy_unresolved'", "\"CatalogResolvedAtUtc\"=now()",
        })
        {
            PostgresException rejected = await Assert.ThrowsExactlyAsync<PostgresException>(() => ExecuteAsync(scenario.ConnectionString,
                $"UPDATE productive_core.production_cycles SET {mutation} WHERE \"Id\"=@id", ("id", created.Id)));
            Assert.Contains("immutable", rejected.MessageText);
        }
        PostgresException missing = await Assert.ThrowsExactlyAsync<PostgresException>(() => ExecuteAsync(scenario.ConnectionString, """
            INSERT INTO productive_core.production_cycles
                ("Id","OrganizationId","ManagementUnitId","CatalogCode","CatalogDisplayName","Purpose","System","SupportLevel","Status","StartDateUtc","CreatedAtUtc")
                VALUES (@id,@org,@field,'X','Caller','grano','secano','FLUJO_GENERICO','active',now(),now())
            """, ("id", Guid.NewGuid()), ("org", scenario.FirstOrganizationId), ("field", fieldId)));
        Assert.Contains("require a resolved catalog snapshot", missing.MessageText);
        ProductionCycleDto closed = await service.CloseCycleAsync(new(scenario.FirstOrganizationId, created.Id, CycleDate.AddDays(2)), CycleContext(scenario), CancellationToken.None);
        Assert.AreEqual(created.CatalogSnapshot, closed.CatalogSnapshot);
        Assert.AreEqual("closed", closed.Status);
        PostgresException reopen = await Assert.ThrowsExactlyAsync<PostgresException>(() => ExecuteAsync(scenario.ConnectionString,
            "UPDATE productive_core.production_cycles SET \"Status\"='active',\"EndDateUtc\"=NULL WHERE \"Id\"=@id", ("id", created.Id)));
        Assert.Contains("transition is invalid", reopen.MessageText);
    }

    [TestMethod]
    public async Task CycleCatalogSnapshotConstraintRejectsIncoherentResolvedReferences()
    {
        await using DatabaseScenario scenario = await DatabaseScenario.CreateAsync();
        Guid fieldId = Guid.NewGuid();
        await InsertFieldWithVersionAsync(scenario, fieldId, Guid.NewGuid(), "Snapshot consistency field");
        ProductionCycleDto created = await CreateCycleService(scenario).StartCycleAsync(
            CycleCommand(scenario.FirstOrganizationId, fieldId), CycleContext(scenario), CancellationToken.None);
        (string Field, string JsonValue)[] invalid =
        [
            ("CatalogVersionId", "null"),
            ("CatalogItemId", "\"00000000-0000-0000-0000-000000000000\""),
            ("CatalogVersionTag", "null"),
            ("CatalogProvenanceStatus", "\"verified_snapshot\""),
            ("CatalogSourceId", "\"forged-legacy-source\""),
            ("DeclaredCatalogSupportLevel", "null"),
        ];
        foreach ((string field, string json) in invalid)
        {
            PostgresException rejected = await Assert.ThrowsExactlyAsync<PostgresException>(() => ExecuteAsync(scenario.ConnectionString, """
                INSERT INTO productive_core.production_cycles
                    SELECT (jsonb_populate_record(NULL::productive_core.production_cycles,
                        jsonb_set(to_jsonb(c),ARRAY[@field],@value::jsonb)||jsonb_build_object('Id',@newId::text))).*
                    FROM productive_core.production_cycles c WHERE c."Id"=@existing
                """, ("field", field), ("value", json), ("newId", Guid.NewGuid()), ("existing", created.Id)));
            Assert.AreEqual(PostgresErrorCodes.CheckViolation, rejected.SqlState, field);
            Assert.AreEqual("production_cycle_catalog_snapshot_shape", rejected.ConstraintName);
        }
        Assert.HasCount(1, await CreateCycleService(scenario).ListCyclesAsync(
            scenario.FirstOrganizationId, fieldId, CycleContext(scenario), CancellationToken.None));
    }

    [TestMethod]
    public async Task CycleMigrationPreservesLegacyMetadataAndNeverResolvesItFromCurrentCatalog()
    {
        await using DatabaseScenario scenario = await DatabaseScenario.CreateAsync();
        Guid fieldId = Guid.NewGuid();
        Guid cycleId = Guid.NewGuid();
        await InsertFieldWithVersionAsync(scenario, fieldId, Guid.NewGuid(), "Legacy catalog field");
        await using ProductiveCoreDbContext database = CreateProductiveDbContext(scenario.ConnectionString);
        await database.GetService<IMigrator>().MigrateAsync("20260904194958_ConfigureInitialFieldGeometry");
        await ExecuteAsync(scenario.ConnectionString, """
            INSERT INTO productive_core.production_cycles
                ("Id","OrganizationId","ManagementUnitId","CatalogCode","CatalogDisplayName","Purpose","System","SupportLevel","Status","StartDateUtc","CreatedAtUtc")
                VALUES (@id,@org,@field,' MiXeD ',' Legacy Name ','grano','secano','ESPECIALIZADA_VALIDADA','active',@date,@date)
            """, ("id", cycleId), ("org", scenario.FirstOrganizationId), ("field", fieldId), ("date", CycleDate));
        await database.Database.MigrateAsync();
        Assert.IsFalse(database.Database.HasPendingModelChanges());
        var resolver = new RejectAnyCatalogResolver();
        var service = new ProductionCycleApplicationService(new PostgresProductiveCoreUnitOfWorkFactory(new TestProductiveDbContextFactory(scenario.RuntimeConnectionString)), TimeProvider.System, resolver);
        ProductionCycleDto legacy = (await service.GetTimelineAsync(scenario.FirstOrganizationId, cycleId, CycleContext(scenario), CancellationToken.None)).Cycle;
        Assert.AreEqual("legacy_unresolved", legacy.CatalogReferenceStatus);
        Assert.IsNull(legacy.CatalogSnapshot);
        Assert.AreEqual(" MiXeD ", legacy.CatalogCode);
        Assert.AreEqual(" Legacy Name ", legacy.CatalogDisplayName);
        Assert.AreEqual("ESPECIALIZADA_VALIDADA", legacy.SupportLevel);
        Assert.AreEqual("FLUJO_GENERICO", legacy.EffectiveSupportLevel);
        Assert.IsEmpty(legacy.Capabilities);
        ProductionCycleDto closed = await service.CloseCycleAsync(new(scenario.FirstOrganizationId, cycleId, CycleDate.AddDays(1)), CycleContext(scenario), CancellationToken.None);
        Assert.AreEqual(legacy.SupportLevel, closed.SupportLevel);
        Assert.IsNull(closed.CatalogSnapshot);
        Assert.HasCount(1, await service.ListCyclesAsync(scenario.FirstOrganizationId, fieldId, CycleContext(scenario), CancellationToken.None));
    }

    private sealed class RejectAnyCatalogResolver : IProductionCatalogResolver
    {
        public Task<ProductionCatalogResolution> ResolveActiveAsync(string catalogCode, Guid? expectedCatalogVersionId, CancellationToken cancellationToken) =>
            throw new AssertFailedException("Historical cycle operations must not consult Catalog.");
    }
}
