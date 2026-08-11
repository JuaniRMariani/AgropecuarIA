using AgropecuarIA.ArchitectureFitness;

namespace AgropecuarIA.ArchitectureFitness.Tests;

[TestClass]
public sealed class TerritoryOpenApiContractTests
{
    [TestMethod]
    public void TerritoryOpenApiMatchesTheReviewedReadOnlyDegradationContract()
    {
        var issues = TerritoryOpenApiContractGuard.Validate(EvidenceFixture.TerritoryOpenApiPath());

        Assert.HasCount(0, issues, string.Join(Environment.NewLine, issues));
    }

    [DataRow("  version: 1.0.0", "  version: 2.0.0", "territory-openapi.version.invalid")]
    [DataRow("/api/territory/search:", "/api/territory/find:", "territory-openapi.search.missing")]
    [DataRow("        - SessionCookie: []", "        - UnknownCookie: []", "territory-openapi.session-security.missing")]
    [DataRow("        status: { type: string, enum: [fresh, stale, unavailable] }", "        status: { type: string, enum: [fresh, stale] }", "territory-openapi.degradation.incomplete")]
    [DataRow("pattern: '^[0-9]+$'", "pattern: '^.*$'", "territory-openapi.parent-code.open")]
    [DataRow("        retryable: { type: boolean }", "", "territory-openapi.problem.retryable.missing")]
    [DataRow("hierarchyLabel: { type: string, minLength: 1, maxLength: 700 }", "hierarchyLabel: { type: string, minLength: 1, maxLength: 500 }", "territory-openapi.hierarchy-label.too-short")]
    [TestMethod]
    public void TerritoryOpenApiRejectsContractDrift(string current, string mutation, string expectedCode)
    {
        string original = File.ReadAllText(EvidenceFixture.TerritoryOpenApiPath());
        string path = Path.Combine(Path.GetTempPath(), $"agro-territory-openapi-{Guid.NewGuid():N}.yaml");
        try
        {
            File.WriteAllText(path, original.Replace(current, mutation, StringComparison.Ordinal));

            var issues = TerritoryOpenApiContractGuard.Validate(path);

            Assert.IsTrue(issues.Any(issue => issue.Code == expectedCode));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [TestMethod]
    public void TerritoryGeorefClientDisablesUriLoggingFromHttpClientFactory()
    {
        string source = File.ReadAllText(Path.Combine(
            EvidenceFixture.RepositoryRoot(),
            "src",
            "AgropecuarIA.Territory",
            "TerritoryServiceCollectionExtensions.cs"));

        StringAssert.Contains(source, ".RemoveAllLoggers()");
    }
}
