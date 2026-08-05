using AgropecuarIA.ArchitectureFitness;

namespace AgropecuarIA.ArchitectureFitness.Tests;

[TestClass]
public sealed class ContractCompatibilityTests
{
    private static readonly string[] ExpectedBreakingCodes =
    [
        "field.type-changed",
        "field.became-required",
        "enum.closed",
        "enum.value-removed",
    ];

    [TestMethod]
    public void PublishedAdditiveChangeIsCompatibleWithNMinusOne()
    {
        var result = ContractCompatibility.Evaluate(
            EvidenceFixture.Contract("n-1.json"),
            EvidenceFixture.Contract("n-additive.json"));

        Assert.IsTrue(result.IsCompatible, string.Join(Environment.NewLine, result.Issues));
    }

    [TestMethod]
    public void PublishedBreakingChangeIdentifiesTypeRequiredAndClosedEnumBreaks()
    {
        var result = ContractCompatibility.Evaluate(
            EvidenceFixture.Contract("n-1.json"),
            EvidenceFixture.Contract("n-breaking.json"));

        Assert.IsFalse(result.IsCompatible);
        CollectionAssert.IsSubsetOf(
            ExpectedBreakingCodes,
            result.Issues.Select(issue => issue.Code).ToArray());
    }

    [TestMethod]
    public void RemovedFieldIsBreaking()
    {
        var previous = EvidenceFixture.Contract("n-1.json");
        var current = EvidenceFixture.Contract("n-additive.json") with
        {
            Fields = EvidenceFixture.Contract("n-additive.json").Fields
                .Where(field => field.Name != "name")
                .ToArray(),
        };

        AssertIssue(previous, current, "field.removed");
    }

    [TestMethod]
    public void NewlyRequiredFieldIsBreaking()
    {
        var previous = EvidenceFixture.Contract("n-1.json");
        var current = EvidenceFixture.Contract("n-additive.json") with
        {
            Fields = [
                .. EvidenceFixture.Contract("n-additive.json").Fields,
                new ContractField("serverRequired", "string", Required: true),
            ],
        };

        AssertIssue(previous, current, "field.required-added");
    }

    [TestMethod]
    public void NMinusOneMustTolerateUnknownFields()
    {
        var previous = EvidenceFixture.Contract("n-1.json") with { ToleratesUnknownFields = false };

        AssertIssue(previous, EvidenceFixture.Contract("n-additive.json"), "n-1.unknown-fields-rejected");
    }

    [TestMethod]
    public void AddingValueToClosedEnumIsBreaking()
    {
        var previous = EvidenceFixture.Contract("n-1.json");
        var closedFields = previous.Fields
            .Select(field => field.Name == "state" ? field with { ExtensibleEnum = false } : field)
            .ToArray();
        var closedPrevious = previous with { Fields = closedFields };

        AssertIssue(closedPrevious, EvidenceFixture.Contract("n-additive.json"), "enum.closed-value-added");
    }

    [TestMethod]
    public void RequiredResponseFieldCannotBecomeOptional()
    {
        var previous = EvidenceFixture.Contract("n-1.json");
        var current = EvidenceFixture.Contract("n-additive.json") with
        {
            Fields = EvidenceFixture.Contract("n-additive.json").Fields
                .Select(field => field.Name == "name" ? field with { Required = false } : field)
                .ToArray(),
        };

        AssertIssue(previous, current, "field.became-optional");
    }

    private static void AssertIssue(ContractSnapshot previous, ContractSnapshot current, string expectedCode)
    {
        var result = ContractCompatibility.Evaluate(previous, current);
        Assert.IsTrue(
            result.Issues.Any(issue => issue.Code == expectedCode),
            $"Expected '{expectedCode}'. Actual:{Environment.NewLine}{string.Join(Environment.NewLine, result.Issues)}");
    }
}
