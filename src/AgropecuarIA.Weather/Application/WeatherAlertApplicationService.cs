using AgropecuarIA.Weather.Domain;
using AgropecuarIA.Weather.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace AgropecuarIA.Weather.Application;

public sealed record IngestCapAlertCommand(
    string Identifier,
    string Sender,
    DateTimeOffset SentUtc,
    string Status,
    string EventName,
    string Severity,
    string Certainty,
    string Headline,
    string Description,
    string? Instruction,
    string AreaDescription,
    string PolygonCoordinatesJson,
    double MinLatitude,
    double MaxLatitude,
    double MinLongitude,
    double MaxLongitude,
    DateTimeOffset EffectiveUtc,
    DateTimeOffset ExpiresUtc);

public sealed record WeatherAlertDto(
    Guid Id,
    string Identifier,
    string Sender,
    DateTimeOffset SentUtc,
    string Status,
    string EventName,
    string Severity,
    string Certainty,
    string Headline,
    string Description,
    string? Instruction,
    string AreaDescription,
    DateTimeOffset EffectiveUtc,
    DateTimeOffset ExpiresUtc);

public sealed class WeatherAlertApplicationService(WeatherDbContext dbContext)
{
    public async Task<WeatherAlertDto> IngestAlertAsync(
        IngestCapAlertCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var existing = await dbContext.WeatherAlerts
            .FirstOrDefaultAsync(x => x.Identifier == command.Identifier, cancellationToken);

        if (existing is not null)
        {
            if (string.Equals(command.Status, WeatherAlertStatuses.Cancel, StringComparison.OrdinalIgnoreCase))
            {
                existing.Cancel();
                await dbContext.SaveChangesAsync(cancellationToken);
            }

            return ToDto(existing);
        }

        DateTimeOffset now = DateTimeOffset.UtcNow;
        var alert = new WeatherAlert(
            Guid.NewGuid(),
            command.Identifier,
            command.Sender,
            command.SentUtc,
            command.Status,
            command.EventName,
            command.Severity,
            command.Certainty,
            command.Headline,
            command.Description,
            command.Instruction,
            command.AreaDescription,
            command.PolygonCoordinatesJson,
            command.MinLatitude,
            command.MaxLatitude,
            command.MinLongitude,
            command.MaxLongitude,
            command.EffectiveUtc,
            command.ExpiresUtc,
            now);

        dbContext.WeatherAlerts.Add(alert);
        await dbContext.SaveChangesAsync(cancellationToken);

        return ToDto(alert);
    }

    public async Task<IReadOnlyList<WeatherAlertDto>> GetActiveAlertsAsync(
        double latitude,
        double longitude,
        CancellationToken cancellationToken)
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;

        var candidates = await dbContext.WeatherAlerts
            .AsNoTracking()
            .Where(x => x.Status != WeatherAlertStatuses.Cancel &&
                        x.EffectiveUtc <= now &&
                        x.ExpiresUtc >= now &&
                        x.MinLatitude <= latitude &&
                        x.MaxLatitude >= latitude &&
                        x.MinLongitude <= longitude &&
                        x.MaxLongitude >= longitude)
            .OrderByDescending(x => x.Severity == WeatherAlertSeverities.Red ? 3 :
                                    x.Severity == WeatherAlertSeverities.Orange ? 2 : 1)
            .ThenByDescending(x => x.EffectiveUtc)
            .ToListAsync(cancellationToken);

        return candidates.Select(ToDto).ToList();
    }

    private static WeatherAlertDto ToDto(WeatherAlert a) =>
        new(
            a.Id,
            a.Identifier,
            a.Sender,
            a.SentUtc,
            a.Status,
            a.EventName,
            a.Severity,
            a.Certainty,
            a.Headline,
            a.Description,
            a.Instruction,
            a.AreaDescription,
            a.EffectiveUtc,
            a.ExpiresUtc);
}
