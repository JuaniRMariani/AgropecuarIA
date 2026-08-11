using AgropecuarIA.Territory.Application;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace AgropecuarIA.Territory.Tests;

[TestClass]
public sealed class TerritoryReferenceServiceTests
{
    private static readonly DateTimeOffset Now =
        new(2026, 8, 11, 12, 0, 0, TimeSpan.Zero);

    [TestMethod]
    public async Task SearchNormalizesQueryAndReturnsDeterministicSourceAndItems()
    {
        FakeTerritoryReferenceReader reader = new()
        {
            Result = new TerritoryReferenceSearchPage(
                new TerritoryReferenceSource("georef", "fixture-1", Now.AddDays(-1)),
                [new("62", "Río Negro", "province", null, null, "Río Negro")]),
        };
        await using ServiceFixture fixture = new(reader, new FakeCoordinateProvider(), Now);

        TerritorySearchResponse response = await fixture.Service.SearchAsync(
            "  RÍO   negro ",
            "PROVINCE",
            null,
            null,
            CancellationToken.None);

        Assert.AreEqual("rio negro", reader.LastCriteria!.NormalizedQuery);
        Assert.AreEqual("province", reader.LastCriteria.Level);
        Assert.AreEqual(10, reader.LastCriteria.Limit);
        Assert.AreEqual(TerritoryReferenceStatuses.Fresh, response.Status);
        Assert.AreEqual("62", response.Items.Single().OfficialCode);
    }

    [TestMethod]
    public async Task SearchWithoutActiveSnapshotFailsWithTyped503()
    {
        await using ServiceFixture fixture = new(
            new FakeTerritoryReferenceReader(),
            new FakeCoordinateProvider(),
            Now);

        TerritoryOperationException exception =
            await Assert.ThrowsExactlyAsync<TerritoryOperationException>(() =>
                fixture.Service.SearchAsync("rio", null, null, null, CancellationToken.None));

        Assert.AreEqual("territory.reference_unavailable", exception.Code);
        Assert.AreEqual(503, exception.StatusCode);
        Assert.IsTrue(exception.Retryable);
    }

    [DataRow("a", null, null, null, "territory.invalid_search_query")]
    [DataRow("rio", "country", null, null, "territory.invalid_level")]
    [DataRow("rio", null, "AR-06", null, "territory.invalid_parent_code")]
    [DataRow("rio", null, null, "21", "territory.invalid_limit")]
    [TestMethod]
    public async Task SearchRejectsInvalidFiltersBeforeReading(
        string query,
        string? level,
        string? parentCode,
        string? limit,
        string expectedCode)
    {
        FakeTerritoryReferenceReader reader = new();
        await using ServiceFixture fixture = new(reader, new FakeCoordinateProvider(), Now);

        TerritoryOperationException exception =
            await Assert.ThrowsExactlyAsync<TerritoryOperationException>(() =>
                fixture.Service.SearchAsync(query, level, parentCode, limit, CancellationToken.None));

        Assert.AreEqual(expectedCode, exception.Code);
        Assert.IsNull(reader.LastCriteria);
    }

    [TestMethod]
    public async Task ResolveUsesProviderThenFallsBackToStaleCacheWithoutSleep()
    {
        MutableTimeProvider clock = new(Now);
        FakeCoordinateProvider provider = new()
        {
            Result = Resolution(Now),
        };
        await using ServiceFixture fixture = new(
            new FakeTerritoryReferenceReader(),
            provider,
            clock);

        TerritoryResolveResponse fresh = await fixture.Service.ResolveAsync(
            "-34.61",
            "-58.44",
            CancellationToken.None);
        clock.Advance(TimeSpan.FromMinutes(16));
        provider.Exception = new TerritoryProviderException("offline");
        TerritoryResolveResponse stale = await fixture.Service.ResolveAsync(
            "-34.61",
            "-58.44",
            CancellationToken.None);

        Assert.AreEqual(TerritoryReferenceStatuses.Fresh, fresh.Status);
        Assert.AreEqual(TerritoryReferenceStatuses.Stale, stale.Status);
        Assert.AreEqual("02000", stale.Unit!.OfficialCode);
        Assert.AreEqual(2, provider.Calls);
    }

    [TestMethod]
    public async Task ResolveWithoutProviderOrCacheReturnsExplicitUnavailableFallback()
    {
        await using ServiceFixture fixture = new(
            new FakeTerritoryReferenceReader(),
            new FakeCoordinateProvider
            {
                Exception = new TerritoryProviderException("offline"),
            },
            Now);

        TerritoryResolveResponse response = await fixture.Service.ResolveAsync(
            "-34.61",
            "-58.44",
            CancellationToken.None);

        Assert.AreEqual(TerritoryReferenceStatuses.Unavailable, response.Status);
        Assert.IsNull(response.Source);
        Assert.IsNull(response.Unit);
        Assert.IsTrue(response.Fallback.SearchAvailable);
    }

    [TestMethod]
    public async Task ResolveProviderIsDisabledByDefaultAndNeverEgresses()
    {
        FakeCoordinateProvider provider = new();
        await using ServiceFixture fixture = new(
            new FakeTerritoryReferenceReader(),
            provider,
            Now,
            coordinateResolutionEnabled: false);

        TerritoryResolveResponse response = await fixture.Service.ResolveAsync(
            "-34.61",
            "-58.44",
            CancellationToken.None);

        Assert.AreEqual(TerritoryReferenceStatuses.Unavailable, response.Status);
        Assert.AreEqual(0, provider.Calls);
    }

    [DataRow("NaN", "-58.44")]
    [DataRow("-90", "-58.44")]
    [DataRow("-34.61", "-52")]
    [DataRow("-21.69", "-58.44")]
    [TestMethod]
    public async Task ResolveRejectsNonFiniteOrOutsideContinentalBounds(
        string latitude,
        string longitude)
    {
        FakeCoordinateProvider provider = new();
        await using ServiceFixture fixture = new(
            new FakeTerritoryReferenceReader(),
            provider,
            Now);

        TerritoryOperationException exception =
            await Assert.ThrowsExactlyAsync<TerritoryOperationException>(() =>
                fixture.Service.ResolveAsync(latitude, longitude, CancellationToken.None));

        Assert.AreEqual("territory.invalid_coordinates", exception.Code);
        Assert.AreEqual(0, provider.Calls);
    }

    private static ProviderTerritoryResolution Resolution(DateTimeOffset capturedAt) => new(
        new TerritoryReferenceSource("georef", "2.0", capturedAt),
        new TerritoryReferenceMatch(
            "02000",
            "Comuna 1",
            "department",
            "02",
            "Ciudad Autónoma de Buenos Aires",
            "Comuna 1, Ciudad Autónoma de Buenos Aires"));

    private sealed class ServiceFixture : IAsyncDisposable
    {
        private readonly TerritoryResolutionCache cache;
        private readonly ServiceProvider telemetryServices;

        public ServiceFixture(
            ITerritoryReferenceReader reader,
            ITerritoryCoordinateProvider provider,
            DateTimeOffset now,
            bool coordinateResolutionEnabled = true)
            : this(reader, provider, new MutableTimeProvider(now), coordinateResolutionEnabled)
        {
        }

        public ServiceFixture(
            ITerritoryReferenceReader reader,
            ITerritoryCoordinateProvider provider,
            TimeProvider timeProvider,
            bool coordinateResolutionEnabled = true)
        {
            TerritoryReferenceOptions options = new()
            {
                CoordinateResolutionEnabled = coordinateResolutionEnabled,
            };
            cache = new TerritoryResolutionCache(options);
            ServiceCollection services = new();
            services.AddMetrics();
            telemetryServices = services.BuildServiceProvider();
            Service = new TerritoryReferenceService(
                reader,
                provider,
                cache,
                timeProvider,
                Options.Create(options),
                new TerritoryTelemetry(
                    telemetryServices.GetRequiredService<System.Diagnostics.Metrics.IMeterFactory>()));
        }

        public TerritoryReferenceService Service { get; }

        public ValueTask DisposeAsync()
        {
            cache.Dispose();
            telemetryServices.Dispose();
            return ValueTask.CompletedTask;
        }
    }

    private sealed class FakeTerritoryReferenceReader : ITerritoryReferenceReader
    {
        public TerritoryReferenceSearchPage? Result { get; init; }

        public TerritoryReferenceSearchCriteria? LastCriteria { get; private set; }

        public Task<TerritoryReferenceSearchPage?> SearchAsync(
            TerritoryReferenceSearchCriteria criteria,
            CancellationToken cancellationToken)
        {
            LastCriteria = criteria;
            return Task.FromResult(Result);
        }
    }

    private sealed class FakeCoordinateProvider : ITerritoryCoordinateProvider
    {
        public ProviderTerritoryResolution? Result { get; init; }

        public TerritoryProviderException? Exception { get; set; }

        public int Calls { get; private set; }

        public Task<ProviderTerritoryResolution?> ResolveAsync(
            double latitude,
            double longitude,
            CancellationToken cancellationToken)
        {
            Calls++;
            if (Exception is not null)
            {
                throw Exception;
            }

            return Task.FromResult(Result);
        }
    }

    private sealed class MutableTimeProvider(DateTimeOffset now) : TimeProvider
    {
        private DateTimeOffset utcNow = now;

        public override DateTimeOffset GetUtcNow() => utcNow;

        public void Advance(TimeSpan elapsed) => utcNow += elapsed;
    }
}
