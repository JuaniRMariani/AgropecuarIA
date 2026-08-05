using AgropecuarIA.ArchitectureFitness;

namespace AgropecuarIA.ArchitectureFitness.Tests;

[TestClass]
public sealed class ArchitectureValidatorTests
{
    [TestMethod]
    public void PublishedEvidenceDefinesValidAcyclicBoundariesAndExactConsumerEdges()
    {
        var issues = ArchitectureValidator.Validate(
            EvidenceFixture.Boundaries(),
            EvidenceFixture.ConsumerMap());

        Assert.HasCount(0, issues, string.Join(Environment.NewLine, issues));
    }

    [TestMethod]
    public void CycleIsRejected()
    {
        var boundaries = MutateModule(
            EvidenceFixture.Boundaries(),
            "audit-compliance",
            module => module with { AllowedDependencies = ["identity-tenancy"] });

        AssertIssue(boundaries, EvidenceFixture.ConsumerMap(), "dependency.cycle");
    }

    [TestMethod]
    public void UnknownDependencyIsRejected()
    {
        var boundaries = MutateModule(
            EvidenceFixture.Boundaries(),
            "identity-tenancy",
            module => module with { AllowedDependencies = [.. module.AllowedDependencies, "unknown-module"] });

        AssertIssue(boundaries, EvidenceFixture.ConsumerMap(), "dependency.unknown");
    }

    [TestMethod]
    public void DuplicateSchemaAndAggregateOwnershipAreRejected()
    {
        var identity = EvidenceFixture.Boundaries().Modules.Single(module => module.Id == "identity-tenancy");
        var boundaries = MutateModule(
            EvidenceFixture.Boundaries(),
            "territory",
            module => module with
            {
                DatabaseSchema = identity.DatabaseSchema,
                OwnedAggregates = [.. module.OwnedAggregates, "ManagementUnit"],
                AggregateScopes = new Dictionary<string, string>(module.AggregateScopes, StringComparer.Ordinal)
                {
                    ["ManagementUnit"] = "tenant",
                },
            });

        var issues = ArchitectureValidator.Validate(boundaries, EvidenceFixture.ConsumerMap());

        Assert.IsTrue(issues.Any(issue => issue.Code == "schema-owner.duplicate"));
        Assert.IsTrue(issues.Any(issue => issue.Code == "aggregate-owner.duplicate"));
        Assert.IsTrue(issues.Any(issue => issue.Code == "management-unit.owner.invalid"));
    }

    [TestMethod]
    public void MissingConsumerMappingIsRejected()
    {
        var map = EvidenceFixture.ConsumerMap();
        var contracts = map.Contracts
            .Select(contract => contract.Name == "AuditEventWriter"
                ? contract with
                {
                    Consumers = contract.Consumers
                        .Where(consumer => consumer != "identity-tenancy")
                        .ToArray(),
                }
                : contract)
            .ToArray();

        AssertIssue(EvidenceFixture.Boundaries(), map with { Contracts = contracts }, "consumer-edge.missing");
    }

    [TestMethod]
    public void InvalidModuleContractAndAggregateScopesAreRejected()
    {
        var boundaries = MutateModule(
            EvidenceFixture.Boundaries(),
            "productive-core",
            module => module with
            {
                DataScope = "implicit",
                AggregateScopes = new Dictionary<string, string>(module.AggregateScopes, StringComparer.Ordinal)
                {
                    ["ManagementUnit"] = "inherited",
                },
            });
        var map = EvidenceFixture.ConsumerMap();
        var contracts = map.Contracts
            .Select(contract => contract.Name == "ManagementUnitRef"
                ? contract with { Scope = "implicit" }
                : contract)
            .ToArray();

        var issues = ArchitectureValidator.Validate(boundaries, map with { Contracts = contracts });

        Assert.IsTrue(issues.Any(issue => issue.Code == "module-scope.invalid"));
        Assert.IsTrue(issues.Any(issue => issue.Code == "aggregate-scope.invalid"));
        Assert.IsTrue(issues.Any(issue => issue.Code == "contract-scope.invalid"));
    }

    [TestMethod]
    public void AggregateScopeKeysMustExactlyMatchOwnedAggregates()
    {
        var boundaries = MutateModule(
            EvidenceFixture.Boundaries(),
            "productive-core",
            module => module with
            {
                AggregateScopes = module.AggregateScopes
                    .Where(pair => pair.Key != "ManagementUnit")
                    .Append(new KeyValuePair<string, string>("ForeignAggregate", "tenant"))
                    .ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal),
            });

        var issues = ArchitectureValidator.Validate(boundaries, EvidenceFixture.ConsumerMap());

        Assert.IsTrue(issues.Any(issue => issue.Code == "aggregate-scope.missing"));
        Assert.IsTrue(issues.Any(issue => issue.Code == "aggregate-scope.unowned"));
    }

    [TestMethod]
    public void MixedScopeMustContainTenantAndPlatformWhileTenantModuleCannotOwnPlatformAggregate()
    {
        var boundaries = MutateModule(
            EvidenceFixture.Boundaries(),
            "identity-tenancy",
            module => module with
            {
                AggregateScopes = module.AggregateScopes.ToDictionary(
                    pair => pair.Key,
                    _ => "platform",
                    StringComparer.Ordinal),
            });
        boundaries = MutateModule(
            boundaries,
            "productive-core",
            module => module with
            {
                AggregateScopes = new Dictionary<string, string>(module.AggregateScopes, StringComparer.Ordinal)
                {
                    ["ManagementUnit"] = "platform",
                },
            });

        var issues = ArchitectureValidator.Validate(boundaries, EvidenceFixture.ConsumerMap());

        Assert.IsTrue(issues.Any(issue => issue.Code == "aggregate-scope.mixed-incomplete"));
        Assert.IsTrue(issues.Any(issue => issue.Code == "aggregate-scope.tenant-module-platform"));
    }

    [TestMethod]
    public void DirectForeignPersistenceInteractionIsRejected()
    {
        var map = EvidenceFixture.ConsumerMap();
        var contracts = map.Contracts
            .Select(contract => contract.Name == "ManagementUnitRef"
                ? contract with { Interaction = "database-schema" }
                : contract)
            .ToArray();

        AssertIssue(EvidenceFixture.Boundaries(), map with { Contracts = contracts }, "contract-interaction.invalid");
    }

    [TestMethod]
    public void ConsumerPoliciesMustCoverAuthorizationConsistencyRollbackAndTelemetry()
    {
        var map = EvidenceFixture.ConsumerMap();
        var incomplete = map with
        {
            DefaultPolicies = map.DefaultPolicies with { Authorization = "trust-client-tenant" },
        };

        AssertIssue(EvidenceFixture.Boundaries(), incomplete, "consumer-policy.unsafe");
    }

    [TestMethod]
    public void SharedKernelRejectsDomainPersistenceAndFiscalPrimitives()
    {
        var boundaries = EvidenceFixture.Boundaries() with
        {
            SharedKernel = [.. EvidenceFixture.Boundaries().SharedKernel, "Cuit", "DbContext"],
        };

        AssertIssue(boundaries, EvidenceFixture.ConsumerMap(), "shared-kernel.invalid");
    }

    private static ModuleBoundaryDocument MutateModule(
        ModuleBoundaryDocument document,
        string moduleId,
        Func<ModuleBoundary, ModuleBoundary> mutation) =>
        document with
        {
            Modules = document.Modules
                .Select(module => module.Id == moduleId ? mutation(module) : module)
                .ToArray(),
        };

    private static void AssertIssue(
        ModuleBoundaryDocument boundaries,
        ConsumerMapDocument map,
        string expectedCode)
    {
        var issues = ArchitectureValidator.Validate(boundaries, map);
        Assert.IsTrue(
            issues.Any(issue => issue.Code == expectedCode),
            $"Expected '{expectedCode}'. Actual:{Environment.NewLine}{string.Join(Environment.NewLine, issues)}");
    }
}
