using AgropecuarIA.ArchitectureFitness;
using AgropecuarIA.ProductiveCore.Domain;

namespace AgropecuarIA.ArchitectureFitness.Tests;

[TestClass]
public sealed class ProductiveCorePublishedEventContractTests
{
    [TestMethod]
    public void RuntimeCatalogExactlyMatchesReviewedProductiveCoreEventAndSchema()
    {
        IReadOnlyList<ValidationIssue> issues = Validate(RuntimeEvents());

        Assert.HasCount(0, issues, string.Join(Environment.NewLine, issues));
    }

    [TestMethod]
    public void ScopeSourceAggregateAndVersionDriftAreRejected()
    {
        RuntimeEventContract created = RuntimeEvents().Single(item =>
            item.Type == ProductiveCoreIntegrationEvents.ManagementUnitCreated.Type);
        RuntimeEventContract[] invalidEvents =
        [
            created with
            {
                Source = "foreign-module",
                Scope = "platform",
                AggregateType = "ForeignAggregate",
                MajorVersion = 2,
            },
        ];

        IReadOnlyList<ValidationIssue> issues = Validate(invalidEvents);

        Assert.IsTrue(issues.Any(issue => issue.Code == "runtime-event.source.mismatch"));
        Assert.IsTrue(issues.Any(issue => issue.Code == "runtime-event.scope.mismatch"));
        Assert.IsTrue(issues.Any(issue => issue.Code == "runtime-event.aggregate.unowned"));
        Assert.IsTrue(issues.Any(issue => issue.Code == "runtime-event.version.unsupported"));
    }

    [TestMethod]
    public void MissingReviewedSchemaRegistrationIsRejected()
    {
        RuntimeMapDocument runtimeMap = EvidenceFixture.RuntimeMap();
        RuntimeModule productiveCore = runtimeMap.Modules.Single(module =>
            module.ModuleId == "productive-core");
        string schemaPath = ProductiveCoreIntegrationEvents.ManagementUnitCreated.PayloadSchemaPath;
        RuntimeMapDocument missingSchema = runtimeMap with
        {
            Modules = runtimeMap.Modules
                .Select(module => module.ModuleId == productiveCore.ModuleId
                    ? module with
                    {
                        Contracts = module.Contracts
                            .Where(contract => !string.Equals(
                                contract.Path,
                                schemaPath,
                                StringComparison.Ordinal))
                            .ToArray(),
                    }
                    : module)
                .ToArray(),
        };

        IReadOnlyList<ValidationIssue> issues = RuntimeEventContractValidator.Validate(
            "productive-core",
            RuntimeEvents(),
            EvidenceFixture.ConsumerMap(),
            missingSchema,
            EvidenceFixture.Boundaries());

        Assert.IsTrue(issues.Any(issue => issue.Code == "runtime-event.schema.missing"));
    }

    [TestMethod]
    public void RenameEventScopeAggregateAndSchemaMustRemainReviewed()
    {
        RuntimeEventContract renamed = RuntimeEvents().Single(item =>
            item.Type == ProductiveCoreIntegrationEvents.ManagementUnitDisplayNameChanged.Type);
        RuntimeEventContract[] invalidEvents = RuntimeEvents()
            .Select(item => item.Type == renamed.Type
                ? item with
                {
                    Scope = "platform",
                    AggregateType = "ForeignAggregate",
                    PayloadSchemaPath = "contracts/unreviewed.json",
                }
                : item)
            .ToArray();

        IReadOnlyList<ValidationIssue> issues = Validate(invalidEvents);

        Assert.IsTrue(issues.Any(issue => issue.Code == "runtime-event.scope.mismatch"));
        Assert.IsTrue(issues.Any(issue => issue.Code == "runtime-event.aggregate.unowned"));
        Assert.IsTrue(issues.Any(issue => issue.Code == "runtime-event.schema.missing"));
    }

    private static RuntimeEventContract[] RuntimeEvents() =>
        ProductiveCoreIntegrationEvents.All
            .Select(item => new RuntimeEventContract(
                item.Type,
                item.MajorVersion,
                item.SchemaVersion,
                item.Source,
                item.Scope,
                item.AggregateType,
                item.PayloadSchemaPath))
            .ToArray();

    private static IReadOnlyList<ValidationIssue> Validate(
        IReadOnlyList<RuntimeEventContract> runtimeEvents) =>
        RuntimeEventContractValidator.Validate(
            "productive-core",
            runtimeEvents,
            EvidenceFixture.ConsumerMap(),
            EvidenceFixture.RuntimeMap(),
            EvidenceFixture.Boundaries());
}
