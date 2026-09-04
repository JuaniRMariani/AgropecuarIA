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
    public sealed record PublishCatalogRequest(string VersionTag, string CandidateHash);

    public static IEndpointRouteBuilder MapCatalogEndpoints(this IEndpointRouteBuilder endpoints)
    {
        RouteGroupBuilder catalog = endpoints.MapGroup("/api/catalog")
            .RequireAuthorization()
            .RequireRateLimiting(RateLimitPolicy);
        catalog.AddEndpointFilter(async (context, next) =>
        {
            context.HttpContext.Response.Headers.CacheControl = "no-store, private";
            try { return await next(context); }
            catch (CatalogOperationException error)
            {
                return Results.Problem(statusCode: error.StatusCode, title: error.Message,
                    extensions: new Dictionary<string, object?> { ["code"] = error.Code, ["retryable"] = false });
            }
        });

        // Map ingestion endpoint
        catalog.MapPost("/ingest", async (
            IngestSourceCommand command,
            HttpContext context,
            IAntiforgery antiforgery,
            CatalogIngestionApplicationService ingestionService,
            CancellationToken cancellationToken) =>
        {
            await antiforgery.ValidateRequestAsync(context);
            bool ingested = await ingestionService.IngestAsync(command, EditorialContext(context), cancellationToken);
            return ingested
                ? Results.Ok(new { status = "ingested", sourceId = command.SourceId })
                : Results.Problem(statusCode: 409, title: "Source snapshot with identical hash already exists.", extensions: new Dictionary<string, object?> { ["code"] = "catalog.duplicate_snapshot" });
        }).RequireAuthorization(EditorialPolicy);

        // Map diff endpoint
        catalog.MapGet("/diff", async (
            CatalogDiffApplicationService diffService,
            CancellationToken cancellationToken) =>
        {
            var report = await diffService.GenerateDiffAsync(cancellationToken);
            return Results.Ok(report);
        }).RequireAuthorization(EditorialPolicy);

        // Map publish endpoint
        catalog.MapPost("/publish", async (
            PublishCatalogRequest request,
            HttpContext context,
            IAntiforgery antiforgery,
            CatalogPublicationApplicationService publicationService,
            CancellationToken cancellationToken) =>
        {
            await antiforgery.ValidateRequestAsync(context);
            PublishVersionCommand command = new(request.VersionTag, request.CandidateHash);
            var result = await publicationService.PublishAsync(command, EditorialContext(context), cancellationToken);
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
            bool rolledBack = await publicationService.RollbackAsync(new RollbackVersionCommand(versionId), EditorialContext(context), cancellationToken);
            return rolledBack
                ? Results.Ok(new { status = "rolled_back", versionId })
                : throw CatalogErrors.VersionNotFound();
        }).RequireAuthorization(EditorialPolicy);

        // Map search items endpoint
        catalog.MapGet("/items", async (
            string? query,
            string? jurisdiction,
            string? category,
            string? supportLevel,
            int? limit,
            Guid? versionId,
            CatalogSearchApplicationService searchService,
            CancellationToken cancellationToken) =>
        {
            var result = await searchService.SearchAsync(
                new SearchCatalogQuery(query, jurisdiction, category, supportLevel, limit ?? 50, versionId),
                cancellationToken);
            return Results.Ok(result);
        });

        // Map item detail endpoint
        catalog.MapGet("/items/{code}", async (
            string code,
            Guid? versionId,
            CatalogSearchApplicationService searchService,
            CancellationToken cancellationToken) =>
        {
            var item = await searchService.GetItemByCodeAsync(code, cancellationToken, versionId);
            return item is not null
                ? Results.Ok(item)
                : throw CatalogErrors.ItemNotFound();
        });

        catalog.MapGet("/versions", async (int? limit, int? offset, CatalogSearchApplicationService searchService, CancellationToken cancellationToken) =>
            Results.Ok(await searchService.ListVersionsAsync(limit ?? 20, offset ?? 0, cancellationToken)));

        return endpoints;
    }

    private static CatalogEditorialContext EditorialContext(HttpContext context)
    {
        var session = AuthenticatedSessionClaims.Read(context.User);
        return new(session.UserId, session.SessionId, context.TraceIdentifier);
    }
}
