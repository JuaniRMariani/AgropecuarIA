using System.Text;
using System.Text.Json;
using System.Globalization;
using AgropecuarIA.ProductiveCore.Domain;

namespace AgropecuarIA.ProductiveCore.Tests;

[TestClass]
public sealed class ManagementUnitDomainTests
{
    private static readonly string[] EventPayloadProperties =
        ["organizationId", "managementUnitId", "unitType", "status", "createdAtUtc"];
    private static readonly string[] RenameEventPayloadProperties =
        ["organizationId", "managementUnitId", "revision", "changedAtUtc"];
    private static readonly string[] InvalidUnicodeNames =
        ["Campo \uD800", "Campo \uDC00", "Campo\u0085Norte"];

    [TestMethod]
    public void ConstructorNormalizesNameAndFreezesInitialState()
    {
        ManagementUnit field = new(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "  Campo Norte e\u0301lite  ",
            DateTimeOffset.Parse("2026-08-18T18:00:00Z", CultureInfo.InvariantCulture),
            Guid.NewGuid());

        Assert.AreEqual("Campo Norte élite", field.DisplayName);
        Assert.AreEqual(ManagementUnitTypes.Field, field.UnitType);
        Assert.AreEqual(ManagementUnitStatuses.Draft, field.Status);
        Assert.AreEqual(ManagementUnitSpatialStatuses.NotConfigured, field.SpatialStatus);
    }

    [TestMethod]
    [DataRow("A")]
    [DataRow("Campo\nNorte")]
    public void ConstructorRejectsInvalidDisplayName(string displayName)
    {
        Assert.ThrowsExactly<ArgumentException>(() => new ManagementUnit(
            Guid.NewGuid(),
            Guid.NewGuid(),
            displayName,
            DateTimeOffset.UtcNow,
            Guid.NewGuid()));
    }

    [TestMethod]
    public void ConstructorTrimsFrozenUnicodeBoundaryBeforeNfc()
    {
        ManagementUnit field = new(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "\uFEFF\u0085 Campo e\u0301 \u0085\uFEFF",
            DateTimeOffset.UtcNow,
            Guid.NewGuid());

        Assert.AreEqual("Campo é", field.DisplayName);
    }

    [TestMethod]
    public void ConstructorRejectsLoneSurrogatesAndInternalControls()
    {
        foreach (string displayName in InvalidUnicodeNames)
        {
            Assert.ThrowsExactly<ArgumentException>(() => new ManagementUnit(
                Guid.NewGuid(),
                Guid.NewGuid(),
                displayName,
                DateTimeOffset.UtcNow,
                Guid.NewGuid()));
        }
    }

    [TestMethod]
    public void EventPayloadContainsNoDisplayNameActorOrIdempotencyMaterial()
    {
        Guid organizationId = Guid.NewGuid();
        Guid fieldId = Guid.NewGuid();
        ProductiveOutboxMessage message = ProductiveOutboxMessage.CreateManagementUnitCreated(
            Guid.NewGuid(),
            "correlation-1",
            new ManagementUnitCreatedIntegrationEventPayload(
                organizationId,
                fieldId,
                ManagementUnitTypes.Field,
                ManagementUnitStatuses.Draft,
                DateTimeOffset.Parse("2026-08-18T18:00:00Z", CultureInfo.InvariantCulture)));

        using JsonDocument payload = JsonDocument.Parse(message.PayloadJson);
        string[] properties = payload.RootElement
            .EnumerateObject()
            .Select(property => property.Name)
            .Order(StringComparer.Ordinal)
            .ToArray();
        CollectionAssert.AreEquivalent(
            EventPayloadProperties,
            properties);
        Assert.IsFalse(message.PayloadJson.Contains("display", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(message.PayloadJson.Contains("actor", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(message.PayloadJson.Contains("key", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(message.PayloadJson.Contains("digest", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void DisplayNameSupportsOneHundredTwentyUnicodeScalars()
    {
        string displayName = string.Concat(Enumerable.Repeat("🌱", 120));

        ManagementUnit field = new(
            Guid.NewGuid(),
            Guid.NewGuid(),
            displayName,
            DateTimeOffset.UtcNow,
            Guid.NewGuid());

        Assert.AreEqual(120, field.DisplayName.EnumerateRunes().Count());
    }

    [TestMethod]
    public void RenameNormalizesAndAdvancesRevisionAndVersionExactlyOnce()
    {
        Guid originalVersion = Guid.NewGuid();
        Guid renamedVersion = Guid.NewGuid();
        ManagementUnit field = new(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Campo Norte",
            DateTimeOffset.UtcNow,
            originalVersion);

        field.Rename("  Campo Sur e\u0301lite  ", originalVersion, renamedVersion);

        Assert.AreEqual("Campo Sur élite", field.DisplayName);
        Assert.AreEqual(2L, field.Revision);
        Assert.AreEqual(renamedVersion, field.Version);
    }

    [TestMethod]
    public void RenameRejectsStaleVersionAndCanonicalNoChangeWithoutEffect()
    {
        Guid originalVersion = Guid.NewGuid();
        ManagementUnit field = new(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Campo Norte",
            DateTimeOffset.UtcNow,
            originalVersion);

        Assert.ThrowsExactly<ManagementUnitVersionConflictException>(() =>
            field.Rename("Campo Sur", Guid.NewGuid(), Guid.NewGuid()));
        Assert.ThrowsExactly<ManagementUnitNoChangeException>(() =>
            field.Rename("  Campo Norte  ", originalVersion, Guid.NewGuid()));
        Assert.AreEqual("Campo Norte", field.DisplayName);
        Assert.AreEqual(1L, field.Revision);
        Assert.AreEqual(originalVersion, field.Version);
    }

    [TestMethod]
    public void RenameEventContainsNoNameActorOrIdempotencyMaterial()
    {
        ProductiveOutboxMessage message =
            ProductiveOutboxMessage.CreateManagementUnitDisplayNameChanged(
                Guid.NewGuid(),
                "correlation-rename",
                new ManagementUnitDisplayNameChangedIntegrationEventPayload(
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    2,
                    DateTimeOffset.UtcNow));

        using JsonDocument payload = JsonDocument.Parse(message.PayloadJson);
        string[] properties = payload.RootElement
            .EnumerateObject()
            .Select(property => property.Name)
            .Order(StringComparer.Ordinal)
            .ToArray();
        CollectionAssert.AreEquivalent(
            RenameEventPayloadProperties,
            properties);
        Assert.IsFalse(message.PayloadJson.Contains("display", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(message.PayloadJson.Contains("actor", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(message.PayloadJson.Contains("key", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(message.PayloadJson.Contains("digest", StringComparison.OrdinalIgnoreCase));
        Assert.AreEqual(2L, message.AggregateVersion);
    }

    [TestMethod]
    public void ConfigureSpatialGeometryUpdatesPropertiesAndStatus()
    {
        Guid initialVersion = Guid.NewGuid();
        Guid newVersion = Guid.NewGuid();
        ManagementUnit field = new(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Lote Norte",
            DateTimeOffset.UtcNow,
            initialVersion);

        Assert.AreEqual(ManagementUnitSpatialStatuses.NotConfigured, field.SpatialStatus);
        Assert.AreEqual(1L, field.Revision);

        string geoJson = "{\"type\":\"Polygon\",\"coordinates\":[[[-60.5,-34.5],[-60.4,-34.5],[-60.4,-34.4],[-60.5,-34.4],[-60.5,-34.5]]]}";
        field.ConfigureSpatialGeometry(
            geoJson,
            declaredAreaHectares: 120.5m,
            calculatedAreaHectares: 119.8m,
            centroidLat: -34.45,
            centroidLng: -60.45,
            provinceCode: "06",
            departmentCode: "06441",
            expectedVersion: initialVersion,
            newVersion: newVersion);

        Assert.AreEqual(ManagementUnitSpatialStatuses.Configured, field.SpatialStatus);
        Assert.AreEqual(120.5m, field.DeclaredAreaHectares);
        Assert.AreEqual(119.8m, field.CalculatedAreaHectares);
        Assert.AreEqual(-34.45, field.CentroidLatitude);
        Assert.AreEqual(-60.45, field.CentroidLongitude);
        Assert.AreEqual("06", field.OfficialProvinceCode);
        Assert.AreEqual("06441", field.OfficialDepartmentCode);
        Assert.AreEqual(2L, field.Revision);
        Assert.AreEqual(newVersion, field.Version);
    }

    [TestMethod]
    public void ConfigureSpatialGeometryWithStaleVersionThrowsConflict()
    {
        Guid initialVersion = Guid.NewGuid();
        ManagementUnit field = new(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Lote Norte",
            DateTimeOffset.UtcNow,
            initialVersion);

        Assert.ThrowsExactly<ManagementUnitVersionConflictException>(() =>
            field.ConfigureSpatialGeometry(
                "{\"type\":\"Polygon\"}",
                100m,
                100m,
                -34.0,
                -60.0,
                "06",
                null,
                expectedVersion: Guid.NewGuid(),
                newVersion: Guid.NewGuid()));
    }
}
