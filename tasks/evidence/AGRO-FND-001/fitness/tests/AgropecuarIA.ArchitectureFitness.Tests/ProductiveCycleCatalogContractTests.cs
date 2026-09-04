using System.Text.Json.Nodes;
using AgropecuarIA.ArchitectureFitness;

namespace AgropecuarIA.ArchitectureFitness.Tests;

[TestClass]
public sealed class ProductiveCycleCatalogContractTests
{
    [TestMethod]
    public void CycleSnapshotExactlyMatchesPublishedPlatformCatalogEntryReference()
    {
        string original = File.ReadAllText(EvidenceFixture.ProductiveCoreOpenApiPath());
        Assert.HasCount(0, ProductiveCycleOpenApiContractGuard.Validate(original));
        JsonObject embedded = Schema(original, "CatalogEntrySnapshot");
        JsonObject published = PublishedSchema();
        foreach (string property in new[] { "type", "additionalProperties", "required", "properties", "oneOf" })
            Assert.IsTrue(JsonNode.DeepEquals(embedded[property], published[property]), property);
        IReadOnlyList<ValidationIssue> issues = PublishedSchemaValidator.Validate("catalog-entry-ref.v1.schema.json", published.ToJsonString());
        Assert.HasCount(0, issues, string.Join(Environment.NewLine, issues));
    }

    [TestMethod]
    [DataRow("catalogDisplayName")]
    [DataRow("supportLevel")]
    [DataRow("catalogSnapshot")]
    [DataRow("actorUserId")]
    public void CycleCreationCannotAcceptClientAuthority(string property)
    {
        AssertRejected(Mutate("StartProductionCycleRequest", schema => schema["properties"]![property] = new JsonObject { ["type"] = "string" }),
            "productive-cycle.request.invalid");
    }

    [TestMethod]
    public void ExpectedCatalogVersionMustRemainOptionalPrecondition()
    {
        AssertRejected(Mutate("StartProductionCycleRequest", schema => schema["required"]!.AsArray().Add("expectedCatalogVersionId")),
            "productive-cycle.request.invalid");
        AssertRejected(Mutate("StartProductionCycleRequest", schema => schema["properties"]!["expectedCatalogVersionId"]!["format"] = "date-time"),
            "productive-cycle.precondition.invalid");
    }

    [TestMethod]
    [DataRow("not a historical selector")]
    [DataRow("before item lookup")]
    [DataRow("not a guarantee of active version at commit")]
    [DataRow("no idempotency ledger")]
    public void CatalogResolutionSemanticsCannotBeSilentlyWeakened(string boundary)
    {
        string original = File.ReadAllText(EvidenceFixture.ProductiveCoreOpenApiPath());
        StringAssert.Contains(original, boundary);
        AssertRejected(original.Replace(boundary, "unreviewed behavior", StringComparison.Ordinal), "productive-cycle.resolution-boundary.missing");
    }

    [TestMethod]
    public void ExistingFieldSecurityCannotMaskAnUnprotectedCycleMutation()
    {
        string original = File.ReadAllText(EvidenceFixture.ProductiveCoreOpenApiPath()).Replace("\r\n", "\n", StringComparison.Ordinal);
        int start = original.IndexOf("    post:\n      operationId: StartProductionCycle\n", StringComparison.Ordinal);
        Assert.IsGreaterThanOrEqualTo(0, start);
        const string Protected = "        - SessionCookie: []\n          AntiforgeryCookie: []";
        int security = original.IndexOf(Protected, start, StringComparison.Ordinal);
        Assert.IsGreaterThanOrEqualTo(0, security);
        string mutated = string.Concat(original.AsSpan(0, security), "        - SessionCookie: []\n        - AntiforgeryCookie: []",
            original.AsSpan(security + Protected.Length));
        AssertRejected(mutated, "productive-cycle.security.invalid");
    }

    [TestMethod]
    public void NeitherLegacyNorResolvedCyclesCanGainSpecializedExecution()
    {
        AssertRejected(Mutate("ProductionCycle", schema => schema["properties"]!["effectiveSupportLevel"]!["const"] = "ESPECIALIZADA_VALIDADA"),
            "productive-cycle.capabilities.invalid");
        AssertRejected(Mutate("ProductionCycle", schema => schema["properties"]!["capabilities"]!["maxItems"] = 1),
            "productive-cycle.capabilities.invalid");
        AssertRejected(Mutate("ProductionCycle", schema => schema["properties"]!["absentCapabilities"]!["const"] = new JsonArray()),
            "productive-cycle.capabilities.invalid");
    }

    [TestMethod]
    public void HistoricalStoredSupportCannotBeRewrittenAsEffectiveSupport()
    {
        AssertRejected(Mutate("ProductionCycle", schema => schema["properties"]!["supportLevel"]!["const"] = "FLUJO_GENERICO"),
            "productive-cycle.legacy-support.rewritten");
    }

    [TestMethod]
    public void ResolvedAndLegacyCyclesMustRetainDifferentSnapshotStates()
    {
        AssertRejected(Mutate("ProductionCycle", schema => schema.Remove("oneOf")), "productive-cycle.snapshot.incoherent");
        AssertRejected(Mutate("ProductionCycle", schema => schema["properties"]!["catalogReferenceStatus"]!["enum"] = new JsonArray("resolved_publication")),
            "productive-cycle.reference-status.invalid");
    }

    [TestMethod]
    [DataRow("versionTag", 65)]
    [DataRow("code", 65)]
    [DataRow("displayName", 257)]
    [DataRow("declaredCatalogSupportLevel", 65)]
    public void CatalogReferenceCannotExpandStoredFactBounds(string property, int bound)
    {
        JsonObject schema = PublishedSchema();
        schema["properties"]![property]!["maxLength"] = bound;
        AssertPublishedRejected(schema, "catalog-reference.bounds.invalid");
    }

    [TestMethod]
    [DataRow("sourceSnapshotId")]
    [DataRow("sourceId")]
    [DataRow("sourceHash")]
    [DataRow("sourceIngestedAtUtc")]
    public void UnavailableLegacySourceFactsMustBeNullableAndCoherent(string property)
    {
        JsonObject schema = PublishedSchema();
        schema["properties"]![property]!["type"] = "string";
        AssertPublishedRejected(schema, "catalog-reference.legacy.nullability");
        schema = PublishedSchema();
        schema["oneOf"]!.AsArray()[1]!["properties"]![property]!["type"] = "string";
        AssertPublishedRejected(schema, "catalog-reference.lineage.incoherent");
    }

    [TestMethod]
    public void CatalogReferenceCannotInventTenantScopeOrDropLineageDiscriminator()
    {
        JsonObject schema = PublishedSchema();
        schema["properties"]!["organizationId"] = new JsonObject { ["type"] = "string" };
        AssertPublishedRejected(schema, "catalog-reference.shape.invalid");
        schema = PublishedSchema();
        schema.Remove("oneOf");
        AssertPublishedRejected(schema, "catalog-reference.lineage.incoherent");
    }

    private static JsonObject PublishedSchema() => JsonNode.Parse(File.ReadAllText(
        Path.Combine(EvidenceFixture.ContractsDirectory(), "catalog-entry-ref.v1.schema.json")))!.AsObject();

    private static JsonObject Schema(string text, string name)
    {
        string prefix = $"    {name}: ";
        string line = text.Split('\n').Single(value => value.StartsWith(prefix, StringComparison.Ordinal));
        return JsonNode.Parse(line[prefix.Length..])!.AsObject();
    }

    private static string Mutate(string name, Action<JsonObject> mutate)
    {
        string original = File.ReadAllText(EvidenceFixture.ProductiveCoreOpenApiPath());
        string prefix = $"    {name}: ";
        string line = original.Split('\n').Single(value => value.StartsWith(prefix, StringComparison.Ordinal));
        JsonObject schema = Schema(original, name);
        mutate(schema);
        return original.Replace(line, prefix + schema.ToJsonString(), StringComparison.Ordinal);
    }

    private static void AssertRejected(string text, string code)
    {
        IReadOnlyList<ValidationIssue> issues = ProductiveCycleOpenApiContractGuard.Validate(text);
        Assert.IsTrue(issues.Any(issue => issue.Code == code), string.Join(Environment.NewLine, issues));
    }

    private static void AssertPublishedRejected(JsonObject schema, string code)
    {
        IReadOnlyList<ValidationIssue> issues = PublishedSchemaValidator.Validate("catalog-entry-ref.v1.schema.json", schema.ToJsonString());
        Assert.IsTrue(issues.Any(issue => issue.Code == code), string.Join(Environment.NewLine, issues));
    }
}
