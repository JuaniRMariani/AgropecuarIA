using AgropecuarIA.Weather.Domain;

namespace AgropecuarIA.Weather.Tests;

[TestClass]
public sealed class WeatherAlertTests
{
    [TestMethod]
    public void AlertCreationSetsPropertiesCorrectly()
    {
        Guid id = Guid.NewGuid();
        DateTimeOffset now = DateTimeOffset.UtcNow;
        DateTimeOffset effective = now.AddHours(-1);
        DateTimeOffset expires = now.AddHours(5);

        WeatherAlert alert = new(
            id,
            "SMN-2026-08-24-001",
            "smn.gob.ar",
            now,
            WeatherAlertStatuses.Actual,
            "Tormentas Fuertes",
            WeatherAlertSeverities.Orange,
            "Likely",
            "Alerta Naranja por Tormentas",
            "Se prevén tormentas de variada intensidad",
            "Evitar actividades al aire libre",
            "Norte de Buenos Aires",
            "[]",
            minLatitude: -35.0,
            maxLatitude: -33.0,
            minLongitude: -61.0,
            maxLongitude: -59.0,
            effectiveUtc: effective,
            expiresUtc: expires,
            createdAtUtc: now);

        Assert.AreEqual(id, alert.Id);
        Assert.AreEqual("SMN-2026-08-24-001", alert.Identifier);
        Assert.AreEqual("Tormentas Fuertes", alert.EventName);
        Assert.AreEqual("orange", alert.Severity);
        Assert.AreEqual("actual", alert.Status);
        Assert.IsTrue(alert.IsActive(now));
        Assert.IsTrue(alert.CoversLocation(-34.0, -60.0));
        Assert.IsFalse(alert.CoversLocation(-32.0, -60.0));
    }

    [TestMethod]
    public void InactiveAlertBeforeEffectiveOrAfterExpiry()
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        DateTimeOffset futureEffective = now.AddHours(2);
        DateTimeOffset futureExpires = now.AddHours(6);

        WeatherAlert alert = new(
            Guid.NewGuid(),
            "SMN-002",
            "smn.gob.ar",
            now,
            WeatherAlertStatuses.Actual,
            "Viento",
            WeatherAlertSeverities.Yellow,
            "Possible",
            "Alerta Amarilla",
            "Vientos fuertes",
            null,
            "Costa Atlántica",
            "[]",
            -39.0, -37.0, -58.0, -56.0,
            futureEffective,
            futureExpires,
            now);

        Assert.IsFalse(alert.IsActive(now));
        Assert.IsTrue(alert.IsActive(futureEffective.AddMinutes(30)));
        Assert.IsFalse(alert.IsActive(futureExpires.AddMinutes(1)));
    }

    [TestMethod]
    public void CancelledAlertIsNotActive()
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        WeatherAlert alert = new(
            Guid.NewGuid(),
            "SMN-003",
            "smn.gob.ar",
            now,
            WeatherAlertStatuses.Actual,
            "Granizo",
            WeatherAlertSeverities.Red,
            "Observed",
            "Alerta Roja",
            "Caída de granizo",
            null,
            "Córdoba Sur",
            "[]",
            -34.0, -32.0, -64.0, -62.0,
            now.AddHours(-1),
            now.AddHours(2),
            now);

        Assert.IsTrue(alert.IsActive(now));
        alert.Cancel();
        Assert.IsFalse(alert.IsActive(now));
        Assert.AreEqual(WeatherAlertStatuses.Cancel, alert.Status);
    }

    [TestMethod]
    public void ExpirationBeforeEffectiveThrows()
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        Assert.ThrowsExactly<ArgumentException>(() =>
            new WeatherAlert(
                Guid.NewGuid(),
                "SMN-ERR",
                "smn",
                now,
                "actual",
                "Viento",
                "yellow",
                "Likely",
                "Headline",
                "Desc",
                null,
                "Area",
                "[]",
                -35.0, -34.0, -60.0, -59.0,
                effectiveUtc: now.AddHours(2),
                expiresUtc: now.AddHours(1),
                createdAtUtc: now));
    }
}
