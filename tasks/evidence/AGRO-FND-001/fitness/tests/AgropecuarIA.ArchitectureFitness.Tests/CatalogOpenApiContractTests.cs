using System.Text.Json.Nodes;
using AgropecuarIA.ArchitectureFitness;

namespace AgropecuarIA.ArchitectureFitness.Tests;

[TestClass]
public sealed class CatalogOpenApiContractTests
{
    [TestMethod]
    public void CatalogV2RequiresReviewedCandidateAndHonestHistoricalProvenance()
    {
        IReadOnlyList<ValidationIssue> issues = CatalogOpenApiContractGuard.Validate(EvidenceFixture.CatalogOpenApiPath());
        Assert.HasCount(0, issues, string.Join(Environment.NewLine, issues));
    }

    [TestMethod]
    [DataRow("  version: 2.0.0", "  version: 1.0.0", "catalog-openapi.version.invalid")]
    [DataRow("  /api/catalog/versions:", "  /api/catalog/unreviewed:", "catalog-openapi.route.missing")]
    [DataRow("        - SessionCookie: []\n          AntiforgeryCookie: []", "        - SessionCookie: []\n        - AntiforgeryCookie: []", "catalog-openapi.mutation.security.invalid")]
    [DataRow("Platform editorial permission required.", "Any reader allowed.", "catalog-openapi.diff.editorial-required")]
    [DataRow("No idempotency ledger or automatic retry", "Automatically retry", "catalog-openapi.retry-boundary.missing")]
    [DataRow("\"name\":\"versionId\",\"in\":\"query\"", "\"name\":\"unreviewedVersion\",\"in\":\"query\"", "catalog-openapi.historical-version.missing")]
    public void CatalogV2RejectsOperationDrift(string current, string replacement, string expectedCode)
    {
        string original = File.ReadAllText(EvidenceFixture.CatalogOpenApiPath());
        StringAssert.Contains(original, current);
        AssertRejected(original.Replace(current, replacement, StringComparison.Ordinal), expectedCode);
    }

    [TestMethod]
    public void PublicationCannotOmitCandidatePreconditionOrAcceptCallerPublisher()
    {
        string mutated = MutateSchema("PublishCatalog", schema =>
        {
            JsonArray required = schema["required"]!.AsArray();
            required.Remove(required.Single(item => item!.GetValue<string>() == "candidateHash"));
            schema["properties"]!["publishedBy"] = new JsonObject { ["type"] = "string" };
        });
        AssertRejected(mutated, "catalog-openapi.required.invalid");
        AssertRejected(mutated, "catalog-openapi.properties.invalid");
    }

    [TestMethod]
    public void SourceUploadCannotClaimSupportOrAcceptUnknownMembers()
    {
        string mutated = MutateSchema("RawCatalogEntry", schema =>
        {
            schema["additionalProperties"] = true;
            schema["properties"]!["supportLevel"] = new JsonObject { ["type"] = "string" };
        });
        AssertRejected(mutated, "catalog-openapi.ingest-support.forbidden");
        AssertRejected(mutated, "catalog-openapi.schema.open");
    }

    [TestMethod]
    [DataRow("sourceSnapshotId")]
    [DataRow("sourceId")]
    [DataRow("sourceHash")]
    [DataRow("sourceIngestedAtUtc")]
    public void LegacyProvenanceMustRemainNullable(string property)
    {
        string mutated = MutateSchema("CatalogItem", schema => schema["properties"]![property]!["type"] = "string");
        AssertRejected(mutated, "catalog-openapi.legacy-provenance.not-nullable");
    }

    [TestMethod]
    public void CatalogCannotAdvertiseSpecializedCapabilitiesOrCollapseLegacyState()
    {
        string mutated = MutateSchema("CatalogItem", schema =>
        {
            schema["properties"]!["capabilities"]!["maxItems"] = 1;
            schema["properties"]!["provenanceStatus"]!["enum"] = new JsonArray("verified_snapshot");
        });
        AssertRejected(mutated, "catalog-openapi.capabilities.open");
        AssertRejected(mutated, "catalog-openapi.provenance-status.invalid");
    }

    [TestMethod]
    [DataRow("CatalogSearch", "items")]
    [DataRow("CatalogVersions", "versions")]
    public void ReadersCannotBecomeUnbounded(string schemaName, string property)
    {
        string mutated = MutateSchema(schemaName, schema => schema["properties"]![property]!["maxItems"] = 101);
        AssertRejected(mutated, "catalog-openapi.read-bound.invalid");
    }

    [TestMethod]
    public void AbsentCapabilitiesCannotBeSilentlyRemoved()
    {
        string mutated = MutateSchema("CatalogItem", schema => schema["properties"]!["absentCapabilities"]!["const"] = new JsonArray());
        AssertRejected(mutated, "catalog-openapi.absent-capabilities.invalid");
    }

    [TestMethod]
    public void CandidateCannotBecomeUnbounded()
    {
        string mutated = MutateSchema("EditorialDiff", schema => schema["properties"]!["selectedSnapshots"]!["maxItems"] = 65);
        AssertRejected(mutated, "catalog-openapi.candidate-bound.invalid");
    }

    private static string MutateSchema(string name, Action<JsonObject> mutation)
    {
        string original = File.ReadAllText(EvidenceFixture.CatalogOpenApiPath());
        string prefix = $"    {name}: ";
        string line = original.Split('\n').Single(value => value.StartsWith(prefix, StringComparison.Ordinal));
        JsonObject schema = JsonNode.Parse(line[prefix.Length..])!.AsObject();
        mutation(schema);
        return original.Replace(line, prefix + schema.ToJsonString(), StringComparison.Ordinal);
    }

    private static void AssertRejected(string text, string code)
    {
        string path = Path.Combine(Path.GetTempPath(), $"agro-catalog-contract-{Guid.NewGuid():N}.yaml");
        try
        {
            File.WriteAllText(path, text);
            IReadOnlyList<ValidationIssue> issues = CatalogOpenApiContractGuard.Validate(path);
            Assert.IsTrue(issues.Any(issue => issue.Code == code), string.Join(Environment.NewLine, issues));
        }
        finally
        {
            File.Delete(path);
        }
    }
}
