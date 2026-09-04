using AgropecuarIA.Weather.Application;
using AgropecuarIA.Weather.Domain;
using AgropecuarIA.Weather.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace AgropecuarIA.Weather.Tests;

[TestClass]
public sealed class WeatherActivityApplicationTests
{
    [TestMethod]
    [DataRow("pulverizacion")]
    [DataRow("siembra")]
    [DataRow("cosecha")]
    [DataRow("fertilizacion")]
    [DataRow(null)]
    public async Task MissingRuleAbstainsWithoutInventingThresholds(string? activityType)
    {
        await using WeatherDbContext db = CreateContext();
        var service = new WeatherActivityApplicationService(db);

        var results = await service.EvaluateSuitabilityAsync(
            Query(Guid.NewGuid(), Guid.NewGuid(), activityType), CancellationToken.None);

        Assert.HasCount(1, results);
        AssertAbstention(results[0], ActivitySuitabilityReasonCodes.RuleUnconfigured);
        Assert.AreEqual(activityType ?? string.Empty, results[0].ActivityType);
        Assert.AreEqual(string.Empty, results[0].RuleName);
        Assert.AreEqual(0, await db.ActivityRules.CountAsync());
    }

    [TestMethod]
    public async Task RulesOutsideOrganizationFieldActivityOrEnabledScopeCannotProvideFallback()
    {
        await using WeatherDbContext db = CreateContext();
        Guid organizationId = Guid.NewGuid();
        Guid fieldId = Guid.NewGuid();
        var disabled = Rule(organizationId, fieldId);
        disabled.SetEnabled(false);
        db.ActivityRules.AddRange(
            Rule(Guid.NewGuid(), fieldId),
            Rule(organizationId, Guid.NewGuid()),
            Rule(organizationId, fieldId, WeatherActivityTypes.Siembra),
            disabled);
        await db.SaveChangesAsync();

        var results = await new WeatherActivityApplicationService(db).EvaluateSuitabilityAsync(
            Query(organizationId, fieldId), CancellationToken.None);

        Assert.HasCount(1, results);
        AssertAbstention(results[0], ActivitySuitabilityReasonCodes.RuleUnconfigured);
    }

    [TestMethod]
    public async Task MissingFieldContextDoesNotEvaluateAnotherFieldsRule()
    {
        await using WeatherDbContext db = CreateContext();
        Guid organizationId = Guid.NewGuid();
        db.ActivityRules.Add(Rule(organizationId, Guid.NewGuid()));
        await db.SaveChangesAsync();

        var results = await new WeatherActivityApplicationService(db).EvaluateSuitabilityAsync(
            Query(organizationId, null), CancellationToken.None);

        Assert.HasCount(1, results);
        AssertAbstention(results[0], ActivitySuitabilityReasonCodes.RuleUnconfigured);
    }

    [TestMethod]
    public async Task ConfiguredOrganizationRuleIsUsedWithoutInventedFallbacks()
    {
        await using WeatherDbContext db = CreateContext();
        Guid organizationId = Guid.NewGuid();
        db.ActivityRules.Add(Rule(organizationId, null));
        await db.SaveChangesAsync();

        var results = await new WeatherActivityApplicationService(db).EvaluateSuitabilityAsync(
            Query(organizationId, Guid.NewGuid()), CancellationToken.None);

        Assert.HasCount(1, results);
        Assert.AreEqual("Explicit test rule", results[0].RuleName);
        Assert.AreEqual(ActivitySuitabilityStatuses.Marginal, results[0].Status);
        Assert.IsFalse(results[0].IsSuitable);
        Assert.IsNull(results[0].ReasonCode);
        Assert.HasCount(1, results[0].RiskFactors);
    }

    [TestMethod]
    public async Task EmptyThresholdRuleAbstainsInsteadOfReportingOptimalConditions()
    {
        await using WeatherDbContext db = CreateContext();
        Guid organizationId = Guid.NewGuid();
        db.ActivityRules.Add(Rule(organizationId, null, maxWindSpeed: null));
        await db.SaveChangesAsync();

        var results = await new WeatherActivityApplicationService(db).EvaluateSuitabilityAsync(
            Query(organizationId, Guid.NewGuid()), CancellationToken.None);

        Assert.HasCount(1, results);
        AssertAbstention(results[0], ActivitySuitabilityReasonCodes.ThresholdsUnconfigured);
    }

    [TestMethod]
    public void DisabledRuleAbstainsEvenWhenEvaluatedDirectly()
    {
        var rule = Rule(Guid.NewGuid(), null);
        rule.SetEnabled(false);

        AssertAbstention(rule.Evaluate(1m, 20m, 0m, 0m, 50m),
            ActivitySuitabilityReasonCodes.RuleDisabled);
    }

    private static WeatherDbContext CreateContext() => new(
        new DbContextOptionsBuilder<WeatherDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N")).Options);

    private static WeatherActivityRule Rule(Guid organizationId, Guid? fieldId,
        string activityType = WeatherActivityTypes.Pulverizacion, decimal? maxWindSpeed = 5m) => new(
            Guid.NewGuid(), organizationId, fieldId, activityType, "Explicit test rule",
            maxWindSpeed, null, null, null, null, null, null, Guid.NewGuid(), DateTimeOffset.UtcNow);

    private static EvaluateActivityConditionsQuery Query(Guid organizationId, Guid? fieldId,
        string? activityType = WeatherActivityTypes.Pulverizacion) => new(
            organizationId, fieldId, activityType, 8m, 20m, 0m, 0m, 50m);

    private static void AssertAbstention(ActivitySuitabilityResult result, string reasonCode)
    {
        Assert.AreEqual(ActivitySuitabilityStatuses.InsufficientData, result.Status);
        Assert.IsFalse(result.IsSuitable);
        Assert.AreEqual(reasonCode, result.ReasonCode);
        Assert.IsNotEmpty(result.RiskFactors);
    }
}
