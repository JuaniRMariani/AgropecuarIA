using AgropecuarIA.ProductiveCore.Domain;
using AgropecuarIA.ProductiveCore.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace AgropecuarIA.ProductiveCore.Application;

public sealed class ProductiveInboxProcessor(ProductiveCoreDbContext dbContext)
{
    public async Task<bool> ProcessMessageAsync(
        Guid messageId,
        string consumerName,
        Guid organizationId,
        Func<Task> handleMessageAsync,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(handleMessageAsync);
        ArgumentException.ThrowIfNullOrWhiteSpace(consumerName);

        if (messageId == Guid.Empty || organizationId == Guid.Empty)
        {
            throw new ArgumentException("MessageId and OrganizationId are required.");
        }

        string normalizedConsumer = consumerName.Trim();

        bool alreadyProcessed = await dbContext.ProductiveInboxEntries
            .AnyAsync(x => x.OrganizationId == organizationId &&
                           x.ConsumerName == normalizedConsumer &&
                           x.MessageId == messageId,
                      cancellationToken);

        if (alreadyProcessed)
        {
            return false; // Idempotent no-op
        }

        await handleMessageAsync();

        DateTimeOffset now = DateTimeOffset.UtcNow;
        var entry = new ProductiveInboxEntry(
            Guid.NewGuid(),
            messageId,
            normalizedConsumer,
            organizationId,
            now);

        dbContext.ProductiveInboxEntries.Add(entry);
        await dbContext.SaveChangesAsync(cancellationToken);

        return true;
    }
}
