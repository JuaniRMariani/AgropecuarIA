using AgropecuarIA.Identity.Application;
using AgropecuarIA.Identity.Domain;

namespace AgropecuarIA.Identity.Tests;

[TestClass]
public sealed class IdentityRequestContextTests
{
    [TestMethod]
    public void PlatformScopeCanRepresentAnonymousAndAuthenticatedRequests()
    {
        var actorId = Guid.NewGuid();

        var anonymous = IdentityRequestContext.ForPlatform("anonymous-request");
        var authenticated = IdentityRequestContext.ForPlatform("authenticated-request", actorId);

        Assert.AreEqual("platform", anonymous.Scope.Kind);
        Assert.IsNull(anonymous.ActorId);
        Assert.IsNull(anonymous.Scope.TenantId);
        Assert.AreEqual(actorId, authenticated.ActorId);
        authenticated.RequirePlatformActor(actorId);
    }

    [TestMethod]
    public void TenantScopeRequiresBothActorAndTenantIdentifiers()
    {
        var actorId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();

        var context = IdentityRequestContext.ForTenant("tenant-request", actorId, tenantId);

        Assert.AreEqual("tenant", context.Scope.Kind);
        Assert.AreEqual(actorId, context.ActorId);
        Assert.AreEqual(tenantId, context.Scope.TenantId);
        Assert.ThrowsExactly<ArgumentException>(() =>
            IdentityRequestContext.ForTenant("tenant-request", Guid.Empty, tenantId));
        Assert.ThrowsExactly<ArgumentException>(() =>
            IdentityRequestContext.ForTenant("tenant-request", actorId, Guid.Empty));
    }

    [TestMethod]
    public void PlatformGuardsRejectCrossPlaneOrImpersonatedContexts()
    {
        var actorId = Guid.NewGuid();
        var tenantContext = IdentityRequestContext.ForTenant(
            "tenant-request",
            actorId,
            Guid.NewGuid());
        var platformContext = IdentityRequestContext.ForPlatform("platform-request", actorId);

        Assert.ThrowsExactly<InvalidOperationException>(tenantContext.RequirePlatform);
        Assert.ThrowsExactly<InvalidOperationException>(() =>
            platformContext.RequirePlatformActor(Guid.NewGuid()));
    }

    [TestMethod]
    public void PlatformUserIssuesStrictlyMonotonicAggregateVersions()
    {
        var user = new PlatformUser(Guid.NewGuid(), "Test user", DateTimeOffset.UtcNow);

        Assert.AreEqual(1L, user.NextVersion());
        Assert.AreEqual(2L, user.NextVersion());
        Assert.AreEqual(2L, user.Version);
    }
}
