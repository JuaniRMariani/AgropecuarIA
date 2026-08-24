using AgropecuarIA.Catalog.Application;
using AgropecuarIA.Catalog.Domain;

namespace AgropecuarIA.Catalog.Tests;

[TestClass]
public sealed class CatalogIngestionApplicationServiceTests
{
    [TestMethod]
    public void SnapshotCreationSetsPropertiesCorrectly()
    {
        Guid id = Guid.NewGuid();
        string sourceId = "SENASA_V1";
        byte[] hash = new byte[32];
        hash[0] = 0xAA;
        DateTimeOffset now = DateTimeOffset.UtcNow;

        CatalogSourceSnapshot snapshot = new(id, sourceId, hash, now);

        Assert.AreEqual(id, snapshot.Id);
        Assert.AreEqual(sourceId, snapshot.SourceId);
        CollectionAssert.AreEqual(hash, snapshot.ContentHash);
        Assert.AreEqual(now, snapshot.CreatedAtUtc);
    }

    [TestMethod]
    public void SnapshotCreationThrowsOnInvalidHashLength()
    {
        byte[] invalidHash = new byte[16];
        Assert.ThrowsExactly<ArgumentException>(() =>
            new CatalogSourceSnapshot(Guid.NewGuid(), "SENASA_V1", invalidHash, DateTimeOffset.UtcNow));
    }

    [TestMethod]
    public void SnapshotCreationThrowsOnEmptyIdOrSourceId()
    {
        byte[] hash = new byte[32];
        Assert.ThrowsExactly<ArgumentException>(() =>
            new CatalogSourceSnapshot(Guid.Empty, "SENASA_V1", hash, DateTimeOffset.UtcNow));

        Assert.ThrowsExactly<ArgumentException>(() =>
            new CatalogSourceSnapshot(Guid.NewGuid(), "   ", hash, DateTimeOffset.UtcNow));
    }

    [TestMethod]
    public void IngestSourceCommandHoldsPayload()
    {
        string source = "INTA_V1";
        string contentBase64 = Convert.ToBase64String("[]"u8.ToArray());
        IngestSourceCommand command = new(source, contentBase64);

        Assert.AreEqual(source, command.SourceId);
        Assert.AreEqual(contentBase64, command.ContentBase64);
    }
}