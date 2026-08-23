namespace AgropecuarIA.Catalog.Domain;

public sealed class CatalogSourceSnapshot
{
    private CatalogSourceSnapshot() { }

    public CatalogSourceSnapshot(
        Guid id,
        string sourceId,
        byte[] contentHash,
        DateTimeOffset createdAtUtc)
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
    }

    public Guid Id { get; private set; }
    public string SourceId { get; private set; } = string.Empty;
    public byte[] ContentHash { get; private set; } = [];
    public DateTimeOffset CreatedAtUtc { get; private set; }
}