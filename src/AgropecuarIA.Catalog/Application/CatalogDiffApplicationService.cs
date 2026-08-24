using AgropecuarIA.Catalog.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace AgropecuarIA.Catalog.Application;

public sealed record CatalogEditorialDiffResult(
    int TotalStaged,
    int Added,
    int Modified,
    int Removed,
    int Conflicts,
    IReadOnlyList<string> ConflictDetails,
    DateTimeOffset GeneratedAtUtc);

public sealed class CatalogDiffApplicationService(CatalogDbContext dbContext)
{
    public async Task<CatalogEditorialDiffResult> GenerateDiffAsync(CancellationToken cancellationToken)
    {
        var staging = await dbContext.CatalogStagingEntries.AsNoTracking().ToListAsync(cancellationToken);
        
        var groupedByCode = staging.GroupBy(x => x.Code);
        var conflictDetails = new List<string>();
        int conflicts = 0;
        int uniqueAdded = 0;

        foreach (var group in groupedByCode)
        {
            var distinctNames = group.Select(x => x.DisplayName).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            if (distinctNames.Count > 1)
            {
                conflicts++;
                conflictDetails.Add($"Code '{group.Key}' has conflicting names: {string.Join(", ", distinctNames)}");
            }
            else
            {
                uniqueAdded++;
            }
        }

        return new CatalogEditorialDiffResult(
            TotalStaged: staging.Count,
            Added: uniqueAdded,
            Modified: 0,
            Removed: 0,
            Conflicts: conflicts,
            ConflictDetails: conflictDetails,
            GeneratedAtUtc: DateTimeOffset.UtcNow);
    }
}