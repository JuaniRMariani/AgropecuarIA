using AgropecuarIA.Catalog.Domain;
using AgropecuarIA.Catalog.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace AgropecuarIA.Catalog.Application;

public sealed record PublishVersionCommand(string VersionTag, string CandidateHash);
public sealed record CatalogPublishResult(Guid VersionId, string VersionTag, int ItemsCount, DateTimeOffset PublishedAtUtc);
public sealed record RollbackVersionCommand(Guid VersionId);

public sealed class CatalogPublicationApplicationService(CatalogDbContext dbContext)
{
    public Task<CatalogPublishResult> PublishAsync(PublishVersionCommand command, CatalogEditorialContext actor, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(actor);
        actor.Validate();
        string tag = command.VersionTag?.Trim() ?? string.Empty;
        if (tag.Length is < 1 or > 64 || tag.Any(char.IsControl) || command.CandidateHash is not { Length: 64 } ||
            command.CandidateHash.Any(c => !char.IsAsciiHexDigit(c) || char.IsUpper(c))) throw CatalogErrors.InvalidRequest();
        return CatalogTransaction.RunAsync(dbContext, true, async ct =>
        {
            CatalogCandidate candidate = await CatalogCandidateBuilder.BuildAsync(dbContext, ct);
            if (candidate.Diff.CandidateHash != command.CandidateHash) throw CatalogErrors.Stale();
            if (candidate.Diff.Conflicts > 0) throw CatalogErrors.Conflict();
            if (candidate.Entries.Count == 0) throw CatalogErrors.EmptyCandidate();
            if (await dbContext.CatalogPublishedVersions.AnyAsync(v => v.VersionTag == tag, ct)) throw CatalogErrors.VersionExists();
            DateTimeOffset now = CatalogTransaction.UtcNow();
            Guid versionId = Guid.NewGuid();
            // Persist deactivation before inserting the new active row; both statements remain inside the outer transaction.
            await dbContext.CatalogPublishedVersions.Where(v => v.IsActive).ExecuteUpdateAsync(setters => setters.SetProperty(v => v.IsActive, false), ct);
            dbContext.CatalogPublishedVersions.Add(new(versionId, tag, true, actor.ActorUserId.ToString("D"), candidate.Entries.Count, now, command.CandidateHash));
            foreach (CatalogSourceSnapshot source in candidate.Snapshots) dbContext.CatalogPublishedSources.Add(new(versionId, source.Id));
            foreach (CatalogStagingEntry entry in candidate.Entries)
                dbContext.CatalogPublishedItems.Add(new(Guid.NewGuid(), versionId, entry.Code, entry.DisplayName, entry.Jurisdiction,
                    CatalogSupportLevels.FlujoGenerico, entry.Category, entry.Synonyms, true, now, entry.SourceSnapshotId));
            var audit = new CatalogEditorialAudit(Guid.NewGuid(), "catalog_published", actor.ActorUserId, actor.SessionId, actor.CorrelationId, versionId, null, now);
            dbContext.CatalogEditorialAudits.Add(audit);
            dbContext.CatalogOutboxMessages.Add(CatalogOutboxMessage.Create(CatalogIntegrationEvents.ProductCatalogPublished, versionId,
                audit.Id, actor.ActorUserId, actor.CorrelationId, now, new ProductCatalogPublishedPayload(versionId, tag, candidate.ActiveVersion?.Id,
                    candidate.Entries.Count, command.CandidateHash, candidate.Snapshots.Select(s => s.Id).ToArray(), now)));
            await dbContext.SaveChangesAsync(ct);
            return new CatalogPublishResult(versionId, tag, candidate.Entries.Count, now);
        }, cancellationToken);
    }

    public Task<bool> RollbackAsync(RollbackVersionCommand command, CatalogEditorialContext actor, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(actor);
        actor.Validate();
        if (command.VersionId == Guid.Empty) throw CatalogErrors.InvalidRequest();
        return CatalogTransaction.RunAsync(dbContext, true, async ct =>
        {
            CatalogPublishedVersion? target = await dbContext.CatalogPublishedVersions.AsNoTracking().SingleOrDefaultAsync(v => v.Id == command.VersionId, ct);
            if (target is null) return false;
            CatalogPublishedVersion? prior = await dbContext.CatalogPublishedVersions.AsNoTracking().SingleOrDefaultAsync(v => v.IsActive, ct);
            if (prior?.Id == target.Id) return true;
            await dbContext.CatalogPublishedVersions.Where(v => v.IsActive).ExecuteUpdateAsync(setters => setters.SetProperty(v => v.IsActive, false), ct);
            await dbContext.CatalogPublishedVersions.Where(v => v.Id == target.Id).ExecuteUpdateAsync(setters => setters.SetProperty(v => v.IsActive, true), ct);
            DateTimeOffset now = CatalogTransaction.UtcNow();
            var audit = new CatalogEditorialAudit(Guid.NewGuid(), "catalog_rolled_back", actor.ActorUserId, actor.SessionId, actor.CorrelationId, target.Id, null, now);
            dbContext.CatalogEditorialAudits.Add(audit);
            dbContext.CatalogOutboxMessages.Add(CatalogOutboxMessage.Create(CatalogIntegrationEvents.ProductCatalogRolledBack, target.Id,
                audit.Id, actor.ActorUserId, actor.CorrelationId, now, new ProductCatalogRolledBackPayload(target.Id, prior?.Id, now)));
            await dbContext.SaveChangesAsync(ct);
            return true;
        }, cancellationToken);
    }
}
