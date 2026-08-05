using AgropecuarIA.CapacityPlanningSpike;

namespace AgropecuarIA.CapacityPlanningSpike.Tests;

[TestClass]
public sealed class SloPolicyTests
{
    [TestMethod]
    public void EvaluateCoreMonthlyReturnsExactErrorBudgets()
    {
        var result = SloPolicy.CoreMonthly.Evaluate(1_000_000);

        Assert.AreEqual(0.999m, result.AvailabilityTarget);
        Assert.AreEqual(30, result.WindowDays);
        Assert.AreEqual(2_592m, result.ErrorBudgetSeconds);
        Assert.AreEqual(1_000L, result.BadEvents);
    }

    [TestMethod]
    public void EvaluateTooFewEligibleEventsFloorsBudgetWithoutInventingAnEvent()
    {
        var result = SloPolicy.CoreMonthly.Evaluate(999);

        Assert.AreEqual(0L, result.BadEvents);
    }

    [TestMethod]
    public void EvaluateInvalidPolicyOrTrafficReturnsTypedValidationError()
    {
        var cases = new (SloPolicy Policy, long EligibleEvents)[]
        {
            (new SloPolicy(0m, 30), 1),
            (new SloPolicy(1m, 30), 1),
            (new SloPolicy(0.999m, 0), 1),
            (SloPolicy.CoreMonthly, 0),
        };

        foreach (var item in cases)
        {
            var exception = Assert.ThrowsExactly<CapacityPlanningException>(
                () => item.Policy.Evaluate(item.EligibleEvents));
            Assert.AreEqual(CapacityPlanningErrorCode.InvalidInput, exception.Code);
        }
    }
}
