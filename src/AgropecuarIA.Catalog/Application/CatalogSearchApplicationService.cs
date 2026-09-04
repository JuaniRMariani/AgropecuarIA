using AgropecuarIA.Catalog.Domain;
using AgropecuarIA.Catalog.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace AgropecuarIA.Catalog.Application;

public sealed record SearchCatalogQuery(string? Query = null, string? Jurisdiction = null, string? Category = null, string? SupportLevel = null, int Limit = 50, Guid? VersionId = null);
public sealed record CatalogPublishedItemDto(Guid Id, string Code, string DisplayName, string Jurisdiction, string SupportLevel, string Category,
    IReadOnlyList<string> Synonyms, Guid VersionId, string VersionTag, Guid? ActiveVersionId, Guid? SourceSnapshotId, string? SourceId,
    string? SourceHash, DateTimeOffset? SourceIngestedAtUtc, string ProvenanceStatus, IReadOnlyList<string> Capabilities, IReadOnlyList<string> AbsentCapabilities);
public sealed record CatalogSearchResult(Guid? VersionId, string? VersionTag, int TotalCount, IReadOnlyList<CatalogPublishedItemDto> Items,
    Guid? ActiveVersionId, DateTimeOffset? PublishedAtUtc, bool IsHistorical);
public sealed record CatalogVersionDto(Guid Id, string VersionTag, DateTimeOffset PublishedAtUtc, bool IsActive, int ItemsCount);
public sealed record CatalogVersionsResult(Guid? ActiveVersionId, int TotalCount, bool HasMore, IReadOnlyList<CatalogVersionDto> Versions);
public enum CatalogActiveItemResolutionStatus { Resolved, NotPublished, VersionStale, ItemNotFound }
public sealed record CatalogActiveItemResolution(CatalogActiveItemResolutionStatus Status, CatalogPublishedItemDto? Item, DateTimeOffset ResolvedAtUtc);

public sealed class CatalogSearchApplicationService(CatalogDbContext dbContext)
{
    private static readonly IReadOnlyList<string> MissingCapabilities = Array.AsReadOnly(new[] { "specialized_rules", "specialized_kpis", "ai_recommendations" });

    /// <summary>Resolves the active publication as observed by one MVCC read snapshot. Expected version is a precondition, never a historical selector.</summary>
    public Task<CatalogActiveItemResolution> ResolveActiveItemAsync(string code, Guid? expectedVersionId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(code) || code.Length > 64 || code.Any(char.IsControl) || expectedVersionId == Guid.Empty)
            throw CatalogErrors.InvalidRequest();
        string normalized = CatalogNameNormalizer.Normalize(code);
        return CatalogTransaction.RunAsync(dbContext, false, async ct =>
        {
            CatalogPublishedVersion? active = await dbContext.CatalogPublishedVersions.AsNoTracking().SingleOrDefaultAsync(v => v.IsActive, ct);
            DateTimeOffset resolvedAt = CatalogTransaction.UtcNow();
            if (expectedVersionId is not null && expectedVersionId != active?.Id)
                return new CatalogActiveItemResolution(CatalogActiveItemResolutionStatus.VersionStale, null, resolvedAt);
            if (active is null) return new CatalogActiveItemResolution(CatalogActiveItemResolutionStatus.NotPublished, null, resolvedAt);
            CatalogPublishedItem? item = await dbContext.CatalogPublishedItems.AsNoTracking()
                .SingleOrDefaultAsync(i => i.VersionId == active.Id && i.NormalizedCode == normalized && i.IsActive, ct);
            if (item is null) return new CatalogActiveItemResolution(CatalogActiveItemResolutionStatus.ItemNotFound, null, resolvedAt);
            Dictionary<Guid, CatalogSourceSnapshot> sources = await SourcesAsync([item], ct);
            return new CatalogActiveItemResolution(CatalogActiveItemResolutionStatus.Resolved, ToDto(item, active, active.Id, sources), resolvedAt);
        }, cancellationToken);
    }

    public Task<CatalogSearchResult> SearchAsync(SearchCatalogQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        ValidateQuery(query);
        return CatalogTransaction.RunAsync(dbContext, false, async ct =>
        {
            (CatalogPublishedVersion? version, Guid? activeId) = await ResolveVersionAsync(query.VersionId, ct);
            if (version is null) return new CatalogSearchResult(null, null, 0, [], null, null, false);
            IQueryable<CatalogPublishedItem> items = dbContext.CatalogPublishedItems.AsNoTracking().Where(i => i.VersionId == version.Id && i.IsActive);
            if (!string.IsNullOrWhiteSpace(query.Query))
            {
                string normalized = CatalogNameNormalizer.Normalize(query.Query);
                items = items.Where(i => i.NormalizedCode.Contains(normalized) || i.NormalizedDisplayName.Contains(normalized) || i.NormalizedSynonyms.Any(alias => alias.Contains(normalized)));
            }
            if (!string.IsNullOrWhiteSpace(query.Jurisdiction))
            {
                string jurisdiction = query.Jurisdiction.Trim().ToUpperInvariant();
                items = items.Where(i => i.Jurisdiction == jurisdiction || i.Jurisdiction == "AR");
            }
            if (!string.IsNullOrWhiteSpace(query.Category))
            {
                string category = query.Category.Trim().ToUpperInvariant();
                items = items.Where(i => i.Category == category);
            }
            if (!string.IsNullOrWhiteSpace(query.SupportLevel))
            {
                string support = query.SupportLevel.Trim().ToUpperInvariant();
                items = items.Where(i => i.SupportLevel == support);
            }

            int total = await items.CountAsync(ct);
            CatalogPublishedItem[] page = await items.OrderBy(i => i.DisplayName).ThenBy(i => i.Code).ThenBy(i => i.Id).Take(query.Limit).ToArrayAsync(ct);
            Dictionary<Guid, CatalogSourceSnapshot> sources = await SourcesAsync(page, ct);
            return new CatalogSearchResult(version.Id, version.VersionTag, total, page.Select(i => ToDto(i, version, activeId, sources)).ToArray(),
                activeId, version.PublishedAtUtc, version.Id != activeId);
        }, cancellationToken);
    }

    public Task<CatalogPublishedItemDto?> GetItemByCodeAsync(string code, CancellationToken cancellationToken, Guid? versionId = null)
    {
        if (string.IsNullOrWhiteSpace(code) || code.Length > 64 || code.Any(char.IsControl) || versionId == Guid.Empty) throw CatalogErrors.InvalidRequest();
        string normalized = CatalogNameNormalizer.Normalize(code);
        return CatalogTransaction.RunAsync<CatalogPublishedItemDto?>(dbContext, false, async ct =>
        {
            (CatalogPublishedVersion? version, Guid? activeId) = await ResolveVersionAsync(versionId, ct);
            if (version is null) return null;
            CatalogPublishedItem? item = await dbContext.CatalogPublishedItems.AsNoTracking()
                .SingleOrDefaultAsync(i => i.VersionId == version.Id && i.NormalizedCode == normalized && i.IsActive, ct);
            if (item is null) return null;
            Dictionary<Guid, CatalogSourceSnapshot> sources = await SourcesAsync([item], ct);
            return ToDto(item, version, activeId, sources);
        }, cancellationToken);
    }

    public Task<CatalogVersionsResult> ListVersionsAsync(int limit, int offset, CancellationToken cancellationToken)
    {
        if (limit is < 1 or > 100 || offset is < 0 or > 10000) throw CatalogErrors.InvalidRequest();
        return CatalogTransaction.RunAsync(dbContext, false, async ct =>
        {
            Guid? active = await dbContext.CatalogPublishedVersions.AsNoTracking().Where(v => v.IsActive).Select(v => (Guid?)v.Id).SingleOrDefaultAsync(ct);
            int total = await dbContext.CatalogPublishedVersions.CountAsync(ct);
            CatalogVersionDto[] versions = await dbContext.CatalogPublishedVersions.AsNoTracking().OrderByDescending(v => v.PublishedAtUtc).ThenByDescending(v => v.Id)
                .Skip(offset).Take(limit).Select(v => new CatalogVersionDto(v.Id, v.VersionTag, v.PublishedAtUtc, v.IsActive, v.ItemsCount)).ToArrayAsync(ct);
            return new CatalogVersionsResult(active, total, offset + versions.Length < total, versions);
        }, cancellationToken);
    }

    private async Task<(CatalogPublishedVersion? Version, Guid? ActiveId)> ResolveVersionAsync(Guid? id, CancellationToken ct)
    {
        CatalogPublishedVersion? active = await dbContext.CatalogPublishedVersions.AsNoTracking().SingleOrDefaultAsync(v => v.IsActive, ct);
        if (id is null) return (active, active?.Id);
        CatalogPublishedVersion version = await dbContext.CatalogPublishedVersions.AsNoTracking().SingleOrDefaultAsync(v => v.Id == id, ct)
            ?? throw CatalogErrors.VersionNotFound();
        return (version, active?.Id);
    }

    private async Task<Dictionary<Guid, CatalogSourceSnapshot>> SourcesAsync(IReadOnlyCollection<CatalogPublishedItem> items, CancellationToken ct)
    {
        Guid[] ids = items.Where(i => i.SourceSnapshotId != null).Select(i => i.SourceSnapshotId!.Value).Distinct().ToArray();
        return await dbContext.CatalogSourceSnapshots.AsNoTracking().Where(s => ids.Contains(s.Id)).ToDictionaryAsync(s => s.Id, ct);
    }

    private static CatalogPublishedItemDto ToDto(CatalogPublishedItem item, CatalogPublishedVersion version, Guid? activeId, Dictionary<Guid, CatalogSourceSnapshot> sources)
    {
        CatalogSourceSnapshot? source = item.SourceSnapshotId is Guid id && sources.TryGetValue(id, out CatalogSourceSnapshot? found) ? found : null;
        if (source is not { IsComplete: true }) source = null;
        return new(item.Id, item.Code, item.DisplayName, item.Jurisdiction, item.SupportLevel, item.Category, item.Synonyms, version.Id, version.VersionTag, activeId,
            source?.Id, source?.SourceId, source is null ? null : Convert.ToHexStringLower(source.ContentHash), source?.CreatedAtUtc,
            source is { IsComplete: true } ? "verified_snapshot" : "legacy_unavailable", [], MissingCapabilities);
    }

    private static void ValidateQuery(SearchCatalogQuery query)
    {
        if (query.Limit is < 1 or > 100 || query.VersionId == Guid.Empty || query.Query?.Length > 256 || query.Jurisdiction?.Length > 64 ||
            query.Category?.Length > 64 || query.SupportLevel?.Length > 64 || query.Query?.Any(char.IsControl) == true ||
            (!string.IsNullOrWhiteSpace(query.Category) && !CatalogCategories.IsValid(query.Category.Trim().ToUpperInvariant())) ||
            (!string.IsNullOrWhiteSpace(query.SupportLevel) && !CatalogSupportLevels.IsValid(query.SupportLevel.Trim().ToUpperInvariant()))) throw CatalogErrors.InvalidRequest();
    }
}
