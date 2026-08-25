namespace AgropecuarIA.ProductiveCore.Domain;

public sealed class ProductiveInboxEntry
{
    private ProductiveInboxEntry() { }

    public ProductiveInboxEntry(
        Guid id,
        Guid messageId,
        string consumerName,
        Guid organizationId,
        DateTimeOffset processedAtUtc)
    {
        if (id == Guid.Empty || messageId == Guid.Empty || organizationId == Guid.Empty)
        {
            throw new ArgumentException("Id, messageId and organizationId are required.");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(consumerName);

        Id = id;
        MessageId = messageId;
        ConsumerName = consumerName.Trim();
        OrganizationId = organizationId;
        ProcessedAtUtc = processedAtUtc;
    }

    public Guid Id { get; private set; }
    public Guid MessageId { get; private set; }
    public string ConsumerName { get; private set; } = string.Empty;
    public Guid OrganizationId { get; private set; }
    public DateTimeOffset ProcessedAtUtc { get; private set; }
}
