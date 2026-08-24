using AgropecuarIA.Catalog.Domain;
using AgropecuarIA.Catalog.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace AgropecuarIA.Catalog.Application;

public sealed record SearchCatalogQuery(
    string? Query = null,
    string? Jurisdiction = null,
    string? Category = null,
    string? SupportLevel = null,
    int Limit = 50);

public sealed record CatalogPublishedItemDto(
    Guid Id,
    string Code,
    string DisplayName,
    string Jurisdiction,
    string SupportLevel,
    string Category,
    IReadOnlyList<string> Synonyms);

public sealed record CatalogSearchResult(
    Guid? VersionId,
    string? VersionTag,
    int TotalCount,
    IReadOnlyList<CatalogPublishedItemDto> Items);

public sealed class CatalogSearchApplicationService(CatalogDbContext dbContext)
{
    public async Task<CatalogSearchResult> SearchAsync(
        SearchCatalogQuery query,
        CancellationToken cancellationToken)
    {
        var activeVersion = await dbContext.CatalogPublishedVersions
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.IsActive, cancellationToken);

        if (activeVersion is null)
        {
            return new CatalogSearchResult(null, null, 0, []);
        }

        var itemsQuery = dbContext.CatalogPublishedItems
            .AsNoTracking()
            .Where(x => x.VersionId == activeVersion.Id && x.IsActive);

        if (!string.IsNullOrWhiteSpace(query.Jurisdiction))
        {
            string jurisdiction = query.Jurisdiction.Trim().ToUpperInvariant();
            itemsQuery = itemsQuery.Where(x => x.Jurisdiction == jurisdiction || x.Jurisdiction == "AR");
        }

        if (!string.IsNullOrWhiteSpace(query.Category))
        {
            string category = query.Category.Trim().ToUpperInvariant();
            itemsQuery = itemsQuery.Where(x => x.Category == category);
        }

        if (!string.IsNullOrWhiteSpace(query.SupportLevel))
        {
            string supportLevel = query.SupportLevel.Trim().ToUpperInvariant();
            itemsQuery = itemsQuery.Where(x => x.SupportLevel == supportLevel);
        }

        if (!string.IsNullOrWhiteSpace(query.Query))
        {
            string normalized = CatalogNameNormalizer.Normalize(query.Query);
            itemsQuery = itemsQuery.Where(x =>
                x.NormalizedCode.Contains(normalized) ||
                x.NormalizedDisplayName.Contains(normalized));
        }

        int limit = Math.Clamp(query.Limit, 1, 100);
        var items = await itemsQuery
            .OrderBy(x => x.DisplayName)
            .Take(limit)
            .Select(x => new CatalogPublishedItemDto(
                x.Id,
                x.Code,
                x.DisplayName,
                x.Jurisdiction,
                x.SupportLevel,
                x.Category,
                x.Synonyms))
            .ToListAsync(cancellationToken);

        int totalCount = await itemsQuery.CountAsync(cancellationToken);

        return new CatalogSearchResult(
            activeVersion.Id,
            activeVersion.VersionTag,
            totalCount,
            items);
    }

    public async Task<CatalogPublishedItemDto?> GetItemByCodeAsync(
        string code,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        string normalized = CatalogNameNormalizer.Normalize(code);

        var activeVersion = await dbContext.CatalogPublishedVersions
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.IsActive, cancellationToken);

        if (activeVersion is null)
            return null;

        var item = await dbContext.CatalogPublishedItems
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.VersionId == activeVersion.Id && x.NormalizedCode == normalized && x.IsActive, cancellationToken);

        return item is null
            ? null
            : new CatalogPublishedItemDto(
                item.Id,
                item.Code,
                item.DisplayName,
                item.Jurisdiction,
                item.SupportLevel,
                item.Category,
                item.Synonyms);
    }
}
