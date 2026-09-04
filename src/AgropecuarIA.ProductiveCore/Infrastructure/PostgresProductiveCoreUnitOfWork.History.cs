using AgropecuarIA.ProductiveCore.Application;
using AgropecuarIA.ProductiveCore.Domain;
using Microsoft.EntityFrameworkCore;

namespace AgropecuarIA.ProductiveCore.Infrastructure;

internal sealed partial class PostgresProductiveCoreUnitOfWork
{
    public async Task<IReadOnlyList<ProductionCycle>> ListProductionCyclePageAsync(
        Guid organizationId, Guid managementUnitId, ProductionHistoryWindow window, CancellationToken cancellationToken)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(window.Limit, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(window.Limit, 100);
        try
        {
            IQueryable<ProductionCycle> query = dbContext.ProductionCycles.AsNoTracking()
                .Where(cycle => cycle.OrganizationId == organizationId && cycle.ManagementUnitId == managementUnitId);
            if (window.Before is { } before)
                query = query.Where(cycle => EF.Functions.LessThan(
                    ValueTuple.Create(cycle.CreatedAtUtc, cycle.Id), ValueTuple.Create(before.RecordedAtUtc, before.Id)));
            return await query.OrderByDescending(cycle => cycle.CreatedAtUtc).ThenByDescending(cycle => cycle.Id)
                .Take(window.Limit + 1).ToArrayAsync(cancellationToken);
        }
        catch (Exception exception) when (IsPersistenceFailure(exception))
        {
            throw Unavailable("read a bounded production cycle page", exception);
        }
    }

    public async Task<IReadOnlyList<ProductionEvent>> ListProductionEventPageAsync(
        Guid organizationId, Guid cycleId, ProductionHistoryWindow window, CancellationToken cancellationToken)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(window.Limit, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(window.Limit, 100);
        try
        {
            IQueryable<ProductionEvent> query = dbContext.ProductionEvents.AsNoTracking()
                .Where(item => item.OrganizationId == organizationId && item.ProductionCycleId == cycleId);
            if (window.Before is { } before)
                query = query.Where(item => EF.Functions.LessThan(
                    ValueTuple.Create(item.RecordedAtUtc, item.Id), ValueTuple.Create(before.RecordedAtUtc, before.Id)));
            return await query.OrderByDescending(item => item.RecordedAtUtc).ThenByDescending(item => item.Id)
                .Take(window.Limit + 1).ToArrayAsync(cancellationToken);
        }
        catch (Exception exception) when (IsPersistenceFailure(exception))
        {
            throw Unavailable("read a bounded production event page", exception);
        }
    }
}
