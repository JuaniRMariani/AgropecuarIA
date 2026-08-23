using AgropecuarIA.Catalog.Domain;
using AgropecuarIA.Catalog.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace AgropecuarIA.Catalog.Application;

public sealed record IngestSourceCommand(string SourceId, string ContentBase64);

public sealed class CatalogIngestionApplicationService(CatalogDbContext dbContext)
{
    public async Task<bool> IngestAsync(IngestSourceCommand command, CancellationToken cancellationToken)
    {
        // MVP: Just hash and snapshot.
        byte[] content = Convert.FromBase64String(command.ContentBase64);
        byte[] hash = System.Security.Cryptography.SHA256.HashData(content);

        bool alreadyIngested = await dbContext.CatalogSourceSnapshots
            .AnyAsync(x => x.SourceId == command.SourceId && x.ContentHash == hash, cancellationToken);

        if (alreadyIngested)
            return false;

        var snapshot = new CatalogSourceSnapshot(Guid.NewGuid(), command.SourceId, hash, DateTimeOffset.UtcNow);
        dbContext.CatalogSourceSnapshots.Add(snapshot);
        
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}