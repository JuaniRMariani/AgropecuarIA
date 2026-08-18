using AgropecuarIA.ArchitectureFitness;
using System.Text.RegularExpressions;

namespace AgropecuarIA.ArchitectureFitness.Tests;

[TestClass]
public sealed class IdentityOpenApiContractTests
{
    [TestMethod]
    public void IdentityOpenApiUsesReviewedVersionClosedProblemAndProblemRateLimit()
    {
        var issues = IdentityOpenApiContractGuard.Validate(EvidenceFixture.IdentityOpenApiPath());

        Assert.HasCount(0, issues, string.Join(Environment.NewLine, issues));
    }

    [TestMethod]
    public void IdentityOpenApiRejectsAnUnsupportedMajorVersion()
    {
        string original = File.ReadAllText(EvidenceFixture.IdentityOpenApiPath());
        string path = Path.Combine(Path.GetTempPath(), $"agro-identity-openapi-{Guid.NewGuid():N}.yaml");
        try
        {
            File.WriteAllText(
                path,
                Regex.Replace(
                    original,
                    "(?m)^  version: [0-9]+\\.[0-9]+\\.[0-9]+$",
                    "  version: 2.0.0",
                    RegexOptions.CultureInvariant,
                    TimeSpan.FromSeconds(1)));

            var issues = IdentityOpenApiContractGuard.Validate(path);

            Assert.IsTrue(issues.Any(issue => issue.Code == "identity-openapi.version.invalid"));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [TestMethod]
    public void IdentityOpenApiRejectsMissingBoundedSyntheticProfile()
    {
        string original = File.ReadAllText(EvidenceFixture.IdentityOpenApiPath());
        string path = Path.Combine(Path.GetTempPath(), $"agro-identity-openapi-{Guid.NewGuid():N}.yaml");
        try
        {
            File.WriteAllText(path, original.Replace(
                "            - google-owner-4\n",
                string.Empty,
                StringComparison.Ordinal));

            var issues = IdentityOpenApiContractGuard.Validate(path);

            Assert.IsTrue(issues.Any(issue =>
                issue.Code == "identity-openapi.development-fixtures.incomplete"));
        }
        finally
        {
            File.Delete(path);
        }
    }
}
