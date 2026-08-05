using System.Collections.Immutable;

namespace AgropecuarIA.ArchitectureFitness;

public sealed record EventStreamScope(ResourceScopeKind Kind, Guid? TenantId)
{
    public static EventStreamScope Platform() => new(ResourceScopeKind.Platform, null);

    public static EventStreamScope Tenant(Guid tenantId) =>
        tenantId == Guid.Empty
            ? throw new ArgumentException("Tenant ID is required.", nameof(tenantId))
            : new EventStreamScope(ResourceScopeKind.Tenant, tenantId);

    public bool IsValid =>
        Kind == ResourceScopeKind.Platform
            ? TenantId is null
            : Kind == ResourceScopeKind.Tenant && TenantId is not null && TenantId != Guid.Empty;
}

public sealed record SequencedEvent(
    Guid EventId,
    string Source,
    EventStreamScope Scope,
    Guid AggregateId,
    int AggregateVersion);

public sealed record EventCursor(
    string Source,
    EventStreamScope Scope,
    Guid AggregateId,
    int LastAppliedVersion,
    ImmutableHashSet<Guid> AppliedEventIds)
{
    public static EventCursor Empty(string source, EventStreamScope scope, Guid aggregateId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(source);
        ArgumentNullException.ThrowIfNull(scope);
        if (!scope.IsValid)
        {
            throw new ArgumentException("Event scope is invalid.", nameof(scope));
        }

        if (aggregateId == Guid.Empty)
        {
            throw new ArgumentException("Aggregate ID is required.", nameof(aggregateId));
        }

        return new EventCursor(source, scope, aggregateId, 0, ImmutableHashSet<Guid>.Empty);
    }
}

public enum EventDisposition
{
    Applied,
    Duplicate,
    OutOfOrder,
    Gap,
}

public sealed record EventSequenceResult(EventDisposition Disposition, EventCursor NextCursor);

public static class EventSequenceGuard
{
    public static EventSequenceResult Evaluate(EventCursor cursor, SequencedEvent candidate)
    {
        ArgumentNullException.ThrowIfNull(cursor);
        ArgumentNullException.ThrowIfNull(candidate);

        if (candidate.EventId == Guid.Empty)
        {
            throw new ArgumentException("Event ID is required.", nameof(candidate));
        }

        if (!candidate.Scope.IsValid
            || candidate.AggregateId != cursor.AggregateId
            || !string.Equals(candidate.Source, cursor.Source, StringComparison.Ordinal)
            || candidate.Scope != cursor.Scope)
        {
            throw new ArgumentException("The event belongs to another source, scope or aggregate.", nameof(candidate));
        }

        if (candidate.AggregateVersion < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(candidate), "Aggregate version must be positive.");
        }

        if (cursor.AppliedEventIds.Contains(candidate.EventId))
        {
            return new EventSequenceResult(EventDisposition.Duplicate, cursor);
        }

        if (candidate.AggregateVersion <= cursor.LastAppliedVersion)
        {
            return new EventSequenceResult(EventDisposition.OutOfOrder, cursor);
        }

        if (candidate.AggregateVersion != cursor.LastAppliedVersion + 1)
        {
            return new EventSequenceResult(EventDisposition.Gap, cursor);
        }

        var next = cursor with
        {
            LastAppliedVersion = candidate.AggregateVersion,
            AppliedEventIds = cursor.AppliedEventIds.Add(candidate.EventId),
        };
        return new EventSequenceResult(EventDisposition.Applied, next);
    }
}
