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
        catalog.MapPost("/ingest", (HttpContext context) =>
        {
            return Results.Ok(new { message = "Catalog ingested" });
        });

        // Map diff endpoint
        catalog.MapGet("/diff", (HttpContext context) =>
        {
            return Results.Ok(new { message = "Editorial diff generated" });
        });

        return endpoints;
    }
}