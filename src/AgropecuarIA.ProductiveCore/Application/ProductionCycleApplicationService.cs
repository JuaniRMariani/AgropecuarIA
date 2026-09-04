using AgropecuarIA.ProductiveCore.Domain;
using Microsoft.AspNetCore.Http;

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

public sealed class ProductionCycleApplicationService(
    IProductiveCoreUnitOfWorkFactory unitOfWorkFactory,
    TimeProvider timeProvider)
{
    public Task<ProductionCycleDto> StartCycleAsync(
        StartProductionCycleCommand command,
        ProductiveRequestContext requestContext,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        return ExecuteAuthorizedAsync(
            command.OrganizationId, requestContext, ProductiveTransactionMode.SerializableWrite,
            async unitOfWork =>
            {
                ManagementUnit? field = await unitOfWork.GetManagementUnitForUpdateAsync(
                    command.OrganizationId, command.ManagementUnitId, cancellationToken);
                RequireField(field, command.OrganizationId, command.ManagementUnitId);
                if (field!.Status == ManagementUnitStatuses.Archived)
                {
                    throw new ProductiveCoreOperationException(
                        "productive_core.field_archived", StatusCodes.Status409Conflict,
                        "New production cycles cannot start on an archived field.");
                }

                var cycle = new ProductionCycle(
                    Guid.NewGuid(), command.OrganizationId, command.ManagementUnitId,
                    command.CatalogCode, command.CatalogDisplayName, command.Purpose,
                    command.System, command.SupportLevel, command.StartDateUtc,
                    timeProvider.GetUtcNow());
                unitOfWork.AddProductionCycle(cycle);
                return ToDto(cycle);
            }, cancellationToken);
    }

    public Task<ProductionEventDto> RecordEventAsync(
        RecordProductionEventCommand command,
        ProductiveRequestContext requestContext,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        return ExecuteAuthorizedAsync(
            command.OrganizationId, requestContext, ProductiveTransactionMode.SerializableWrite,
            async unitOfWork =>
            {
                ProductionCycle cycle = await RequireCycleAsync(
                    unitOfWork, command.OrganizationId, command.ProductionCycleId,
                    forUpdate: true, cancellationToken);
                if (cycle.Status != ProductionCycleStatuses.Active)
                {
                    throw new ProductiveCoreOperationException(
                        "productive_core.cycle_not_active", StatusCodes.Status409Conflict,
                        "The production cycle is not active.");
                }

                var productionEvent = new ProductionEvent(
                    Guid.NewGuid(), command.OrganizationId, command.ProductionCycleId,
                    command.EventType, command.EffectiveDateUtc, timeProvider.GetUtcNow(),
                    command.Quantity, command.Unit, command.Notes, command.Origin);
                unitOfWork.AddProductionEvent(productionEvent);
                return ToEventDto(productionEvent);
            }, cancellationToken);
    }

    public Task<ProductionCycleDto> CloseCycleAsync(
        CloseProductionCycleCommand command,
        ProductiveRequestContext requestContext,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        return ExecuteAuthorizedAsync(
            command.OrganizationId, requestContext, ProductiveTransactionMode.SerializableWrite,
            async unitOfWork =>
            {
                ProductionCycle cycle = await RequireCycleAsync(
                    unitOfWork, command.OrganizationId, command.ProductionCycleId,
                    forUpdate: true, cancellationToken);
                cycle.Close(command.EndDateUtc);
                return ToDto(cycle);
            }, cancellationToken);
    }

    public Task<IReadOnlyList<ProductionCycleDto>> ListCyclesAsync(
        Guid organizationId,
        Guid managementUnitId,
        ProductiveRequestContext requestContext,
        CancellationToken cancellationToken) =>
        ExecuteAuthorizedAsync<IReadOnlyList<ProductionCycleDto>>(
            organizationId, requestContext, ProductiveTransactionMode.Read,
            async unitOfWork =>
            {
                ManagementUnit? field = await unitOfWork.GetManagementUnitAsync(
                    organizationId, managementUnitId, cancellationToken);
                RequireField(field, organizationId, managementUnitId);
                IReadOnlyList<ProductionCycle> cycles = await unitOfWork.ListProductionCyclesAsync(
                    organizationId, managementUnitId, cancellationToken);
                return cycles.Select(ToDto).ToArray();
            }, cancellationToken);

    public Task<ProductionTimelineResult> GetTimelineAsync(
        Guid organizationId,
        Guid productionCycleId,
        ProductiveRequestContext requestContext,
        CancellationToken cancellationToken) =>
        ExecuteAuthorizedAsync(
            organizationId, requestContext, ProductiveTransactionMode.Read,
            async unitOfWork =>
            {
                ProductionCycle cycle = await RequireCycleAsync(
                    unitOfWork, organizationId, productionCycleId,
                    forUpdate: false, cancellationToken);
                IReadOnlyList<ProductionEvent> events = await unitOfWork.ListProductionEventsAsync(
                    organizationId, productionCycleId, cancellationToken);
                return new ProductionTimelineResult(ToDto(cycle), events.Select(ToEventDto).ToArray());
            }, cancellationToken);

    private async Task<T> ExecuteAuthorizedAsync<T>(
        Guid organizationId,
        ProductiveRequestContext requestContext,
        ProductiveTransactionMode mode,
        Func<IProductiveCoreUnitOfWork, Task<T>> operation,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(requestContext);
        if (organizationId == Guid.Empty || organizationId != requestContext.OrganizationId)
        {
            throw ProductiveCoreErrors.FieldNotAvailable();
        }

        try
        {
            await using IProductiveCoreUnitOfWork unitOfWork =
                await unitOfWorkFactory.BeginAsync(mode, cancellationToken);
            if (await unitOfWork.AuthorizeOwnerAsync(requestContext, cancellationToken) is null)
            {
                throw ProductiveCoreErrors.FieldNotAvailable();
            }

            T result = await operation(unitOfWork);
            if (mode == ProductiveTransactionMode.SerializableWrite)
            {
                await unitOfWork.SaveChangesAsync(cancellationToken);
            }

            await unitOfWork.CommitAsync(cancellationToken);
            return result;
        }
        catch (ArgumentException)
        {
            throw new ProductiveCoreOperationException(
                "productive_core.invalid_cycle_request", StatusCodes.Status400BadRequest,
                "The production cycle request is invalid.");
        }
        catch (Exception exception) when (exception is ProductivePersistenceUnavailableException or
            ProductiveSerializationRaceException or ProductiveCommitOutcomeUnknownException or
            ProductiveStaleVersionException or ProductiveIdempotencyRaceException)
        {
            // These commands have no idempotency ledger: never retry a possibly committed write here.
            throw new ProductiveCoreOperationException(
                "productive_core.cycle_unavailable", StatusCodes.Status503ServiceUnavailable,
                "The production cycle operation could not be confirmed.");
        }
    }

    private static async Task<ProductionCycle> RequireCycleAsync(
        IProductiveCoreUnitOfWork unitOfWork,
        Guid organizationId,
        Guid cycleId,
        bool forUpdate,
        CancellationToken cancellationToken)
    {
        ProductionCycle? cycle = forUpdate
            ? await unitOfWork.GetProductionCycleForUpdateAsync(organizationId, cycleId, cancellationToken)
            : await unitOfWork.GetProductionCycleAsync(organizationId, cycleId, cancellationToken);
        if (cycle is null || cycle.OrganizationId != organizationId || cycle.Id != cycleId)
        {
            throw ProductiveCoreErrors.FieldNotAvailable();
        }

        ManagementUnit? field = await unitOfWork.GetManagementUnitAsync(
            organizationId, cycle.ManagementUnitId, cancellationToken);
        RequireField(field, organizationId, cycle.ManagementUnitId);
        return cycle;
    }

    private static void RequireField(ManagementUnit? field, Guid organizationId, Guid fieldId)
    {
        if (field is null || field.OrganizationId != organizationId || field.Id != fieldId)
        {
            throw ProductiveCoreErrors.FieldNotAvailable();
        }
    }

    private static ProductionCycleDto ToDto(ProductionCycle c) =>
        new(c.Id, c.OrganizationId, c.ManagementUnitId, c.CatalogCode, c.CatalogDisplayName,
            c.Purpose, c.System, c.SupportLevel, c.Status, c.StartDateUtc, c.EndDateUtc, c.CreatedAtUtc);

    private static ProductionEventDto ToEventDto(ProductionEvent e) =>
        new(e.Id, e.OrganizationId, e.ProductionCycleId, e.EventType, e.EffectiveDateUtc,
            e.RecordedAtUtc, e.Quantity, e.Unit, e.Notes, e.Origin);
}
