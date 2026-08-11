using AgropecuarIA.Territory.Application;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AgropecuarIA.Territory.Delivery;

public static class TerritoryEndpoints
{
    public const string RateLimitPolicy = "territory";

    public static IEndpointRouteBuilder MapTerritoryEndpoints(this IEndpointRouteBuilder endpoints)
    {
        RouteGroupBuilder territory = endpoints.MapGroup("/api/territory")
            .RequireAuthorization()
            .RequireRateLimiting(RateLimitPolicy);

        territory.MapGet("/search", async (
            string? query,
            string? level,
            string? parentCode,
            string? limit,
            TerritoryReferenceService service,
            CancellationToken cancellationToken) =>
        {
            TerritorySearchResponse result = await service.SearchAsync(
                query,
                level,
                parentCode,
                limit,
                cancellationToken);
            return TypedResults.Ok(result);
        });

        territory.MapGet("/resolve", async (
            string? latitude,
            string? longitude,
            TerritoryReferenceService service,
            CancellationToken cancellationToken) =>
        {
            TerritoryResolveResponse result = await service.ResolveAsync(
                latitude,
                longitude,
                cancellationToken);
            return TypedResults.Ok(result);
        });

        return endpoints;
    }
}
