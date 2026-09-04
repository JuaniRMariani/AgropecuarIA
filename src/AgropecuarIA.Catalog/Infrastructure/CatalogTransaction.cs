using System.Data;
using AgropecuarIA.Catalog.Application;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;

namespace AgropecuarIA.Catalog.Infrastructure;

internal static class CatalogTransaction
{
    public static DateTimeOffset UtcNow()
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        return new DateTimeOffset(now.Ticks - (now.Ticks % 10), TimeSpan.Zero);
    }

    public static async Task<T> RunAsync<T>(CatalogDbContext database, bool editorial,
        Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken)
    {
        try
        {
            await using IDbContextTransaction transaction = await database.Database.BeginTransactionAsync(
                editorial ? IsolationLevel.ReadCommitted : IsolationLevel.RepeatableRead, cancellationToken);
            if (editorial)
            {
                // Shared transaction-scoped lock for every editorial operation, acquired BEFORE reading any source or active version.
                // READ COMMITTED is intentional: a waiter must observe the preceding holder's committed publication.
                await database.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(1480870993, 1128354865)", cancellationToken);
            }

            T result = await operation(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return result;
        }
        catch (CatalogOperationException)
        {
            database.ChangeTracker.Clear();
            throw;
        }
        catch (Exception exception) when (exception is DbUpdateException or NpgsqlException or TimeoutException or InvalidOperationException)
        {
            database.ChangeTracker.Clear();
            throw CatalogErrors.Unavailable();
        }
    }
}
