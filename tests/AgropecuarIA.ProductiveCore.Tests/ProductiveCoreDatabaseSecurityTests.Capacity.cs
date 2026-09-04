using System.Data.Common;
using AgropecuarIA.ProductiveCore.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace AgropecuarIA.ProductiveCore.Tests;

public sealed partial class ProductiveCoreDatabaseSecurityTests
{
    // Both requests observe 99 fields. The second save starts only after the first
    // transaction commits, so PostgreSQL must reject its stale serializable write.
    // No scheduler timing, sleeps, test retries, or synthetic database errors.
    private sealed class CapacityRaceCoordinator
    {
        private readonly TaskCompletionSource bothSaving = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource firstCommitted = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int saves;
        public Exception? SaveFailure { get; private set; }

        public async Task BeforeSaveAsync(CancellationToken cancellationToken)
        {
            if (Interlocked.Increment(ref saves) == 1)
            {
                await bothSaving.Task.WaitAsync(TimeSpan.FromSeconds(30), cancellationToken);
            }
            else
            {
                bothSaving.TrySetResult();
                await firstCommitted.Task.WaitAsync(TimeSpan.FromSeconds(30), cancellationToken);
            }
        }

        public void Committed() => firstCommitted.TrySetResult();

        public void SaveFailed(Exception exception) => SaveFailure = exception;
    }

    private sealed class CapacitySaveInterceptor(CapacityRaceCoordinator coordinator) : SaveChangesInterceptor
    {
        public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            await coordinator.BeforeSaveAsync(cancellationToken);
            return result;
        }

        public override Task SaveChangesFailedAsync(
            DbContextErrorEventData eventData,
            CancellationToken cancellationToken = default)
        {
            coordinator.SaveFailed(eventData.Exception);
            return Task.CompletedTask;
        }
    }

    private sealed class CapacityCommitInterceptor(CapacityRaceCoordinator coordinator) : DbTransactionInterceptor
    {
        public override Task TransactionCommittedAsync(
            DbTransaction transaction,
            TransactionEndEventData eventData,
            CancellationToken cancellationToken = default)
        {
            coordinator.Committed();
            return Task.CompletedTask;
        }
    }

    private sealed class CapacityRaceDbContextFactory(
        string connectionString,
        CapacityRaceCoordinator coordinator) : IDbContextFactory<ProductiveCoreDbContext>
    {
        public ProductiveCoreDbContext CreateDbContext() => new(
            new DbContextOptionsBuilder<ProductiveCoreDbContext>()
                .UseNpgsql(connectionString)
                .AddInterceptors(new CapacitySaveInterceptor(coordinator), new CapacityCommitInterceptor(coordinator))
                .Options);

        public Task<ProductiveCoreDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(CreateDbContext());
    }
}
