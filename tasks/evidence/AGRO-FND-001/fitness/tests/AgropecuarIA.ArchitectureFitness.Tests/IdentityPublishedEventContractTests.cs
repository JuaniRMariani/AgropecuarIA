using AgropecuarIA.ArchitectureFitness;
using AgropecuarIA.Identity.Domain;

namespace AgropecuarIA.ArchitectureFitness.Tests;

[TestClass]
public sealed class IdentityPublishedEventContractTests
{
    [TestMethod]
    public void RuntimeCatalogExactlyMatchesReviewedIdentityEventsAndSchemas()
    {
        var issues = Validate(RuntimeEvents());

        Assert.HasCount(0, issues, string.Join(Environment.NewLine, issues));
    }

    [TestMethod]
    public void RuntimeEventWithoutReviewedContractIsRejected()
    {
        var map = EvidenceFixture.ConsumerMap();
        var withoutStepUp = map with
        {
            Contracts = map.Contracts
                .Where(contract => contract.Name != "IdentityStepUpCompleted")
                .ToArray(),
        };

        AssertIssue(RuntimeEvents(), withoutStepUp, EvidenceFixture.RuntimeMap(), "runtime-event.unpublished");
    }

    [TestMethod]
    public void ReviewedEventWithoutRuntimeImplementationIsRejected()
    {
        var map = EvidenceFixture.ConsumerMap();
        var withOrphan = map with
        {
            Contracts =
            [
                .. map.Contracts,
                new PublishedContract("identity-tenancy", [], "IdentityOrphaned", "event", "platform", ["1.x"]),
            ],
        };

        AssertIssue(RuntimeEvents(), withOrphan, EvidenceFixture.RuntimeMap(), "runtime-event.contract.unimplemented");
    }

    [TestMethod]
    public void ScopeSourceAndVersionDriftAreRejected()
    {
        RuntimeEventContract stepUp = RuntimeEvents().Single(item => item.Type == "IdentityStepUpCompleted");
        RuntimeEventContract[] invalidEvents =
        [
            .. RuntimeEvents().Where(item => item.Type != stepUp.Type),
            stepUp with { Source = "foreign-module", Scope = "tenant", MajorVersion = 2 },
        ];

        var issues = Validate(invalidEvents);

        Assert.IsTrue(issues.Any(issue => issue.Code == "runtime-event.source.mismatch"));
        Assert.IsTrue(issues.Any(issue => issue.Code == "runtime-event.scope.mismatch"));
        Assert.IsTrue(issues.Any(issue => issue.Code == "runtime-event.aggregate.scope-mismatch"));
        Assert.IsTrue(issues.Any(issue => issue.Code == "runtime-event.version.unsupported"));
    }

    [TestMethod]
    public void EventAggregateMustBeOwnedByThePublishingModule()
    {
        RuntimeEventContract linked = RuntimeEvents().Single(item => item.Type == "IdentityLinked");
        RuntimeEventContract[] invalidEvents =
        [
            .. RuntimeEvents().Where(item => item.Type != linked.Type),
            linked with { AggregateType = "ForeignAggregate" },
        ];

        var issues = Validate(invalidEvents);

        Assert.IsTrue(issues.Any(issue => issue.Code == "runtime-event.aggregate.unowned"));
    }

    [TestMethod]
    public void MissingOrMismatchedPayloadSchemaRegistrationIsRejected()
    {
        RuntimeMapDocument map = EvidenceFixture.RuntimeMap();
        RuntimeModule identity = map.Modules.Single(module => module.ModuleId == "identity-tenancy");
        string stepUpPath = IdentityIntegrationEvents.IdentityStepUpCompleted.PayloadSchemaPath;
        var missingSchema = map with
        {
            Modules =
            [
                identity with
                {
                    Contracts = identity.Contracts
                        .Where(contract => !string.Equals(contract.Path, stepUpPath, StringComparison.Ordinal))
                        .ToArray(),
                },
            ],
        };
        AssertIssue(RuntimeEvents(), EvidenceFixture.ConsumerMap(), missingSchema, "runtime-event.schema.missing");

        var mismatchedSchema = map with
        {
            Modules =
            [
                identity with
                {
                    Contracts = identity.Contracts
                        .Select(contract => string.Equals(contract.Path, stepUpPath, StringComparison.Ordinal)
                            ? contract with { Version = "2.0.0" }
                            : contract)
                        .ToArray(),
                },
            ],
        };
        AssertIssue(RuntimeEvents(), EvidenceFixture.ConsumerMap(), mismatchedSchema, "runtime-event.schema.version-mismatch");
    }

    private static RuntimeEventContract[] RuntimeEvents() =>
        IdentityIntegrationEvents.All
            .Select(item => new RuntimeEventContract(
                item.Type,
                item.MajorVersion,
                item.SchemaVersion,
                item.Source,
                item.Scope,
                item.AggregateType,
                item.PayloadSchemaPath))
            .ToArray();

    private static IReadOnlyList<ValidationIssue> Validate(IReadOnlyList<RuntimeEventContract> runtimeEvents) =>
        RuntimeEventContractValidator.Validate(
            "identity-tenancy",
            runtimeEvents,
            EvidenceFixture.ConsumerMap(),
            EvidenceFixture.RuntimeMap(),
            EvidenceFixture.Boundaries());

    private static void AssertIssue(
        IReadOnlyList<RuntimeEventContract> runtimeEvents,
        ConsumerMapDocument consumerMap,
        RuntimeMapDocument runtimeMap,
        string expectedCode)
    {
        var issues = RuntimeEventContractValidator.Validate(
            "identity-tenancy",
            runtimeEvents,
            consumerMap,
            runtimeMap,
            EvidenceFixture.Boundaries());

        Assert.IsTrue(
            issues.Any(issue => issue.Code == expectedCode),
            $"Expected '{expectedCode}'. Actual:{Environment.NewLine}{string.Join(Environment.NewLine, issues)}");
    }
}
