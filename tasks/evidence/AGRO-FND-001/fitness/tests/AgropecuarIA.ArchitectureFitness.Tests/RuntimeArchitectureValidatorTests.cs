using System.Text.Json;
using AgropecuarIA.ArchitectureFitness;

namespace AgropecuarIA.ArchitectureFitness.Tests;

[TestClass]
public sealed class RuntimeArchitectureValidatorTests
{
    [TestMethod]
    public void PublishedRuntimeMapMatchesRepositoryProjectsAndAllowedDependencies()
    {
        var issues = RuntimeArchitectureValidator.Validate(
            EvidenceFixture.RuntimeMap(),
            EvidenceFixture.Boundaries(),
            EvidenceFixture.RepositoryRoot());

        Assert.HasCount(0, issues, string.Join(Environment.NewLine, issues));
    }

    [TestMethod]
    public void LoaderRejectsUnknownProperties()
    {
        const string json = """
            {
              "schemaVersion": 1,
              "modules": [],
              "compositionRoots": [],
              "unexpected": true
            }
            """;

        string path = WriteTemporaryRuntimeMap(json);
        try
        {
            _ = Assert.ThrowsExactly<InvalidDataException>(() => RuntimeMapLoader.Load(path));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [TestMethod]
    public void InvalidSchemaVersionDuplicateProjectsAndEscapingPathsAreRejected()
    {
        RuntimeMapDocument published = EvidenceFixture.RuntimeMap();
        RuntimeModule identity = published.Modules.Single(module =>
            module.ModuleId == "identity-tenancy");
        var invalid = published with
        {
            SchemaVersion = 2,
            Modules =
            [
                identity,
                identity with { ModuleId = "duplicate-project", Contracts = [] },
            ],
            CompositionRoots =
            [
                new CompositionRoot("../outside.csproj", ["unknown-module"]),
            ],
        };

        var issues = RuntimeArchitectureValidator.Validate(
            invalid,
            EvidenceFixture.Boundaries(),
            EvidenceFixture.RepositoryRoot());

        Assert.IsTrue(issues.Any(issue => issue.Code == "runtime-map.schema-version.invalid"));
        Assert.IsTrue(issues.Any(issue => issue.Code == "runtime-module.project.duplicate"));
        Assert.IsTrue(issues.Any(issue => issue.Code == "composition-root.project.invalid"));
        Assert.IsTrue(issues.Any(issue => issue.Code == "composition-root.module.unknown"));
    }

    [TestMethod]
    public void ModuleSchemaAndContractVersionMustMatchReviewedBoundary()
    {
        RuntimeMapDocument published = EvidenceFixture.RuntimeMap();
        RuntimeModule identity = published.Modules.Single(module =>
            module.ModuleId == "identity-tenancy");
        var invalid = published with
        {
            Modules = published.Modules
                .Select(module => module.ModuleId == identity.ModuleId
                    ? identity with
                    {
                        DatabaseSchema = "public",
                        Contracts = [new RuntimeContract("contracts/identity.openapi.yaml", "draft")],
                    }
                    : module)
                .ToArray(),
        };

        var issues = RuntimeArchitectureValidator.Validate(
            invalid,
            EvidenceFixture.Boundaries(),
            EvidenceFixture.RepositoryRoot());

        Assert.IsTrue(issues.Any(issue => issue.Code == "runtime-module.schema.mismatch"));
        Assert.IsTrue(issues.Any(issue => issue.Code == "runtime-contract.version.invalid"));
    }

    [TestMethod]
    public void ProductProjectInRootSolutionCannotEscapeRuntimeRegistration()
    {
        RuntimeMapDocument published = EvidenceFixture.RuntimeMap();
        var invalid = published with { Modules = [] };

        var issues = RuntimeArchitectureValidator.Validate(
            invalid,
            EvidenceFixture.Boundaries(),
            EvidenceFixture.RepositoryRoot());

        Assert.IsTrue(issues.Any(issue => issue.Code == "root-solution.module.unregistered"));
    }

    [TestMethod]
    public void ModuleProjectReferenceMustBeAllowedByReviewedBoundaries()
    {
        string repositoryRoot = Path.Combine(
            Path.GetTempPath(),
            $"agro-runtime-fitness-{Guid.NewGuid():N}");
        Directory.CreateDirectory(Path.Combine(repositoryRoot, "src", "Identity"));
        Directory.CreateDirectory(Path.Combine(repositoryRoot, "src", "Territory"));

        try
        {
            File.WriteAllText(
                Path.Combine(repositoryRoot, RuntimeArchitectureValidator.RootSolutionName),
                """
                <Solution>
                  <Project Path="src/Identity/Identity.csproj" />
                  <Project Path="src/Territory/Territory.csproj" />
                </Solution>
                """);
            File.WriteAllText(
                Path.Combine(repositoryRoot, "src", "Identity", "Identity.csproj"),
                """
                <Project Sdk="Microsoft.NET.Sdk">
                  <ItemGroup>
                    <ProjectReference Include="../Territory/Territory.csproj" />
                  </ItemGroup>
                </Project>
                """);
            File.WriteAllText(
                Path.Combine(repositoryRoot, "src", "Territory", "Territory.csproj"),
                "<Project Sdk=\"Microsoft.NET.Sdk\" />");

            var runtimeMap = new RuntimeMapDocument(
                1,
                [
                    new RuntimeModule("identity-tenancy", "src/Identity/Identity.csproj", "identity", []),
                    new RuntimeModule("territory", "src/Territory/Territory.csproj", "territory", []),
                ],
                []);

            var issues = RuntimeArchitectureValidator.Validate(
                runtimeMap,
                EvidenceFixture.Boundaries(),
                repositoryRoot);

            Assert.IsTrue(issues.Any(issue => issue.Code == "runtime-module.reference.forbidden"));
        }
        finally
        {
            Directory.Delete(repositoryRoot, recursive: true);
        }
    }

    private static string WriteTemporaryRuntimeMap(string json)
    {
        string path = Path.Combine(Path.GetTempPath(), $"agro-runtime-map-{Guid.NewGuid():N}.json");
        File.WriteAllText(path, json);
        return path;
    }
}
