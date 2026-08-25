using AgropecuarIA.ProductiveCore.Domain;

namespace AgropecuarIA.ProductiveCore.Tests;

[TestClass]
public sealed class ProductiveInboxTests
{
    [TestMethod]
    public void InboxEntrySetsPropertiesCorrectly()
    {
        Guid id = Guid.NewGuid();
        Guid messageId = Guid.NewGuid();
        Guid orgId = Guid.NewGuid();
        DateTimeOffset now = DateTimeOffset.UtcNow;

        ProductiveInboxEntry entry = new(
            id,
            messageId,
            "weather-consumer",
            orgId,
            now);

        Assert.AreEqual(id, entry.Id);
        Assert.AreEqual(messageId, entry.MessageId);
        Assert.AreEqual("weather-consumer", entry.ConsumerName);
        Assert.AreEqual(orgId, entry.OrganizationId);
        Assert.AreEqual(now, entry.ProcessedAtUtc);
    }

    [TestMethod]
    public void InboxEntryThrowsOnEmptyIds()
    {
        Assert.ThrowsExactly<ArgumentException>(() =>
            new ProductiveInboxEntry(
                Guid.Empty,
                Guid.NewGuid(),
                "consumer",
                Guid.NewGuid(),
                DateTimeOffset.UtcNow));

        Assert.ThrowsExactly<ArgumentException>(() =>
            new ProductiveInboxEntry(
                Guid.NewGuid(),
                Guid.Empty,
                "consumer",
                Guid.NewGuid(),
                DateTimeOffset.UtcNow));

        Assert.ThrowsExactly<ArgumentException>(() =>
            new ProductiveInboxEntry(
                Guid.NewGuid(),
                Guid.NewGuid(),
                "consumer",
                Guid.Empty,
                DateTimeOffset.UtcNow));
    }

    [TestMethod]
    public void InboxEntryThrowsOnNullOrEmptyConsumer()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
            new ProductiveInboxEntry(
                Guid.NewGuid(),
                Guid.NewGuid(),
                null!,
                Guid.NewGuid(),
                DateTimeOffset.UtcNow));

        Assert.ThrowsExactly<ArgumentException>(() =>
            new ProductiveInboxEntry(
                Guid.NewGuid(),
                Guid.NewGuid(),
                "   ",
                Guid.NewGuid(),
                DateTimeOffset.UtcNow));
    }
}
