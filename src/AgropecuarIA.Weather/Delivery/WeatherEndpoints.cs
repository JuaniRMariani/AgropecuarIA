using AgropecuarIA.Identity.Application;
using AgropecuarIA.Identity.Delivery;
using AgropecuarIA.Weather.Application;
using AgropecuarIA.Weather.Domain;
using AgropecuarIA.Weather.Infrastructure;
using Microsoft.AspNetCore.Antiforgery;
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

public sealed record CreateActivityRuleRequest(
    Guid? FieldId,
    string ActivityType,
    string RuleName,
    decimal? MaxWindSpeedKmh,
    decimal? MinTemperatureCelsius,
    decimal? MaxTemperatureCelsius,
    decimal? MaxPrecipitationProbability,
    decimal? MaxPrecipitationMm,
    decimal? MinRelativeHumidity,
    decimal? MaxRelativeHumidity);

public static class WeatherEndpoints
{
    public const string RateLimitPolicy = "weather";
    public const string AlertIngestionPolicy = "WeatherAlertIngestion";

    public static IEndpointRouteBuilder MapWeatherEndpoints(this IEndpointRouteBuilder endpoints)
    {
        RouteGroupBuilder orgWeather = endpoints.MapGroup("/api/organizations/{organizationId:guid}/weather")
            .RequireAuthorization()
            .RequireRateLimiting(RateLimitPolicy);
        orgWeather.AddEndpointFilter<WeatherResourceFilter>();

        orgWeather.MapPost("/rules", async (
            Guid organizationId,
            CreateActivityRuleRequest request,
            HttpContext context,
            WeatherActivityApplicationService service,
            CancellationToken cancellationToken) =>
        {
            AuthenticatedSession session = AuthenticatedSessionClaims.Read(context.User);
            var rule = await service.CreateRuleAsync(
                new CreateActivityRuleCommand(
                    organizationId,
                    request.FieldId,
                    request.ActivityType,
                    request.RuleName,
                    request.MaxWindSpeedKmh,
                    request.MinTemperatureCelsius,
                    request.MaxTemperatureCelsius,
                    request.MaxPrecipitationProbability,
                    request.MaxPrecipitationMm,
                    request.MinRelativeHumidity,
                    request.MaxRelativeHumidity,
                    session.UserId),
                cancellationToken);

            return Results.Created($"/api/organizations/{organizationId:D}/weather/rules/{rule.Id:D}", rule);
        });

        orgWeather.MapGet("/rules", async (
            Guid organizationId,
            Guid? fieldId,
            WeatherActivityApplicationService service,
            CancellationToken cancellationToken) =>
        {
            var rules = await service.ListRulesAsync(organizationId, fieldId, cancellationToken);
            return Results.Ok(rules);
        });

        RouteGroupBuilder weather = endpoints.MapGroup("/api/organizations/{organizationId:guid}/fields/{fieldId:guid}/weather")
            .RequireAuthorization()
            .RequireRateLimiting(RateLimitPolicy);
        weather.AddEndpointFilter<WeatherResourceFilter>();

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

        weather.MapGet("/alerts", async (
            Guid organizationId,
            Guid fieldId,
            double latitude,
            double longitude,
            WeatherAlertApplicationService service,
            CancellationToken cancellationToken) =>
        {
            var alerts = await service.GetActiveAlertsAsync(latitude, longitude, cancellationToken);
            return Results.Ok(alerts);
        });

        weather.MapGet("/suitability", async (
            Guid organizationId,
            Guid fieldId,
            string? activityType,
            decimal windSpeedKmh,
            decimal temperatureCelsius,
            decimal precipitationProbability,
            decimal precipitationMm,
            decimal relativeHumidity,
            WeatherActivityApplicationService service,
            CancellationToken cancellationToken) =>
        {
            var evaluations = await service.EvaluateSuitabilityAsync(
                new EvaluateActivityConditionsQuery(
                    organizationId,
                    fieldId,
                    activityType,
                    windSpeedKmh,
                    temperatureCelsius,
                    precipitationProbability,
                    precipitationMm,
                    relativeHumidity),
                cancellationToken);

            return Results.Ok(evaluations);
        });

        RouteGroupBuilder globalWeather = endpoints.MapGroup("/api/weather")
            .RequireAuthorization(AlertIngestionPolicy)
            .RequireRateLimiting(RateLimitPolicy);

        globalWeather.MapPost("/alerts/ingest", async (
            IngestCapAlertCommand request,
            HttpContext context,
            IAntiforgery antiforgery,
            WeatherDbContext database,
            WeatherAlertApplicationService service,
            CancellationToken cancellationToken) =>
        {
            await antiforgery.ValidateRequestAsync(context);
            await using var transaction = await WeatherRequestScope.BeginEditorialAsync(database, cancellationToken);
            var alert = await service.IngestAlertAsync(request, cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return Results.Created($"/api/weather/alerts/{alert.Id:D}", alert);
        });

        return endpoints;
    }
}
