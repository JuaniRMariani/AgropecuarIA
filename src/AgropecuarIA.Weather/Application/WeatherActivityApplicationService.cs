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

        rulesQuery = rulesQuery.Where(x => x.FieldId == null ||
            (query.FieldId.HasValue && x.FieldId == query.FieldId.Value));

        if (!string.IsNullOrWhiteSpace(query.ActivityType))
        {
            string act = query.ActivityType.Trim().ToLowerInvariant();
            rulesQuery = rulesQuery.Where(x => x.ActivityType == act);
        }

        var rules = await rulesQuery.ToListAsync(cancellationToken);

        if (rules.Count == 0)
        {
            return [new ActivitySuitabilityResult(
                query.ActivityType?.Trim().ToLowerInvariant() ?? string.Empty,
                string.Empty,
                ActivitySuitabilityStatuses.InsufficientData,
                false,
                ["No hay una regla habilitada y configurada para esta actividad y alcance; no se evalúa aptitud."],
                ActivitySuitabilityReasonCodes.RuleUnconfigured)];
        }

        return rules.Select(r => r.Evaluate(
            query.WindSpeedKmh,
            query.TemperatureCelsius,
            query.PrecipitationProbability,
            query.PrecipitationMm,
            query.RelativeHumidity)).ToList();
    }

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
