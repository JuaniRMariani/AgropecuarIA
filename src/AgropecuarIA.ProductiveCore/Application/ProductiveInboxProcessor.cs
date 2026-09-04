using AgropecuarIA.ProductiveCore.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace AgropecuarIA.ProductiveCore.Application;

/// <summary>
/// Deduplicates database-only handlers in the same local transaction. This is not an
/// exactly-once guarantee for HTTP calls, files, messages, or another database.
/// No dispatcher or external consumer is provisioned by this component.
/// </summary>
public sealed class ProductiveInboxProcessor(ProductiveCoreDbContext dbContext, TimeProvider timeProvider)
{
    public async Task<bool> ProcessMessageAsync(
        Guid messageId,
        string consumerName,
        Guid organizationId,
        Func<ProductiveCoreDbContext, CancellationToken, Task> handleMessageAsync,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(handleMessageAsync);
        ArgumentException.ThrowIfNullOrWhiteSpace(consumerName);

        if (messageId == Guid.Empty || organizationId == Guid.Empty)
        {
            throw new ArgumentException("MessageId and OrganizationId are required.");
        }

        string normalizedConsumer = consumerName.Trim();
        if (normalizedConsumer.Length > 128)
        {
            throw new ArgumentException("ConsumerName must contain at most 128 characters.", nameof(consumerName));
        }
        if (dbContext.Database.CurrentTransaction is not null || dbContext.ChangeTracker.HasChanges())
        {
            throw new InvalidOperationException("Inbox processing requires a clean, dedicated database context.");
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            // The unique key arbitrates concurrent attempts before invoking a handler.
            // A rollback removes this provisional marker together with its local effects.
            int claimed = await dbContext.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT INTO productive_core.inbox_entries
                    ("Id", "MessageId", "ConsumerName", "OrganizationId", "ProcessedAtUtc")
                VALUES ({Guid.NewGuid()}, {messageId}, {normalizedConsumer}, {organizationId}, {timeProvider.GetUtcNow()})
                ON CONFLICT ("OrganizationId", "ConsumerName", "MessageId") DO NOTHING
                """, cancellationToken);
            if (claimed == 0)
            {
                await transaction.CommitAsync(cancellationToken);
                return false;
            }

            await handleMessageAsync(dbContext, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return true;
        }
        catch
        {
            await transaction.RollbackAsync(CancellationToken.None);
            dbContext.ChangeTracker.Clear();
            throw;
        }
    }
}
