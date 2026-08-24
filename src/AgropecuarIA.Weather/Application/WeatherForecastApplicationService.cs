using AgropecuarIA.Weather.Domain;
using AgropecuarIA.Weather.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace AgropecuarIA.Weather.Application;

public sealed record WeatherForecastResult(
    string Freshness,
    string Provider,
    string ModelName,
    double CentroidLatitude,
    double CentroidLongitude,
    DateTimeOffset IssuedAtUtc,
    DateTimeOffset ValidUntilUtc,
    string HourlyVariablesJson,
    string DailyVariablesJson,
    bool IsCached);

public sealed record RecordObservedRainCommand(
    Guid OrganizationId,
    Guid FieldId,
    DateTimeOffset ObservedDateUtc,
    decimal AmountMillimeters,
    string Method,
    string? Notes,
    Guid ActorUserId,
    Guid? RectifiedFromId = null);

public sealed record ObservedRainDto(
    Guid Id,
    Guid OrganizationId,
    Guid FieldId,
    DateTimeOffset ObservedDateUtc,
    decimal AmountMillimeters,
    string Method,
    string? Notes,
    Guid RecordedByUserId,
    DateTimeOffset RecordedAtUtc,
    Guid? RectifiedFromId);

public sealed class WeatherForecastApplicationService(
    WeatherDbContext dbContext,
    IWeatherForecastClient forecastClient)
{
    public async Task<WeatherForecastResult> GetForecastAsync(
        double latitude,
        double longitude,
        CancellationToken cancellationToken)
    {
        double lat = Math.Round(latitude, 4);
        double lng = Math.Round(longitude, 4);
        DateTimeOffset now = DateTimeOffset.UtcNow;

        // 1. Check for fresh cached snapshot
        var cached = await dbContext.ForecastSnapshots
            .AsNoTracking()
            .Where(x => Math.Abs(x.CentroidLatitude - lat) < 0.05 &&
                        Math.Abs(x.CentroidLongitude - lng) < 0.05)
            .OrderByDescending(x => x.IssuedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        if (cached is not null && cached.IsFresh(now))
        {
            return new WeatherForecastResult(
                WeatherFreshnessStatuses.Fresh,
                cached.Provider,
                cached.ModelName,
                cached.CentroidLatitude,
                cached.CentroidLongitude,
                cached.IssuedAtUtc,
                cached.ValidUntilUtc,
                cached.HourlyVariablesJson,
                cached.DailyVariablesJson,
                IsCached: true);
        }

        // 2. Fetch from external provider
        var fetched = await forecastClient.FetchForecastAsync(lat, lng, cancellationToken);
        if (fetched is not null)
        {
            // Check if snapshot with this hash already exists (idempotency)
            bool hashExists = await dbContext.ForecastSnapshots
                .AnyAsync(x => x.SnapshotHash == fetched.SnapshotHash, cancellationToken);

            if (!hashExists)
            {
                var snapshot = new WeatherForecastSnapshot(
                    Guid.NewGuid(),
                    lat,
                    lng,
                    fetched.Provider,
                    fetched.ModelName,
                    fetched.IssuedAtUtc,
                    fetched.ValidUntilUtc,
                    fetched.HourlyVariablesJson,
                    fetched.DailyVariablesJson,
                    fetched.SnapshotHash,
                    now);

                dbContext.ForecastSnapshots.Add(snapshot);
                await dbContext.SaveChangesAsync(cancellationToken);
            }

            return new WeatherForecastResult(
                WeatherFreshnessStatuses.Fresh,
                fetched.Provider,
                fetched.ModelName,
                lat,
                lng,
                fetched.IssuedAtUtc,
                fetched.ValidUntilUtc,
                fetched.HourlyVariablesJson,
                fetched.DailyVariablesJson,
                IsCached: false);
        }

        // 3. Degradation / Fallback to stale cached snapshot if available
        if (cached is not null)
        {
            return new WeatherForecastResult(
                WeatherFreshnessStatuses.Stale,
                cached.Provider,
                cached.ModelName,
                cached.CentroidLatitude,
                cached.CentroidLongitude,
                cached.IssuedAtUtc,
                cached.ValidUntilUtc,
                cached.HourlyVariablesJson,
                cached.DailyVariablesJson,
                IsCached: true);
        }

        // 4. Unavailable fallback without breaking calling transactions
        return new WeatherForecastResult(
            WeatherFreshnessStatuses.Unavailable,
            "none",
            "none",
            lat,
            lng,
            now,
            now,
            "{}",
            "{}",
            IsCached: false);
    }

    public async Task<ObservedRainDto> RecordObservedRainAsync(
        RecordObservedRainCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        DateTimeOffset now = DateTimeOffset.UtcNow;
        var observed = new WeatherObservedRain(
            Guid.NewGuid(),
            command.OrganizationId,
            command.FieldId,
            command.ObservedDateUtc,
            command.AmountMillimeters,
            command.Method,
            command.Notes,
            command.ActorUserId,
            now,
            command.RectifiedFromId);

        dbContext.ObservedRains.Add(observed);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new ObservedRainDto(
            observed.Id,
            observed.OrganizationId,
            observed.FieldId,
            observed.ObservedDateUtc,
            observed.AmountMillimeters,
            observed.Method,
            observed.Notes,
            observed.RecordedByUserId,
            observed.RecordedAtUtc,
            observed.RectifiedFromId);
    }

    public async Task<IReadOnlyList<ObservedRainDto>> ListObservedRainAsync(
        Guid organizationId,
        Guid fieldId,
        CancellationToken cancellationToken)
    {
        var records = await dbContext.ObservedRains
            .AsNoTracking()
            .Where(x => x.OrganizationId == organizationId && x.FieldId == fieldId)
            .OrderByDescending(x => x.ObservedDateUtc)
            .ToListAsync(cancellationToken);

        return records.Select(r => new ObservedRainDto(
            r.Id,
            r.OrganizationId,
            r.FieldId,
            r.ObservedDateUtc,
            r.AmountMillimeters,
            r.Method,
            r.Notes,
            r.RecordedByUserId,
            r.RecordedAtUtc,
            r.RectifiedFromId)).ToList();
    }
}
