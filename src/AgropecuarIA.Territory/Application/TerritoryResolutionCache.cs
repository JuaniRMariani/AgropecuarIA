using Microsoft.Extensions.Caching.Memory;

namespace AgropecuarIA.Territory.Application;

public sealed class TerritoryResolutionCache : IDisposable
{
    private readonly MemoryCache cache;

    public TerritoryResolutionCache(TerritoryReferenceOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        cache = new MemoryCache(new MemoryCacheOptions
        {
            SizeLimit = options.ResolutionCacheEntries,
        });
    }

    public bool TryGet(
        double latitude,
        double longitude,
        out CachedTerritoryResolution? resolution) =>
        cache.TryGetValue(new CoordinateCacheKey(latitude, longitude), out resolution);

    public void Set(
        double latitude,
        double longitude,
        ProviderTerritoryResolution resolution,
        DateTimeOffset cachedAtUtc,
        TimeSpan staleFor)
    {
        cache.Set(
            new CoordinateCacheKey(latitude, longitude),
            new CachedTerritoryResolution(resolution, cachedAtUtc),
            new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = staleFor,
                Size = 1,
            });
    }

    public void Dispose() => cache.Dispose();

    private readonly record struct CoordinateCacheKey(long LatitudeBits, long LongitudeBits)
    {
        public CoordinateCacheKey(double latitude, double longitude)
            : this(BitConverter.DoubleToInt64Bits(latitude), BitConverter.DoubleToInt64Bits(longitude))
        {
        }
    }
}

public sealed record CachedTerritoryResolution(
    ProviderTerritoryResolution Resolution,
    DateTimeOffset CachedAtUtc);
