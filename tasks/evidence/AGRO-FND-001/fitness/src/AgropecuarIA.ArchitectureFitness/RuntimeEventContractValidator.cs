namespace AgropecuarIA.ArchitectureFitness;

public sealed record RuntimeEventContract(
    string Type,
    int MajorVersion,
    string SchemaVersion,
    string Source,
    string Scope,
    string AggregateType,
    string PayloadSchemaPath);

public static class RuntimeEventContractValidator
{
    public static IReadOnlyList<ValidationIssue> Validate(
        string moduleId,
        IReadOnlyList<RuntimeEventContract> runtimeEvents,
        ConsumerMapDocument consumerMap,
        RuntimeMapDocument runtimeMap,
        ModuleBoundaryDocument boundaries)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(moduleId);
        ArgumentNullException.ThrowIfNull(runtimeEvents);
        ArgumentNullException.ThrowIfNull(consumerMap);
        ArgumentNullException.ThrowIfNull(runtimeMap);
        ArgumentNullException.ThrowIfNull(boundaries);

        var issues = new List<ValidationIssue>();
        var publishedEvents = (consumerMap.Contracts ?? [])
            .Where(contract =>
                string.Equals(contract.Provider, moduleId, StringComparison.Ordinal)
                && string.Equals(contract.Interaction, "event", StringComparison.Ordinal))
            .ToArray();
        var runtimeModule = (runtimeMap.Modules ?? [])
            .FirstOrDefault(module => string.Equals(module.ModuleId, moduleId, StringComparison.Ordinal));
        var boundary = (boundaries.Modules ?? [])
            .FirstOrDefault(module => string.Equals(module.Id, moduleId, StringComparison.Ordinal));

        ValidateUnique(runtimeEvents.Select(item => item.Type), "runtime-event.type.duplicate", issues);
        ValidateUnique(publishedEvents.Select(item => item.Name), "runtime-event.contract.duplicate", issues);

        var runtimeByType = runtimeEvents
            .GroupBy(item => item.Type, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);
        var publishedByName = publishedEvents
            .GroupBy(item => item.Name, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);

        foreach (var runtimeEvent in runtimeByType.Values)
        {
            if (!publishedByName.TryGetValue(runtimeEvent.Type, out PublishedContract? published))
            {
                Add(issues, "runtime-event.unpublished", $"Runtime event '{runtimeEvent.Type}' is not in the reviewed consumer map.");
                continue;
            }

            if (!string.Equals(runtimeEvent.Source, moduleId, StringComparison.Ordinal))
            {
                Add(issues, "runtime-event.source.mismatch", $"Runtime event '{runtimeEvent.Type}' source must be '{moduleId}'.");
            }

            if (!string.Equals(runtimeEvent.Scope, published.Scope, StringComparison.Ordinal))
            {
                Add(issues, "runtime-event.scope.mismatch", $"Runtime event '{runtimeEvent.Type}' scope differs from the reviewed contract.");
            }

            if (boundary is null
                || !(boundary.OwnedAggregates ?? []).Contains(runtimeEvent.AggregateType, StringComparer.Ordinal))
            {
                Add(issues, "runtime-event.aggregate.unowned", $"Runtime event '{runtimeEvent.Type}' uses an aggregate not owned by '{moduleId}'.");
            }
            else if (boundary.AggregateScopes is null
                || !boundary.AggregateScopes.TryGetValue(runtimeEvent.AggregateType, out string? aggregateScope)
                || !string.Equals(aggregateScope, runtimeEvent.Scope, StringComparison.Ordinal))
            {
                Add(issues, "runtime-event.aggregate.scope-mismatch", $"Runtime event '{runtimeEvent.Type}' scope differs from its aggregate scope.");
            }

            string supportedMajor = $"{runtimeEvent.MajorVersion}.x";
            if (runtimeEvent.MajorVersion <= 0
                || !(published.SupportedVersions ?? []).Contains(supportedMajor, StringComparer.Ordinal)
                || !Version.TryParse(runtimeEvent.SchemaVersion, out Version? schemaVersion)
                || schemaVersion.Major != runtimeEvent.MajorVersion)
            {
                Add(issues, "runtime-event.version.unsupported", $"Runtime event '{runtimeEvent.Type}' version is outside the reviewed compatibility family.");
            }

            RuntimeContract? registeredSchema = (runtimeModule?.Contracts ?? [])
                .FirstOrDefault(contract => string.Equals(
                    NormalizePath(contract.Path),
                    NormalizePath(runtimeEvent.PayloadSchemaPath),
                    StringComparison.OrdinalIgnoreCase));
            if (registeredSchema is null)
            {
                Add(issues, "runtime-event.schema.missing", $"Runtime event '{runtimeEvent.Type}' payload schema is not registered in runtime-map.json.");
            }
            else if (!string.Equals(registeredSchema.Version, runtimeEvent.SchemaVersion, StringComparison.Ordinal))
            {
                Add(issues, "runtime-event.schema.version-mismatch", $"Runtime event '{runtimeEvent.Type}' payload schema version differs from runtime-map.json.");
            }
        }

        foreach (var orphan in publishedByName.Keys.Except(runtimeByType.Keys, StringComparer.Ordinal))
        {
            Add(issues, "runtime-event.contract.unimplemented", $"Reviewed event '{orphan}' is not published by the runtime catalog.");
        }

        return issues
            .OrderBy(issue => issue.Code, StringComparer.Ordinal)
            .ThenBy(issue => issue.Message, StringComparer.Ordinal)
            .ToArray();
    }

    private static void ValidateUnique(
        IEnumerable<string> values,
        string code,
        ICollection<ValidationIssue> issues)
    {
        foreach (var value in values
            .GroupBy(item => item, StringComparer.Ordinal)
            .Where(group => string.IsNullOrWhiteSpace(group.Key) || group.Count() > 1)
            .Select(group => group.Key))
        {
            Add(issues, code, $"Runtime event contract '{value}' is missing or duplicated.");
        }
    }

    private static string NormalizePath(string path) =>
        (path ?? string.Empty).Replace('\\', '/').TrimStart('/');

    private static void Add(ICollection<ValidationIssue> issues, string code, string message) =>
        issues.Add(new ValidationIssue(code, message));
}
