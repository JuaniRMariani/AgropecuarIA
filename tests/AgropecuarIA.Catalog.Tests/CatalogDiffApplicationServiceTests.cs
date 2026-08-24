using AgropecuarIA.Catalog.Domain;

namespace AgropecuarIA.Catalog.Tests;

[TestClass]
public sealed class CatalogDiffApplicationServiceTests
{
    [TestMethod]
    public void StagingEntryCreationSetsPropertiesCorrectly()
    {
        Guid id = Guid.NewGuid();
        string sourceId = "SENASA_V1";
        byte[] hash = new byte[32];
        hash[1] = 0xBB;
        string code = "BOV-01";
        string displayName = "Bovino Ternero";
        string jurisdiction = "AR";
        DateTimeOffset now = DateTimeOffset.UtcNow;

        CatalogStagingEntry entry = new(id, sourceId, hash, code, displayName, jurisdiction, now);

        Assert.AreEqual(id, entry.Id);
        Assert.AreEqual(sourceId, entry.SourceId);
        CollectionAssert.AreEqual(hash, entry.SourceHash);
        Assert.AreEqual(code, entry.Code);
        Assert.AreEqual(displayName, entry.DisplayName);
        Assert.AreEqual(jurisdiction, entry.Jurisdiction);
        Assert.AreEqual(now, entry.CreatedAtUtc);
    }

    [TestMethod]
    public void StagingEntryCreationThrowsOnInvalidHashLength()
    {
        byte[] invalidHash = new byte[10];
        Assert.ThrowsExactly<ArgumentException>(() =>
            new CatalogStagingEntry(Guid.NewGuid(), "SENASA_V1", invalidHash, "BOV", "Bovino", "AR", DateTimeOffset.UtcNow));
    }

    [TestMethod]
    public void StagingEntryCreationThrowsOnEmptyIdOrRequiredFields()
    {
        byte[] hash = new byte[32];
        Assert.ThrowsExactly<ArgumentException>(() =>
            new CatalogStagingEntry(Guid.Empty, "SENASA_V1", hash, "BOV", "Bovino", "AR", DateTimeOffset.UtcNow));

        Assert.ThrowsExactly<ArgumentException>(() =>
            new CatalogStagingEntry(Guid.NewGuid(), "  ", hash, "BOV", "Bovino", "AR", DateTimeOffset.UtcNow));

        Assert.ThrowsExactly<ArgumentException>(() =>
            new CatalogStagingEntry(Guid.NewGuid(), "SENASA_V1", hash, "  ", "Bovino", "AR", DateTimeOffset.UtcNow));

        Assert.ThrowsExactly<ArgumentException>(() =>
            new CatalogStagingEntry(Guid.NewGuid(), "SENASA_V1", hash, "BOV", "  ", "AR", DateTimeOffset.UtcNow));
    }
}