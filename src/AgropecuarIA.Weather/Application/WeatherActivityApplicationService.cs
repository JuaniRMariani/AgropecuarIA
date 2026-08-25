using AgropecuarIA.Weather.Domain;
using AgropecuarIA.Weather.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace AgropecuarIA.Weather.Application;

public sealed record CreateActivityRuleCommand(
    Guid OrganizationId,
    Guid? FieldId,
    string ActivityType,
    string RuleName,
    decimal? MaxWindSpeedKmh,
    decimal? MinTemperatureCelsius,
    decimal? MaxTemperatureCelsius,
    decimal? MaxPrecipitationProbability,
    decimal? MaxPrecipitationMm,
    decimal? MinRelativeHumidity,
    decimal? MaxRelativeHumidity,
    Guid ActorUserId);

public sealed record ActivityRuleDto(
    Guid Id,
    Guid OrganizationId,
    Guid? FieldId,
    string ActivityType,
    string RuleName,
    decimal? MaxWindSpeedKmh,
    decimal? MinTemperatureCelsius,
    decimal? MaxTemperatureCelsius,
    decimal? MaxPrecipitationProbability,
    decimal? MaxPrecipitationMm,
    decimal? MinRelativeHumidity,
    decimal? MaxRelativeHumidity,
    bool IsEnabled,
    DateTimeOffset CreatedAtUtc);

public sealed record EvaluateActivityConditionsQuery(
    Guid OrganizationId,
    Guid? FieldId,
    string? ActivityType,
    decimal WindSpeedKmh,
    decimal TemperatureCelsius,
    decimal PrecipitationProbability,
    decimal PrecipitationMm,
    decimal RelativeHumidity);

public sealed class WeatherActivityApplicationService(WeatherDbContext dbContext)
{
    public async Task<ActivityRuleDto> CreateRuleAsync(
        CreateActivityRuleCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        DateTimeOffset now = DateTimeOffset.UtcNow;
        var rule = new WeatherActivityRule(
            Guid.NewGuid(),
            command.OrganizationId,
            command.FieldId,
            command.ActivityType,
            command.RuleName,
            command.MaxWindSpeedKmh,
            command.MinTemperatureCelsius,
            command.MaxTemperatureCelsius,
            command.MaxPrecipitationProbability,
            command.MaxPrecipitationMm,
            command.MinRelativeHumidity,
            command.MaxRelativeHumidity,
            command.ActorUserId,
            now);

        dbContext.ActivityRules.Add(rule);
        await dbContext.SaveChangesAsync(cancellationToken);

        return ToDto(rule);
    }

    public async Task<IReadOnlyList<ActivityRuleDto>> ListRulesAsync(
        Guid organizationId,
        Guid? fieldId,
        CancellationToken cancellationToken)
    {
        var query = dbContext.ActivityRules
            .AsNoTracking()
            .Where(x => x.OrganizationId == organizationId);

        if (fieldId.HasValue)
        {
            query = query.Where(x => x.FieldId == null || x.FieldId == fieldId.Value);
        }

        var rules = await query
            .OrderBy(x => x.ActivityType)
            .ThenBy(x => x.RuleName)
            .ToListAsync(cancellationToken);

        return rules.Select(ToDto).ToList();
    }

    public async Task<IReadOnlyList<ActivitySuitabilityResult>> EvaluateSuitabilityAsync(
        EvaluateActivityConditionsQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var rulesQuery = dbContext.ActivityRules
            .AsNoTracking()
            .Where(x => x.OrganizationId == query.OrganizationId && x.IsEnabled);

        if (query.FieldId.HasValue)
        {
            rulesQuery = rulesQuery.Where(x => x.FieldId == null || x.FieldId == query.FieldId.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.ActivityType))
        {
            string act = query.ActivityType.Trim().ToLowerInvariant();
            rulesQuery = rulesQuery.Where(x => x.ActivityType == act);
        }

        var rules = await rulesQuery.ToListAsync(cancellationToken);

        // If no custom rules configured, use standard agronomical defaults
        if (rules.Count == 0 && !string.IsNullOrWhiteSpace(query.ActivityType))
        {
            var defaultRule = GetDefaultRule(query.OrganizationId, query.ActivityType);
            if (defaultRule is not null)
            {
                rules.Add(defaultRule);
            }
        }

        return rules.Select(r => r.Evaluate(
            query.WindSpeedKmh,
            query.TemperatureCelsius,
            query.PrecipitationProbability,
            query.PrecipitationMm,
            query.RelativeHumidity)).ToList();
    }

    private static WeatherActivityRule? GetDefaultRule(Guid organizationId, string activityType) =>
        activityType.Trim().ToLowerInvariant() switch
        {
            WeatherActivityTypes.Pulverizacion => new WeatherActivityRule(
                Guid.NewGuid(), organizationId, null, WeatherActivityTypes.Pulverizacion,
                "Pulverización Estándar (Buenas Prácticas)",
                maxWindSpeedKmh: 15m,
                minTemperatureCelsius: 10m,
                maxTemperatureCelsius: 30m,
                maxPrecipitationProbability: 30m,
                maxPrecipitationMm: 1m,
                minRelativeHumidity: 40m,
                maxRelativeHumidity: 80m,
                Guid.Empty, DateTimeOffset.UtcNow),

            WeatherActivityTypes.Siembra => new WeatherActivityRule(
                Guid.NewGuid(), organizationId, null, WeatherActivityTypes.Siembra,
                "Siembra Estándar",
                maxWindSpeedKmh: 35m,
                minTemperatureCelsius: 5m,
                maxTemperatureCelsius: 38m,
                maxPrecipitationProbability: 60m,
                maxPrecipitationMm: 15m,
                minRelativeHumidity: null,
                maxRelativeHumidity: null,
                Guid.Empty, DateTimeOffset.UtcNow),

            WeatherActivityTypes.Cosecha => new WeatherActivityRule(
                Guid.NewGuid(), organizationId, null, WeatherActivityTypes.Cosecha,
                "Cosecha Estándar",
                maxWindSpeedKmh: 40m,
                minTemperatureCelsius: 0m,
                maxTemperatureCelsius: 40m,
                maxPrecipitationProbability: 20m,
                maxPrecipitationMm: 0.5m,
                minRelativeHumidity: null,
                maxRelativeHumidity: 70m,
                Guid.Empty, DateTimeOffset.UtcNow),

            _ => null
        };

    private static ActivityRuleDto ToDto(WeatherActivityRule r) =>
        new(
            r.Id,
            r.OrganizationId,
            r.FieldId,
            r.ActivityType,
            r.RuleName,
            r.MaxWindSpeedKmh,
            r.MinTemperatureCelsius,
            r.MaxTemperatureCelsius,
            r.MaxPrecipitationProbability,
            r.MaxPrecipitationMm,
            r.MinRelativeHumidity,
            r.MaxRelativeHumidity,
            r.IsEnabled,
            r.CreatedAtUtc);
}
