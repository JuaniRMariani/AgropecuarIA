using AgropecuarIA.Catalog.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace AgropecuarIA.Catalog.Application;

public sealed class CatalogDiffApplicationService(CatalogDbContext dbContext)
{
    public async Task<object> GenerateDiffAsync(CancellationToken cancellationToken)
    {
        // MVP: Just return staging entries vs empty active entries.
        var staging = await dbContext.CatalogStagingEntries.ToListAsync(cancellationToken);
        return new { Added = staging.Count, Modified = 0, Removed = 0, Conflicts = 0 };
    }
}