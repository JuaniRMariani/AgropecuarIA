using AgropecuarIA.ArchitectureFitness;

namespace AgropecuarIA.ArchitectureFitness.Tests;

[TestClass]
public sealed class ResourceCommandPolicyTests
{
    [TestMethod]
    [DataRow(false, true, true, true, true, true, 401, "authentication_required")]
    [DataRow(true, false, true, true, true, true, 404, "resource_not_found")]
    [DataRow(true, true, false, true, true, true, 404, "resource_not_found")]
    [DataRow(true, true, true, false, true, true, 404, "resource_not_found")]
    [DataRow(true, true, true, true, false, true, 412, "precondition_failed")]
    [DataRow(true, true, true, true, true, false, 409, "resource_conflict")]
    [DataRow(true, true, true, true, true, true, 204, "command_accepted")]
    public void TenantPolicyMapsBoundaryStatesToStableCodes(
        bool authenticated,
        bool exists,
        bool sameTenant,
        bool hasResourceAccess,
        bool etagMatches,
        bool stateAllows,
        int expectedStatus,
        string expectedCode)
    {
        var facts = TenantFacts() with
        {
            IsAuthenticated = authenticated,
            ResourceExists = exists,
            IsSameTenant = sameTenant,
            HasResourceAccess = hasResourceAccess,
            HasMatchingEtag = etagMatches,
            ResourceStateAllowsCommand = stateAllows,
        };

        var decision = ResourceCommandPolicy.Evaluate(facts);

        Assert.AreEqual(expectedStatus, decision.StatusCode);
        Assert.AreEqual(expectedCode, decision.Code);
    }

    [TestMethod]
    public void ResourceAuthorizationIsEvaluatedBeforeEtagPrecondition()
    {
        var decision = ResourceCommandPolicy.Evaluate(
            TenantFacts() with { HasResourceAccess = false, HasMatchingEtag = false });

        Assert.AreEqual(404, decision.StatusCode);
        Assert.AreEqual("resource_not_found", decision.Code);
    }

    [TestMethod]
    public void TenantContextCannotUseTenantCapabilityForPlatformResource()
    {
        var decision = ResourceCommandPolicy.Evaluate(
            TenantFacts() with
            {
                ResourceScope = ResourceScopeKind.Platform,
                HasPlatformCapability = false,
            });

        Assert.AreEqual(403, decision.StatusCode);
        Assert.AreEqual("action_forbidden", decision.Code);
    }

    [TestMethod]
    public void PlatformContextCannotUsePlatformCapabilityForTenantResource()
    {
        var decision = ResourceCommandPolicy.Evaluate(
            TenantFacts() with
            {
                EffectiveScope = ResourceScopeKind.Platform,
                HasPlatformCapability = true,
                HasTenantCapability = false,
            });

        Assert.AreEqual(403, decision.StatusCode);
        Assert.AreEqual("action_forbidden", decision.Code);
    }

    [TestMethod]
    public void PlatformCommandRequiresPlatformCapability()
    {
        var facts = TenantFacts() with
        {
            EffectiveScope = ResourceScopeKind.Platform,
            ResourceScope = ResourceScopeKind.Platform,
            HasPlatformCapability = true,
            HasTenantCapability = false,
            IsSameTenant = false,
        };

        var decision = ResourceCommandPolicy.Evaluate(facts);

        Assert.AreEqual(204, decision.StatusCode);
    }

    [TestMethod]
    public void UnknownResourceScopeKindFailsClosed()
    {
        var decision = ResourceCommandPolicy.Evaluate(
            TenantFacts() with
            {
                EffectiveScope = (ResourceScopeKind)999,
                ResourceScope = (ResourceScopeKind)999,
                HasPlatformCapability = true,
                HasTenantCapability = true,
            });

        Assert.AreEqual(403, decision.StatusCode);
        Assert.AreEqual("action_forbidden", decision.Code);
    }

    private static ResourceCommandFacts TenantFacts() =>
        new(
            IsAuthenticated: true,
            EffectiveScope: ResourceScopeKind.Tenant,
            ResourceScope: ResourceScopeKind.Tenant,
            HasPlatformCapability: false,
            HasTenantCapability: true,
            ResourceExists: true,
            IsSameTenant: true,
            HasResourceAccess: true,
            HasMatchingEtag: true,
            ResourceStateAllowsCommand: true);
}
