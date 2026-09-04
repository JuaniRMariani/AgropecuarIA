namespace AgropecuarIA.Catalog.Domain;

public sealed class CatalogSourceSnapshot
{
    private CatalogSourceSnapshot() { }

    public CatalogSourceSnapshot(
        Guid id,
        string sourceId,
        byte[] contentHash,
        DateTimeOffset createdAtUtc,
        byte[]? rawContent = null,
        int entryCount = 0,
        Guid? ingestedBy = null)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Id is required.");

        ArgumentException.ThrowIfNullOrWhiteSpace(sourceId);

        if (contentHash is not { Length: 32 })
            throw new ArgumentException("The content hash must contain 32 bytes.", nameof(contentHash));

        Id = id;
        SourceId = sourceId;
        ContentHash = contentHash.ToArray();
        CreatedAtUtc = createdAtUtc;
        RawContent = rawContent?.ToArray();
        EntryCount = entryCount;
        IngestedBy = ingestedBy;
        IsComplete = rawContent is not null && ingestedBy is not null;
    }

    public Guid Id { get; private set; }
    public string SourceId { get; private set; } = string.Empty;
    public byte[] ContentHash { get; private set; } = [];
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public byte[]? RawContent { get; private set; }
    public int EntryCount { get; private set; }
    public Guid? IngestedBy { get; private set; }
    public bool IsComplete { get; private set; }
    public long IngestionSequence { get; private set; }
}
