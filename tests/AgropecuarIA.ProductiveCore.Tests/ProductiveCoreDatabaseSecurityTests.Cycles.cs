using AgropecuarIA.ProductiveCore.Application;
using AgropecuarIA.ProductiveCore.Domain;
using AgropecuarIA.ProductiveCore.Infrastructure;
using Npgsql;

namespace AgropecuarIA.ProductiveCore.Tests;

public sealed partial class ProductiveCoreDatabaseSecurityTests
{
    [TestMethod]
    public async Task CycleArchivedFieldRejectsNewCyclesButPreservesHistory()
    {
        await using DatabaseScenario scenario = await DatabaseScenario.CreateAsync();
        Guid fieldId = Guid.NewGuid();
        Guid version = Guid.NewGuid();
        await InsertFieldWithVersionAsync(scenario, fieldId, version, "Archived cycle field");
        ProductionCycleApplicationService service = CreateCycleService(scenario);
        ProductiveRequestContext context = CycleContext(scenario);
        ProductionCycleDto cycle = await service.StartCycleAsync(
            CycleCommand(scenario.FirstOrganizationId, fieldId), context, CancellationToken.None);
        ProductionEventDto recorded = await service.RecordEventAsync(
            EventCommand(scenario.FirstOrganizationId, cycle.Id), context, CancellationToken.None);
        await CreateArchiveService(scenario).ArchiveFieldDraftAsync(
            ArchiveCommand(scenario, fieldId, version), context, CancellationToken.None);

        ProductiveCoreOperationException rejected = await Assert.ThrowsExactlyAsync<ProductiveCoreOperationException>(
            () => service.StartCycleAsync(CycleCommand(scenario.FirstOrganizationId, fieldId), context, CancellationToken.None));
        Assert.AreEqual(409, rejected.StatusCode);
        Assert.AreEqual("productive_core.field_archived", rejected.Code);
        Assert.HasCount(1, await service.ListCyclesAsync(
            scenario.FirstOrganizationId, fieldId, context, CancellationToken.None));
        ProductionTimelineResult history = await service.GetTimelineAsync(
            scenario.FirstOrganizationId, cycle.Id, context, CancellationToken.None);
        Assert.HasCount(1, history.Events);
        Assert.AreEqual(recorded.Id, history.Events[0].Id);

        // A caller bypassing the application guard is still denied by the restricted database role.
        var factory = new PostgresProductiveCoreUnitOfWorkFactory(
            new TestProductiveDbContextFactory(scenario.RuntimeConnectionString));
        await using IProductiveCoreUnitOfWork unitOfWork = await factory.BeginAsync(
            ProductiveTransactionMode.SerializableWrite, CancellationToken.None);
        Assert.IsNotNull(await unitOfWork.AuthorizeOwnerAsync(context, CancellationToken.None));
        unitOfWork.AddProductionCycle(new ProductionCycle(
            Guid.NewGuid(), scenario.FirstOrganizationId, fieldId, "SOJ", "Soja", "grano", "secano",
            "FLUJO_GENERICO", CycleDate, CycleDate));
        ProductivePersistenceUnavailableException blocked = await Assert.ThrowsExactlyAsync<ProductivePersistenceUnavailableException>(
            () => unitOfWork.SaveChangesAsync(CancellationToken.None));
        Assert.IsInstanceOfType<PostgresException>(blocked.InnerException?.InnerException);
        Assert.AreEqual(PostgresErrorCodes.InsufficientPrivilege,
            ((PostgresException)blocked.InnerException.InnerException).SqlState);
    }

    [TestMethod]
    public async Task CycleOwnerCanCreateRecordListAndClose()
    {
        await using DatabaseScenario scenario = await DatabaseScenario.CreateAsync();
        Guid fieldId = Guid.NewGuid();
        await InsertFieldWithVersionAsync(scenario, fieldId, Guid.NewGuid(), "Cycle field");
        ProductionCycleApplicationService service = CreateCycleService(scenario);
        ProductiveRequestContext context = CycleContext(scenario);
        ProductionCycleDto cycle = await service.StartCycleAsync(
            CycleCommand(scenario.FirstOrganizationId, fieldId), context, CancellationToken.None);
        ProductionEventDto later = await service.RecordEventAsync(
            EventCommand(scenario.FirstOrganizationId, cycle.Id) with { EffectiveDateUtc = CycleDate.AddDays(2) },
            context, CancellationToken.None);
        ProductionEventDto earlier = await service.RecordEventAsync(
            EventCommand(scenario.FirstOrganizationId, cycle.Id), context, CancellationToken.None);

        IReadOnlyList<ProductionCycleDto> cycles = await service.ListCyclesAsync(
            scenario.FirstOrganizationId, fieldId, context, CancellationToken.None);
        Assert.HasCount(1, cycles);
        Assert.AreEqual(cycle.Id, cycles[0].Id);
        ProductionTimelineResult timeline = await service.GetTimelineAsync(
            scenario.FirstOrganizationId, cycle.Id, context, CancellationToken.None);
        Assert.HasCount(2, timeline.Events);
        Assert.AreEqual(earlier.Id, timeline.Events[0].Id);
        Assert.AreEqual(later.Id, timeline.Events[1].Id);

        ProductionCycleDto closed = await service.CloseCycleAsync(
            new CloseProductionCycleCommand(scenario.FirstOrganizationId, cycle.Id, CycleDate.AddDays(3)),
            context, CancellationToken.None);
        Assert.AreEqual(ProductionCycleStatuses.Closed, closed.Status);
        ProductiveCoreOperationException rejected = await Assert.ThrowsExactlyAsync<ProductiveCoreOperationException>(
            () => service.RecordEventAsync(EventCommand(scenario.FirstOrganizationId, cycle.Id), context, CancellationToken.None));
        Assert.AreEqual(409, rejected.StatusCode);
        timeline = await service.GetTimelineAsync(scenario.FirstOrganizationId, cycle.Id, context, CancellationToken.None);
        Assert.HasCount(2, timeline.Events);
    }

    [TestMethod]
    public async Task CycleForeignMembershipDeniesEveryOperationWithoutWriting()
    {
        await using DatabaseScenario scenario = await DatabaseScenario.CreateAsync();
        Guid fieldId = Guid.NewGuid();
        await InsertFieldWithVersionAsync(scenario, fieldId, Guid.NewGuid(), "Private cycle field");
        ProductionCycleApplicationService service = CreateCycleService(scenario);
        ProductiveRequestContext owner = CycleContext(scenario);
        ProductionCycleDto cycle = await service.StartCycleAsync(
            CycleCommand(scenario.FirstOrganizationId, fieldId), owner, CancellationToken.None);
        var foreign = new ProductiveRequestContext(
            "foreign-cycle", scenario.SecondActorId, scenario.SecondSessionId, scenario.FirstOrganizationId);

        await AssertCycleNotAvailableAsync(() => service.StartCycleAsync(
            CycleCommand(scenario.FirstOrganizationId, fieldId), foreign, CancellationToken.None));
        await AssertCycleNotAvailableAsync(() => service.ListCyclesAsync(
            scenario.FirstOrganizationId, fieldId, foreign, CancellationToken.None));
        await AssertCycleNotAvailableAsync(() => service.GetTimelineAsync(
            scenario.FirstOrganizationId, cycle.Id, foreign, CancellationToken.None));
        await AssertCycleNotAvailableAsync(() => service.RecordEventAsync(
            EventCommand(scenario.FirstOrganizationId, cycle.Id), foreign, CancellationToken.None));
        await AssertCycleNotAvailableAsync(() => service.CloseCycleAsync(
            new CloseProductionCycleCommand(scenario.FirstOrganizationId, cycle.Id, CycleDate.AddDays(1)), foreign, CancellationToken.None));

        ProductionTimelineResult untouched = await service.GetTimelineAsync(
            scenario.FirstOrganizationId, cycle.Id, owner, CancellationToken.None);
        Assert.IsEmpty(untouched.Events);
        Assert.AreEqual(ProductionCycleStatuses.Active, untouched.Cycle.Status);
        Assert.HasCount(1, await service.ListCyclesAsync(scenario.FirstOrganizationId, fieldId, owner, CancellationToken.None));
    }

    [TestMethod]
    public async Task CycleForeignFieldAndCycleAreNeutralWithinAnAuthorizedOrganization()
    {
        await using DatabaseScenario scenario = await DatabaseScenario.CreateAsync();
        Guid foreignField = Guid.NewGuid();
        await ExecuteAsync(scenario.ConnectionString,
            """
            INSERT INTO productive_core.management_units
                ("Id", "OrganizationId", "DisplayName", "UnitType", "Status", "SpatialStatus", "CreatedAtUtc", "Version")
            VALUES (@field, @organization, 'Foreign cycle field', 'field', 'draft', 'not_configured', now(), gen_random_uuid())
            """, ("field", foreignField), ("organization", scenario.SecondOrganizationId));
        ProductionCycleApplicationService service = CreateCycleService(scenario);
        ProductiveRequestContext context = CycleContext(scenario);
        var foreignContext = new ProductiveRequestContext(
            "second-cycle", scenario.SecondActorId, scenario.SecondSessionId, scenario.SecondOrganizationId);
        ProductionCycleDto cycle = await service.StartCycleAsync(
            CycleCommand(scenario.SecondOrganizationId, foreignField), foreignContext, CancellationToken.None);

        await AssertCycleNotAvailableAsync(() => service.StartCycleAsync(
            CycleCommand(scenario.FirstOrganizationId, foreignField), context, CancellationToken.None));
        await AssertCycleNotAvailableAsync(() => service.ListCyclesAsync(
            scenario.FirstOrganizationId, foreignField, context, CancellationToken.None));
        await AssertCycleNotAvailableAsync(() => service.GetTimelineAsync(
            scenario.FirstOrganizationId, cycle.Id, context, CancellationToken.None));
        await AssertCycleNotAvailableAsync(() => service.RecordEventAsync(
            EventCommand(scenario.FirstOrganizationId, cycle.Id), context, CancellationToken.None));
        await AssertCycleNotAvailableAsync(() => service.CloseCycleAsync(
            new CloseProductionCycleCommand(scenario.FirstOrganizationId, cycle.Id, CycleDate.AddDays(1)), context, CancellationToken.None));
        await AssertCycleNotAvailableAsync(() => service.ListCyclesAsync(
            scenario.SecondOrganizationId, foreignField, context, CancellationToken.None));
        await AssertCycleNotAvailableAsync(() => service.GetTimelineAsync(
            scenario.FirstOrganizationId, Guid.NewGuid(), context, CancellationToken.None));

        var factory = new PostgresProductiveCoreUnitOfWorkFactory(
            new TestProductiveDbContextFactory(scenario.RuntimeConnectionString));
        await using IProductiveCoreUnitOfWork unitOfWork = await factory.BeginAsync(
            ProductiveTransactionMode.SerializableWrite, CancellationToken.None);
        Assert.IsNotNull(await unitOfWork.AuthorizeOwnerAsync(context, CancellationToken.None));
        unitOfWork.AddProductionCycle(new ProductionCycle(
            Guid.NewGuid(), scenario.FirstOrganizationId, foreignField, "SOJ", "Soja", "grano", "secano",
            "FLUJO_GENERICO", CycleDate, CycleDate));
        ProductivePersistenceUnavailableException blocked = await Assert.ThrowsExactlyAsync<ProductivePersistenceUnavailableException>(
            () => unitOfWork.SaveChangesAsync(CancellationToken.None));
        Assert.IsInstanceOfType<PostgresException>(blocked.InnerException?.InnerException);
        Assert.AreEqual(PostgresErrorCodes.InsufficientPrivilege,
            ((PostgresException)blocked.InnerException.InnerException).SqlState);
    }

    [TestMethod]
    [DataRow("session")]
    [DataRow("membership")]
    public async Task CycleRevokedAuthorityCannotReadOrAppend(string authority)
    {
        await using DatabaseScenario scenario = await DatabaseScenario.CreateAsync();
        Guid fieldId = Guid.NewGuid();
        await InsertFieldWithVersionAsync(scenario, fieldId, Guid.NewGuid(), "Revoked cycle field");
        ProductionCycleApplicationService service = CreateCycleService(scenario);
        ProductiveRequestContext context = CycleContext(scenario);
        ProductionCycleDto cycle = await service.StartCycleAsync(
            CycleCommand(scenario.FirstOrganizationId, fieldId), context, CancellationToken.None);
        await ExecuteAsync(scenario.ConnectionString,
            authority == "session"
                ? "UPDATE identity.sessions SET \"RevokedAtUtc\" = now() WHERE \"Id\" = @id"
                : "UPDATE identity.memberships SET \"Status\" = 'removed', \"RemovedAtUtc\" = now(), \"RemovedByUserId\" = @id, \"SecurityVersion\" = \"SecurityVersion\" + 1 WHERE \"UserId\" = @id",
            ("id", authority == "session" ? scenario.FirstSessionId : scenario.FirstActorId));

        await AssertCycleNotAvailableAsync(() => service.GetTimelineAsync(
            scenario.FirstOrganizationId, cycle.Id, context, CancellationToken.None));
        await AssertCycleNotAvailableAsync(() => service.RecordEventAsync(
            EventCommand(scenario.FirstOrganizationId, cycle.Id), context, CancellationToken.None));
    }

    [TestMethod]
    public async Task CycleRlsRequiresContextAndEventsCannotBeOverwritten()
    {
        await using DatabaseScenario scenario = await DatabaseScenario.CreateAsync();
        Guid fieldId = Guid.NewGuid();
        await InsertFieldWithVersionAsync(scenario, fieldId, Guid.NewGuid(), "RLS cycle field");
        ProductionCycleApplicationService service = CreateCycleService(scenario);
        ProductiveRequestContext context = CycleContext(scenario);
        ProductionCycleDto cycle = await service.StartCycleAsync(
            CycleCommand(scenario.FirstOrganizationId, fieldId), context, CancellationToken.None);
        await service.RecordEventAsync(EventCommand(scenario.FirstOrganizationId, cycle.Id), context, CancellationToken.None);

        await using var connection = new NpgsqlConnection(scenario.RuntimeConnectionString);
        await connection.OpenAsync();
        await using (NpgsqlTransaction noContext = await connection.BeginTransactionAsync())
        {
            await SetRoleAsync(connection, noContext, "agro_productive_app");
            Assert.AreEqual(0L, await ScalarInt64Async(connection, noContext, "SELECT count(*) FROM productive_core.production_cycles"));
            Assert.AreEqual(0L, await ScalarInt64Async(connection, noContext, "SELECT count(*) FROM productive_core.production_events"));
            await noContext.RollbackAsync();
        }

        await using (NpgsqlTransaction otherTenant = await BeginAuthorizedAsync(
            connection, scenario.SecondActorId, scenario.SecondOrganizationId,
            scenario.SecondSessionId, scenario.SecondAuthorizationVersion))
        {
            Assert.AreEqual(0L, await ScalarInt64Async(connection, otherTenant, "SELECT count(*) FROM productive_core.production_cycles"));
            Assert.AreEqual(0L, await ScalarInt64Async(connection, otherTenant, "SELECT count(*) FROM productive_core.production_events"));
            await otherTenant.RollbackAsync();
        }

        await using NpgsqlTransaction authorized = await BeginAuthorizedAsync(
            connection, scenario.FirstActorId, scenario.FirstOrganizationId,
            scenario.FirstSessionId, scenario.FirstAuthorizationVersion);
        Assert.AreEqual(1L, await ScalarInt64Async(connection, authorized, "SELECT count(*) FROM productive_core.production_cycles"));
        Assert.AreEqual(1L, await ScalarInt64Async(connection, authorized, "SELECT count(*) FROM productive_core.production_events"));
        await using var overwrite = new NpgsqlCommand("UPDATE productive_core.production_events SET \"Notes\" = 'rewrite'", connection, authorized);
        PostgresException denied = await Assert.ThrowsExactlyAsync<PostgresException>(
            () => overwrite.ExecuteNonQueryAsync());
        Assert.AreEqual(PostgresErrorCodes.InsufficientPrivilege, denied.SqlState);
    }

    private static DateTimeOffset CycleDate => new(2026, 8, 1, 0, 0, 0, TimeSpan.Zero);

    private static ProductionCycleApplicationService CreateCycleService(DatabaseScenario scenario) =>
        new(new PostgresProductiveCoreUnitOfWorkFactory(new TestProductiveDbContextFactory(scenario.RuntimeConnectionString)), TimeProvider.System);

    private static ProductiveRequestContext CycleContext(DatabaseScenario scenario) =>
        new("cycle-security", scenario.FirstActorId, scenario.FirstSessionId, scenario.FirstOrganizationId);

    private static StartProductionCycleCommand CycleCommand(Guid organizationId, Guid fieldId) =>
        new(organizationId, fieldId, "SOJ", "Soja", "grano", "secano", "FLUJO_GENERICO", CycleDate);

    private static RecordProductionEventCommand EventCommand(Guid organizationId, Guid cycleId) =>
        new(organizationId, cycleId, "siembra", CycleDate.AddDays(1), 100m, "kg", "Local test observation");

    private static async Task AssertCycleNotAvailableAsync(Func<Task> operation)
    {
        ProductiveCoreOperationException exception = await Assert.ThrowsExactlyAsync<ProductiveCoreOperationException>(operation);
        Assert.AreEqual(404, exception.StatusCode);
        Assert.AreEqual("productive_core.field_not_available", exception.Code);
    }
}
