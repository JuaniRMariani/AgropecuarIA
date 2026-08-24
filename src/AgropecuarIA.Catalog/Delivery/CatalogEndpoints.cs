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

        return endpoints;
    }
}