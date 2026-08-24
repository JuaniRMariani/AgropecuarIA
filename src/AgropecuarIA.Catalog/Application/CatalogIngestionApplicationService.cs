using System.Text.Json;
using AgropecuarIA.Catalog.Domain;
using AgropecuarIA.Catalog.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace AgropecuarIA.Catalog.Application;

public sealed record IngestSourceCommand(string SourceId, string ContentBase64);

public sealed record RawCatalogEntryDto(string Code, string DisplayName, string? Jurisdiction);

public sealed class CatalogIngestionApplicationService(CatalogDbContext dbContext)
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task<bool> IngestAsync(IngestSourceCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentException.ThrowIfNullOrWhiteSpace(command.SourceId);
        ArgumentException.ThrowIfNullOrWhiteSpace(command.ContentBase64);

        byte[] content = Convert.FromBase64String(command.ContentBase64);
        byte[] hash = System.Security.Cryptography.SHA256.HashData(content);

        bool alreadyIngested = await dbContext.CatalogSourceSnapshots
            .AnyAsync(x => x.SourceId == command.SourceId && x.ContentHash == hash, cancellationToken);

        if (alreadyIngested)
            return false;

        DateTimeOffset now = DateTimeOffset.UtcNow;
        var snapshot = new CatalogSourceSnapshot(Guid.NewGuid(), command.SourceId, hash, now);
        dbContext.CatalogSourceSnapshots.Add(snapshot);

        try
        {
            var entries = JsonSerializer.Deserialize<List<RawCatalogEntryDto>>(content, SerializerOptions);

            if (entries is not null && entries.Count > 0)
            {
                foreach (var item in entries)
                {
                    if (string.IsNullOrWhiteSpace(item.Code) || string.IsNullOrWhiteSpace(item.DisplayName))
                        continue;

                    var stagingEntry = new CatalogStagingEntry(
                        Guid.NewGuid(),
                        command.SourceId,
                        hash,
                        item.Code,
                        item.DisplayName,
                        item.Jurisdiction ?? "AR",
                        now);

                    dbContext.CatalogStagingEntries.Add(stagingEntry);
                }
            }
        }
        catch (JsonException)
        {
            // Non-JSON binary or raw source snapshot preserved as snapshot without staging records
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}