using AgropecuarIA.Territory.Application;
using AgropecuarIA.Territory.Domain;
using Microsoft.EntityFrameworkCore;

namespace AgropecuarIA.Territory.Infrastructure;

public sealed class PostgresTerritoryReferenceRepository(TerritoryDbContext dbContext)
    : ITerritoryReferenceReader, ITerritorySnapshotImporter
{
    public async Task<TerritoryReferenceSearchPage?> SearchAsync(
        TerritoryReferenceSearchCriteria criteria,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(criteria);

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        await dbContext.Database.ExecuteSqlRawAsync(
            "SET LOCAL ROLE agro_territory_app",
            cancellationToken);

        OfficialTerritorySnapshot? snapshot = await dbContext.OfficialTerritorySnapshots
            .AsNoTracking()
            .SingleOrDefaultAsync(
                item => item.Status == TerritorySnapshotStatuses.Active,
                cancellationToken);
        if (snapshot is null)
        {
            await transaction.CommitAsync(cancellationToken);
            return null;
        }

        IQueryable<OfficialTerritoryUnit> query = dbContext.OfficialTerritoryUnits
            .AsNoTracking()
            .Where(item =>
                item.SnapshotId == snapshot.Id &&
                item.NormalizedName.Contains(criteria.NormalizedQuery));
        if (criteria.Level is not null)
        {
            query = query.Where(item => item.Level == criteria.Level);
        }

        if (criteria.ParentCode is not null)
        {
            query = query.Where(item => item.ParentCode == criteria.ParentCode);
        }

        List<OfficialTerritoryUnit> matches = await query
            .OrderBy(item => item.NormalizedName.StartsWith(criteria.NormalizedQuery) ? 0 : 1)
            .ThenBy(item => item.NormalizedName)
            .ThenBy(item => item.Name)
            .ThenBy(item => item.OfficialCode)
            .Take(criteria.Limit)
            .ToListAsync(cancellationToken);

        Dictionary<string, OfficialTerritoryUnit> hierarchy = matches
            .ToDictionary(item => item.OfficialCode, StringComparer.Ordinal);
        HashSet<string> unresolvedParents = matches
            .Where(item => item.ParentCode is not null)
            .Select(item => item.ParentCode!)
            .ToHashSet(StringComparer.Ordinal);
        while (unresolvedParents.Count > 0)
        {
            string[] requestedCodes = unresolvedParents
                .Where(code => !hierarchy.ContainsKey(code))
                .ToArray();
            if (requestedCodes.Length == 0)
            {
                break;
            }

            List<OfficialTerritoryUnit> parents = await dbContext.OfficialTerritoryUnits
                .AsNoTracking()
                .Where(item =>
                    item.SnapshotId == snapshot.Id &&
                    requestedCodes.Contains(item.OfficialCode))
                .ToListAsync(cancellationToken);
            unresolvedParents.Clear();
            foreach (OfficialTerritoryUnit parent in parents)
            {
                hierarchy[parent.OfficialCode] = parent;
                if (parent.ParentCode is not null && !hierarchy.ContainsKey(parent.ParentCode))
                {
                    unresolvedParents.Add(parent.ParentCode);
                }
            }

            if (parents.Count == 0)
            {
                break;
            }
        }

        TerritoryReferenceMatch[] items = matches
            .Select(item => ToMatch(item, hierarchy))
            .ToArray();
        await transaction.CommitAsync(cancellationToken);
        return new TerritoryReferenceSearchPage(
            new TerritoryReferenceSource(
                snapshot.Provider,
                snapshot.Version,
                snapshot.CapturedAtUtc),
            items);
    }

    public async Task ImportAndActivateAsync(
        ValidatedTerritorySnapshot snapshot,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        await dbContext.Database.ExecuteSqlRawAsync(
            "SET LOCAL ROLE agro_territory_importer",
            cancellationToken);
        dbContext.OfficialTerritorySnapshots.Add(snapshot.Snapshot);
        dbContext.OfficialTerritoryUnits.AddRange(snapshot.Units);
        await dbContext.SaveChangesAsync(cancellationToken);
        await dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT territory.activate_official_snapshot({snapshot.Snapshot.Id})",
            cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        dbContext.ChangeTracker.Clear();
    }

    private static TerritoryReferenceMatch ToMatch(
        OfficialTerritoryUnit unit,
        Dictionary<string, OfficialTerritoryUnit> hierarchy)
    {
        List<string> labels = [unit.Name];
        HashSet<string> visited = [unit.OfficialCode];
        string? parentCode = unit.ParentCode;
        while (parentCode is not null && hierarchy.TryGetValue(parentCode, out OfficialTerritoryUnit? parent))
        {
            if (!visited.Add(parent.OfficialCode))
            {
                throw new InvalidOperationException("An active territory hierarchy contains a cycle.");
            }

            labels.Add(parent.Name);
            parentCode = parent.ParentCode;
        }

        string? parentName = unit.ParentCode is not null &&
            hierarchy.TryGetValue(unit.ParentCode, out OfficialTerritoryUnit? immediateParent)
                ? immediateParent.Name
                : null;
        return new TerritoryReferenceMatch(
            unit.OfficialCode,
            unit.Name,
            unit.Level,
            unit.ParentCode,
            parentName,
            string.Join(", ", labels));
    }
}
