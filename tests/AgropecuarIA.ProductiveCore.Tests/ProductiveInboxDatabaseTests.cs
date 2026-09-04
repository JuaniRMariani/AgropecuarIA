using AgropecuarIA.ProductiveCore.Application;
using AgropecuarIA.ProductiveCore.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace AgropecuarIA.ProductiveCore.Tests;

public sealed partial class ProductiveCoreDatabaseSecurityTests
{
    [TestMethod]
    public async Task InboxConcurrentDuplicatesCommitOneLocalEffectAndMarker()
    {
        await using DatabaseScenario scenario = await DatabaseScenario.CreateAsync();
        await CreateInboxEffectTableAsync(scenario.ConnectionString);
        Guid message = Guid.NewGuid();
        int invoked = 0;
        async Task<bool> AttemptAsync()
        {
            await using ProductiveCoreDbContext context = CreateProductiveDbContext(scenario.ConnectionString);
            var processor = new ProductiveInboxProcessor(context, TimeProvider.System);
            return await processor.ProcessMessageAsync(message, "synthetic-consumer", scenario.FirstOrganizationId,
                async (transactionContext, cancellationToken) =>
                {
                    Interlocked.Increment(ref invoked);
                    await transactionContext.Database.ExecuteSqlInterpolatedAsync(
                        $"INSERT INTO productive_core.inbox_test_effects (message_id) VALUES ({message})", cancellationToken);
                }, CancellationToken.None);
        }

        bool[] outcomes = await Task.WhenAll(AttemptAsync(), AttemptAsync(), AttemptAsync());
        Assert.AreEqual(1, outcomes.Count(processed => processed));
        Assert.AreEqual(1, invoked);
        await using ProductiveCoreDbContext verification = CreateProductiveDbContext(scenario.ConnectionString);
        Assert.AreEqual(1, await verification.ProductiveInboxEntries.CountAsync());
        Assert.AreEqual(1L, await verification.Database.SqlQueryRaw<long>(
            "SELECT count(*) AS \"Value\" FROM productive_core.inbox_test_effects").SingleAsync());
    }

    [TestMethod]
    public async Task InboxFailedHandlerRollsBackMarkerAndEffectThenAllowsRetry()
    {
        await using DatabaseScenario scenario = await DatabaseScenario.CreateAsync();
        await CreateInboxEffectTableAsync(scenario.ConnectionString);
        Guid message = Guid.NewGuid();
        await using ProductiveCoreDbContext context = CreateProductiveDbContext(scenario.ConnectionString);
        var processor = new ProductiveInboxProcessor(context, TimeProvider.System);
        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => processor.ProcessMessageAsync(
            message, "synthetic-consumer", scenario.FirstOrganizationId,
            async (transactionContext, cancellationToken) =>
            {
                await transactionContext.Database.ExecuteSqlInterpolatedAsync(
                    $"INSERT INTO productive_core.inbox_test_effects (message_id) VALUES ({message})", cancellationToken);
                throw new InvalidOperationException("Injected failure after local effect.");
            }, CancellationToken.None));
        Assert.AreEqual(0, await context.ProductiveInboxEntries.CountAsync());
        Assert.AreEqual(0L, await context.Database.SqlQueryRaw<long>(
            "SELECT count(*) AS \"Value\" FROM productive_core.inbox_test_effects").SingleAsync());
        bool retry = await processor.ProcessMessageAsync(message, "synthetic-consumer", scenario.FirstOrganizationId,
            async (transactionContext, cancellationToken) =>
                await transactionContext.Database.ExecuteSqlInterpolatedAsync(
                    $"INSERT INTO productive_core.inbox_test_effects (message_id) VALUES ({message})", cancellationToken),
            CancellationToken.None);
        Assert.IsTrue(retry);
        Assert.AreEqual(1, await context.ProductiveInboxEntries.CountAsync());
        Assert.AreEqual(1L, await context.Database.SqlQueryRaw<long>(
            "SELECT count(*) AS \"Value\" FROM productive_core.inbox_test_effects").SingleAsync());
    }

    private static async Task CreateInboxEffectTableAsync(string connectionString)
    {
        // Synthetic local side effect only; this does not certify a job principal or external consumer.
        await using ProductiveCoreDbContext context = CreateProductiveDbContext(connectionString);
        await context.Database.ExecuteSqlRawAsync(
            "CREATE TABLE productive_core.inbox_test_effects (message_id uuid NOT NULL)");
    }
}
