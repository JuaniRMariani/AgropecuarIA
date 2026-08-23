namespace AgropecuarIA.Catalog.Domain;

public sealed class CatalogStagingEntry
{
    private CatalogStagingEntry() { }

    public CatalogStagingEntry(
        Guid id,
        string sourceId,
        byte[] sourceHash,
        string code,
        string displayName,
        string jurisdiction,
        DateTimeOffset createdAtUtc)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Id is required.");

        ArgumentException.ThrowIfNullOrWhiteSpace(sourceId);
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);

        if (sourceHash is not { Length: 32 })
            throw new ArgumentException("The source hash must contain 32 bytes.", nameof(sourceHash));

        Id = id;
        SourceId = sourceId;
        SourceHash = sourceHash.ToArray();
        Code = code;
        DisplayName = displayName;
        Jurisdiction = jurisdiction;
        CreatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; private set; }
    public string SourceId { get; private set; } = string.Empty;
    public byte[] SourceHash { get; private set; } = [];
    public string Code { get; private set; } = string.Empty;
    public string DisplayName { get; private set; } = string.Empty;
    public string Jurisdiction { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; private set; }
}