using AgropecuarIA.Weather.Domain;

namespace AgropecuarIA.Weather.Tests;

[TestClass]
public sealed class WeatherForecastTests
{
    [TestMethod]
    public void ForecastSnapshotCreationSetsPropertiesAndRoundsCoordinates()
    {
        Guid id = Guid.NewGuid();
        DateTimeOffset now = DateTimeOffset.UtcNow;
        DateTimeOffset validUntil = now.AddHours(2);

        WeatherForecastSnapshot snapshot = new(
            id,
            centroidLatitude: -34.567891,
            centroidLongitude: -60.123456,
            provider: "open-meteo",
            modelName: "gfs_seamless",
            issuedAtUtc: now,
            validUntilUtc: validUntil,
            hourlyVariablesJson: "{\"temperature\":[20.5]}",
            dailyVariablesJson: "{\"precipitation_sum\":[15.2]}",
            snapshotHash: "abc123hash",
            createdAtUtc: now);

        Assert.AreEqual(id, snapshot.Id);
        Assert.AreEqual(-34.5679, snapshot.CentroidLatitude);
        Assert.AreEqual(-60.1235, snapshot.CentroidLongitude);
        Assert.AreEqual("open-meteo", snapshot.Provider);
        Assert.AreEqual("gfs_seamless", snapshot.ModelName);
        Assert.AreEqual("abc123hash", snapshot.SnapshotHash);
        Assert.IsTrue(snapshot.IsFresh(now));
        Assert.IsFalse(snapshot.IsFresh(validUntil.AddMinutes(1)));
    }

    [TestMethod]
    public void ForecastSnapshotThrowsOnInvalidCoordinates()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
            new WeatherForecastSnapshot(
                Guid.NewGuid(),
                centroidLatitude: 95.0,
                centroidLongitude: 0.0,
                provider: "test",
                modelName: "test",
                issuedAtUtc: DateTimeOffset.UtcNow,
                validUntilUtc: DateTimeOffset.UtcNow.AddHours(1),
                hourlyVariablesJson: "{}",
                dailyVariablesJson: "{}",
                snapshotHash: "hash",
                createdAtUtc: DateTimeOffset.UtcNow));
    }

    [TestMethod]
    public void ObservedRainSetsPropertiesAndValidatesMethod()
    {
        Guid id = Guid.NewGuid();
        Guid orgId = Guid.NewGuid();
        Guid fieldId = Guid.NewGuid();
        Guid userId = Guid.NewGuid();
        DateTimeOffset observedDate = DateTimeOffset.UtcNow.AddDays(-1);
        DateTimeOffset now = DateTimeOffset.UtcNow;

        WeatherObservedRain rain = new(
            id,
            orgId,
            fieldId,
            observedDate,
            amountMillimeters: 25.5m,
            method: WeatherObservedRainMethods.ManualPluviometer,
            notes: "Lluvia de la madrugada",
            recordedByUserId: userId,
            recordedAtUtc: now);

        Assert.AreEqual(id, rain.Id);
        Assert.AreEqual(orgId, rain.OrganizationId);
        Assert.AreEqual(fieldId, rain.FieldId);
        Assert.AreEqual(observedDate, rain.ObservedDateUtc);
        Assert.AreEqual(25.5m, rain.AmountMillimeters);
        Assert.AreEqual("manual_pluviometer", rain.Method);
        Assert.AreEqual("Lluvia de la madrugada", rain.Notes);
        Assert.AreEqual(userId, rain.RecordedByUserId);
        Assert.IsNull(rain.RectifiedFromId);
    }

    [TestMethod]
    public void ObservedRainWithRectificationSetsPredecessor()
    {
        Guid predecessorId = Guid.NewGuid();
        WeatherObservedRain rectified = new(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            DateTimeOffset.UtcNow,
            amountMillimeters: 30.0m,
            method: WeatherObservedRainMethods.WeatherStation,
            notes: "Lectura corregida tras calibración",
            recordedByUserId: Guid.NewGuid(),
            recordedAtUtc: DateTimeOffset.UtcNow,
            rectifiedFromId: predecessorId);

        Assert.AreEqual(predecessorId, rectified.RectifiedFromId);
        Assert.AreEqual(30.0m, rectified.AmountMillimeters);
        Assert.AreEqual("weather_station", rectified.Method);
    }

    [TestMethod]
    public void ObservedRainThrowsOnNegativeAmount()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
            new WeatherObservedRain(
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid(),
                DateTimeOffset.UtcNow,
                amountMillimeters: -5.0m,
                method: WeatherObservedRainMethods.ManualPluviometer,
                notes: null,
                recordedByUserId: Guid.NewGuid(),
                recordedAtUtc: DateTimeOffset.UtcNow));
    }
}
