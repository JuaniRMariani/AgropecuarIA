using AgropecuarIA.Catalog.Application;
using AgropecuarIA.Identity.Delivery;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using System.Text.Json.Serialization;

namespace AgropecuarIA.Catalog.Delivery;

public static class CatalogEndpoints
{
    public const string RateLimitPolicy = "catalog";
    public const string EditorialPolicy = "CatalogEditorial";

    [JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
    public sealed record PublishCatalogRequest(string VersionTag);

    public static IEndpointRouteBuilder MapCatalogEndpoints(this IEndpointRouteBuilder endpoints)
    {
        RouteGroupBuilder catalog = endpoints.MapGroup("/api/catalog")
            .RequireAuthorization()
            .RequireRateLimiting(RateLimitPolicy);

        // Map ingestion endpoint
        catalog.MapPost("/ingest", async (
            IngestSourceCommand command,
            HttpContext context,
            IAntiforgery antiforgery,
            CatalogIngestionApplicationService ingestionService,
            CancellationToken cancellationToken) =>
        {
            await antiforgery.ValidateRequestAsync(context);
            bool ingested = await ingestionService.IngestAsync(command, cancellationToken);
            return ingested
                ? Results.Ok(new { status = "ingested", sourceId = command.SourceId })
                : Results.Conflict(new { status = "duplicate_snapshot", message = "Source snapshot with identical hash already exists." });
        }).RequireAuthorization(EditorialPolicy);

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
            PublishCatalogRequest request,
            HttpContext context,
            IAntiforgery antiforgery,
            CatalogPublicationApplicationService publicationService,
            CancellationToken cancellationToken) =>
        {
            await antiforgery.ValidateRequestAsync(context);
            Guid actorUserId = AuthenticatedSessionClaims.Read(context.User).UserId;
            PublishVersionCommand command = new(request.VersionTag, actorUserId.ToString("D"));
            var result = await publicationService.PublishAsync(command, cancellationToken);
            return Results.Ok(result);
        }).RequireAuthorization(EditorialPolicy);

        // Map rollback endpoint
        catalog.MapPost("/rollback/{versionId:guid}", async (
            Guid versionId,
            HttpContext context,
            IAntiforgery antiforgery,
            CatalogPublicationApplicationService publicationService,
            CancellationToken cancellationToken) =>
        {
            await antiforgery.ValidateRequestAsync(context);
            bool rolledBack = await publicationService.RollbackAsync(new RollbackVersionCommand(versionId), cancellationToken);
            return rolledBack
                ? Results.Ok(new { status = "rolled_back", versionId })
                : Results.NotFound(new { status = "version_not_found", message = $"Version '{versionId}' not found." });
        }).RequireAuthorization(EditorialPolicy);

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
