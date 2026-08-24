using AgropecuarIA.ProductiveCore.Application;
using AgropecuarIA.ProductiveCore.Domain;

namespace AgropecuarIA.ProductiveCore.Tests;

[TestClass]
public sealed class ProductionCycleTests
{
    [TestMethod]
    public void CycleCreationSetsPropertiesCorrectly()
    {
        Guid id = Guid.NewGuid();
        Guid organizationId = Guid.NewGuid();
        Guid managementUnitId = Guid.NewGuid();
        DateTimeOffset start = DateTimeOffset.UtcNow;
        DateTimeOffset now = DateTimeOffset.UtcNow;

        ProductionCycle cycle = new(
            id,
            organizationId,
            managementUnitId,
            "MAIZ-01",
            "Maíz Grano",
            "grano_comercial",
            "siembra_directa",
            "FLUJO_GENERICO",
            start,
            now);

        Assert.AreEqual(id, cycle.Id);
        Assert.AreEqual(organizationId, cycle.OrganizationId);
        Assert.AreEqual(managementUnitId, cycle.ManagementUnitId);
        Assert.AreEqual("MAIZ-01", cycle.CatalogCode);
        Assert.AreEqual("Maíz Grano", cycle.CatalogDisplayName);
        Assert.AreEqual("grano_comercial", cycle.Purpose);
        Assert.AreEqual("siembra_directa", cycle.System);
        Assert.AreEqual("FLUJO_GENERICO", cycle.SupportLevel);
        Assert.AreEqual(ProductionCycleStatuses.Active, cycle.Status);
        Assert.AreEqual(start, cycle.StartDateUtc);
        Assert.IsNull(cycle.EndDateUtc);
    }

    [TestMethod]
    public void CycleCloseSetsEndDateAndStatus()
    {
        Guid id = Guid.NewGuid();
        DateTimeOffset start = DateTimeOffset.UtcNow.AddMonths(-3);
        DateTimeOffset end = DateTimeOffset.UtcNow;

        ProductionCycle cycle = new(
            id,
            Guid.NewGuid(),
            Guid.NewGuid(),
            "SOJ",
            "Soja",
            "grano",
            "secano",
            "FLUJO_GENERICO",
            start,
            start);

        cycle.Close(end);

        Assert.AreEqual(ProductionCycleStatuses.Closed, cycle.Status);
        Assert.AreEqual(end, cycle.EndDateUtc);
    }

    [TestMethod]
    public void CycleCloseThrowsOnEndDateBeforeStart()
    {
        DateTimeOffset start = DateTimeOffset.UtcNow;
        DateTimeOffset invalidEnd = start.AddDays(-1);

        ProductionCycle cycle = new(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "SOJ",
            "Soja",
            "grano",
            "secano",
            "FLUJO_GENERICO",
            start,
            start);

        Assert.ThrowsExactly<ArgumentException>(() => cycle.Close(invalidEnd));
    }

    [TestMethod]
    public void EventCreationWithQuantityRequiresUnit()
    {
        Guid id = Guid.NewGuid();
        Guid organizationId = Guid.NewGuid();
        Guid cycleId = Guid.NewGuid();
        DateTimeOffset now = DateTimeOffset.UtcNow;

        Assert.ThrowsExactly<ArgumentException>(() =>
            new ProductionEvent(
                id,
                organizationId,
                cycleId,
                "siembra",
                now,
                now,
                quantity: 150m,
                unit: null,
                notes: "Siembra lote norte",
                origin: ProductionOrigins.Manual));

        ProductionEvent valid = new(
            id,
            organizationId,
            cycleId,
            "siembra",
            now,
            now,
            quantity: 150m,
            unit: "kg/ha",
            notes: "Siembra lote norte",
            origin: ProductionOrigins.Manual);

        Assert.AreEqual(150m, valid.Quantity);
        Assert.AreEqual("kg/ha", valid.Unit);
        Assert.AreEqual(ProductionOrigins.Manual, valid.Origin);
    }

    [TestMethod]
    public void EventCreationThrowsOnInvalidOrigin()
    {
        Assert.ThrowsExactly<ArgumentException>(() =>
            new ProductionEvent(
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid(),
                "riego",
                DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow,
                quantity: null,
                unit: null,
                notes: null,
                origin: "ORIGEN_INVALIDO"));
    }
}
