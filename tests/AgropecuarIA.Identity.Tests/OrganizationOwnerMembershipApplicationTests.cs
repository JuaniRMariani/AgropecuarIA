using AgropecuarIA.Identity.Domain;

namespace AgropecuarIA.Identity.Tests;

[TestClass]
public sealed class OrganizationOwnerMembershipApplicationTests
{
    private static readonly DateTimeOffset CreatedAtUtc =
        new(2026, 8, 18, 10, 0, 0, TimeSpan.Zero);

    [TestMethod]
    public void RemoveIsASingleTerminalVersionedTransition()
    {
        Guid removerId = Guid.NewGuid();
        Guid newVersion = Guid.NewGuid();
        OrganizationMembershipAssignment membership = new(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            CreatedAtUtc);

        membership.Remove(removerId, CreatedAtUtc.AddMinutes(1), newVersion);

        Assert.AreEqual(OrganizationMembershipStatuses.Removed, membership.Status);
        Assert.AreEqual(2, membership.SecurityVersion);
        Assert.AreEqual(removerId, membership.RemovedByUserId);
        Assert.AreEqual(CreatedAtUtc.AddMinutes(1), membership.RemovedAtUtc);
        Assert.AreEqual(newVersion, membership.Version);
        Assert.ThrowsExactly<InvalidOperationException>(() =>
            membership.Remove(Guid.NewGuid(), CreatedAtUtc.AddMinutes(2), Guid.NewGuid()));
    }

    [TestMethod]
    public void RemovalLedgerBindsTenantActorSessionMembershipAndFencedResult()
    {
        DateTimeOffset startedAtUtc = CreatedAtUtc.AddMinutes(2);
        Guid leaseOwner = Guid.NewGuid();
        Guid resultVersion = Guid.NewGuid();
        OrganizationOwnerRemovalLedger ledger = new(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Enumerable.Repeat((byte)0x5a, 32).ToArray(),
            leaseOwner,
            startedAtUtc,
            startedAtUtc.AddMinutes(1));

        ledger.Complete(
            leaseOwner,
            ledger.FenceToken,
            resultVersion,
            2,
            startedAtUtc.AddSeconds(1),
            startedAtUtc.AddSeconds(1));

        Assert.AreEqual(OrganizationOwnerRemovalProtocol.States.Succeeded, ledger.State);
        Assert.AreEqual(resultVersion, ledger.ResultMembershipVersion);
        Assert.AreEqual(2, ledger.ResultAuthorizationVersion);
        Assert.ThrowsExactly<InvalidOperationException>(() =>
            ledger.Complete(
                leaseOwner,
                ledger.FenceToken,
                Guid.NewGuid(),
                3,
                startedAtUtc.AddSeconds(2),
                startedAtUtc.AddSeconds(2)));
    }
}
