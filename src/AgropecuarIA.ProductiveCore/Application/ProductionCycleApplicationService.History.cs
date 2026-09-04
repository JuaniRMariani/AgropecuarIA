using AgropecuarIA.ProductiveCore.Domain;

namespace AgropecuarIA.ProductiveCore.Application;

public sealed partial class ProductionCycleApplicationService
{
    public Task<ProductionCyclePage> ListCyclePageAsync(
        Guid organizationId, Guid managementUnitId, int? limit, string? cursor,
        ProductiveRequestContext requestContext, CancellationToken cancellationToken) =>
        ExecuteAuthorizedAsync(organizationId, requestContext, ProductiveTransactionMode.Read, async unitOfWork =>
        {
            ManagementUnit? field = await unitOfWork.GetManagementUnitAsync(organizationId, managementUnitId, cancellationToken);
            RequireField(field, organizationId, managementUnitId);
            ProductionHistoryWindow window = ProductionHistoryPaging.Parse(limit, cursor, "cycles", organizationId, managementUnitId);
            IReadOnlyList<ProductionCycle> rows = await unitOfWork.ListProductionCyclePageAsync(
                organizationId, managementUnitId, window, cancellationToken);
            bool hasMore = rows.Count > window.Limit;
            ProductionCycle[] items = rows.Take(window.Limit).ToArray();
            string? next = hasMore ? ProductionHistoryPaging.Encode("cycles", organizationId, managementUnitId,
                items[^1].CreatedAtUtc, items[^1].Id) : null;
            return new ProductionCyclePage(items.Select(ToDto).ToArray(), hasMore, next);
        }, cancellationToken);

    public Task<ProductionTimelinePage> GetTimelinePageAsync(
        Guid organizationId, Guid cycleId, int? limit, string? cursor,
        ProductiveRequestContext requestContext, CancellationToken cancellationToken) =>
        ExecuteAuthorizedAsync(organizationId, requestContext, ProductiveTransactionMode.Read, async unitOfWork =>
        {
            ProductionCycle cycle = await RequireCycleAsync(unitOfWork, organizationId, cycleId, forUpdate: false, cancellationToken);
            ProductionHistoryWindow window = ProductionHistoryPaging.Parse(limit, cursor, "events", organizationId, cycleId);
            IReadOnlyList<ProductionEvent> rows = await unitOfWork.ListProductionEventPageAsync(
                organizationId, cycleId, window, cancellationToken);
            bool hasMore = rows.Count > window.Limit;
            ProductionEvent[] items = rows.Take(window.Limit).ToArray();
            string? next = hasMore ? ProductionHistoryPaging.Encode("events", organizationId, cycleId,
                items[^1].RecordedAtUtc, items[^1].Id) : null;
            return new ProductionTimelinePage(ToDto(cycle), items.Select(ToEventDto).ToArray(), hasMore, next);
        }, cancellationToken);
}
