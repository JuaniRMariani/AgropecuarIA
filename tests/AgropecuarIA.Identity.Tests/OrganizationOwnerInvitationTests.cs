using AgropecuarIA.Identity.Application;
using AgropecuarIA.Identity.Domain;

namespace AgropecuarIA.Identity.Tests;

[TestClass]
public sealed class OrganizationOwnerInvitationTests
{
    private static readonly DateTimeOffset CreatedAtUtc =
        new(2026, 8, 11, 12, 0, 0, TimeSpan.Zero);

    [TestMethod]
    public void InvitationStartsPendingAndExpiryIsDerivedWithoutMutation()
    {
        OrganizationOwnerInvitation invitation = CreateInvitation();

        Assert.AreEqual(
            OrganizationOwnerInvitationStatuses.Pending,
            invitation.GetEffectiveStatus(CreatedAtUtc.AddDays(6)));
        Assert.AreEqual(
            OrganizationOwnerInvitationStatuses.Expired,
            invitation.GetEffectiveStatus(CreatedAtUtc.AddDays(7)));
        Assert.AreEqual(OrganizationOwnerInvitationStatuses.Pending, invitation.Status);
        Assert.IsNull(invitation.AcceptedAtUtc);
        Assert.IsNull(invitation.RevokedAtUtc);
    }

    [TestMethod]
    public void AcceptanceIsTerminalAndRecordsOnlyMembershipReference()
    {
        OrganizationOwnerInvitation invitation = CreateInvitation();
        Guid userId = Guid.NewGuid();
        Guid membershipId = Guid.NewGuid();
        Guid initialVersion = invitation.Version;

        invitation.Accept(userId, membershipId, CreatedAtUtc.AddHours(1));

        Assert.AreEqual(OrganizationOwnerInvitationStatuses.Accepted, invitation.Status);
        Assert.AreEqual(userId, invitation.AcceptedByUserId);
        Assert.AreEqual(membershipId, invitation.AcceptedMembershipId);
        Assert.AreNotEqual(initialVersion, invitation.Version);
        Assert.ThrowsExactly<InvalidOperationException>(() =>
            invitation.Accept(userId, membershipId, CreatedAtUtc.AddHours(2)));
        Assert.ThrowsExactly<InvalidOperationException>(() =>
            invitation.Revoke(userId, CreatedAtUtc.AddHours(2)));
    }

    [TestMethod]
    public void RevocationIsTerminal()
    {
        OrganizationOwnerInvitation invitation = CreateInvitation();
        Guid actorId = Guid.NewGuid();

        invitation.Revoke(actorId, CreatedAtUtc.AddHours(1));

        Assert.AreEqual(OrganizationOwnerInvitationStatuses.Revoked, invitation.Status);
        Assert.AreEqual(actorId, invitation.RevokedByUserId);
        Assert.IsNotNull(invitation.RevokedAtUtc);
        Assert.ThrowsExactly<InvalidOperationException>(() =>
            invitation.Accept(Guid.NewGuid(), Guid.NewGuid(), CreatedAtUtc.AddHours(2)));
    }

    [TestMethod]
    public void ExactExpiryCannotBeAcceptedOrRevoked()
    {
        OrganizationOwnerInvitation acceptedCandidate = CreateInvitation();
        OrganizationOwnerInvitation revokedCandidate = CreateInvitation();
        DateTimeOffset expiresAtUtc = CreatedAtUtc.AddDays(7);

        Assert.ThrowsExactly<InvalidOperationException>(() =>
            acceptedCandidate.Accept(Guid.NewGuid(), Guid.NewGuid(), expiresAtUtc));
        Assert.ThrowsExactly<InvalidOperationException>(() =>
            revokedCandidate.Revoke(Guid.NewGuid(), expiresAtUtc));
    }

    [TestMethod]
    public void ConstructorRejectsNonSha256DigestsAndInvalidLifetime()
    {
        Assert.ThrowsExactly<ArgumentException>(() => new OrganizationOwnerInvitation(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "v1",
            new byte[31],
            "v1",
            new byte[32],
            CreatedAtUtc,
            CreatedAtUtc.AddDays(7)));
        Assert.ThrowsExactly<ArgumentException>(() => new OrganizationOwnerInvitation(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "v1",
            new byte[32],
            "v1",
            new byte[32],
            CreatedAtUtc,
            CreatedAtUtc));
    }

    [TestMethod]
    public void TenantContextRequiresExactActorAndTenant()
    {
        Guid actorId = Guid.NewGuid();
        Guid tenantId = Guid.NewGuid();
        IdentityRequestContext context = IdentityRequestContext.ForTenant(
            "invitation-test",
            actorId,
            tenantId);

        context.RequireTenantActor(actorId, tenantId);
        Assert.ThrowsExactly<InvalidOperationException>(() =>
            context.RequireTenantActor(Guid.NewGuid(), tenantId));
        Assert.ThrowsExactly<InvalidOperationException>(() =>
            context.RequireTenantActor(actorId, Guid.NewGuid()));
    }

    [TestMethod]
    public void ManageOrganizationOwnersIsAnExplicitSupportedPurpose()
    {
        string configuredPurpose = string.Join('_', "manage", "organization", "owners");

        Assert.IsTrue(StepUpPurposes.IsSupported(configuredPurpose));
    }

    private static OrganizationOwnerInvitation CreateInvitation() =>
        new(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "v1",
            Enumerable.Repeat((byte)0x11, 32).ToArray(),
            "v1",
            Enumerable.Repeat((byte)0x22, 32).ToArray(),
            CreatedAtUtc,
            CreatedAtUtc.AddDays(7));
}
