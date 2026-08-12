using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace AgropecuarIA.ArchitectureFitness;

public sealed record AuthorizationSurfaceDocument(
    int SchemaVersion,
    string Scope,
    IReadOnlyList<AuthorizationOperation> Operations,
    IReadOnlyList<FrameworkEntrypoint> FrameworkEntrypoints,
    IReadOnlyList<AbsentSurface> AbsentSurfaces);

public sealed record AuthorizationOperation(
    string Id,
    string Method,
    string Path,
    string Boundary,
    string Resource,
    string Action,
    string Authentication,
    string ActorSource,
    string TenantSource,
    string ApplicationAuthorization,
    string StorageBoundary,
    string NeutralErrors,
    string Owner,
    IReadOnlyList<string> Tests);

public sealed record FrameworkEntrypoint(
    string Id,
    string Path,
    string Owner,
    string Control,
    IReadOnlyList<string> Tests);

public sealed record AbsentSurface(string Name, string Status);

public static partial class AuthorizationSurfaceLoader
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = false,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
    };

    public static AuthorizationSurfaceDocument Load(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        try
        {
            return JsonSerializer.Deserialize<AuthorizationSurfaceDocument>(
                    File.ReadAllText(path),
                    Options)
                ?? throw new InvalidDataException("Authorization surface register cannot be null.");
        }
        catch (JsonException exception)
        {
            throw new InvalidDataException("Authorization surface register is invalid.", exception);
        }
    }
}

public static partial class AuthorizationSurfaceValidator
{
    public const string RegisterRelativePath =
        "tasks/evidence/AGRO-SEC-002/authorization-surface-register.json";

    private static readonly HashSet<string> AllowedBoundaries =
    [
        "public-platform",
        "authenticated-platform",
        "tenant",
        "shared-reference",
        "development-test-only",
    ];

    private static readonly HashSet<string> ExpectedAbsentSurfaces =
        ["jobs", "storage", "export", "ai", "retrieval"];

    public static IReadOnlyList<ValidationIssue> Validate(
        AuthorizationSurfaceDocument document,
        string repositoryRoot)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentException.ThrowIfNullOrWhiteSpace(repositoryRoot);
        var issues = new List<ValidationIssue>();

        if (document.SchemaVersion != 1)
        {
            Add(issues, "authorization-register.schema-version.invalid", "Schema version must be 1.");
        }

        if (!string.Equals(document.Scope, "identity-territory-integrated-local", StringComparison.Ordinal))
        {
            Add(issues, "authorization-register.scope.invalid", "Scope must remain the bounded integrated-local Identity/Territory slice.");
        }

        ValidateOperations(document.Operations, repositoryRoot, issues);
        ValidateFrameworkEntrypoints(document.FrameworkEntrypoints, repositoryRoot, issues);
        ValidateAbsentSurfaces(document.AbsentSurfaces, issues);
        ValidateExactCoverage(document.Operations, repositoryRoot, issues);
        return issues;
    }

    private static void ValidateOperations(
        IReadOnlyList<AuthorizationOperation> operations,
        string repositoryRoot,
        ICollection<ValidationIssue> issues)
    {
        if (operations.Count == 0)
        {
            Add(issues, "authorization-register.operations.empty", "At least one operation is required.");
            return;
        }

        foreach (IGrouping<string, AuthorizationOperation> duplicate in operations
                     .GroupBy(OperationKey, StringComparer.Ordinal)
                     .Where(group => group.Count() > 1))
        {
            Add(issues, "authorization-register.operation.duplicate", $"Duplicate operation '{duplicate.Key}'.");
        }

        foreach (IGrouping<string, AuthorizationOperation> duplicate in operations
                     .GroupBy(operation => operation.Id, StringComparer.Ordinal)
                     .Where(group => group.Count() > 1))
        {
            Add(issues, "authorization-register.id.duplicate", $"Duplicate operation id '{duplicate.Key}'.");
        }

        foreach (AuthorizationOperation operation in operations)
        {
            string key = OperationKey(operation);
            RequireText(operation.Id, key, "id", issues);
            RequireText(operation.Resource, key, "resource", issues);
            RequireText(operation.Action, key, "action", issues);
            RequireText(operation.Authentication, key, "authentication", issues);
            RequireText(operation.ActorSource, key, "actorSource", issues);
            RequireText(operation.TenantSource, key, "tenantSource", issues);
            RequireText(operation.ApplicationAuthorization, key, "applicationAuthorization", issues);
            RequireText(operation.StorageBoundary, key, "storageBoundary", issues);
            RequireText(operation.NeutralErrors, key, "neutralErrors", issues);
            RequireText(operation.Owner, key, "owner", issues);

            if (!AllowedBoundaries.Contains(operation.Boundary))
            {
                Add(issues, "authorization-register.boundary.invalid", $"Operation '{key}' has unknown boundary '{operation.Boundary}'.");
            }

            if (operation.Tests.Count == 0)
            {
                Add(issues, "authorization-register.tests.missing", $"Operation '{key}' has no executable test evidence.");
            }
            else
            {
                ValidateReferences(operation.Tests, repositoryRoot, key, issues);
            }

            ValidateBoundaryInvariants(operation, key, issues);
        }
    }

    private static void ValidateBoundaryInvariants(
        AuthorizationOperation operation,
        string key,
        ICollection<ValidationIssue> issues)
    {
        if (operation.Boundary == "tenant")
        {
            if (operation.Authentication == "anonymous")
            {
                Add(issues, "authorization-register.tenant.authentication.missing", $"Tenant operation '{key}' must authenticate.");
            }

            if (!operation.TenantSource.StartsWith("server-derived", StringComparison.Ordinal))
            {
                Add(issues, "authorization-register.tenant.client-authoritative", $"Tenant operation '{key}' must derive and revalidate tenant server-side.");
            }

            if (!operation.StorageBoundary.StartsWith("force-rls:", StringComparison.Ordinal))
            {
                Add(issues, "authorization-register.tenant.rls.missing", $"Tenant operation '{key}' must declare FORCE RLS evidence.");
            }

            if (!ContainsAuthorizationEvidence(operation.ApplicationAuthorization))
            {
                Add(issues, "authorization-register.tenant.application-authorization.missing", $"Tenant operation '{key}' must declare application authorization.");
            }

            if (!ContainsNeutralEvidence(operation.NeutralErrors))
            {
                Add(issues, "authorization-register.tenant.neutral-error.missing", $"Tenant operation '{key}' must fail without an existence oracle.");
            }
        }

        if (operation.Boundary == "shared-reference")
        {
            if (operation.Authentication == "anonymous" || operation.TenantSource != "none")
            {
                Add(issues, "authorization-register.shared-reference.boundary.invalid", $"Shared reference operation '{key}' must authenticate without tenant context.");
            }

            if (!string.Equals(operation.Owner, "Territory", StringComparison.Ordinal))
            {
                Add(issues, "authorization-register.shared-reference.owner.invalid", $"Shared reference operation '{key}' must be owned by Territory.");
            }

            bool isSearch = operation.Action == "search";
            bool validStorage = isSearch
                ? operation.StorageBoundary.StartsWith("territory-readonly", StringComparison.Ordinal)
                : operation.StorageBoundary.Contains("no-tenant-data", StringComparison.Ordinal);
            bool validEgress = isSearch ||
                operation.ApplicationAuthorization.Contains("egress default-off", StringComparison.Ordinal);
            if (!validStorage || !validEgress)
            {
                Add(issues, "authorization-register.shared-reference.data-boundary.invalid", $"Shared reference operation '{key}' must remain read-only/no-tenant-data and resolution egress must be default-off.");
            }
        }

        if (operation.Boundary == "authenticated-platform" && operation.TenantSource != "none")
        {
            if (!operation.TenantSource.StartsWith("server-", StringComparison.Ordinal))
            {
                Add(issues, "authorization-register.platform-transition.client-authoritative", $"Platform transition '{key}' must create or resolve tenant server-side.");
            }

            if (!operation.StorageBoundary.StartsWith("force-rls:", StringComparison.Ordinal))
            {
                Add(issues, "authorization-register.platform-transition.rls.missing", $"Platform transition '{key}' must enter tenant storage through FORCE RLS.");
            }

            if (!operation.ApplicationAuthorization.Contains("actor", StringComparison.OrdinalIgnoreCase))
            {
                Add(issues, "authorization-register.platform-transition.authorization.missing", $"Platform transition '{key}' must reauthorize the actor before tenant lookup or creation.");
            }
        }

        if (operation.Boundary == "development-test-only")
        {
            if (!operation.ApplicationAuthorization.Contains("environment plus", StringComparison.Ordinal) ||
                !operation.NeutralErrors.Contains("absent outside Development/Test", StringComparison.Ordinal))
            {
                Add(issues, "authorization-register.development.boundary.invalid", $"Development operation '{key}' must be environment+flag gated and absent elsewhere.");
            }
        }

        if (operation.Boundary == "public-platform" && operation.TenantSource != "none")
        {
            Add(issues, "authorization-register.public.tenant.invalid", $"Public platform operation '{key}' cannot accept tenant authority.");
        }
    }

    private static void ValidateFrameworkEntrypoints(
        IReadOnlyList<FrameworkEntrypoint> entrypoints,
        string repositoryRoot,
        ICollection<ValidationIssue> issues)
    {
        FrameworkEntrypoint? callback = entrypoints.SingleOrDefault(entrypoint =>
            entrypoint.Id == "identity.oidc.callback" && entrypoint.Path == "/signin-oidc");
        if (callback is null)
        {
            Add(issues, "authorization-register.oidc-callback.missing", "The framework-owned OIDC callback must be registered.");
            return;
        }

        if (!callback.Control.Contains("state", StringComparison.Ordinal) ||
            !callback.Control.Contains("PKCE", StringComparison.Ordinal) ||
            !callback.Control.Contains("auth_time", StringComparison.Ordinal))
        {
            Add(issues, "authorization-register.oidc-callback.controls.incomplete", "OIDC callback controls must include state, PKCE and auth_time.");
        }

        ValidateReferences(callback.Tests, repositoryRoot, callback.Id, issues);
    }

    private static void ValidateAbsentSurfaces(
        IReadOnlyList<AbsentSurface> surfaces,
        ICollection<ValidationIssue> issues)
    {
        var actual = surfaces.Select(surface => surface.Name).ToHashSet(StringComparer.Ordinal);
        if (!actual.SetEquals(ExpectedAbsentSurfaces) ||
            surfaces.Any(surface => surface.Status != "not-present"))
        {
            Add(issues, "authorization-register.absent-surfaces.invalid", "Jobs, storage, export, AI and retrieval must remain explicitly not-present in this slice.");
        }
    }

    private static void ValidateExactCoverage(
        IReadOnlyList<AuthorizationOperation> operations,
        string repositoryRoot,
        ICollection<ValidationIssue> issues)
    {
        HashSet<string> registered = operations.Select(OperationKey).ToHashSet(StringComparer.Ordinal);
        HashSet<string> openApi = ExtractOpenApiOperations(
            Path.Combine(repositoryRoot, "contracts", "identity.openapi.yaml"));
        openApi.UnionWith(ExtractOpenApiOperations(
            Path.Combine(repositoryRoot, "contracts", "territory.openapi.yaml")));
        HashSet<string> runtime = ExtractRuntimeOperations(repositoryRoot, issues);

        AddSetDifference(registered, openApi, "authorization-register.openapi", issues);
        AddSetDifference(registered, runtime, "authorization-register.runtime", issues);
    }

    private static HashSet<string> ExtractOpenApiOperations(string path)
    {
        var operations = new HashSet<string>(StringComparer.Ordinal);
        string? currentPath = null;
        foreach (string rawLine in File.ReadLines(path))
        {
            if (rawLine.StartsWith("  /api/", StringComparison.Ordinal) && rawLine.EndsWith(':'))
            {
                currentPath = rawLine.Trim()[..^1];
                continue;
            }

            if (currentPath is not null && MethodLine().Match(rawLine) is { Success: true } match)
            {
                operations.Add($"{match.Groups[1].Value.ToUpperInvariant()} {currentPath}");
            }
        }

        return operations;
    }

    private static HashSet<string> ExtractRuntimeOperations(
        string repositoryRoot,
        ICollection<ValidationIssue> issues)
    {
        var operations = new HashSet<string>(StringComparer.Ordinal);
        foreach (string sourceRoot in new[] { "apps", "src" })
        {
            string fullSourceRoot = Path.Combine(repositoryRoot, sourceRoot);
            foreach (string sourcePath in Directory.EnumerateFiles(
                         fullSourceRoot,
                         "*.cs",
                         SearchOption.AllDirectories)
                     .Where(path => !IsBuildArtifact(path)))
            {
                ExtractMappedOperations(File.ReadAllText(sourcePath), operations, issues);
            }
        }

        return operations;
    }

    private static bool IsBuildArtifact(string path) =>
        path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase) ||
        path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase);

    private static void ExtractMappedOperations(
        string source,
        HashSet<string> operations,
        ICollection<ValidationIssue> issues)
    {
        var prefixes = RouteGroup().Matches(source)
            .ToDictionary(
                match => match.Groups["receiver"].Value,
                match => match.Groups["prefix"].Value,
                StringComparer.Ordinal);

        foreach (Match match in MappedEndpoint().Matches(source))
        {
            string receiver = match.Groups["receiver"].Value;
            string path = RouteConstraint().Replace(match.Groups["path"].Value, "}");
            string? prefix = prefixes.GetValueOrDefault(receiver);
            if (prefix is null && path.StartsWith("/api/", StringComparison.Ordinal))
            {
                prefix = string.Empty;
            }

            if (prefix is null)
            {
                continue;
            }

            operations.Add($"{match.Groups["method"].Value.ToUpperInvariant()} {prefix}{path}");
        }

        foreach (Match match in UnsupportedMappedEndpoint().Matches(source))
        {
            string receiver = match.Groups["receiver"].Value;
            string path = match.Groups["path"].Value;
            if (prefixes.ContainsKey(receiver) || path.StartsWith("/api/", StringComparison.Ordinal))
            {
                Add(issues, "authorization-register.runtime.mapping.unsupported", $"Route '{receiver}.MapMethods({path})' must use an explicitly extractable HTTP method.");
            }
        }
    }

    private static void ValidateReferences(
        IEnumerable<string> references,
        string repositoryRoot,
        string operation,
        ICollection<ValidationIssue> issues)
    {
        foreach (string reference in references)
        {
            string[] parts = reference.Split('#', 2);
            if (parts.Length != 2 || string.IsNullOrWhiteSpace(parts[0]) || string.IsNullOrWhiteSpace(parts[1]))
            {
                Add(issues, "authorization-register.test-reference.invalid", $"Operation '{operation}' has malformed test reference '{reference}'.");
                continue;
            }

            string fullPath = Path.GetFullPath(Path.Combine(repositoryRoot, parts[0]));
            string fullRoot = Path.GetFullPath(repositoryRoot) + Path.DirectorySeparatorChar;
            if (!fullPath.StartsWith(fullRoot, StringComparison.OrdinalIgnoreCase) || !File.Exists(fullPath))
            {
                Add(issues, "authorization-register.test-file.missing", $"Operation '{operation}' references missing test file '{parts[0]}'.");
                continue;
            }

            if (!File.ReadAllText(fullPath).Contains(parts[1], StringComparison.Ordinal))
            {
                Add(issues, "authorization-register.test-symbol.missing", $"Operation '{operation}' references missing test symbol '{parts[1]}'.");
            }
        }
    }

    private static void AddSetDifference(
        HashSet<string> registered,
        HashSet<string> actual,
        string prefix,
        ICollection<ValidationIssue> issues)
    {
        foreach (string missing in actual.Except(registered, StringComparer.Ordinal).Order())
        {
            Add(issues, $"{prefix}.operation.unregistered", $"Operation '{missing}' is not registered.");
        }

        foreach (string stale in registered.Except(actual, StringComparer.Ordinal).Order())
        {
            Add(issues, $"{prefix}.operation.stale", $"Registered operation '{stale}' does not exist.");
        }
    }

    private static string OperationKey(AuthorizationOperation operation) =>
        $"{operation.Method.ToUpperInvariant()} {operation.Path}";

    private static bool ContainsAuthorizationEvidence(string value) =>
        value.Contains("owner", StringComparison.OrdinalIgnoreCase) ||
        value.Contains("membership", StringComparison.OrdinalIgnoreCase);

    private static bool ContainsNeutralEvidence(string value) =>
        value.Contains("neutral", StringComparison.OrdinalIgnoreCase) ||
        value.Contains("without existence", StringComparison.OrdinalIgnoreCase);

    private static void RequireText(
        string value,
        string key,
        string property,
        ICollection<ValidationIssue> issues)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            Add(issues, "authorization-register.property.empty", $"Operation '{key}' has empty '{property}'.");
        }
    }

    private static void Add(ICollection<ValidationIssue> issues, string code, string message) =>
        issues.Add(new ValidationIssue(code, message));

    [GeneratedRegex("^    (get|post|put|patch|delete|options|head):\\s*$", RegexOptions.CultureInvariant)]
    private static partial Regex MethodLine();

    [GeneratedRegex("(?<receiver>[A-Za-z_][A-Za-z0-9_]*)\\.Map(?<method>Get|Post|Put|Patch|Delete|Options|Head)\\s*\\(\\s*\"(?<path>[^\"]+)\"", RegexOptions.CultureInvariant)]
    private static partial Regex MappedEndpoint();

    [GeneratedRegex("(?:RouteGroupBuilder|var)\\s+(?<receiver>[A-Za-z_][A-Za-z0-9_]*)\\s*=\\s*[A-Za-z_][A-Za-z0-9_]*\\.MapGroup\\(\"(?<prefix>/api/[^\"]*)\"\\)", RegexOptions.CultureInvariant)]
    private static partial Regex RouteGroup();

    [GeneratedRegex("(?<receiver>[A-Za-z_][A-Za-z0-9_]*)\\.MapMethods\\s*\\(\\s*\"(?<path>[^\"]+)\"", RegexOptions.CultureInvariant)]
    private static partial Regex UnsupportedMappedEndpoint();

    [GeneratedRegex(":guid}", RegexOptions.CultureInvariant)]
    private static partial Regex RouteConstraint();
}
