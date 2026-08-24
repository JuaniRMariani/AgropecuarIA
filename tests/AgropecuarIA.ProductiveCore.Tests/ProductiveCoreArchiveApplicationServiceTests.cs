using AgropecuarIA.ProductiveCore.Domain;

namespace AgropecuarIA.ProductiveCore.Tests;

[TestClass]
public sealed class ProductiveCoreArchiveApplicationServiceTests
{
    [TestMethod]
    public void ArchiveDraftChangesStatusAndIncrementsRevision()
    {
        Guid initialVersion = Guid.NewGuid();
        Guid newVersion = Guid.NewGuid();
        ManagementUnit field = new(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Lote Test",
            DateTimeOffset.UtcNow,
            initialVersion);

        Assert.AreEqual(ManagementUnitStatuses.Draft, field.Status);
        Assert.AreEqual(1L, field.Revision);

        field.Archive(initialVersion, newVersion);

        Assert.AreEqual(ManagementUnitStatuses.Archived, field.Status);
        Assert.AreEqual(2L, field.Revision);
        Assert.AreEqual(newVersion, field.Version);
    }

    [TestMethod]
    public void ArchiveWithStaleVersionThrowsConflict()
    {
        Guid initialVersion = Guid.NewGuid();
        ManagementUnit field = new(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Lote Test",
            DateTimeOffset.UtcNow,
            initialVersion);

        Assert.ThrowsExactly<ManagementUnitVersionConflictException>(() =>
            field.Archive(Guid.NewGuid(), Guid.NewGuid()));
    }
}