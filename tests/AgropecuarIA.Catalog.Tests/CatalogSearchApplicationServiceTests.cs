using AgropecuarIA.Catalog.Application;
using AgropecuarIA.Catalog.Domain;

namespace AgropecuarIA.Catalog.Tests;

[TestClass]
public sealed class CatalogSearchApplicationServiceTests
{
    [TestMethod]
    public void CatalogNameNormalizerDecomposesDiacriticsAndFoldsCase()
    {
        string input = "  Maíz   Pisingallo   RÍO NEGRO \t ";
        string normalized = CatalogNameNormalizer.Normalize(input);

        Assert.AreEqual("maiz pisingallo rio negro", normalized);
    }

    [TestMethod]
    public void CatalogNameNormalizerHandlesEmptyOrWhitespace()
    {
        Assert.AreEqual(string.Empty, CatalogNameNormalizer.Normalize("   "));
        Assert.ThrowsExactly<ArgumentNullException>(() => CatalogNameNormalizer.Normalize(null!));
    }

    [TestMethod]
    public void SearchCatalogQueryHoldsParameters()
    {
        SearchCatalogQuery query = new(
            Query: "soja",
            Jurisdiction: "BA",
            Category: CatalogCategories.Agricultura,
            SupportLevel: CatalogSupportLevels.FlujoGenerico,
            Limit: 25);

        Assert.AreEqual("soja", query.Query);
        Assert.AreEqual("BA", query.Jurisdiction);
        Assert.AreEqual(CatalogCategories.Agricultura, query.Category);
        Assert.AreEqual(CatalogSupportLevels.FlujoGenerico, query.SupportLevel);
        Assert.AreEqual(25, query.Limit);
    }
}
