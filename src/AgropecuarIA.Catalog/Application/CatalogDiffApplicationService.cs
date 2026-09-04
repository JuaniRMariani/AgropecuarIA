using System.Security.Cryptography;
using System.Text.Json;
using AgropecuarIA.Catalog.Domain;
using AgropecuarIA.Catalog.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace AgropecuarIA.Catalog.Application;

public sealed record CatalogSelectedSnapshot(Guid SnapshotId, string SourceId, string ContentHash, DateTimeOffset IngestedAtUtc, int EntryCount);
public sealed record CatalogEditorialDiffResult(int TotalStaged, int Added, int Modified, int Removed, int Conflicts,
    IReadOnlyList<string> ConflictDetails, DateTimeOffset GeneratedAtUtc, string CandidateHash, Guid? ActiveVersionId,
    IReadOnlyList<CatalogSelectedSnapshot> SelectedSnapshots);

internal sealed record CatalogCandidate(CatalogPublishedVersion? ActiveVersion, IReadOnlyList<CatalogSourceSnapshot> Snapshots,
    IReadOnlyList<CatalogStagingEntry> Entries, CatalogEditorialDiffResult Diff);

public sealed class CatalogDiffApplicationService(CatalogDbContext dbContext)
{
    public Task<CatalogEditorialDiffResult> GenerateDiffAsync(CancellationToken cancellationToken) =>
        CatalogTransaction.RunAsync(dbContext, true, async ct => (await CatalogCandidateBuilder.BuildAsync(dbContext, ct)).Diff, cancellationToken);
}

internal static class CatalogCandidateBuilder
{
    public static async Task<CatalogCandidate> BuildAsync(CatalogDbContext database, CancellationToken ct)
    {
        CatalogPublishedVersion? active = await database.CatalogPublishedVersions.AsNoTracking().SingleOrDefaultAsync(x => x.IsActive, ct);
        CatalogSourceSnapshot[] selected = await database.CatalogSourceSnapshots.AsNoTracking()
            .Where(s => s.IsComplete && !database.CatalogSourceSnapshots.Any(newer => newer.IsComplete && newer.SourceId == s.SourceId && newer.IngestionSequence > s.IngestionSequence))
            .OrderBy(s => s.SourceId).Take(65).ToArrayAsync(ct);
        if (selected.Length > 64) throw CatalogErrors.TooLarge();
        Guid[] selectedIds = selected.Select(s => s.Id).ToArray();
        CatalogStagingEntry[] entries = await database.CatalogStagingEntries.AsNoTracking()
            .Where(s => s.SourceSnapshotId != null && selectedIds.Contains(s.SourceSnapshotId.Value)).Take(50001).ToArrayAsync(ct);
        if (entries.Length > 50000) throw CatalogErrors.TooLarge();
        if (selected.Any(s => entries.Count(e => e.SourceSnapshotId == s.Id) != s.EntryCount)) throw CatalogErrors.Unavailable();
        entries = entries.OrderBy(e => e.NormalizedCode, StringComparer.Ordinal).ThenBy(e => e.SourceId, StringComparer.Ordinal).ToArray();
        var conflicts = new List<string>();
        var identifiers = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (CatalogStagingEntry entry in entries)
        {
            foreach (string identifier in new[] { entry.NormalizedCode }.Concat(entry.Synonyms.Select(CatalogNameNormalizer.Normalize)))
            {
                if (!identifiers.TryAdd(identifier, entry.SourceId)) conflicts.Add($"Normalized identifier '{identifier}' occurs more than once in the selected sources.");
            }
        }

        CatalogPublishedItem[] prior = active is null ? [] : await database.CatalogPublishedItems.AsNoTracking().Where(i => i.VersionId == active.Id).ToArrayAsync(ct);
        Dictionary<string, CatalogPublishedItem> previous = prior.ToDictionary(i => i.NormalizedCode, StringComparer.Ordinal);
        HashSet<string> nextCodes = entries.Select(e => e.NormalizedCode).ToHashSet(StringComparer.Ordinal);
        int added = nextCodes.Count(code => !previous.ContainsKey(code));
        int removed = previous.Keys.Count(code => !nextCodes.Contains(code));
        int modified = entries.GroupBy(e => e.NormalizedCode, StringComparer.Ordinal).Count(group => previous.TryGetValue(group.Key, out CatalogPublishedItem? old) &&
            group.Any(entry => !Equivalent(entry, old)));
        var manifests = selected.OrderBy(s => s.SourceId, StringComparer.Ordinal).Select(s => new CatalogSelectedSnapshot(s.Id, s.SourceId,
            Convert.ToHexStringLower(s.ContentHash), s.CreatedAtUtc, s.EntryCount)).ToArray();
        byte[] canonical = JsonSerializer.SerializeToUtf8Bytes(new
        {
            protocol = "catalog-candidate-v2",
            activeVersionId = active?.Id,
            sources = manifests.Select(s => new { s.SnapshotId, s.SourceId, s.ContentHash, s.EntryCount }),
            items = entries.Select(e => new
            {
                e.SourceSnapshotId,
                e.Code,
                e.NormalizedCode,
                e.DisplayName,
                e.Jurisdiction,
                e.Category,
                synonyms = e.Synonyms.Order(StringComparer.Ordinal),
                supportLevel = CatalogSupportLevels.FlujoGenerico
            }),
        });
        var diff = new CatalogEditorialDiffResult(entries.Length, added, modified, removed, conflicts.Count, conflicts.Take(100).ToArray(),
            DateTimeOffset.UtcNow, Convert.ToHexStringLower(SHA256.HashData(canonical)), active?.Id, manifests);
        return new(active, selected, entries, diff);
    }

    private static bool Equivalent(CatalogStagingEntry current, CatalogPublishedItem previous) =>
        current.Code == previous.Code && current.DisplayName == previous.DisplayName && current.Jurisdiction == previous.Jurisdiction &&
        current.Category == previous.Category && previous.SupportLevel == CatalogSupportLevels.FlujoGenerico && previous.IsActive &&
        current.SourceSnapshotId == previous.SourceSnapshotId &&
        current.Synonyms.Order(StringComparer.Ordinal).SequenceEqual(previous.Synonyms.Order(StringComparer.Ordinal), StringComparer.Ordinal);
}
