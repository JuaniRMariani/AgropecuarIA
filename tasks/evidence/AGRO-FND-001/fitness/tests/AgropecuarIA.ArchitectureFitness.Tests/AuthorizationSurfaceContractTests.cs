using AgropecuarIA.ArchitectureFitness;

namespace AgropecuarIA.ArchitectureFitness.Tests;

[TestClass]
public sealed class AuthorizationSurfaceContractTests
{
    [TestMethod]
    public void PublishedRegisterMatchesOpenApiRuntimeAndExecutableEvidence()
    {
        var issues = Validate(Published());

        Assert.HasCount(0, issues, string.Join(Environment.NewLine, issues));
    }

    [TestMethod]
    public void MissingRuntimeOperationCannotEscapeTheRegister()
    {
        AuthorizationSurfaceDocument published = Published();
        AuthorizationOperation removed = published.Operations.Single(operation =>
            operation.Id == "identity.owner-invitation.revoke");
        var invalid = published with
        {
            Operations = published.Operations.Where(operation => operation != removed).ToArray(),
        };

        AssertIssue(invalid, "authorization-register.openapi.operation.unregistered");
        AssertIssue(invalid, "authorization-register.runtime.operation.unregistered");
    }

    [TestMethod]
    public void NewPutOnANewApiGroupCannotEscapeOpenApiAndRuntimeExtraction()
    {
        string temporaryRoot = Path.Combine(Path.GetTempPath(), $"agro-auth-surface-{Guid.NewGuid():N}");
        try
        {
            CopyRuntimeEvidence(temporaryRoot);
            string newEndpointSource = Path.Combine(temporaryRoot, "src", "NewFeature", "Endpoints.cs");
            Directory.CreateDirectory(Path.GetDirectoryName(newEndpointSource)!);
            File.WriteAllText(
                newEndpointSource,
                "var admin = endpoints.MapGroup(\"/api/admin\");\nadmin.MapPut(\"/thing\", () => 204);\n");
            string identityOpenApi = Path.Combine(temporaryRoot, "contracts", "identity.openapi.yaml");
            File.AppendAllText(identityOpenApi, "\n  /api/admin/thing:\n    put:\n      responses: {}\n");

            var issues = AuthorizationSurfaceValidator.Validate(Published(), temporaryRoot);

            Assert.IsTrue(issues.Any(issue => issue.Code == "authorization-register.openapi.operation.unregistered"));
            Assert.IsTrue(issues.Any(issue => issue.Code == "authorization-register.runtime.operation.unregistered"));
        }
        finally
        {
            Directory.Delete(temporaryRoot, recursive: true);
        }
    }

    [TestMethod]
    public void TenantBoundaryCannotBecomeClientAuthoritative()
    {
        AuthorizationSurfaceDocument invalid = ReplaceOperation(
            "identity.owner-invitation.list",
            operation => operation with { TenantSource = "client-route-authoritative" });

        AssertIssue(invalid, "authorization-register.tenant.client-authoritative");
    }

    [TestMethod]
    public void UnknownBoundaryCannotDisguiseTenantScope()
    {
        AuthorizationSurfaceDocument invalid = ReplaceOperation(
            "identity.owner-invitation.list",
            operation => operation with { Boundary = "tenant-ish" });

        AssertIssue(invalid, "authorization-register.boundary.invalid");
    }

    [TestMethod]
    public void TenantBoundaryRequiresApplicationAuthorizationAndForcedRls()
    {
        AuthorizationSurfaceDocument invalid = ReplaceOperation(
            "identity.owner-invitation.create",
            operation => operation with
            {
                ApplicationAuthorization = "none",
                StorageBoundary = "plain-table",
            });

        AssertIssue(invalid, "authorization-register.tenant.application-authorization.missing");
        AssertIssue(invalid, "authorization-register.tenant.rls.missing");
    }

    [TestMethod]
    public void PlatformToTenantTransitionsRequireServerAuthorityRlsAndActorReauthorization()
    {
        AuthorizationSurfaceDocument createInvalid = ReplaceOperation(
            "identity.organization.create",
            operation => operation with
            {
                TenantSource = "client-authoritative",
                StorageBoundary = "plain-table",
                ApplicationAuthorization = "lookup first",
            });
        AuthorizationSurfaceDocument acceptInvalid = ReplaceOperation(
            "identity.owner-invitation.accept",
            operation => operation with
            {
                TenantSource = "client-authoritative",
                StorageBoundary = "plain-table",
                ApplicationAuthorization = "lookup first",
            });

        foreach (AuthorizationSurfaceDocument invalid in new[] { createInvalid, acceptInvalid })
        {
            AssertIssue(invalid, "authorization-register.platform-transition.client-authoritative");
            AssertIssue(invalid, "authorization-register.platform-transition.rls.missing");
            AssertIssue(invalid, "authorization-register.platform-transition.authorization.missing");
        }
    }

    [TestMethod]
    public void TenantBoundaryRequiresNeutralCrossTenantErrors()
    {
        AuthorizationSurfaceDocument invalid = ReplaceOperation(
            "identity.owner-invitation.revoke",
            operation => operation with { NeutralErrors = "returns foreign resource details" });

        AssertIssue(invalid, "authorization-register.tenant.neutral-error.missing");
    }

    [TestMethod]
    public void SharedReferenceCannotAcquireTenantAuthorityOrBecomeAnonymous()
    {
        AuthorizationSurfaceDocument invalid = ReplaceOperation(
            "territory.resolve",
            operation => operation with
            {
                Authentication = "anonymous",
                TenantSource = "client-tenant",
            });

        AssertIssue(invalid, "authorization-register.shared-reference.boundary.invalid");
    }

    [TestMethod]
    public void SharedResolutionMustRemainNoTenantDataAndEgressDefaultOff()
    {
        AuthorizationSurfaceDocument invalid = ReplaceOperation(
            "territory.resolve",
            operation => operation with
            {
                ApplicationAuthorization = "authenticated platform capability",
                StorageBoundary = "tenant-coordinate-history",
            });

        AssertIssue(invalid, "authorization-register.shared-reference.data-boundary.invalid");
    }

    [TestMethod]
    public void DevelopmentEndpointMustRemainEnvironmentFlagGatedAndProductionAbsent()
    {
        AuthorizationSurfaceDocument invalid = ReplaceOperation(
            "development.identity.signin",
            operation => operation with
            {
                ApplicationAuthorization = "always enabled",
                NeutralErrors = "available in every environment",
            });

        AssertIssue(invalid, "authorization-register.development.boundary.invalid");
    }

    [TestMethod]
    public void MissingTestFileAndSymbolAreRejected()
    {
        AuthorizationSurfaceDocument missingFile = ReplaceOperation(
            "territory.search",
            operation => operation with { Tests = ["tests/missing.cs#Missing"] });
        AuthorizationSurfaceDocument missingSymbol = ReplaceOperation(
            "territory.search",
            operation => operation with
            {
                Tests = ["tests/AgropecuarIA.Territory.Tests/TerritoryEndpointsTests.cs#DefinitelyMissingSymbol"],
            });

        AssertIssue(missingFile, "authorization-register.test-file.missing");
        AssertIssue(missingSymbol, "authorization-register.test-symbol.missing");
    }

    [TestMethod]
    public void MissingOwnerCannotPassTheRegister()
    {
        AuthorizationSurfaceDocument invalid = ReplaceOperation(
            "territory.search",
            operation => operation with { Owner = string.Empty });

        AssertIssue(invalid, "authorization-register.property.empty");
    }

    [TestMethod]
    public void OperationIdsMustRemainUnique()
    {
        AuthorizationSurfaceDocument published = Published();
        string duplicateId = published.Operations[0].Id;
        var invalid = published with
        {
            Operations = published.Operations
                .Select((operation, index) => index == 1
                    ? operation with { Id = duplicateId }
                    : operation)
                .ToArray(),
        };

        AssertIssue(invalid, "authorization-register.id.duplicate");
    }

    [TestMethod]
    public void FrameworkOwnedOidcCallbackCannotLoseStatePkceOrFreshnessControls()
    {
        AuthorizationSurfaceDocument published = Published();
        FrameworkEntrypoint callback = published.FrameworkEntrypoints.Single();
        var invalid = published with
        {
            FrameworkEntrypoints = [callback with { Control = "issuer only" }],
        };

        AssertIssue(invalid, "authorization-register.oidc-callback.controls.incomplete");
    }

    [TestMethod]
    public void FutureSurfacesCannotBeClaimedAsIntegrated()
    {
        AuthorizationSurfaceDocument published = Published();
        var invalid = published with
        {
            AbsentSurfaces = published.AbsentSurfaces
                .Select(surface => surface.Name == "jobs"
                    ? surface with { Status = "approved" }
                    : surface)
                .ToArray(),
        };

        AssertIssue(invalid, "authorization-register.absent-surfaces.invalid");
    }

    [TestMethod]
    public void UnknownJsonPropertiesAreRejected()
    {
        string source = File.ReadAllText(RegisterPath());
        string invalidJson = source.Replace(
            "\"schemaVersion\": 1,",
            "\"schemaVersion\": 1, \"unexpected\": true,",
            StringComparison.Ordinal);
        string path = Path.Combine(Path.GetTempPath(), $"agro-authorization-{Guid.NewGuid():N}.json");
        File.WriteAllText(path, invalidJson);
        try
        {
            _ = Assert.ThrowsExactly<InvalidDataException>(() => AuthorizationSurfaceLoader.Load(path));
        }
        finally
        {
            File.Delete(path);
        }
    }

    private static AuthorizationSurfaceDocument Published() =>
        AuthorizationSurfaceLoader.Load(RegisterPath());

    private static AuthorizationSurfaceDocument ReplaceOperation(
        string id,
        Func<AuthorizationOperation, AuthorizationOperation> mutate)
    {
        AuthorizationSurfaceDocument published = Published();
        return published with
        {
            Operations = published.Operations
                .Select(operation => operation.Id == id ? mutate(operation) : operation)
                .ToArray(),
        };
    }

    private static IReadOnlyList<ValidationIssue> Validate(AuthorizationSurfaceDocument document) =>
        AuthorizationSurfaceValidator.Validate(document, EvidenceFixture.RepositoryRoot());

    private static void AssertIssue(AuthorizationSurfaceDocument document, string code) =>
        Assert.IsTrue(
            Validate(document).Any(issue => issue.Code == code),
            $"Expected issue '{code}'.");

    private static string RegisterPath() => Path.Combine(
        EvidenceFixture.RepositoryRoot(),
        AuthorizationSurfaceValidator.RegisterRelativePath);

    private static void CopyRuntimeEvidence(string temporaryRoot)
    {
        string repositoryRoot = EvidenceFixture.RepositoryRoot();
        string[] relativePaths =
        [
            "contracts/identity.openapi.yaml",
            "contracts/territory.openapi.yaml",
            "apps/AgropecuarIA.Api/IdentityEndpoints.cs",
            "src/AgropecuarIA.Territory/Delivery/TerritoryEndpoints.cs",
        ];
        foreach (string relativePath in relativePaths)
        {
            string destination = Path.Combine(temporaryRoot, relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
            File.Copy(Path.Combine(repositoryRoot, relativePath), destination);
        }
    }
}
