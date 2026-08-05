using AgropecuarIA.ArchitectureFitness;

namespace AgropecuarIA.ArchitectureFitness.Tests;

[TestClass]
public sealed class EventSequenceGuardTests
{
    [TestMethod]
    public void ContiguousEventAdvancesCursorImmutably()
    {
        var aggregateId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var current = EventCursor.Empty("productive-core", EventStreamScope.Tenant(tenantId), aggregateId);
        var candidate = Event(current, 1);

        var result = EventSequenceGuard.Evaluate(current, candidate);

        Assert.AreEqual(EventDisposition.Applied, result.Disposition);
        Assert.AreEqual(0, current.LastAppliedVersion);
        Assert.HasCount(0, current.AppliedEventIds);
        Assert.AreEqual(1, result.NextCursor.LastAppliedVersion);
        Assert.IsTrue(result.NextCursor.AppliedEventIds.Contains(candidate.EventId));
    }

    [TestMethod]
    public void DuplicateEventDoesNotMutateCursor()
    {
        var current = Cursor();
        var candidate = Event(current, 1);
        var applied = EventSequenceGuard.Evaluate(current, candidate).NextCursor;

        var duplicate = EventSequenceGuard.Evaluate(applied, candidate);

        Assert.AreEqual(EventDisposition.Duplicate, duplicate.Disposition);
        Assert.AreSame(applied, duplicate.NextCursor);
    }

    [TestMethod]
    public void UniqueOlderEventDoesNotMutateCursor()
    {
        var current = Cursor();
        var first = EventSequenceGuard.Evaluate(current, Event(current, 1)).NextCursor;
        var second = EventSequenceGuard.Evaluate(first, Event(first, 2)).NextCursor;

        var result = EventSequenceGuard.Evaluate(second, Event(second, 1));

        Assert.AreEqual(EventDisposition.OutOfOrder, result.Disposition);
        Assert.AreSame(second, result.NextCursor);
        Assert.AreEqual(2, second.LastAppliedVersion);
    }

    [TestMethod]
    public void VersionGapDoesNotPartiallyAdvanceCursor()
    {
        var current = Cursor();

        var result = EventSequenceGuard.Evaluate(current, Event(current, 2));

        Assert.AreEqual(EventDisposition.Gap, result.Disposition);
        Assert.AreSame(current, result.NextCursor);
        Assert.AreEqual(0, current.LastAppliedVersion);
        Assert.HasCount(0, current.AppliedEventIds);
    }

    [TestMethod]
    public void SameAggregateIdFromAnotherTenantIsRejected()
    {
        var current = Cursor();
        var foreign = Event(current, 1) with { Scope = EventStreamScope.Tenant(Guid.NewGuid()) };

        Assert.ThrowsExactly<ArgumentException>(() => EventSequenceGuard.Evaluate(current, foreign));
        Assert.AreEqual(0, current.LastAppliedVersion);
    }

    [TestMethod]
    public void SameAggregateAndTenantFromAnotherSourceIsRejected()
    {
        var current = Cursor();
        var foreign = Event(current, 1) with { Source = "integrations" };

        Assert.ThrowsExactly<ArgumentException>(() => EventSequenceGuard.Evaluate(current, foreign));
        Assert.AreEqual(0, current.LastAppliedVersion);
    }

    [TestMethod]
    public void PlatformScopeCannotCarryTenantId()
    {
        var invalid = new EventStreamScope(ResourceScopeKind.Platform, Guid.NewGuid());

        Assert.ThrowsExactly<ArgumentException>(
            () => EventCursor.Empty("national-catalog", invalid, Guid.NewGuid()));
    }

    [TestMethod]
    public void UnknownScopeKindIsRejected()
    {
        var invalid = new EventStreamScope((ResourceScopeKind)999, Guid.NewGuid());

        Assert.ThrowsExactly<ArgumentException>(
            () => EventCursor.Empty("productive-core", invalid, Guid.NewGuid()));
    }

    private static EventCursor Cursor() =>
        EventCursor.Empty("productive-core", EventStreamScope.Tenant(Guid.NewGuid()), Guid.NewGuid());

    private static SequencedEvent Event(EventCursor cursor, int version) =>
        new(Guid.NewGuid(), cursor.Source, cursor.Scope, cursor.AggregateId, version);
}
