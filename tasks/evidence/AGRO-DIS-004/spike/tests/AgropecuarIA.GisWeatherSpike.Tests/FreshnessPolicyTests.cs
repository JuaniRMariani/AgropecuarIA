namespace AgropecuarIA.GisWeatherSpike.Tests;

[TestClass]
public sealed class FreshnessPolicyTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 5, 12, 0, 0, TimeSpan.Zero);
    private readonly FixedFreshnessPolicy _policy = new(TimeSpan.FromHours(1));

    [TestMethod]
    public void ClassifyRecentValidValueIsFresh()
    {
        var actual = _policy.Classify(new FreshnessContext(
            Now,
            Now.AddMinutes(-30),
            Now.AddHours(1),
            HasUsableValue: true,
            Error: null));

        Assert.AreEqual(WeatherAvailability.Fresh, actual);
    }

    [TestMethod]
    public void ClassifyOldOrExpiredValueIsStale()
    {
        var old = _policy.Classify(new FreshnessContext(
            Now,
            Now.AddHours(-2),
            Now.AddHours(1),
            HasUsableValue: true,
            Error: null));
        var expired = _policy.Classify(new FreshnessContext(
            Now,
            Now.AddMinutes(-10),
            Now.AddSeconds(-1),
            HasUsableValue: true,
            Error: null));

        Assert.AreEqual(WeatherAvailability.Stale, old);
        Assert.AreEqual(WeatherAvailability.Stale, expired);
    }

    [TestMethod]
    public void ClassifyMissingValueErrorOrFutureIngestionIsUnavailable()
    {
        var missing = _policy.Classify(new FreshnessContext(Now, Now, Now.AddHours(1), false, null));
        var providerError = _policy.Classify(new FreshnessContext(
            Now,
            Now,
            Now.AddHours(1),
            true,
            new ProviderError(ProviderErrorCode.ProviderError, "failure")));
        var future = _policy.Classify(new FreshnessContext(Now, Now.AddMinutes(6), Now.AddHours(1), true, null));

        Assert.AreEqual(WeatherAvailability.Unavailable, missing);
        Assert.AreEqual(WeatherAvailability.Unavailable, providerError);
        Assert.AreEqual(WeatherAvailability.Unavailable, future);
    }
}
