using AgropecuarIA.ArchitectureFitness;

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
}
