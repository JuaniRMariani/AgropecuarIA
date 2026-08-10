using AgropecuarIA.Identity.Domain;

namespace AgropecuarIA.Identity.Tests;

[TestClass]
public sealed class StepUpAttemptTests
{
    private static readonly DateTimeOffset StartedAtUtc =
        new(2026, 8, 10, 18, 0, 0, TimeSpan.Zero);

    [TestMethod]
    public void ConstructorRejectsUnknownPurpose()
    {
        Assert.ThrowsExactly<ArgumentException>(() => new StepUpAttempt(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "arbitrary_action",
            StartedAtUtc,
            StartedAtUtc.AddMinutes(5)));
    }

    [TestMethod]
    public void AttemptCanOnlyBeConsumedOnce()
    {
        StepUpAttempt attempt = CreateAttempt();

        attempt.Consume(StartedAtUtc.AddMinutes(1));

        Assert.AreEqual(StartedAtUtc.AddMinutes(1), attempt.ConsumedAtUtc);
        Assert.ThrowsExactly<InvalidOperationException>(() =>
            attempt.Consume(StartedAtUtc.AddMinutes(2)));
    }

    [TestMethod]
    public void ConsumptionCannotPredateAttempt()
    {
        StepUpAttempt attempt = CreateAttempt();

        Assert.ThrowsExactly<ArgumentException>(() =>
            attempt.Consume(StartedAtUtc.AddSeconds(-1)));
        Assert.IsNull(attempt.ConsumedAtUtc);
    }

    [TestMethod]
    public void ExpiredAttemptCannotBeConsumed()
    {
        StepUpAttempt attempt = CreateAttempt();

        Assert.ThrowsExactly<ArgumentException>(() =>
            attempt.Consume(StartedAtUtc.AddMinutes(5)));
        Assert.IsNull(attempt.ConsumedAtUtc);
    }

    private static StepUpAttempt CreateAttempt() =>
        new(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            StepUpPurposes.ManageAuthenticationMethods,
            StartedAtUtc,
            StartedAtUtc.AddMinutes(5));
}
