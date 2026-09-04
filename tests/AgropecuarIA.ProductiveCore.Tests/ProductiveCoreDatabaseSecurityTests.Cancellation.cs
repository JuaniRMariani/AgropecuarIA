using System.Data;
using System.Data.Common;
using AgropecuarIA.ProductiveCore.Application;
using AgropecuarIA.ProductiveCore.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace AgropecuarIA.ProductiveCore.Tests;

public sealed partial class ProductiveCoreDatabaseSecurityTests
{
    [TestMethod]
    [DataRow(ProductiveTransactionMode.Read, false)]
    [DataRow(ProductiveTransactionMode.Read, true)]
    [DataRow(ProductiveTransactionMode.SerializableWrite, false)]
    [DataRow(ProductiveTransactionMode.SerializableWrite, true)]
    public async Task CancelledUnitOfWorkOpeningDisposesUntransferredResources(
        ProductiveTransactionMode mode, bool afterTransactionStarted)
    {
        await using DatabaseScenario scenario = await DatabaseScenario.CreateAsync();
        using var cancellation = new CancellationTokenSource();
        var interceptor = new CancelRoleCommandInterceptor(cancellation);
        var options = new DbContextOptionsBuilder<ProductiveCoreDbContext>()
            .UseNpgsql(scenario.RuntimeConnectionString);
        if (afterTransactionStarted) options.AddInterceptors(interceptor);
        await using var captured = new ProductiveCoreDbContext(options.Options);
        DbConnection connection = captured.Database.GetDbConnection();
        var factory = new PostgresProductiveCoreUnitOfWorkFactory(
            new CancelAfterContextCreationFactory(captured, cancellation, !afterTransactionStarted));

        OperationCanceledException exception = await Assert.ThrowsAsync<OperationCanceledException>(
            async () =>
            {
                await using IProductiveCoreUnitOfWork unexpected = await factory.BeginAsync(mode, cancellation.Token);
            });

        Assert.AreEqual(cancellation.Token, exception.CancellationToken);
        Assert.AreEqual(afterTransactionStarted, interceptor.TransactionWasStarted);
        Assert.AreEqual(ConnectionState.Closed, connection.State);
        Assert.ThrowsExactly<ObjectDisposedException>(() => _ = captured.ChangeTracker);
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public async Task FailedUnitOfWorkOpeningDisposesResourcesWithoutChangingErrorClassification(bool persistenceFailure)
    {
        await using DatabaseScenario scenario = await DatabaseScenario.CreateAsync();
        using var cancellation = new CancellationTokenSource();
        Exception original = persistenceFailure
            ? new TimeoutException("Synthetic transaction setup failure")
            : new ArgumentException("Synthetic transaction setup failure");
        var interceptor = new FailRoleCommandInterceptor(original);
        await using var captured = new ProductiveCoreDbContext(new DbContextOptionsBuilder<ProductiveCoreDbContext>()
            .UseNpgsql(scenario.RuntimeConnectionString).AddInterceptors(interceptor).Options);
        DbConnection connection = captured.Database.GetDbConnection();
        var factory = new PostgresProductiveCoreUnitOfWorkFactory(
            new CancelAfterContextCreationFactory(captured, cancellation, false));

        async Task OpenAsync()
        {
            await using IProductiveCoreUnitOfWork unexpected = await factory.BeginAsync(ProductiveTransactionMode.Read, CancellationToken.None);
        }
        if (persistenceFailure)
        {
            ProductivePersistenceUnavailableException actual = await Assert.ThrowsExactlyAsync<ProductivePersistenceUnavailableException>(OpenAsync);
            Assert.AreSame(original, actual.InnerException);
        }
        else
        {
            ArgumentException actual = await Assert.ThrowsExactlyAsync<ArgumentException>(OpenAsync);
            Assert.AreSame(original, actual);
        }
        Assert.IsTrue(interceptor.TransactionWasStarted);
        Assert.AreEqual(ConnectionState.Closed, connection.State);
        Assert.ThrowsExactly<ObjectDisposedException>(() => _ = captured.ChangeTracker);
    }

    [TestMethod]
    [DataRow(ProductiveTransactionMode.Read)]
    [DataRow(ProductiveTransactionMode.SerializableWrite)]
    public async Task SuccessfulUnitOfWorkOpeningTransfersResourceOwnership(ProductiveTransactionMode mode)
    {
        await using DatabaseScenario scenario = await DatabaseScenario.CreateAsync();
        using var cancellation = new CancellationTokenSource();
        await using var captured = new ProductiveCoreDbContext(new DbContextOptionsBuilder<ProductiveCoreDbContext>()
            .UseNpgsql(scenario.RuntimeConnectionString).Options);
        DbConnection connection = captured.Database.GetDbConnection();
        var factory = new PostgresProductiveCoreUnitOfWorkFactory(
            new CancelAfterContextCreationFactory(captured, cancellation, false));
        await using (IProductiveCoreUnitOfWork unitOfWork = await factory.BeginAsync(mode, CancellationToken.None))
        {
            Assert.AreEqual(ConnectionState.Open, connection.State);
            Assert.IsNotNull(captured.ChangeTracker);
            Assert.IsNotNull(await unitOfWork.AuthorizeOwnerAsync(CycleContext(scenario), CancellationToken.None));
            await unitOfWork.CommitAsync(CancellationToken.None);
        }
        Assert.AreEqual(ConnectionState.Closed, connection.State);
        Assert.ThrowsExactly<ObjectDisposedException>(() => _ = captured.ChangeTracker);
    }

    private sealed class FailRoleCommandInterceptor(Exception original) : DbCommandInterceptor
    {
        public bool TransactionWasStarted { get; private set; }

        public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(
            DbCommand command, CommandEventData eventData, InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            if (command.CommandText == "SET LOCAL ROLE agro_productive_app")
            {
                TransactionWasStarted = command.Transaction is not null;
                throw original;
            }
            return base.NonQueryExecutingAsync(command, eventData, result, cancellationToken);
        }
    }

    private sealed class CancelAfterContextCreationFactory(
        ProductiveCoreDbContext context, CancellationTokenSource cancellation, bool cancelAfterCreation)
        : IDbContextFactory<ProductiveCoreDbContext>
    {
        public ProductiveCoreDbContext CreateDbContext() => context;

        public Task<ProductiveCoreDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            // Deterministically exercise the race after ownership of the context is acquired.
            if (cancelAfterCreation) cancellation.Cancel();
            return Task.FromResult(context);
        }
    }

    private sealed class CancelRoleCommandInterceptor(CancellationTokenSource cancellation) : DbCommandInterceptor
    {
        public bool TransactionWasStarted { get; private set; }

        public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(
            DbCommand command, CommandEventData eventData, InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            if (command.CommandText == "SET LOCAL ROLE agro_productive_app")
            {
                TransactionWasStarted = command.Transaction is not null;
                cancellation.Cancel();
                cancellationToken.ThrowIfCancellationRequested();
            }
            return base.NonQueryExecutingAsync(command, eventData, result, cancellationToken);
        }
    }
}
