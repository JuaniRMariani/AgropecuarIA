using AgropecuarIA.ProductiveCore.Application;
using AgropecuarIA.ProductiveCore.Domain;
using AgropecuarIA.ProductiveCore.Infrastructure;

namespace AgropecuarIA.ProductiveCore.Tests;

public sealed partial class ProductiveCoreDatabaseSecurityTests
{
    [TestMethod]
    public async Task HistoryPagesBoundDatabaseReadsAndUseUuidTieBreakersWithoutCatalogCalls()
    {
        await using DatabaseScenario scenario = await DatabaseScenario.CreateAsync();
        Guid field = Guid.NewGuid();
        await InsertFieldWithVersionAsync(scenario, field, Guid.NewGuid(), "Paged history field");
        var factory = new PostgresProductiveCoreUnitOfWorkFactory(new TestProductiveDbContextFactory(scenario.RuntimeConnectionString));
        ProductiveRequestContext context = CycleContext(scenario);
        Guid firstCycle = HistoryId(1);
        await using (IProductiveCoreUnitOfWork write = await factory.BeginAsync(ProductiveTransactionMode.SerializableWrite, CancellationToken.None))
        {
            Assert.IsNotNull(await write.AuthorizeOwnerAsync(context, CancellationToken.None));
            for (int index = 1; index <= 105; index++)
            {
                write.AddProductionCycle(new ProductionCycle(HistoryId(index), scenario.FirstOrganizationId, field,
                    TestCatalogSnapshot(), "grano", "secano", CycleDate, CycleDate));
                write.AddProductionEvent(new ProductionEvent(HistoryId(1000 + index), scenario.FirstOrganizationId, firstCycle,
                    "observacion", CycleDate.AddDays(106 - index), CycleDate, null, null, "Synthetic history", ProductionOrigins.Manual));
            }
            await write.SaveChangesAsync(CancellationToken.None);
            await write.CommitAsync(CancellationToken.None);
        }

        var service = new ProductionCycleApplicationService(factory, TimeProvider.System, new RejectAnyCatalogResolver());
        ProductionCyclePage first = await service.ListCyclePageAsync(scenario.FirstOrganizationId, field, null, null, context, CancellationToken.None);
        Assert.HasCount(20, first.Items);
        Assert.IsTrue(first.HasMore);
        Assert.IsNotNull(first.NextCursor);
        Assert.IsLessThanOrEqualTo(512, first.NextCursor.Length);
        Assert.AreEqual(HistoryId(105), first.Items[0].Id);
        Assert.IsTrue(first.Items.All(item => item.CatalogSnapshot == TestCatalogSnapshot()));
        var cycleIds = new List<Guid>();
        string? cursor = null;
        do
        {
            ProductionCyclePage page = await service.ListCyclePageAsync(scenario.FirstOrganizationId, field, 20, cursor, context, CancellationToken.None);
            Assert.IsLessThanOrEqualTo(20, page.Items.Count);
            Assert.AreEqual(page.HasMore, page.NextCursor is not null);
            cycleIds.AddRange(page.Items.Select(item => item.Id));
            cursor = page.NextCursor;
        } while (cursor is not null);
        CollectionAssert.AreEqual(Enumerable.Range(1, 105).Reverse().Select(HistoryId).ToArray(), cycleIds.ToArray());

        var eventIds = new List<Guid>();
        do
        {
            ProductionTimelinePage page = await service.GetTimelinePageAsync(scenario.FirstOrganizationId, firstCycle, 17, cursor, context, CancellationToken.None);
            Assert.AreEqual(firstCycle, page.Cycle.Id);
            Assert.IsLessThanOrEqualTo(17, page.Events.Count);
            Assert.AreEqual(page.HasMore, page.NextCursor is not null);
            eventIds.AddRange(page.Events.Select(item => item.Id));
            cursor = page.NextCursor;
        } while (cursor is not null);
        CollectionAssert.AreEqual(Enumerable.Range(1001, 105).Reverse().Select(HistoryId).ToArray(), eventIds.ToArray());
        ProductionTimelinePage latest = await service.GetTimelinePageAsync(scenario.FirstOrganizationId, firstCycle, 1, null, context, CancellationToken.None);
        Assert.AreEqual(CycleDate.AddDays(1), latest.Events[0].EffectiveDateUtc);
        Assert.AreEqual(CycleDate, latest.Events[0].RecordedAtUtc);

        ProductionCyclePage maximum = await service.ListCyclePageAsync(scenario.FirstOrganizationId, field, 100, null, context, CancellationToken.None);
        Assert.HasCount(100, maximum.Items);
        Assert.IsTrue(maximum.HasMore);
        await using IProductiveCoreUnitOfWork read = await factory.BeginAsync(ProductiveTransactionMode.Read, CancellationToken.None);
        Assert.IsNotNull(await read.AuthorizeOwnerAsync(context, CancellationToken.None));
        Assert.HasCount(3, await read.ListProductionCyclePageAsync(scenario.FirstOrganizationId, field, new(2, null), CancellationToken.None));
        Assert.HasCount(3, await read.ListProductionEventPageAsync(scenario.FirstOrganizationId, firstCycle, new(2, null), CancellationToken.None));
        await read.CommitAsync(CancellationToken.None);
        using var cancelled = new CancellationTokenSource();
        await cancelled.CancelAsync();
        await Assert.ThrowsAsync<OperationCanceledException>(() => service.ListCyclePageAsync(
            scenario.FirstOrganizationId, field, 20, null, context, cancelled.Token));
    }

    private static Guid HistoryId(int index) => Guid.Parse(
        "00000000-0000-4000-8000-" + index.ToString("D12", System.Globalization.CultureInfo.InvariantCulture));
}
