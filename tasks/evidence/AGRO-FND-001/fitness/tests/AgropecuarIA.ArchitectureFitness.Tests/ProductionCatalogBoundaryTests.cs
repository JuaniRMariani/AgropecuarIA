using System.Reflection;
using System.Text.Json;
using AgropecuarIA.ArchitectureFitness;
using AgropecuarIA.Catalog.Application;
using AgropecuarIA.ProductiveCore.Application;
using AgropecuarIA.ProductiveCore.Domain;

namespace AgropecuarIA.ArchitectureFitness.Tests;

[TestClass]
public sealed class ProductionCatalogBoundaryTests
{
    private static readonly JsonSerializerOptions WebJson = new(JsonSerializerDefaults.Web);
    private static readonly string[] ImplementedCatalogConsumers = ["productive-core"];

    [TestMethod]
    public void ProductiveOwnsThePortAndSnapshotWithoutReferencingCatalogAssembly()
    {
        Assembly productive = typeof(ProductionCycleApplicationService).Assembly;
        Assert.AreEqual(productive, typeof(IProductionCatalogResolver).Assembly);
        Assert.AreEqual(productive, typeof(ProductionCatalogResolution).Assembly);
        Assert.AreEqual(productive, typeof(ProductionCatalogSnapshot).Assembly);
        Assert.IsFalse(productive.GetReferencedAssemblies().Any(reference => reference.Name == "AgropecuarIA.Catalog"));
        MethodInfo method = typeof(IProductionCatalogResolver).GetMethod(nameof(IProductionCatalogResolver.ResolveActiveAsync))!;
        Assert.AreEqual(typeof(Task<ProductionCatalogResolution>), method.ReturnType);
        CollectionAssert.AreEqual(new[] { typeof(string), typeof(Guid?), typeof(CancellationToken) },
            method.GetParameters().Select(parameter => parameter.ParameterType).ToArray());
        Assert.IsTrue(typeof(ProductionCycleApplicationService).GetConstructors().Single().GetParameters()
            .Any(parameter => parameter.ParameterType == typeof(IProductionCatalogResolver)));
        Assert.AreEqual(typeof(ProductionCatalogSnapshot), typeof(ProductionCycleDto).GetProperty("CatalogSnapshot")!.PropertyType);
        Assert.IsFalse(typeof(ProductionCatalogSnapshot).GetProperties().Any(property =>
            property.PropertyType.Assembly == typeof(CatalogPublishedItemDto).Assembly));
    }

    [TestMethod]
    public void ApiCompositionAdapterUsesOnlyThePublicCatalogApplicationService()
    {
        // Reflection does not run Program, start a host or substitute for the real HTTP/PG tests.
        Type adapter = typeof(Program).Assembly.GetType("AgropecuarIA.Api.ProductionCatalogResolver", throwOnError: true)!;
        Assert.IsFalse(adapter.IsPublic);
        Assert.IsTrue(typeof(IProductionCatalogResolver).IsAssignableFrom(adapter));
        CollectionAssert.AreEqual(new[] { typeof(CatalogSearchApplicationService) },
            adapter.GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Single()
                .GetParameters().Select(parameter => parameter.ParameterType).ToArray());
        Assert.IsTrue(typeof(CatalogSearchApplicationService).IsPublic);
        Assert.IsNotNull(typeof(CatalogSearchApplicationService).GetMethod(nameof(CatalogSearchApplicationService.ResolveActiveItemAsync)));
        Assert.IsFalse(adapter.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .Any(field => field.FieldType.Namespace?.StartsWith("AgropecuarIA.Catalog.Infrastructure", StringComparison.Ordinal) == true));
    }

    [TestMethod]
    public void ConcreteCatalogReferenceIsPlatformScopedWhilePlannedConsumersRemainInArchitectureMap()
    {
        PublishedContract reference = EvidenceFixture.ConsumerMap().Contracts.Single(contract => contract.Name == "CatalogEntryRef");
        Assert.AreEqual("national-catalog", reference.Provider);
        Assert.AreEqual("application-port", reference.Interaction);
        Assert.AreEqual("platform", reference.Scope);
        CollectionAssert.Contains(reference.Consumers.ToArray(), "productive-core");
        CollectionAssert.Contains(reference.Consumers.ToArray(), "integrations");
        RuntimeMapDocument runtime = EvidenceFixture.RuntimeMap();
        CollectionAssert.AreEqual(ImplementedCatalogConsumers, reference.Consumers
            .Intersect(runtime.Modules.Select(module => module.ModuleId), StringComparer.Ordinal).ToArray());
        Assert.IsTrue(runtime.Modules.Single(module => module.ModuleId == "national-catalog").Contracts.Any(contract =>
            contract.Path == "tasks/evidence/AGRO-FND-001/contracts/catalog-entry-ref.v1.schema.json" && contract.Version == "1.0.0"));
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void RuntimeSnapshotSerializesTheReviewedFieldsIncludingExplicitUnavailableLineage(bool verified)
    {
        var snapshot = new ProductionCatalogSnapshot(Guid.NewGuid(), Guid.NewGuid(), "fixture-v1", "MAIZ", "Synthetic maize",
            "ESPECIALIZADA_VALIDADA", verified ? Guid.NewGuid() : null, verified ? "synthetic-source" : null,
            verified ? new string('a', 64) : null, verified ? DateTimeOffset.Parse("2026-09-04T10:00:00Z", System.Globalization.CultureInfo.InvariantCulture) : null,
            verified ? "verified_snapshot" : "legacy_unavailable", DateTimeOffset.Parse("2026-09-04T11:00:00Z", System.Globalization.CultureInfo.InvariantCulture));
        snapshot.Validate();
        JsonElement actual = JsonSerializer.SerializeToElement(snapshot, WebJson);
        using JsonDocument schema = JsonDocument.Parse(File.ReadAllText(Path.Combine(EvidenceFixture.ContractsDirectory(), "catalog-entry-ref.v1.schema.json")));
        CollectionAssert.AreEquivalent(schema.RootElement.GetProperty("required").EnumerateArray().Select(value => value.GetString()!).ToArray(),
            actual.EnumerateObject().Select(property => property.Name).ToArray());
        foreach (string property in new[] { "sourceSnapshotId", "sourceId", "sourceHash", "sourceIngestedAtUtc" })
            Assert.AreEqual(verified ? JsonValueKind.String : JsonValueKind.Null, actual.GetProperty(property).ValueKind);
        Assert.AreEqual("ESPECIALIZADA_VALIDADA", actual.GetProperty("declaredCatalogSupportLevel").GetString());
        Assert.AreEqual(snapshot.ProvenanceStatus, actual.GetProperty("provenanceStatus").GetString());
    }

    [TestMethod]
    [DataRow("catalogDisplayName")]
    [DataRow("supportLevel")]
    [DataRow("catalogSnapshot")]
    public void RuntimeStartRequestRejectsCallerAuthorityFields(string field)
    {
        var request = new Dictionary<string, object?>
        {
            ["catalogCode"] = "MAIZ", ["purpose"] = "grain", ["system"] = "generic", ["startDateUtc"] = "2026-09-04T10:00:00Z",
            [field] = "untrusted",
        };
        Assert.ThrowsExactly<JsonException>(() => JsonSerializer.Deserialize<StartProductionCycleRequest>(JsonSerializer.Serialize(request), WebJson));
    }
}
