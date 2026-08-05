namespace AgropecuarIA.ArchitectureFitness;

public static class ArchitectureValidator
{
    private const int ExpectedModuleCount = 15;

    private static readonly HashSet<string> AllowedModuleScopes = new(StringComparer.Ordinal)
    {
        "tenant",
        "platform",
        "mixed-explicit",
    };

    private static readonly HashSet<string> AllowedContractScopes = new(StringComparer.Ordinal)
    {
        "tenant",
        "platform",
        "tenant-or-platform-explicit",
    };

    private static readonly HashSet<string> ExpectedSharedKernel = new(StringComparer.Ordinal)
    {
        "ActorId",
        "TenantId",
        "CorrelationId",
        "RequestScope",
        "ContractVersion",
    };

    private static readonly HashSet<string> AllowedAggregateScopes = new(StringComparer.Ordinal)
    {
        "tenant",
        "platform",
    };

    private static readonly HashSet<string> AllowedInteractions = new(StringComparer.Ordinal)
    {
        "application-port",
        "event",
        "request-context",
    };

    public static IReadOnlyList<ValidationIssue> Validate(
        ModuleBoundaryDocument boundaries,
        ConsumerMapDocument consumerMap)
    {
        ArgumentNullException.ThrowIfNull(boundaries);
        ArgumentNullException.ThrowIfNull(consumerMap);

        var issues = new List<ValidationIssue>();
        var modules = boundaries.Modules ?? [];
        var contracts = consumerMap.Contracts ?? [];

        if (!string.Equals(boundaries.DependencyDirection, "consumer-to-provider", StringComparison.Ordinal))
        {
            Add(issues, "dependency-direction.invalid", "Dependency direction must be consumer-to-provider.");
        }

        if (!string.Equals(consumerMap.CompatibilityWindow, "N/N-1", StringComparison.Ordinal))
        {
            Add(issues, "compatibility-window.invalid", "Compatibility window must be N/N-1.");
        }

        ValidateConsumerPolicies(consumerMap.DefaultPolicies, issues);

        if (modules.Count != ExpectedModuleCount)
        {
            Add(issues, "module-count.invalid", $"Expected {ExpectedModuleCount} modules but found {modules.Count}.");
        }

        ValidateSharedKernel(boundaries.SharedKernel, issues);

        ValidateUniqueValues(modules, module => module.Id, "module-id.duplicate", "module ID", issues);
        ValidateUniqueValues(modules, module => module.DatabaseSchema, "schema-owner.duplicate", "database schema", issues);

        var aggregateOwners = modules
            .SelectMany(module => (module.OwnedAggregates ?? []).Select(aggregate => (Aggregate: aggregate, Module: module.Id)))
            .GroupBy(item => item.Aggregate, StringComparer.Ordinal);
        foreach (var group in aggregateOwners.Where(group => group.Count() > 1))
        {
            Add(
                issues,
                "aggregate-owner.duplicate",
                $"Aggregate '{group.Key}' is owned by multiple modules: {string.Join(", ", group.Select(item => item.Module).Order(StringComparer.Ordinal))}.");
        }

        foreach (var module in modules)
        {
            ValidateModuleShape(module, issues);
        }

        var modulesById = modules
            .GroupBy(module => module.Id, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);
        var declaredEdges = ValidateDependencies(modulesById, issues);
        ValidateAcyclic(modulesById, issues);
        ValidateManagementUnitOwnership(modulesById, issues);

        var mappedEdges = ValidateContracts(contracts, modulesById, issues);
        ValidateEdgeCoverage(declaredEdges, mappedEdges, issues);

        return issues
            .OrderBy(issue => issue.Code, StringComparer.Ordinal)
            .ThenBy(issue => issue.Message, StringComparer.Ordinal)
            .ToArray();
    }

    private static void ValidateModuleShape(ModuleBoundary module, ICollection<ValidationIssue> issues)
    {
        if (string.IsNullOrWhiteSpace(module.Id))
        {
            Add(issues, "module-id.missing", "Every module requires an ID.");
        }

        if (string.IsNullOrWhiteSpace(module.Owner))
        {
            Add(issues, "module-owner.missing", $"Module '{module.Id}' requires an owner.");
        }

        if (string.IsNullOrWhiteSpace(module.DatabaseSchema))
        {
            Add(issues, "schema-owner.missing", $"Module '{module.Id}' requires an owned database schema.");
        }

        if (!AllowedModuleScopes.Contains(module.DataScope))
        {
            Add(issues, "module-scope.invalid", $"Module '{module.Id}' has non-explicit data scope '{module.DataScope}'.");
        }

        ValidateAggregateScopes(module, issues);
    }

    private static void ValidateConsumerPolicies(
        ConsumerMapPolicies? policies,
        ICollection<ValidationIssue> issues)
    {
        if (policies is null
            || !string.Equals(policies.Authorization, "provider-rechecks-effective-scope-and-resource-before-lookup", StringComparison.Ordinal)
            || !string.Equals(policies.Consistency, "provider-owned-transaction; references are opaque and versioned", StringComparison.Ordinal)
            || !string.Equals(policies.Rollback, "consumer owner can roll back within N/N-1; data changes roll forward", StringComparison.Ordinal)
            || !string.Equals(policies.Telemetry, "contract version, consumer and bounded result only; no tenant, CUIT, resource ID, coordinates or payload", StringComparison.Ordinal))
        {
            Add(issues, "consumer-policy.unsafe", "Consumer policies must match the reviewed authorization, consistency, rollback and telemetry defaults.");
        }
    }

    private static void ValidateSharedKernel(
        IReadOnlyList<string>? sharedKernel,
        ICollection<ValidationIssue> issues)
    {
        var actual = (sharedKernel ?? []).ToHashSet(StringComparer.Ordinal);
        if (!actual.SetEquals(ExpectedSharedKernel) || actual.Count != (sharedKernel?.Count ?? 0))
        {
            Add(issues, "shared-kernel.invalid", "The shared kernel is limited to reviewed IDs, scope, correlation and contract version primitives.");
        }
    }

    private static void ValidateAggregateScopes(ModuleBoundary module, ICollection<ValidationIssue> issues)
    {
        var owned = (module.OwnedAggregates ?? []).ToHashSet(StringComparer.Ordinal);
        var declaredScopes = module.AggregateScopes
            ?? new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var aggregate in owned.Except(declaredScopes.Keys).Order(StringComparer.Ordinal))
        {
            Add(issues, "aggregate-scope.missing", $"Aggregate '{module.Id}.{aggregate}' requires an explicit scope.");
        }

        foreach (var aggregate in declaredScopes.Keys.Except(owned).Order(StringComparer.Ordinal))
        {
            Add(issues, "aggregate-scope.unowned", $"Module '{module.Id}' declares scope for unowned aggregate '{aggregate}'.");
        }

        foreach (var aggregateScope in declaredScopes)
        {
            if (!AllowedAggregateScopes.Contains(aggregateScope.Value))
            {
                Add(issues, "aggregate-scope.invalid", $"Aggregate '{module.Id}.{aggregateScope.Key}' has invalid scope '{aggregateScope.Value}'.");
            }
        }

        if (string.Equals(module.DataScope, "tenant", StringComparison.Ordinal)
            && declaredScopes.Values.Any(scope => !string.Equals(scope, "tenant", StringComparison.Ordinal)))
        {
            Add(issues, "aggregate-scope.tenant-module-platform", $"Tenant module '{module.Id}' cannot own platform-scoped aggregates.");
        }

        if (string.Equals(module.DataScope, "platform", StringComparison.Ordinal)
            && declaredScopes.Values.Any(scope => !string.Equals(scope, "platform", StringComparison.Ordinal)))
        {
            Add(issues, "aggregate-scope.platform-module-tenant", $"Platform module '{module.Id}' cannot own tenant-scoped aggregates.");
        }

        if (string.Equals(module.DataScope, "mixed-explicit", StringComparison.Ordinal)
            && !(declaredScopes.Values.Contains("tenant", StringComparer.Ordinal)
                && declaredScopes.Values.Contains("platform", StringComparer.Ordinal)))
        {
            Add(issues, "aggregate-scope.mixed-incomplete", $"Mixed module '{module.Id}' must identify at least one tenant and one platform aggregate.");
        }
    }

    private static HashSet<ModuleEdge> ValidateDependencies(
        IReadOnlyDictionary<string, ModuleBoundary> modules,
        ICollection<ValidationIssue> issues)
    {
        var edges = new HashSet<ModuleEdge>();
        foreach (var module in modules.Values)
        {
            foreach (var dependency in module.AllowedDependencies ?? [])
            {
                if (string.Equals(module.Id, dependency, StringComparison.Ordinal))
                {
                    Add(issues, "dependency.self", $"Module '{module.Id}' cannot depend on itself.");
                    continue;
                }

                if (!modules.ContainsKey(dependency))
                {
                    Add(issues, "dependency.unknown", $"Module '{module.Id}' references unknown dependency '{dependency}'.");
                    continue;
                }

                if (!edges.Add(new ModuleEdge(module.Id, dependency)))
                {
                    Add(issues, "dependency.duplicate", $"Dependency '{module.Id}' -> '{dependency}' is declared more than once.");
                }
            }
        }

        return edges;
    }

    private static void ValidateAcyclic(
        IReadOnlyDictionary<string, ModuleBoundary> modules,
        ICollection<ValidationIssue> issues)
    {
        var states = new Dictionary<string, VisitState>(StringComparer.Ordinal);
        var stack = new Stack<string>();

        foreach (var moduleId in modules.Keys.Order(StringComparer.Ordinal))
        {
            if (Visit(moduleId, modules, states, stack, out var cycle))
            {
                Add(issues, "dependency.cycle", $"Dependency cycle detected: {string.Join(" -> ", cycle)}.");
                return;
            }
        }
    }

    private static bool Visit(
        string moduleId,
        IReadOnlyDictionary<string, ModuleBoundary> modules,
        IDictionary<string, VisitState> states,
        Stack<string> stack,
        out IReadOnlyList<string> cycle)
    {
        if (states.TryGetValue(moduleId, out var state))
        {
            if (state == VisitState.Visited)
            {
                cycle = [];
                return false;
            }

            var path = stack.Reverse().ToList();
            var start = path.FindIndex(item => string.Equals(item, moduleId, StringComparison.Ordinal));
            cycle = path.Skip(start).Append(moduleId).ToArray();
            return true;
        }

        states[moduleId] = VisitState.Visiting;
        stack.Push(moduleId);
        foreach (var dependency in modules[moduleId].AllowedDependencies ?? [])
        {
            if (modules.ContainsKey(dependency)
                && Visit(dependency, modules, states, stack, out cycle))
            {
                return true;
            }
        }

        _ = stack.Pop();
        states[moduleId] = VisitState.Visited;
        cycle = [];
        return false;
    }

    private static void ValidateManagementUnitOwnership(
        IReadOnlyDictionary<string, ModuleBoundary> modules,
        ICollection<ValidationIssue> issues)
    {
        if (!OwnsExactly(modules, "ManagementUnit", "productive-core"))
        {
            Add(issues, "management-unit.owner.invalid", "Productive Core must exclusively own ManagementUnit.");
        }

        if (!OwnsExactly(modules, "SpatialRepresentationVersion", "territory"))
        {
            Add(issues, "spatial-version.owner.invalid", "Territory must exclusively own SpatialRepresentationVersion.");
        }
    }

    private static bool OwnsExactly(
        IReadOnlyDictionary<string, ModuleBoundary> modules,
        string aggregate,
        string expectedModule) =>
        modules.Values
            .Where(module => (module.OwnedAggregates ?? []).Contains(aggregate, StringComparer.Ordinal))
            .Select(module => module.Id)
            .SequenceEqual([expectedModule], StringComparer.Ordinal);

    private static List<ModuleEdge> ValidateContracts(
        IReadOnlyList<PublishedContract> contracts,
        Dictionary<string, ModuleBoundary> modules,
        ICollection<ValidationIssue> issues)
    {
        var edges = new List<ModuleEdge>();
        var contractKeys = new HashSet<string>(StringComparer.Ordinal);

        foreach (var contract in contracts)
        {
            var key = $"{contract.Provider}:{contract.Name}";
            if (!contractKeys.Add(key))
            {
                Add(issues, "contract.duplicate", $"Published contract '{key}' is duplicated.");
            }

            if (!modules.ContainsKey(contract.Provider))
            {
                Add(issues, "contract-provider.unknown", $"Contract '{contract.Name}' references unknown provider '{contract.Provider}'.");
            }

            if (!AllowedInteractions.Contains(contract.Interaction))
            {
                Add(issues, "contract-interaction.invalid", $"Contract '{contract.Name}' uses forbidden interaction '{contract.Interaction}'; direct persistence access is not a module contract.");
            }

            if (!AllowedContractScopes.Contains(contract.Scope))
            {
                Add(issues, "contract-scope.invalid", $"Contract '{contract.Name}' has non-explicit scope '{contract.Scope}'.");
            }

            if (contract.SupportedVersions is null
                || !contract.SupportedVersions.Contains("1.x", StringComparer.Ordinal))
            {
                Add(issues, "contract-version.invalid", $"Contract '{contract.Name}' must declare support for version family 1.x.");
            }

            foreach (var consumer in contract.Consumers ?? [])
            {
                if (!modules.ContainsKey(consumer))
                {
                    Add(issues, "contract-consumer.unknown", $"Contract '{contract.Name}' references unknown consumer '{consumer}'.");
                    continue;
                }

                edges.Add(new ModuleEdge(consumer, contract.Provider));
            }
        }

        return edges;
    }

    private static void ValidateEdgeCoverage(
        IReadOnlySet<ModuleEdge> declaredEdges,
        IReadOnlyList<ModuleEdge> mappedEdges,
        ICollection<ValidationIssue> issues)
    {
        var mapped = mappedEdges.ToHashSet();
        foreach (var edge in declaredEdges.Except(mapped).OrderBy(edge => edge.Consumer, StringComparer.Ordinal).ThenBy(edge => edge.Provider, StringComparer.Ordinal))
        {
            Add(issues, "consumer-edge.missing", $"Dependency '{edge.Consumer}' -> '{edge.Provider}' has no public contract.");
        }

        foreach (var edge in mapped.Except(declaredEdges).OrderBy(edge => edge.Consumer, StringComparer.Ordinal).ThenBy(edge => edge.Provider, StringComparer.Ordinal))
        {
            Add(issues, "consumer-edge.undeclared", $"Consumer map edge '{edge.Consumer}' -> '{edge.Provider}' is not an allowed dependency.");
        }
    }

    private static void ValidateUniqueValues<T>(
        IEnumerable<T> items,
        Func<T, string> selector,
        string code,
        string label,
        ICollection<ValidationIssue> issues)
    {
        foreach (var group in items.GroupBy(selector, StringComparer.Ordinal).Where(group => group.Count() > 1))
        {
            Add(issues, code, $"The {label} '{group.Key}' is duplicated.");
        }
    }

    private static void Add(ICollection<ValidationIssue> issues, string code, string message) =>
        issues.Add(new ValidationIssue(code, message));

    private sealed record ModuleEdge(string Consumer, string Provider);

    private enum VisitState
    {
        Visiting,
        Visited,
    }
}
