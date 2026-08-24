using AgropecuarIA.Catalog.Application;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AgropecuarIA.Catalog.Delivery;

public static class CatalogEndpoints
{
    public const string RateLimitPolicy = "catalog";

    public static IEndpointRouteBuilder MapCatalogEndpoints(this IEndpointRouteBuilder endpoints)
    {
        RouteGroupBuilder catalog = endpoints.MapGroup("/api/catalog")
            .RequireRateLimiting(RateLimitPolicy);

        // Map ingestion endpoint
        catalog.MapPost("/ingest", async (
            IngestSourceCommand command,
            CatalogIngestionApplicationService ingestionService,
            CancellationToken cancellationToken) =>
        {
            bool ingested = await ingestionService.IngestAsync(command, cancellationToken);
            return ingested
                ? Results.Ok(new { status = "ingested", sourceId = command.SourceId })
                : Results.Conflict(new { status = "duplicate_snapshot", message = "Source snapshot with identical hash already exists." });
        });

        // Map diff endpoint
        catalog.MapGet("/diff", async (
            CatalogDiffApplicationService diffService,
            CancellationToken cancellationToken) =>
        {
            var report = await diffService.GenerateDiffAsync(cancellationToken);
            return Results.Ok(report);
        });

        // Map publish endpoint
        catalog.MapPost("/publish", async (
            PublishVersionCommand command,
            CatalogPublicationApplicationService publicationService,
            CancellationToken cancellationToken) =>
        {
            var result = await publicationService.PublishAsync(command, cancellationToken);
            return Results.Ok(result);
        });

        // Map rollback endpoint
        catalog.MapPost("/rollback/{versionId:guid}", async (
            Guid versionId,
            CatalogPublicationApplicationService publicationService,
            CancellationToken cancellationToken) =>
        {
            bool rolledBack = await publicationService.RollbackAsync(new RollbackVersionCommand(versionId), cancellationToken);
            return rolledBack
                ? Results.Ok(new { status = "rolled_back", versionId })
                : Results.NotFound(new { status = "version_not_found", message = $"Version '{versionId}' not found." });
        });

        // Map search items endpoint
        catalog.MapGet("/items", async (
            string? query,
            string? jurisdiction,
            string? category,
            string? supportLevel,
            int? limit,
            CatalogSearchApplicationService searchService,
            CancellationToken cancellationToken) =>
        {
            var result = await searchService.SearchAsync(
                new SearchCatalogQuery(query, jurisdiction, category, supportLevel, limit ?? 50),
                cancellationToken);
            return Results.Ok(result);
        });

        // Map item detail endpoint
        catalog.MapGet("/items/{code}", async (
            string code,
            CatalogSearchApplicationService searchService,
            CancellationToken cancellationToken) =>
        {
            var item = await searchService.GetItemByCodeAsync(code, cancellationToken);
            return item is not null
                ? Results.Ok(item)
                : Results.NotFound(new { status = "item_not_found", message = $"Item '{code}' was not found in the active catalog." });
        });

        return endpoints;
    }
}