using System.Text.Json;
using AgropecuarIA.Territory.Application;
using AgropecuarIA.Territory.Domain;

namespace AgropecuarIA.Territory.Tests;

[TestClass]
public sealed class TerritorySnapshotValidatorTests
{
    private const string ExpectedFixtureHash =
        "ee27e73d27b1fe45a5010b758e97f073fcf8909f0d8bb46541b8bb4eb9eb6fe7";

    [TestMethod]
    public void NormalizeRemovesAccentsFoldsCaseAndCollapsesWhitespace()
    {
        string normalized = TerritoryNameNormalizer.Normalize("  RÍO\t  Negro  ");

        Assert.AreEqual("rio negro", normalized);
    }

    [TestMethod]
    public void ComputeContentHashMatchesTheFrozenNationalFixture()
    {
        IReadOnlyList<TerritoryUnitImport> units = ReadNationalFixture();

        string hash = Convert.ToHexString(
            TerritorySnapshotValidator.ComputeContentHash(units)).ToLowerInvariant();

        Assert.AreEqual(ExpectedFixtureHash, hash);
    }

    [TestMethod]
    public void ContentHashCoversTheExactNfcDisplayName()
    {
        TerritoryUnitImport[] units = ReadNationalFixture();
        TerritoryUnitImport[] changed = [.. units];
        changed[3] = changed[3] with { Name = "Cordoba" };

        Assert.IsFalse(
            TerritorySnapshotValidator.ComputeContentHash(units)
                .SequenceEqual(TerritorySnapshotValidator.ComputeContentHash(changed)));
    }

    [TestMethod]
    public void ValidateAcceptsThe24ProvinceSnapshotAndPreservesCanonicalData()
    {
        DateTimeOffset capturedAt = new(2026, 8, 5, 16, 33, 0, TimeSpan.Zero);
        TerritorySnapshotImport import = new(
            Guid.NewGuid(),
            "georef",
            "national-points-1.0.0",
            capturedAt,
            ExpectedFixtureHash,
            ReadNationalFixture());

        ValidatedTerritorySnapshot validated = TerritorySnapshotValidator.Validate(
            import,
            capturedAt.AddMinutes(1));

        Assert.AreEqual(24, validated.Units.Count);
        Assert.AreEqual(TerritorySnapshotStatuses.Staging, validated.Snapshot.Status);
        Assert.AreEqual(32, validated.Snapshot.ContentHash.Length);
        Assert.IsTrue(validated.Units.All(unit => unit.Level == TerritoryLevels.Province));
        Assert.IsTrue(validated.Units.Any(unit => unit.OfficialCode == "94"));
    }

    [TestMethod]
    public void ValidateRejectsMissingNationalCoverage()
    {
        IReadOnlyList<TerritoryUnitImport> units = ReadNationalFixture()[..^1];
        TerritorySnapshotImport import = CreateImport(units);

        Assert.ThrowsExactly<TerritorySnapshotValidationException>(() =>
            TerritorySnapshotValidator.Validate(import, DateTimeOffset.UtcNow));
    }

    [TestMethod]
    public void ValidateRejectsDuplicateCodesBeforeActivation()
    {
        List<TerritoryUnitImport> units = [.. ReadNationalFixture()];
        units.Add(units[0] with { Name = "Duplicada" });
        TerritorySnapshotImport import = CreateImport(units);

        TerritorySnapshotValidationException exception =
            Assert.ThrowsExactly<TerritorySnapshotValidationException>(() =>
                TerritorySnapshotValidator.Validate(import, DateTimeOffset.UtcNow));

        StringAssert.Contains(exception.Message, "Duplicate official code");
    }

    [TestMethod]
    public void ValidateRejectsInvalidParentAndCycles()
    {
        List<TerritoryUnitImport> invalidParent =
        [
            .. ReadNationalFixture(),
            new("06001", "Departamento", TerritoryLevels.Department, "999"),
        ];
        TerritorySnapshotImport invalidParentImport = CreateImport(invalidParent);

        Assert.ThrowsExactly<TerritorySnapshotValidationException>(() =>
            TerritorySnapshotValidator.Validate(invalidParentImport, DateTimeOffset.UtcNow));

        List<TerritoryUnitImport> cycle =
        [
            .. ReadNationalFixture(),
            new("06001", "Departamento A", TerritoryLevels.Department, "06002"),
            new("06002", "Departamento B", TerritoryLevels.Department, "06001"),
        ];
        TerritorySnapshotImport cycleImport = CreateImport(cycle);

        Assert.ThrowsExactly<TerritorySnapshotValidationException>(() =>
            TerritorySnapshotValidator.Validate(cycleImport, DateTimeOffset.UtcNow));
    }

    [TestMethod]
    public void ValidateRejectsHashMismatchAndPartialCentroid()
    {
        IReadOnlyList<TerritoryUnitImport> units = ReadNationalFixture();
        TerritorySnapshotImport wrongHash = new(
            Guid.NewGuid(),
            "georef",
            "v1",
            DateTimeOffset.UtcNow.AddMinutes(-1),
            new string('0', 64),
            units);

        Assert.ThrowsExactly<TerritorySnapshotValidationException>(() =>
            TerritorySnapshotValidator.Validate(wrongHash, DateTimeOffset.UtcNow));

        List<TerritoryUnitImport> partialCentroid = [.. units];
        partialCentroid[0] = partialCentroid[0] with { CentroidLongitude = null };
        TerritorySnapshotImport partialImport = new(
            Guid.NewGuid(),
            "georef",
            "test",
            DateTimeOffset.UtcNow.AddMinutes(-1),
            new string('0', 64),
            partialCentroid);

        Assert.ThrowsExactly<TerritorySnapshotValidationException>(() =>
            TerritorySnapshotValidator.Validate(partialImport, DateTimeOffset.UtcNow));
    }

    [TestMethod]
    public void ValidateRejectsUnsupportedProviderAndHashDelimitersInNames()
    {
        IReadOnlyList<TerritoryUnitImport> units = ReadNationalFixture();
        TerritorySnapshotImport unsupported = CreateImport(units) with
        {
            Provider = "other",
        };
        Assert.ThrowsExactly<TerritorySnapshotValidationException>(() =>
            TerritorySnapshotValidator.Validate(unsupported, DateTimeOffset.UtcNow));

        TerritoryUnitImport[] ambiguous = [.. units];
        ambiguous[0] = ambiguous[0] with { Name = "Buenos\u001fAires" };
        Assert.ThrowsExactly<TerritorySnapshotValidationException>(() =>
            TerritorySnapshotValidator.ComputeContentHash(ambiguous));
    }

    private static TerritorySnapshotImport CreateImport(
        IReadOnlyCollection<TerritoryUnitImport> units)
    {
        string hash = Convert.ToHexString(
            TerritorySnapshotValidator.ComputeContentHash(units)).ToLowerInvariant();
        return new TerritorySnapshotImport(
            Guid.NewGuid(),
            "georef",
            "test",
            DateTimeOffset.UtcNow.AddMinutes(-1),
            hash,
            units);
    }

    private static TerritoryUnitImport[] ReadNationalFixture()
    {
        string fixturePath = FindRepositoryFile(
            "tasks",
            "evidence",
            "AGRO-DIS-004",
            "fixtures",
            "territory",
            "national-points.json");
        using JsonDocument document = JsonDocument.Parse(File.ReadAllBytes(fixturePath));
        return document.RootElement.GetProperty("points")
            .EnumerateArray()
            .Select(point => new TerritoryUnitImport(
                point.GetProperty("id").GetString()!,
                point.GetProperty("name").GetString()!,
                TerritoryLevels.Province,
                CentroidLatitude: point.GetProperty("latitude").GetDouble(),
                CentroidLongitude: point.GetProperty("longitude").GetDouble()))
            .ToArray();
    }

    private static string FindRepositoryFile(params string[] segments)
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null)
        {
            string candidate = Path.Combine([directory.FullName, .. segments]);
            if (File.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        throw new FileNotFoundException("Could not locate the national territory fixture.");
    }
}
