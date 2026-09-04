using System.Security.Cryptography;
using System.Text.Json.Serialization;
using AgropecuarIA.Catalog.Domain;
using AgropecuarIA.Catalog.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace AgropecuarIA.Catalog.Application;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record IngestSourceCommand(string SourceId, string ContentBase64);

public sealed class CatalogIngestionApplicationService(CatalogDbContext dbContext)
{
    public Task<bool> IngestAsync(IngestSourceCommand command, CatalogEditorialContext actor, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(actor);
        actor.Validate();
        ParsedCatalogSource parsed = CatalogSourceParser.Parse(command);
        byte[] hash = SHA256.HashData(parsed.Content);
        return CatalogTransaction.RunAsync(dbContext, true, async ct =>
        {
            // Legacy IDs were not canonicalized. Compare bounded metadata in .NET so Unicode trim semantics also agree across platforms.
            var sameHash = await dbContext.CatalogSourceSnapshots.AsNoTracking().Where(x => x.ContentHash == hash)
                .Select(x => new { x.SourceId, x.IsComplete }).Take(10001).ToArrayAsync(ct);
            if (sameHash.Length > 10000) throw CatalogErrors.TooLarge();
            var duplicates = sameHash.Where(x => string.Equals(x.SourceId.Trim().ToLowerInvariant(), parsed.SourceId, StringComparison.Ordinal)).ToArray();
            if (duplicates.Length > 0)
            {
                if (duplicates.Any(x => !x.IsComplete)) throw CatalogErrors.LegacySnapshot();
                if (duplicates.Length > 1) throw CatalogErrors.Conflict();
                return false;
            }

            DateTimeOffset now = CatalogTransaction.UtcNow();
            var snapshot = new CatalogSourceSnapshot(Guid.NewGuid(), parsed.SourceId, hash, now, parsed.Content, parsed.Entries.Count, actor.ActorUserId);
            dbContext.CatalogSourceSnapshots.Add(snapshot);
            foreach (ParsedCatalogEntry item in parsed.Entries)
                dbContext.CatalogStagingEntries.Add(new CatalogStagingEntry(Guid.NewGuid(), parsed.SourceId, hash, item.Code, item.DisplayName,
                    item.Jurisdiction, now, snapshot.Id, item.Category, item.Synonyms));
            dbContext.CatalogEditorialAudits.Add(new(Guid.NewGuid(), "source_ingested", actor.ActorUserId, actor.SessionId,
                actor.CorrelationId, null, snapshot.Id, now));
            await dbContext.SaveChangesAsync(ct);
            return true;
        }, cancellationToken);
    }
}
