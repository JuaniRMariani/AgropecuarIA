using AgropecuarIA.ArchitectureFitness;
using AgropecuarIA.Catalog.Domain;

namespace AgropecuarIA.ArchitectureFitness.Tests;

[TestClass]
public sealed class CatalogPublishedEventContractTests
{
    [TestMethod]
    public void CatalogRuntimeEventsMatchRegisteredPlatformPublicationContracts()
    {
        IReadOnlyList<ValidationIssue> issues = Validate(RuntimeEvents());
        Assert.HasCount(0, issues, string.Join(Environment.NewLine, issues));
    }

    [TestMethod]
    public void CatalogEventSourceScopeAndUnownedAggregateDriftAreRejected()
    {
        RuntimeEventContract[] mutated = RuntimeEvents().Select(item => item with
        {
            Source = "catalog",
            Scope = "tenant",
            AggregateType = "UnreviewedCatalogAggregate",
        }).ToArray();
        IReadOnlyList<ValidationIssue> issues = Validate(mutated);
        Assert.IsTrue(issues.Any(issue => issue.Code == "runtime-event.source.mismatch"));
        Assert.IsTrue(issues.Any(issue => issue.Code == "runtime-event.scope.mismatch"));
        Assert.IsTrue(issues.Any(issue => issue.Code == "runtime-event.aggregate.unowned"));
    }

    [TestMethod]
    public void CatalogCannotRegisterAnEventThatRuntimeDoesNotPublish()
    {
        IReadOnlyList<ValidationIssue> issues = Validate(RuntimeEvents().Take(1).ToArray());
        Assert.IsTrue(issues.Any(issue => issue.Code == "runtime-event.contract.unimplemented"));
    }

    private static RuntimeEventContract[] RuntimeEvents() => CatalogIntegrationEvents.All.Select(item =>
        new RuntimeEventContract(item.Type, item.MajorVersion, item.SchemaVersion, item.Source,
            item.Scope, item.AggregateType, item.PayloadSchemaPath)).ToArray();

    private static IReadOnlyList<ValidationIssue> Validate(IReadOnlyList<RuntimeEventContract> events) =>
        RuntimeEventContractValidator.Validate("national-catalog", events, EvidenceFixture.ConsumerMap(),
            EvidenceFixture.RuntimeMap(), EvidenceFixture.Boundaries());
}
