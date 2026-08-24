using AgropecuarIA.Identity.Application;
using AgropecuarIA.Identity.Delivery;
using AgropecuarIA.Weather.Application;
using AgropecuarIA.Weather.Domain;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AgropecuarIA.Weather.Delivery;

public sealed record RecordObservedRainRequest(
    DateTimeOffset ObservedDateUtc,
    decimal AmountMillimeters,
    string? Method,
    string? Notes,
    Guid? RectifiedFromId);

public static class WeatherEndpoints
{
    public const string RateLimitPolicy = "weather";

    public static IEndpointRouteBuilder MapWeatherEndpoints(this IEndpointRouteBuilder endpoints)
    {
        RouteGroupBuilder weather = endpoints.MapGroup("/api/organizations/{organizationId:guid}/fields/{fieldId:guid}/weather")
            .RequireAuthorization()
            .RequireRateLimiting(RateLimitPolicy);

        weather.MapGet("/forecast", async (
            Guid organizationId,
            Guid fieldId,
            double latitude,
            double longitude,
            WeatherForecastApplicationService service,
            CancellationToken cancellationToken) =>
        {
            var result = await service.GetForecastAsync(latitude, longitude, cancellationToken);
            return Results.Ok(result);
        });

        weather.MapPost("/rain", async (
            Guid organizationId,
            Guid fieldId,
            RecordObservedRainRequest request,
            HttpContext context,
            WeatherForecastApplicationService service,
            CancellationToken cancellationToken) =>
        {
            AuthenticatedSession session = AuthenticatedSessionClaims.Read(context.User);
            var result = await service.RecordObservedRainAsync(
                new RecordObservedRainCommand(
                    organizationId,
                    fieldId,
                    request.ObservedDateUtc,
                    request.AmountMillimeters,
                    request.Method ?? WeatherObservedRainMethods.ManualPluviometer,
                    request.Notes,
                    session.UserId,
                    request.RectifiedFromId),
                cancellationToken);

            return Results.Created(
                $"/api/organizations/{organizationId:D}/fields/{fieldId:D}/weather/rain/{result.Id:D}",
                result);
        });

        weather.MapGet("/rain", async (
            Guid organizationId,
            Guid fieldId,
            WeatherForecastApplicationService service,
            CancellationToken cancellationToken) =>
        {
            var results = await service.ListObservedRainAsync(organizationId, fieldId, cancellationToken);
            return Results.Ok(results);
        });

        return endpoints;
    }
}
