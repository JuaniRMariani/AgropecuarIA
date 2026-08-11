using System.Text.Json;
using System.Text.Json.Serialization;
using System.Xml.Linq;

namespace AgropecuarIA.ArchitectureFitness;

public sealed record RuntimeMapDocument(
    int SchemaVersion,
    IReadOnlyList<RuntimeModule> Modules,
    IReadOnlyList<CompositionRoot> CompositionRoots);

public sealed record RuntimeModule(
    string ModuleId,
    string ProjectPath,
    string DatabaseSchema,
    IReadOnlyList<RuntimeContract> Contracts);

public sealed record RuntimeContract(string Path, string Version);

public sealed record CompositionRoot(string ProjectPath, IReadOnlyList<string> AllowedModuleIds);

public static class RuntimeMapLoader
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
    };

    public static RuntimeMapDocument Load(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        try
        {
            using var stream = File.OpenRead(path);
            return JsonSerializer.Deserialize<RuntimeMapDocument>(stream, SerializerOptions)
                ?? throw new InvalidDataException($"Runtime map '{path}' contains JSON null.");
        }
        catch (JsonException exception)
        {
            throw new InvalidDataException($"Runtime map '{path}' is invalid.", exception);
        }
    }
}

public static class RepositoryLocator
{
    public static string FindRoot(string startPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(startPath);

        var directory = new DirectoryInfo(Path.GetFullPath(startPath));
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, RuntimeArchitectureValidator.RootSolutionName)))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException(
            $"Could not find {RuntimeArchitectureValidator.RootSolutionName} from '{startPath}'.");
    }
}

public static class RuntimeArchitectureValidator
{
    public const string RootSolutionName = "AgropecuarIA.slnx";

    public const string FitnessTestProjectPath =
        "tasks/evidence/AGRO-FND-001/fitness/tests/AgropecuarIA.ArchitectureFitness.Tests/AgropecuarIA.ArchitectureFitness.Tests.csproj";

    public static IReadOnlyList<ValidationIssue> Validate(
        RuntimeMapDocument runtimeMap,
        ModuleBoundaryDocument boundaries,
        string repositoryRoot)
    {
        ArgumentNullException.ThrowIfNull(runtimeMap);
        ArgumentNullException.ThrowIfNull(boundaries);
        ArgumentException.ThrowIfNullOrWhiteSpace(repositoryRoot);

        var issues = new List<ValidationIssue>();
        string root = Path.GetFullPath(repositoryRoot);
        var modules = runtimeMap.Modules ?? [];
        var compositionRoots = runtimeMap.CompositionRoots ?? [];

        if (runtimeMap.SchemaVersion != 1)
        {
            Add(issues, "runtime-map.schema-version.invalid", "Runtime map schemaVersion must be 1.");
        }

        ValidateUnique(
            modules,
            module => module.ModuleId,
            "runtime-module.id.duplicate",
            "runtime module ID",
            issues);
        ValidateUnique(
            modules,
            module => NormalizeRelativePath(module.ProjectPath),
            "runtime-module.project.duplicate",
            "runtime module project",
            issues);
        ValidateUnique(
            compositionRoots,
            item => NormalizeRelativePath(item.ProjectPath),
            "composition-root.project.duplicate",
            "composition root project",
            issues);

        var boundariesById = (boundaries.Modules ?? [])
            .GroupBy(module => module.Id, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);
        var runtimeModulesByProject = new Dictionary<string, RuntimeModule>(StringComparer.OrdinalIgnoreCase);

        foreach (var module in modules)
        {
            ValidateRuntimeModule(module, boundariesById, root, issues);
            if (TryResolveRepositoryPath(root, module.ProjectPath, out string? projectPath))
            {
                runtimeModulesByProject.TryAdd(projectPath, module);
            }
        }

        foreach (var compositionRoot in compositionRoots)
        {
            ValidateCompositionRoot(compositionRoot, boundariesById, root, issues);
        }

        ValidateRootSolution(runtimeMap, root, issues);
        ValidateProjectReferences(modules, compositionRoots, boundariesById, runtimeModulesByProject, root, issues);

        return issues
            .OrderBy(issue => issue.Code, StringComparer.Ordinal)
            .ThenBy(issue => issue.Message, StringComparer.Ordinal)
            .ToArray();
    }

    private static void ValidateRuntimeModule(
        RuntimeModule module,
        Dictionary<string, ModuleBoundary> boundaries,
        string root,
        ICollection<ValidationIssue> issues)
    {
        if (string.IsNullOrWhiteSpace(module.ModuleId) || !boundaries.TryGetValue(module.ModuleId, out ModuleBoundary? boundary))
        {
            Add(issues, "runtime-module.id.unknown", $"Runtime module '{module.ModuleId}' is not declared in module boundaries.");
        }
        else if (!string.Equals(module.DatabaseSchema, boundary.DatabaseSchema, StringComparison.Ordinal))
        {
            Add(
                issues,
                "runtime-module.schema.mismatch",
                $"Runtime module '{module.ModuleId}' uses schema '{module.DatabaseSchema}' instead of '{boundary.DatabaseSchema}'.");
        }

        ValidateProjectPath(module.ProjectPath, "runtime-module.project", root, issues);

        var contracts = module.Contracts ?? [];
        ValidateUnique(
            contracts,
            contract => NormalizeRelativePath(contract.Path),
            "runtime-contract.path.duplicate",
            $"runtime contract path for '{module.ModuleId}'",
            issues);
        foreach (var contract in contracts)
        {
            ValidateFilePath(contract.Path, "runtime-contract.path", root, issues);
            if (!Version.TryParse(contract.Version, out Version? version) || version.Major <= 0)
            {
                Add(
                    issues,
                    "runtime-contract.version.invalid",
                    $"Contract '{contract.Path}' must declare a positive semantic version.");
            }
        }
    }

    private static void ValidateCompositionRoot(
        CompositionRoot compositionRoot,
        Dictionary<string, ModuleBoundary> boundaries,
        string root,
        ICollection<ValidationIssue> issues)
    {
        ValidateProjectPath(compositionRoot.ProjectPath, "composition-root.project", root, issues);
        var allowedModuleIds = compositionRoot.AllowedModuleIds ?? [];
        if (allowedModuleIds.Count == 0)
        {
            Add(
                issues,
                "composition-root.modules.missing",
                $"Composition root '{compositionRoot.ProjectPath}' must allow at least one module.");
        }

        foreach (string moduleId in allowedModuleIds)
        {
            if (!boundaries.ContainsKey(moduleId))
            {
                Add(
                    issues,
                    "composition-root.module.unknown",
                    $"Composition root '{compositionRoot.ProjectPath}' allows unknown module '{moduleId}'.");
            }
        }

        if (allowedModuleIds.Count != allowedModuleIds.Distinct(StringComparer.Ordinal).Count())
        {
            Add(
                issues,
                "composition-root.module.duplicate",
                $"Composition root '{compositionRoot.ProjectPath}' repeats an allowed module.");
        }
    }

    private static void ValidateRootSolution(
        RuntimeMapDocument runtimeMap,
        string root,
        ICollection<ValidationIssue> issues)
    {
        string solutionPath = Path.Combine(root, RootSolutionName);
        if (!File.Exists(solutionPath))
        {
            Add(issues, "root-solution.missing", $"Root solution '{RootSolutionName}' does not exist.");
            return;
        }

        XDocument solution;
        try
        {
            solution = XDocument.Load(solutionPath, LoadOptions.None);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or System.Xml.XmlException)
        {
            Add(issues, "root-solution.invalid", $"Root solution cannot be read: {exception.Message}");
            return;
        }

        var solutionProjects = solution
            .Descendants("Project")
            .Select(element => NormalizeRelativePath((string?)element.Attribute("Path") ?? string.Empty))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var registeredModuleProjects = (runtimeMap.Modules ?? [])
            .Select(module => NormalizeRelativePath(module.ProjectPath))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (string sourceProject in solutionProjects.Where(IsProductModuleProject))
        {
            if (!registeredModuleProjects.Contains(sourceProject))
            {
                Add(
                    issues,
                    "root-solution.module.unregistered",
                    $"Product module project '{sourceProject}' is not registered in runtime-map.json.");
            }
        }

        IEnumerable<string> requiredProjects = (runtimeMap.Modules ?? [])
            .Select(module => module.ProjectPath)
            .Concat((runtimeMap.CompositionRoots ?? []).Select(compositionRoot => compositionRoot.ProjectPath))
            .Append(FitnessTestProjectPath);

        foreach (string project in requiredProjects)
        {
            string normalized = NormalizeRelativePath(project);
            if (!solutionProjects.Contains(normalized))
            {
                Add(issues, "root-solution.project.missing", $"Root solution does not include '{normalized}'.");
            }
        }
    }

    private static void ValidateProjectReferences(
        IReadOnlyList<RuntimeModule> modules,
        IReadOnlyList<CompositionRoot> compositionRoots,
        Dictionary<string, ModuleBoundary> boundaries,
        Dictionary<string, RuntimeModule> runtimeModulesByProject,
        string root,
        ICollection<ValidationIssue> issues)
    {
        foreach (var module in modules)
        {
            if (!TryResolveRepositoryPath(root, module.ProjectPath, out string? projectPath)
                || !File.Exists(projectPath)
                || !boundaries.TryGetValue(module.ModuleId, out ModuleBoundary? boundary))
            {
                continue;
            }

            foreach (string reference in ReadProjectReferences(projectPath, issues))
            {
                if (!runtimeModulesByProject.TryGetValue(reference, out RuntimeModule? dependency))
                {
                    continue;
                }

                if (!(boundary.AllowedDependencies ?? []).Contains(dependency.ModuleId, StringComparer.Ordinal))
                {
                    Add(
                        issues,
                        "runtime-module.reference.forbidden",
                        $"Module '{module.ModuleId}' references forbidden module '{dependency.ModuleId}'.");
                }
            }
        }

        foreach (var compositionRoot in compositionRoots)
        {
            if (!TryResolveRepositoryPath(root, compositionRoot.ProjectPath, out string? projectPath)
                || !File.Exists(projectPath))
            {
                continue;
            }

            var allowed = (compositionRoot.AllowedModuleIds ?? []).ToHashSet(StringComparer.Ordinal);
            foreach (string reference in ReadProjectReferences(projectPath, issues))
            {
                if (runtimeModulesByProject.TryGetValue(reference, out RuntimeModule? module)
                    && !allowed.Contains(module.ModuleId))
                {
                    Add(
                        issues,
                        "composition-root.reference.forbidden",
                        $"Composition root '{compositionRoot.ProjectPath}' references forbidden module '{module.ModuleId}'.");
                }
            }
        }
    }

    private static string[] ReadProjectReferences(
        string projectPath,
        ICollection<ValidationIssue> issues)
    {
        try
        {
            XDocument project = XDocument.Load(projectPath, LoadOptions.None);
            string projectDirectory = Path.GetDirectoryName(projectPath)!;
            return project
                .Descendants("ProjectReference")
                .Select(element => (string?)element.Attribute("Include"))
                .Where(include => !string.IsNullOrWhiteSpace(include))
                .Select(include => Path.GetFullPath(include!, projectDirectory))
                .ToArray();
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or System.Xml.XmlException)
        {
            Add(issues, "runtime-project.invalid", $"Project '{projectPath}' cannot be read: {exception.Message}");
            return [];
        }
    }

    private static void ValidateProjectPath(
        string path,
        string issuePrefix,
        string root,
        ICollection<ValidationIssue> issues)
    {
        if (!TryResolveRepositoryPath(root, path, out string? resolved))
        {
            Add(issues, $"{issuePrefix}.invalid", $"Path '{path}' must be relative and remain inside the repository.");
            return;
        }

        if (!File.Exists(resolved) || !string.Equals(Path.GetExtension(resolved), ".csproj", StringComparison.OrdinalIgnoreCase))
        {
            Add(issues, $"{issuePrefix}.missing", $"Project '{path}' does not exist.");
        }
    }

    private static void ValidateFilePath(
        string path,
        string issuePrefix,
        string root,
        ICollection<ValidationIssue> issues)
    {
        if (!TryResolveRepositoryPath(root, path, out string? resolved))
        {
            Add(issues, $"{issuePrefix}.invalid", $"Path '{path}' must be relative and remain inside the repository.");
            return;
        }

        if (!File.Exists(resolved))
        {
            Add(issues, $"{issuePrefix}.missing", $"File '{path}' does not exist.");
        }
    }

    private static bool TryResolveRepositoryPath(string root, string path, out string resolved)
    {
        resolved = string.Empty;
        if (string.IsNullOrWhiteSpace(path) || Path.IsPathFullyQualified(path))
        {
            return false;
        }

        string fullRoot = Path.GetFullPath(root);
        string candidate = Path.GetFullPath(path.Replace('/', Path.DirectorySeparatorChar), fullRoot);
        string rootPrefix = fullRoot.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            + Path.DirectorySeparatorChar;
        if (!candidate.StartsWith(rootPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        resolved = candidate;
        return true;
    }

    private static string NormalizeRelativePath(string path) =>
        (path ?? string.Empty).Replace('\\', '/').TrimStart('/');

    private static bool IsProductModuleProject(string path) =>
        path.StartsWith("src/", StringComparison.OrdinalIgnoreCase)
        && path.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase);

    private static void ValidateUnique<T>(
        IEnumerable<T> items,
        Func<T, string> selector,
        string code,
        string label,
        ICollection<ValidationIssue> issues)
    {
        foreach (var group in items
            .GroupBy(selector, StringComparer.OrdinalIgnoreCase)
            .Where(group => string.IsNullOrWhiteSpace(group.Key) || group.Count() > 1))
        {
            Add(issues, code, $"The {label} '{group.Key}' is missing or duplicated.");
        }
    }

    private static void Add(ICollection<ValidationIssue> issues, string code, string message) =>
        issues.Add(new ValidationIssue(code, message));
}

public static class IdentityOpenApiContractGuard
{
    public static IReadOnlyList<ValidationIssue> Validate(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        string text = File.ReadAllText(path);
        var issues = new List<ValidationIssue>();

        if (!HasSupportedMajorVersion(text))
        {
            issues.Add(new ValidationIssue(
                "identity-openapi.version.invalid",
                "Identity OpenAPI version must be a semantic 1.x version."));
        }

        string problemSection = ExtractIndentedSection(text, "    Problem:", 4);
        if (!problemSection.SplitLines().Any(line => line.Trim() == "additionalProperties: false"))
        {
            issues.Add(new ValidationIssue(
                "identity-openapi.problem.open",
                "Problem schema must set additionalProperties to false."));
        }

        string rateLimitedSection = ExtractIndentedSection(text, "    RateLimited:", 4);
        if (!rateLimitedSection.SplitLines().Any(line => line.Trim() == "application/problem+json:"))
        {
            issues.Add(new ValidationIssue(
                "identity-openapi.rate-limit.media-type.invalid",
                "RateLimited response must use application/problem+json."));
        }

        string developmentFixtureSection = ExtractIndentedSection(text, "    DevelopmentFixture:", 4);
        string[] syntheticProfiles =
        [
            "email-owner-1",
            "email-owner-2",
            "email-owner-3",
            "email-owner-4",
            "google-owner-1",
            "google-owner-2",
            "google-owner-3",
            "google-owner-4",
        ];
        if (syntheticProfiles.Any(profile =>
                !developmentFixtureSection.SplitLines().Any(line =>
                    string.Equals(line.Trim(), $"- {profile}", StringComparison.Ordinal))))
        {
            issues.Add(new ValidationIssue(
                "identity-openapi.development-fixtures.incomplete",
                "DevelopmentFixture must enumerate every bounded synthetic identity profile."));
        }

        return issues;
    }

    private static bool HasSupportedMajorVersion(string text)
    {
        int start = text.IndexOf("info:", StringComparison.Ordinal);
        string? versionLine = start < 0
            ? null
            : text[start..].SplitLines().Take(8)
                .SingleOrDefault(line => line.StartsWith("  version: ", StringComparison.Ordinal));
        return versionLine is not null &&
            Version.TryParse(versionLine["  version: ".Length..], out Version? version) &&
            version.Major == 1;
    }

    private static string ExtractIndentedSection(string text, string header, int indentation)
    {
        string[] lines = text.SplitLines();
        int start = Array.FindIndex(lines, line => string.Equals(line, header, StringComparison.Ordinal));
        if (start < 0)
        {
            return string.Empty;
        }

        int end = start + 1;
        while (end < lines.Length)
        {
            string line = lines[end];
            if (!string.IsNullOrWhiteSpace(line) && CountLeadingSpaces(line) <= indentation)
            {
                break;
            }

            end++;
        }

        return string.Join(Environment.NewLine, lines[start..end]);
    }

    private static int CountLeadingSpaces(string value) => value.TakeWhile(character => character == ' ').Count();

    private static string[] SplitLines(this string value) =>
        value.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');
}
