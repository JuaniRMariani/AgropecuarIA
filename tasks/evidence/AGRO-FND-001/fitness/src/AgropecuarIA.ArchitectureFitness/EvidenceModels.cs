namespace AgropecuarIA.ArchitectureFitness;

public sealed record ModuleBoundaryDocument(
    string Version,
    string DependencyDirection,
    IReadOnlyList<string> SharedKernel,
    IReadOnlyList<ModuleBoundary> Modules);

public sealed record ModuleBoundary(
    string Id,
    string Owner,
    string DataScope,
    string DatabaseSchema,
    IReadOnlyList<string> OwnedAggregates,
    IReadOnlyDictionary<string, string> AggregateScopes,
    IReadOnlyList<string> AllowedDependencies);

public sealed record ConsumerMapDocument(
    string Version,
    string CompatibilityWindow,
    ConsumerMapPolicies DefaultPolicies,
    IReadOnlyList<PublishedContract> Contracts);

public sealed record ConsumerMapPolicies(
    string Authorization,
    string Consistency,
    string Rollback,
    string Telemetry);

public sealed record PublishedContract(
    string Provider,
    IReadOnlyList<string> Consumers,
    string Name,
    string Interaction,
    string Scope,
    IReadOnlyList<string> SupportedVersions);

public sealed record ContractSnapshot(
    string Name,
    string Version,
    bool ToleratesUnknownFields,
    IReadOnlyList<ContractField> Fields);

public sealed record ContractField(
    string Name,
    string Type,
    bool Required,
    bool ExtensibleEnum = false,
    IReadOnlyList<string>? EnumValues = null);

public sealed record ValidationIssue(string Code, string Message);
