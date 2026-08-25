using AgropecuarIA.Weather.Domain;

namespace AgropecuarIA.Weather.Tests;

[TestClass]
public sealed class WeatherActivityRuleTests
{
    [TestMethod]
    public void RuleCreationSetsPropertiesAndValidatesActivityType()
    {
        Guid id = Guid.NewGuid();
        Guid orgId = Guid.NewGuid();
        Guid userId = Guid.NewGuid();
        DateTimeOffset now = DateTimeOffset.UtcNow;

        WeatherActivityRule rule = new(
            id,
            orgId,
            null,
            WeatherActivityTypes.Pulverizacion,
            "Pulverización de lote",
            maxWindSpeedKmh: 15m,
            minTemperatureCelsius: 10m,
            maxTemperatureCelsius: 30m,
            maxPrecipitationProbability: 30m,
            maxPrecipitationMm: 1m,
            minRelativeHumidity: 40m,
            maxRelativeHumidity: 80m,
            userId,
            now);

        Assert.AreEqual(id, rule.Id);
        Assert.AreEqual(orgId, rule.OrganizationId);
        Assert.IsNull(rule.FieldId);
        Assert.AreEqual("pulverizacion", rule.ActivityType);
        Assert.AreEqual("Pulverización de lote", rule.RuleName);
        Assert.AreEqual(15m, rule.MaxWindSpeedKmh);
        Assert.IsTrue(rule.IsEnabled);
    }

    [TestMethod]
    public void PulverizationWithHighWindEvaluatesAsNoApta()
    {
        WeatherActivityRule rule = new(
            Guid.NewGuid(),
            Guid.NewGuid(),
            null,
            WeatherActivityTypes.Pulverizacion,
            "Regla Pulverización",
            maxWindSpeedKmh: 15m,
            minTemperatureCelsius: 10m,
            maxTemperatureCelsius: 30m,
            maxPrecipitationProbability: 30m,
            maxPrecipitationMm: 1m,
            minRelativeHumidity: 40m,
            maxRelativeHumidity: 80m,
            Guid.NewGuid(),
            DateTimeOffset.UtcNow);

        // Wind 22 km/h (>15), Temp 32°C (>30) -> 2 risk factors -> no_apta
        ActivitySuitabilityResult result = rule.Evaluate(
            windSpeedKmh: 22m,
            temperatureCelsius: 32m,
            precipitationProbability: 10m,
            precipitationMm: 0m,
            relativeHumidity: 60m);

        Assert.AreEqual(ActivitySuitabilityStatuses.NoApta, result.Status);
        Assert.IsFalse(result.IsSuitable);
        Assert.AreEqual(2, result.RiskFactors.Count);
    }

    [TestMethod]
    public void OptimalConditionsEvaluateAsOptima()
    {
        WeatherActivityRule rule = new(
            Guid.NewGuid(),
            Guid.NewGuid(),
            null,
            WeatherActivityTypes.Pulverizacion,
            "Regla Pulverización",
            maxWindSpeedKmh: 15m,
            minTemperatureCelsius: 10m,
            maxTemperatureCelsius: 30m,
            maxPrecipitationProbability: 30m,
            maxPrecipitationMm: 1m,
            minRelativeHumidity: 40m,
            maxRelativeHumidity: 80m,
            Guid.NewGuid(),
            DateTimeOffset.UtcNow);

        ActivitySuitabilityResult result = rule.Evaluate(
            windSpeedKmh: 8m,
            temperatureCelsius: 22m,
            precipitationProbability: 0m,
            precipitationMm: 0m,
            relativeHumidity: 65m);

        Assert.AreEqual(ActivitySuitabilityStatuses.Optima, result.Status);
        Assert.IsTrue(result.IsSuitable);
        Assert.AreEqual(0, result.RiskFactors.Count);
    }

    [TestMethod]
    public void SingleRiskFactorEvaluatesAsMarginal()
    {
        WeatherActivityRule rule = new(
            Guid.NewGuid(),
            Guid.NewGuid(),
            null,
            WeatherActivityTypes.Cosecha,
            "Regla Cosecha",
            maxWindSpeedKmh: 35m,
            minTemperatureCelsius: 5m,
            maxTemperatureCelsius: 40m,
            maxPrecipitationProbability: 20m,
            maxPrecipitationMm: 1m,
            minRelativeHumidity: null,
            maxRelativeHumidity: null,
            Guid.NewGuid(),
            DateTimeOffset.UtcNow);

        // Precipitation probability 35% (>20%) -> 1 risk factor -> marginal
        ActivitySuitabilityResult result = rule.Evaluate(
            windSpeedKmh: 15m,
            temperatureCelsius: 25m,
            precipitationProbability: 35m,
            precipitationMm: 0m,
            relativeHumidity: 50m);

        Assert.AreEqual(ActivitySuitabilityStatuses.Marginal, result.Status);
        Assert.IsFalse(result.IsSuitable);
        Assert.AreEqual(1, result.RiskFactors.Count);
    }
}
