using AgropecuarIA.Identity.Domain;

namespace AgropecuarIA.Identity.Tests;

[TestClass]
public sealed class OrganizationMembershipTests
{
    [TestMethod]
    public void ValidRolesAreAccepted()
    {
        Assert.IsTrue(OrganizationMembershipRoles.IsValid("owner"));
        Assert.IsTrue(OrganizationMembershipRoles.IsValid("admin"));
        Assert.IsTrue(OrganizationMembershipRoles.IsValid("agronomist"));
        Assert.IsTrue(OrganizationMembershipRoles.IsValid("operator"));
        Assert.IsTrue(OrganizationMembershipRoles.IsValid("accountant"));
        Assert.IsTrue(OrganizationMembershipRoles.IsValid("viewer"));
        Assert.IsFalse(OrganizationMembershipRoles.IsValid("superadmin"));
        Assert.IsFalse(OrganizationMembershipRoles.IsValid(""));
    }

    [TestMethod]
    public void MembershipCreationSetsRoleAndProperties()
    {
        Guid id = Guid.NewGuid();
        Guid orgId = Guid.NewGuid();
        Guid userId = Guid.NewGuid();
        DateTimeOffset now = DateTimeOffset.UtcNow;

        OrganizationMembershipAssignment membership = new(
            id,
            orgId,
            userId,
            now,
            OrganizationMembershipRoles.Agronomist);

        Assert.AreEqual(id, membership.Id);
        Assert.AreEqual(orgId, membership.OrganizationId);
        Assert.AreEqual(userId, membership.UserId);
        Assert.AreEqual("agronomist", membership.Role);
        Assert.AreEqual(OrganizationMembershipStatuses.Active, membership.Status);
        Assert.AreEqual(1L, membership.SecurityVersion);
    }

    [TestMethod]
    public void MembershipCreationThrowsOnInvalidRole()
    {
        Assert.ThrowsExactly<ArgumentException>(() =>
            new OrganizationMembershipAssignment(
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid(),
                DateTimeOffset.UtcNow,
                "role_invalido"));
    }

    [TestMethod]
    public void MembershipUpdateRoleIncrementsSecurityVersion()
    {
        Guid id = Guid.NewGuid();
        Guid orgId = Guid.NewGuid();
        Guid userId = Guid.NewGuid();
        Guid editorId = Guid.NewGuid();
        Guid newVersion = Guid.NewGuid();
        DateTimeOffset now = DateTimeOffset.UtcNow;

        OrganizationMembershipAssignment membership = new(
            id,
            orgId,
            userId,
            now,
            OrganizationMembershipRoles.Viewer);

        membership.UpdateRole(OrganizationMembershipRoles.Operator, editorId, now, newVersion);

        Assert.AreEqual("operator", membership.Role);
        Assert.AreEqual(2L, membership.SecurityVersion);
        Assert.AreEqual(newVersion, membership.Version);
    }

    [TestMethod]
    public void FieldScopeAssignmentSetsProperties()
    {
        Guid id = Guid.NewGuid();
        Guid orgId = Guid.NewGuid();
        Guid membershipId = Guid.NewGuid();
        Guid fieldId = Guid.NewGuid();
        Guid grantedBy = Guid.NewGuid();
        DateTimeOffset now = DateTimeOffset.UtcNow;

        OrganizationFieldScopeAssignment scope = new(
            id,
            orgId,
            membershipId,
            fieldId,
            grantedBy,
            now);

        Assert.AreEqual(id, scope.Id);
        Assert.AreEqual(orgId, scope.OrganizationId);
        Assert.AreEqual(membershipId, scope.MembershipId);
        Assert.AreEqual(fieldId, scope.FieldId);
        Assert.AreEqual(grantedBy, scope.GrantedByUserId);
        Assert.AreEqual(now, scope.GrantedAtUtc);
    }
}
