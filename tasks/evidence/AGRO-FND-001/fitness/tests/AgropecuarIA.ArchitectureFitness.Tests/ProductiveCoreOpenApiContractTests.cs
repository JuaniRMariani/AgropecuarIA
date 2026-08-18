using AgropecuarIA.ArchitectureFitness;

namespace AgropecuarIA.ArchitectureFitness.Tests;

[TestClass]
public sealed class ProductiveCoreOpenApiContractTests
{
    [TestMethod]
    public void ProductiveCoreOpenApiMatchesTheReviewedNonSpatialFieldContract()
    {
        IReadOnlyList<ValidationIssue> issues = ProductiveCoreOpenApiContractGuard.Validate(
            EvidenceFixture.ProductiveCoreOpenApiPath());

        Assert.HasCount(0, issues, string.Join(Environment.NewLine, issues));
    }

    [TestMethod]
    [DataRow("  version: 1.1.0", "  version: 2.0.0", "productive-openapi.version.invalid")]
    [DataRow("/{organizationId}/fields/{fieldId}:", "/{organizationId}/units/{fieldId}:", "productive-openapi.detail.missing")]
    [DataRow(
        "        - SessionCookie: []\n          AntiforgeryCookie: []",
        "        - SessionCookie: []\n        - AntiforgeryCookie: []",
        "productive-openapi.create-security.conjunctive-required")]
    [DataRow(
        "        - SessionCookie: []\n          AntiforgeryCookie: []",
        "        - SessionCookie: []\n          UnknownCookie: []",
        "productive-openapi.create-security.conjunctive-required")]
    [DataRow("    patch:", "    trace:", "productive-openapi.rename.missing")]
    [DataRow("          name: If-Match", "          name: X-Weak-Version", "productive-openapi.if-match.missing")]
    [DataRow("            ETag:", "            X-Entity-Version:", "productive-openapi.detail-etag.missing")]
    [DataRow("            ETag:", "            X-Entity-Version:", "productive-openapi.rename-etag.missing")]
    [DataRow("        '412': { $ref: '#/components/responses/PreconditionFailed' }", "        '410': { $ref: '#/components/responses/PreconditionFailed' }", "productive-openapi.precondition.missing")]
    [DataRow("schema: { $ref: '#/components/schemas/RenameFieldRequest' }", "schema: { $ref: '#/components/schemas/CreateFieldRequest' }", "productive-openapi.rename-request.missing")]
    [DataRow("schema: { $ref: '#/components/schemas/RenamedField' }", "schema: { $ref: '#/components/schemas/CreatedField' }", "productive-openapi.rename-response.missing")]
    [DataRow("        revision: { type: integer, minimum: 2 }", "        revision: { type: integer, minimum: 1 }", "productive-openapi.rename-revision.invalid")]
    [DataRow("        type: { type: string, const: field }", "        type: { type: string }", "productive-openapi.type.open")]
    [DataRow("        status: { type: string, const: draft }", "        status: { type: string }", "productive-openapi.status.open")]
    [DataRow("        spatialStatus: { type: string, const: not_configured }", "        spatialStatus: { type: string }", "productive-openapi.spatial-status.open")]
    [DataRow("        retryable: { type: boolean }", "", "productive-openapi.problem.retryable.missing")]
    [DataRow("organization field capacity reached (productive_core.management_unit_capacity_reached)", "field conflict", "productive-openapi.capacity-conflict.missing")]
    [DataRow("                maxItems: 100", "                maxItems: 101", "productive-openapi.list-bound.invalid")]
    [DataRow("displayName trims leading and trailing Unicode White_Space plus U+FEFF, then normalizes to NFC, counts Unicode scalars and rejects controls or lone surrogates.", "displayName is trimmed.", "productive-openapi.name-canonicalization.missing")]
    public void ProductiveCoreOpenApiRejectsContractDrift(
        string current,
        string mutation,
        string expectedCode)
    {
        string original = File.ReadAllText(EvidenceFixture.ProductiveCoreOpenApiPath());
        StringAssert.Contains(original, current);
        string path = Path.Combine(Path.GetTempPath(), $"agro-productive-openapi-{Guid.NewGuid():N}.yaml");
        try
        {
            File.WriteAllText(path, original.Replace(current, mutation, StringComparison.Ordinal));
            IReadOnlyList<ValidationIssue> issues = ProductiveCoreOpenApiContractGuard.Validate(path);
            Assert.IsTrue(issues.Any(issue => issue.Code == expectedCode));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [TestMethod]
    public void ProductiveCoreRenameRequiresConjunctiveSessionAndAntiforgerySecurity()
    {
        const string ConjunctiveSecurity =
            "        - SessionCookie: []\n          AntiforgeryCookie: []";
        const string DisjunctiveSecurity =
            "        - SessionCookie: []\n        - AntiforgeryCookie: []";
        string original = File.ReadAllText(EvidenceFixture.ProductiveCoreOpenApiPath());
        int renameSecurity = original.LastIndexOf(ConjunctiveSecurity, StringComparison.Ordinal);
        Assert.IsGreaterThanOrEqualTo(0, renameSecurity);
        string mutation = string.Concat(
            original.AsSpan(0, renameSecurity),
            DisjunctiveSecurity,
            original.AsSpan(renameSecurity + ConjunctiveSecurity.Length));
        string path = Path.Combine(Path.GetTempPath(), $"agro-productive-rename-security-{Guid.NewGuid():N}.yaml");
        try
        {
            File.WriteAllText(path, mutation);
            IReadOnlyList<ValidationIssue> issues = ProductiveCoreOpenApiContractGuard.Validate(path);
            Assert.IsTrue(issues.Any(issue =>
                issue.Code == "productive-openapi.rename-security.conjunctive-required"));
        }
        finally
        {
            File.Delete(path);
        }
    }
}
