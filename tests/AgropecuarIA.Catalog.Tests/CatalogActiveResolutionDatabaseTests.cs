using System.Data.Common;
using AgropecuarIA.Catalog.Application;
using AgropecuarIA.Catalog.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace AgropecuarIA.Catalog.Tests;

[TestClass]
[DoNotParallelize]
public sealed class CatalogActiveResolutionDatabaseTests
{
    [TestMethod]
    public async Task ActiveResolutionChecksExpectedVersionBeforeMissingItemIncludingAbsentPublication()
    {
        await using CatalogDatabaseScenario scenario = await CatalogDatabaseScenario.CreateAsync();
        await using CatalogDbContext database = scenario.OpenContext();
        var resolver = new CatalogSearchApplicationService(database);
        Assert.AreEqual(CatalogActiveItemResolutionStatus.NotPublished,
            (await resolver.ResolveActiveItemAsync("MISSING", null, CancellationToken.None)).Status);
        Assert.AreEqual(CatalogActiveItemResolutionStatus.VersionStale,
            (await resolver.ResolveActiveItemAsync("MISSING", Guid.NewGuid(), CancellationToken.None)).Status);
        await scenario.IngestAsync("fixture", "[{\"code\":\"X\",\"displayName\":\"Synthetic\"}]");
        CatalogPublishResult published = await scenario.PublishAsync("active");
        Assert.AreEqual(CatalogActiveItemResolutionStatus.VersionStale,
            (await resolver.ResolveActiveItemAsync("MISSING", Guid.NewGuid(), CancellationToken.None)).Status);
        Assert.AreEqual(CatalogActiveItemResolutionStatus.ItemNotFound,
            (await resolver.ResolveActiveItemAsync("MISSING", published.VersionId, CancellationToken.None)).Status);
        CatalogActiveItemResolution found = await resolver.ResolveActiveItemAsync("x", published.VersionId, CancellationToken.None);
        Assert.AreEqual(CatalogActiveItemResolutionStatus.Resolved, found.Status);
        Assert.AreEqual(published.VersionId, found.Item!.VersionId);
        Assert.AreEqual("verified_snapshot", found.Item.ProvenanceStatus);
        Assert.AreEqual(0L, found.ResolvedAtUtc.Ticks % 10);
    }

    [TestMethod]
    public async Task ActiveResolutionRetainsOneMvccSnapshotWhenPublicationChangesAfterActiveRead()
    {
        await using CatalogDatabaseScenario scenario = await CatalogDatabaseScenario.CreateAsync();
        await scenario.IngestAsync("fixture", "[{\"code\":\"X\",\"displayName\":\"Before\"}]");
        CatalogPublishResult first = await scenario.PublishAsync("before");
        var barrier = new ActiveReadBarrier();
        await using var database = new CatalogDbContext(new DbContextOptionsBuilder<CatalogDbContext>()
            .UseNpgsql(scenario.ConnectionString).AddInterceptors(barrier).Options);
        Task<CatalogActiveItemResolution> pending = new CatalogSearchApplicationService(database)
            .ResolveActiveItemAsync("X", first.VersionId, CancellationToken.None);
        try
        {
            await barrier.Observed.Task.WaitAsync(TimeSpan.FromSeconds(15));
            await scenario.IngestAsync("fixture", "[{\"code\":\"Y\",\"displayName\":\"After\"}]");
            await scenario.PublishAsync("after");
        }
        finally { barrier.Continue.TrySetResult(); }
        CatalogActiveItemResolution resolved = await pending.WaitAsync(TimeSpan.FromSeconds(15));
        Assert.AreEqual(CatalogActiveItemResolutionStatus.Resolved, resolved.Status);
        Assert.AreEqual(first.VersionId, resolved.Item!.VersionId);
        Assert.AreEqual(first.VersionId, resolved.Item.ActiveVersionId);
        Assert.AreEqual("Before", resolved.Item.DisplayName);
        Assert.IsNotNull(resolved.Item.SourceSnapshotId);
        await using CatalogDbContext refreshed = scenario.OpenContext();
        Assert.AreEqual(CatalogActiveItemResolutionStatus.VersionStale,
            (await new CatalogSearchApplicationService(refreshed).ResolveActiveItemAsync("X", first.VersionId, CancellationToken.None)).Status);
    }

    private sealed class ActiveReadBarrier : DbCommandInterceptor
    {
        public TaskCompletionSource Observed { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Continue { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public override async ValueTask<DbDataReader> ReaderExecutedAsync(DbCommand command, CommandExecutedEventData eventData,
            DbDataReader result, CancellationToken cancellationToken = default)
        {
            if (command.CommandText.Contains("FROM catalog.catalog_published_versions", StringComparison.Ordinal) && Observed.TrySetResult())
                await Continue.Task.WaitAsync(cancellationToken);
            return result;
        }
    }
}
