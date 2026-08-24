using AgropecuarIA.ProductiveCore.Domain;
using AgropecuarIA.ProductiveCore.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace AgropecuarIA.ProductiveCore.Application;

public sealed record StartProductionCycleCommand(
    Guid OrganizationId,
    Guid ManagementUnitId,
    string CatalogCode,
    string CatalogDisplayName,
    string Purpose,
    string System,
    string SupportLevel,
    DateTimeOffset StartDateUtc);

public sealed record RecordProductionEventCommand(
    Guid OrganizationId,
    Guid ProductionCycleId,
    string EventType,
    DateTimeOffset EffectiveDateUtc,
    decimal? Quantity,
    string? Unit,
    string? Notes,
    string Origin = ProductionOrigins.Manual);

public sealed record CloseProductionCycleCommand(
    Guid OrganizationId,
    Guid ProductionCycleId,
    DateTimeOffset EndDateUtc);

public sealed record ProductionCycleDto(
    Guid Id,
    Guid OrganizationId,
    Guid ManagementUnitId,
    string CatalogCode,
    string CatalogDisplayName,
    string Purpose,
    string System,
    string SupportLevel,
    string Status,
    DateTimeOffset StartDateUtc,
    DateTimeOffset? EndDateUtc,
    DateTimeOffset CreatedAtUtc);

public sealed record ProductionEventDto(
    Guid Id,
    Guid OrganizationId,
    Guid ProductionCycleId,
    string EventType,
    DateTimeOffset EffectiveDateUtc,
    DateTimeOffset RecordedAtUtc,
    decimal? Quantity,
    string? Unit,
    string? Notes,
    string Origin);

public sealed record ProductionTimelineResult(
    ProductionCycleDto Cycle,
    IReadOnlyList<ProductionEventDto> Events);

public sealed class ProductionCycleApplicationService(ProductiveCoreDbContext dbContext)
{
    public async Task<ProductionCycleDto> StartCycleAsync(
        StartProductionCycleCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        bool fieldExists = await dbContext.ManagementUnits
            .AnyAsync(x => x.Id == command.ManagementUnitId && x.OrganizationId == command.OrganizationId, cancellationToken);

        if (!fieldExists)
        {
            throw new InvalidOperationException("Management unit not found in this organization.");
        }

        DateTimeOffset now = DateTimeOffset.UtcNow;
        var cycle = new ProductionCycle(
            Guid.NewGuid(),
            command.OrganizationId,
            command.ManagementUnitId,
            command.CatalogCode,
            command.CatalogDisplayName,
            command.Purpose,
            command.System,
            command.SupportLevel,
            command.StartDateUtc,
            now);

        dbContext.ProductionCycles.Add(cycle);
        await dbContext.SaveChangesAsync(cancellationToken);

        return ToDto(cycle);
    }

    public async Task<ProductionEventDto> RecordEventAsync(
        RecordProductionEventCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var cycle = await dbContext.ProductionCycles
            .FirstOrDefaultAsync(x => x.Id == command.ProductionCycleId && x.OrganizationId == command.OrganizationId, cancellationToken);

        if (cycle is null)
        {
            throw new InvalidOperationException("Production cycle not found in this organization.");
        }

        if (cycle.Status != ProductionCycleStatuses.Active)
        {
            throw new InvalidOperationException($"Cannot record event in a cycle with status '{cycle.Status}'.");
        }

        DateTimeOffset now = DateTimeOffset.UtcNow;
        var evt = new ProductionEvent(
            Guid.NewGuid(),
            command.OrganizationId,
            command.ProductionCycleId,
            command.EventType,
            command.EffectiveDateUtc,
            now,
            command.Quantity,
            command.Unit,
            command.Notes,
            command.Origin);

        dbContext.ProductionEvents.Add(evt);
        await dbContext.SaveChangesAsync(cancellationToken);

        return ToEventDto(evt);
    }

    public async Task<ProductionCycleDto> CloseCycleAsync(
        CloseProductionCycleCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var cycle = await dbContext.ProductionCycles
            .FirstOrDefaultAsync(x => x.Id == command.ProductionCycleId && x.OrganizationId == command.OrganizationId, cancellationToken);

        if (cycle is null)
        {
            throw new InvalidOperationException("Production cycle not found in this organization.");
        }

        cycle.Close(command.EndDateUtc);
        await dbContext.SaveChangesAsync(cancellationToken);

        return ToDto(cycle);
    }

    public async Task<IReadOnlyList<ProductionCycleDto>> ListCyclesAsync(
        Guid organizationId,
        Guid managementUnitId,
        CancellationToken cancellationToken)
    {
        var cycles = await dbContext.ProductionCycles
            .AsNoTracking()
            .Where(x => x.OrganizationId == organizationId && x.ManagementUnitId == managementUnitId)
            .OrderByDescending(x => x.StartDateUtc)
            .ToListAsync(cancellationToken);

        return cycles.Select(ToDto).ToList();
    }

    public async Task<ProductionTimelineResult?> GetTimelineAsync(
        Guid organizationId,
        Guid productionCycleId,
        CancellationToken cancellationToken)
    {
        var cycle = await dbContext.ProductionCycles
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == productionCycleId && x.OrganizationId == organizationId, cancellationToken);

        if (cycle is null)
            return null;

        var events = await dbContext.ProductionEvents
            .AsNoTracking()
            .Where(x => x.OrganizationId == organizationId && x.ProductionCycleId == productionCycleId)
            .OrderBy(x => x.EffectiveDateUtc)
            .ToListAsync(cancellationToken);

        return new ProductionTimelineResult(
            ToDto(cycle),
            events.Select(ToEventDto).ToList());
    }

    private static ProductionCycleDto ToDto(ProductionCycle c) =>
        new(c.Id, c.OrganizationId, c.ManagementUnitId, c.CatalogCode, c.CatalogDisplayName,
            c.Purpose, c.System, c.SupportLevel, c.Status, c.StartDateUtc, c.EndDateUtc, c.CreatedAtUtc);

    private static ProductionEventDto ToEventDto(ProductionEvent e) =>
        new(e.Id, e.OrganizationId, e.ProductionCycleId, e.EventType, e.EffectiveDateUtc,
            e.RecordedAtUtc, e.Quantity, e.Unit, e.Notes, e.Origin);
}
