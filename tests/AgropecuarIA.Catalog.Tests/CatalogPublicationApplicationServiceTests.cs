using AgropecuarIA.Catalog.Application;
using AgropecuarIA.Catalog.Domain;

namespace AgropecuarIA.Catalog.Tests;

[TestClass]
public sealed class CatalogPublicationApplicationServiceTests
{
    [TestMethod]
    public void PublishedVersionCreationSetsProperties()
    {
        Guid id = Guid.NewGuid();
        DateTimeOffset now = DateTimeOffset.UtcNow;

        CatalogPublishedVersion version = new(
            id,
            "v1.0.0",
            isActive: true,
            publishedBy: "editor@agropecuaria.com",
            itemsCount: 42,
            publishedAtUtc: now);

        Assert.AreEqual(id, version.Id);
        Assert.AreEqual("v1.0.0", version.VersionTag);
        Assert.IsTrue(version.IsActive);
        Assert.AreEqual("editor@agropecuaria.com", version.PublishedBy);
        Assert.AreEqual(42, version.ItemsCount);
        Assert.AreEqual(now, version.PublishedAtUtc);

        version.SetActive(false);
        Assert.IsFalse(version.IsActive);
    }

    [TestMethod]
    public void PublishedVersionThrowsOnInvalidArguments()
    {
        Assert.ThrowsExactly<ArgumentException>(() =>
            new CatalogPublishedVersion(Guid.Empty, "v1", true, "editor", 10, DateTimeOffset.UtcNow));

        Assert.ThrowsExactly<ArgumentException>(() =>
            new CatalogPublishedVersion(Guid.NewGuid(), "  ", true, "editor", 10, DateTimeOffset.UtcNow));

        Assert.ThrowsExactly<ArgumentException>(() =>
            new CatalogPublishedVersion(Guid.NewGuid(), "v1", true, "  ", 10, DateTimeOffset.UtcNow));
    }

    [TestMethod]
    public void PublishedItemCreationNormalizesCodeAndDisplayName()
    {
        Guid id = Guid.NewGuid();
        Guid versionId = Guid.NewGuid();
        DateTimeOffset now = DateTimeOffset.UtcNow;

        CatalogPublishedItem item = new(
            id,
            versionId,
            "  trig-01  ",
            "  Trigo Candeal Superior  ",
            "  BA  ",
            CatalogSupportLevels.EspecializadaValidada,
            CatalogCategories.Agricultura,
            synonyms: ["Triticum durum", "  Trigo fideos  "],
            isActive: true,
            createdAtUtc: now);

        Assert.AreEqual("TRIG-01", item.Code);
        Assert.AreEqual("trig-01", item.NormalizedCode);
        Assert.AreEqual("Trigo Candeal Superior", item.DisplayName);
        Assert.AreEqual("trigo candeal superior", item.NormalizedDisplayName);
        Assert.AreEqual("BA", item.Jurisdiction);
        Assert.AreEqual(CatalogSupportLevels.EspecializadaValidada, item.SupportLevel);
        Assert.AreEqual(CatalogCategories.Agricultura, item.Category);
        Assert.AreEqual(2, item.Synonyms.Count);
        Assert.IsTrue(item.IsActive);
    }

    [TestMethod]
    public void PublishedItemThrowsOnInvalidSupportLevel()
    {
        Assert.ThrowsExactly<ArgumentException>(() =>
            new CatalogPublishedItem(
                Guid.NewGuid(),
                Guid.NewGuid(),
                "SOJ",
                "Soja",
                "AR",
                "NIVEL_INVENTADO",
                CatalogCategories.Agricultura,
                synonyms: null,
                isActive: true,
                createdAtUtc: DateTimeOffset.UtcNow));
    }
}
