using AgropecuarIA.Catalog.Domain;
using AgropecuarIA.Catalog.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace AgropecuarIA.Catalog.Application;

public sealed record PublishVersionCommand(string VersionTag, string PublishedBy);

public sealed record CatalogPublishResult(
    Guid VersionId,
    string VersionTag,
    int ItemsCount,
    DateTimeOffset PublishedAtUtc);

public sealed record RollbackVersionCommand(Guid VersionId);

public sealed class CatalogPublicationApplicationService(CatalogDbContext dbContext)
{
    public async Task<CatalogPublishResult> PublishAsync(
        PublishVersionCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentException.ThrowIfNullOrWhiteSpace(command.VersionTag);
        ArgumentException.ThrowIfNullOrWhiteSpace(command.PublishedBy);

        string versionTag = command.VersionTag.Trim();

        bool versionExists = await dbContext.CatalogPublishedVersions
            .AnyAsync(x => x.VersionTag == versionTag, cancellationToken);

        if (versionExists)
        {
            throw new InvalidOperationException($"Catalog version '{versionTag}' already exists.");
        }

        var stagingEntries = await dbContext.CatalogStagingEntries
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        if (stagingEntries.Count == 0)
        {
            throw new InvalidOperationException("Cannot publish empty catalog: staging area contains no entries.");
        }

        DateTimeOffset now = DateTimeOffset.UtcNow;
        Guid versionId = Guid.NewGuid();

        // Deactivate existing active versions
        var activeVersions = await dbContext.CatalogPublishedVersions
            .Where(x => x.IsActive)
            .ToListAsync(cancellationToken);

        foreach (var v in activeVersions)
        {
            v.SetActive(false);
        }

        var publishedVersion = new CatalogPublishedVersion(
            versionId,
            versionTag,
            isActive: true,
            command.PublishedBy,
            stagingEntries.Count,
            now);

        dbContext.CatalogPublishedVersions.Add(publishedVersion);

        foreach (var entry in stagingEntries)
        {
            var publishedItem = new CatalogPublishedItem(
                Guid.NewGuid(),
                versionId,
                entry.Code,
                entry.DisplayName,
                entry.Jurisdiction,
                CatalogSupportLevels.FlujoGenerico,
                CatalogCategories.Otros,
                synonyms: null,
                isActive: true,
                now);

            dbContext.CatalogPublishedItems.Add(publishedItem);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return new CatalogPublishResult(
            versionId,
            versionTag,
            stagingEntries.Count,
            now);
    }

    public async Task<bool> RollbackAsync(
        RollbackVersionCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (command.VersionId == Guid.Empty)
            throw new ArgumentException("VersionId is required.", nameof(command));

        var targetVersion = await dbContext.CatalogPublishedVersions
            .FirstOrDefaultAsync(x => x.Id == command.VersionId, cancellationToken);

        if (targetVersion is null)
        {
            return false;
        }

        var activeVersions = await dbContext.CatalogPublishedVersions
            .Where(x => x.IsActive)
            .ToListAsync(cancellationToken);

        foreach (var v in activeVersions)
        {
            v.SetActive(false);
        }

        targetVersion.SetActive(true);
        await dbContext.SaveChangesAsync(cancellationToken);

        return true;
    }
}
