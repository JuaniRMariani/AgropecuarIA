using System.Globalization;
using System.Text;
using AgropecuarIA.Territory.Domain;
using Microsoft.Extensions.Options;

namespace AgropecuarIA.Territory.Application;

public sealed class TerritoryReferenceService(
    ITerritoryReferenceReader referenceReader,
    ITerritoryCoordinateProvider coordinateProvider,
    TerritoryResolutionCache resolutionCache,
    TimeProvider timeProvider,
    IOptions<TerritoryReferenceOptions> configuredOptions,
    TerritoryTelemetry telemetry)
{
    private const double MinimumLatitude = -55.2;
    private const double MaximumLatitude = -21.7;
    private const double MinimumLongitude = -73.7;
    private const double MaximumLongitude = -53.5;

    private readonly TerritoryReferenceOptions options = configuredOptions.Value;

    public async Task<TerritorySearchResponse> SearchAsync(
        string? query,
        string? level,
        string? parentCode,
        string? limit,
        CancellationToken cancellationToken)
    {
        string trimmedQuery = query?.Trim() ?? string.Empty;
        int queryLength = trimmedQuery.EnumerateRunes().Count();
        if (queryLength is < 2 or > 80)
        {
            throw TerritoryErrors.InvalidSearchQuery();
        }

        string normalizedQuery = TerritoryNameNormalizer.Normalize(trimmedQuery);
        if (normalizedQuery.EnumerateRunes().Count() is < 2 or > 80)
        {
            throw TerritoryErrors.InvalidSearchQuery();
        }

        string? normalizedLevel = string.IsNullOrWhiteSpace(level)
            ? null
            : level.Trim().ToLowerInvariant();
        if (normalizedLevel is not null && !TerritoryLevels.IsSupported(normalizedLevel))
        {
            throw TerritoryErrors.InvalidLevel();
        }

        string? normalizedParentCode = string.IsNullOrWhiteSpace(parentCode)
            ? null
            : parentCode.Trim();
        if (normalizedParentCode is not null && !IsOfficialCode(normalizedParentCode))
        {
            throw TerritoryErrors.InvalidParentCode();
        }

        int parsedLimit = 10;
        if (limit is not null &&
            (!int.TryParse(limit, NumberStyles.None, CultureInfo.InvariantCulture, out parsedLimit) ||
             parsedLimit is < 1 or > 20))
        {
            throw TerritoryErrors.InvalidLimit();
        }

        TerritoryReferenceSearchPage? page = await referenceReader.SearchAsync(
            new TerritoryReferenceSearchCriteria(
                normalizedQuery,
                normalizedLevel,
                normalizedParentCode,
                parsedLimit),
            cancellationToken);
        if (page is null)
        {
            throw TerritoryErrors.ReferenceUnavailable();
        }

        DateTimeOffset now = timeProvider.GetUtcNow();
        string status = page.Source.CapturedAtUtc <= now &&
            now - page.Source.CapturedAtUtc <= options.SnapshotFreshFor
                ? TerritoryReferenceStatuses.Fresh
                : TerritoryReferenceStatuses.Stale;
        telemetry.RecordSearch(
            status,
            normalizedLevel ?? "all",
            page.Items.Count,
            now - page.Source.CapturedAtUtc);

        return new TerritorySearchResponse(
            status,
            ToResponse(page.Source),
            page.Items.Select(ToResponse).ToArray());
    }

    public async Task<TerritoryResolveResponse> ResolveAsync(
        string? latitude,
        string? longitude,
        CancellationToken cancellationToken)
    {
        if (!TryParseCoordinate(latitude, out double parsedLatitude) ||
            !TryParseCoordinate(longitude, out double parsedLongitude) ||
            parsedLatitude is < MinimumLatitude or > MaximumLatitude ||
            parsedLongitude is < MinimumLongitude or > MaximumLongitude)
        {
            throw TerritoryErrors.InvalidCoordinates();
        }

        if (!options.CoordinateResolutionEnabled)
        {
            telemetry.RecordResolve(TerritoryReferenceStatuses.Unavailable, "disabled", "georef");
            return UnavailableResponse();
        }

        DateTimeOffset now = timeProvider.GetUtcNow();
        CachedTerritoryResolution? staleCandidate = null;
        if (resolutionCache.TryGet(parsedLatitude, parsedLongitude, out CachedTerritoryResolution? cached) &&
            cached is not null)
        {
            if (now >= cached.CachedAtUtc && now - cached.CachedAtUtc <= options.ResolutionFreshFor)
            {
                telemetry.RecordResolve(
                    TerritoryReferenceStatuses.Fresh,
                    "cache",
                    cached.Resolution.Source.Provider);
                return ToResolveResponse(TerritoryReferenceStatuses.Fresh, cached.Resolution);
            }

            if (now >= cached.CachedAtUtc && now - cached.CachedAtUtc <= options.ResolutionStaleFor)
            {
                staleCandidate = cached;
            }
        }

        try
        {
            ProviderTerritoryResolution? resolved = await coordinateProvider.ResolveAsync(
                parsedLatitude,
                parsedLongitude,
                cancellationToken);
            if (resolved is not null)
            {
                resolutionCache.Set(
                    parsedLatitude,
                    parsedLongitude,
                    resolved,
                    now,
                    options.ResolutionStaleFor);
                telemetry.RecordResolve(
                    TerritoryReferenceStatuses.Fresh,
                    "provider",
                    resolved.Source.Provider);
                return ToResolveResponse(TerritoryReferenceStatuses.Fresh, resolved);
            }
        }
        catch (TerritoryProviderException)
        {
            // Provider failures are represented as explicit degradation below.
        }

        if (staleCandidate is not null)
        {
            telemetry.RecordResolve(
                TerritoryReferenceStatuses.Stale,
                "cache",
                staleCandidate.Resolution.Source.Provider);
            return ToResolveResponse(TerritoryReferenceStatuses.Stale, staleCandidate.Resolution);
        }

        telemetry.RecordResolve(TerritoryReferenceStatuses.Unavailable, "none", "georef");
        return UnavailableResponse();
    }

    private static TerritoryResolveResponse UnavailableResponse() => new(
        TerritoryReferenceStatuses.Unavailable,
        null,
        null,
        new TerritoryFallbackResponse(SearchAvailable: true));

    private static TerritoryResolveResponse ToResolveResponse(
        string status,
        ProviderTerritoryResolution resolution) => new(
            status,
            ToResponse(resolution.Source),
            ToResponse(resolution.Unit),
            new TerritoryFallbackResponse(SearchAvailable: true));

    private static TerritorySourceResponse ToResponse(TerritoryReferenceSource source) => new(
        source.Provider,
        source.Version,
        source.CapturedAtUtc);

    private static TerritoryUnitResponse ToResponse(TerritoryReferenceMatch unit) => new(
        unit.OfficialCode,
        unit.Name,
        unit.Level,
        unit.ParentCode,
        unit.ParentName,
        unit.HierarchyLabel);

    private static bool TryParseCoordinate(string? value, out double coordinate) =>
        double.TryParse(
            value,
            NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint,
            CultureInfo.InvariantCulture,
            out coordinate) &&
        double.IsFinite(coordinate);

    private static bool IsOfficialCode(string value) =>
        value.Length is >= 2 and <= 16 && value.All(char.IsAsciiDigit);
}
