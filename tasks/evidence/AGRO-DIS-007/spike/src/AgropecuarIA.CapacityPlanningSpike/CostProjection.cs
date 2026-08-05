using System.Collections.ObjectModel;

namespace AgropecuarIA.CapacityPlanningSpike;

public static class CostDriverIds
{
    public const string ComputeHour = "compute-hour";
    public const string StorageGibMonth = "storage-gib-month";
    public const string EgressGib = "egress-gib";
    public const string JobMillionRows = "job-million-rows";
    public const string SupportMonth = "support-month";

    public static IReadOnlyList<string> Required { get; } =
        Array.AsReadOnly([
            ComputeHour,
            StorageGibMonth,
            EgressGib,
            JobMillionRows,
            SupportMonth,
        ]);
}

public sealed record CostUsage(
    decimal ComputeHours,
    decimal StorageGibMonths,
    decimal EgressGib,
    decimal JobMillionRows,
    decimal SupportMonths)
{
    public decimal QuantityFor(string driverId) => driverId switch
    {
        CostDriverIds.ComputeHour => ComputeHours,
        CostDriverIds.StorageGibMonth => StorageGibMonths,
        CostDriverIds.EgressGib => EgressGib,
        CostDriverIds.JobMillionRows => JobMillionRows,
        CostDriverIds.SupportMonth => SupportMonths,
        _ => throw new CapacityPlanningException(
            CapacityPlanningErrorCode.InvalidInput,
            nameof(driverId),
            $"Unknown cost driver '{driverId}'."),
    };
}

public sealed record UnitCostDriver(
    string Id,
    decimal? Low,
    decimal? Base,
    decimal? High);

public enum UnitCostCatalogStatus
{
    Incomplete,
    SyntheticTestOnly,
    Approved,
}

public sealed record UnitCostCatalog(
    UnitCostCatalogStatus Status,
    string Currency,
    string? Region,
    string? Source,
    DateOnly? AsOf,
    bool? TaxIncluded,
    IReadOnlyCollection<UnitCostDriver> Drivers);

public abstract record CostProjection(string Currency);

public sealed record IncompleteCostProjection(
    string Currency,
    IReadOnlyList<string> MissingDrivers)
    : CostProjection(Currency);

public sealed record EstimatedCostProjection(
    string Currency,
    decimal Low,
    decimal Base,
    decimal High)
    : CostProjection(Currency);

public static class CostProjector
{
    public static CostProjection Project(CostUsage usage, UnitCostCatalog catalog)
    {
        ArgumentNullException.ThrowIfNull(usage);
        ArgumentNullException.ThrowIfNull(catalog);
        ValidateCurrency(catalog.Currency);
        ValidateUsage(usage);
        ArgumentNullException.ThrowIfNull(catalog.Drivers);

        var drivers = BuildDriverIndex(catalog.Drivers);
        var missingDrivers = CostDriverIds.Required
            .Where(required =>
                !drivers.TryGetValue(required, out var price)
                || price.Low is null
                || price.Base is null
                || price.High is null)
            .Order(StringComparer.Ordinal)
            .ToList();

        if (catalog.Status == UnitCostCatalogStatus.Incomplete && missingDrivers.Count == 0)
        {
            missingDrivers.Add("catalog-approval");
        }

        if (missingDrivers.Count > 0)
        {
            return new IncompleteCostProjection(
                catalog.Currency,
                Array.AsReadOnly(missingDrivers.Order(StringComparer.Ordinal).ToArray()));
        }

        ValidateEstimationMetadata(catalog);

        try
        {
            checked
            {
                var low = 0m;
                var baseCost = 0m;
                var high = 0m;

                foreach (var driverId in CostDriverIds.Required)
                {
                    var driver = drivers[driverId];
                    ValidatePriceBand(driver);
                    var quantity = usage.QuantityFor(driverId);
                    low += quantity * driver.Low!.Value;
                    baseCost += quantity * driver.Base!.Value;
                    high += quantity * driver.High!.Value;
                }

                return new EstimatedCostProjection(catalog.Currency, low, baseCost, high);
            }
        }
        catch (OverflowException exception)
        {
            throw new CapacityPlanningException(
                CapacityPlanningErrorCode.CalculationOverflow,
                nameof(usage),
                "The cost projection exceeds the deterministic model's numeric range.",
                exception);
        }
    }

    private static ReadOnlyDictionary<string, UnitCostDriver> BuildDriverIndex(
        IReadOnlyCollection<UnitCostDriver> drivers)
    {
        var result = new Dictionary<string, UnitCostDriver>(StringComparer.Ordinal);
        foreach (var driver in drivers)
        {
            ArgumentNullException.ThrowIfNull(driver);
            if (!CostDriverIds.Required.Contains(driver.Id, StringComparer.Ordinal))
            {
                throw Invalid(nameof(drivers), $"Unknown cost driver '{driver.Id}'.");
            }

            if (!result.TryAdd(driver.Id, driver))
            {
                throw Invalid(nameof(drivers), $"Cost driver '{driver.Id}' is duplicated.");
            }
        }

        return new ReadOnlyDictionary<string, UnitCostDriver>(result);
    }

    private static void ValidateCurrency(string currency)
    {
        if (string.IsNullOrEmpty(currency)
            || currency.Length != 3
            || currency.Any(character => character is < 'A' or > 'Z'))
        {
            throw Invalid(nameof(currency), "Currency must be a three-letter uppercase ISO code.");
        }
    }

    private static void ValidateEstimationMetadata(UnitCostCatalog catalog)
    {
        if (catalog.Status is not (UnitCostCatalogStatus.SyntheticTestOnly or UnitCostCatalogStatus.Approved))
        {
            throw Invalid(nameof(catalog.Status), "Only approved or explicitly synthetic test catalogs can produce estimates.");
        }

        if (string.IsNullOrWhiteSpace(catalog.Region))
        {
            throw Invalid(nameof(catalog.Region), "A priced catalog requires a region.");
        }

        if (string.IsNullOrWhiteSpace(catalog.Source))
        {
            throw Invalid(nameof(catalog.Source), "A priced catalog requires a traceable source.");
        }

        if (catalog.AsOf is null)
        {
            throw Invalid(nameof(catalog.AsOf), "A priced catalog requires an as-of date.");
        }

        if (catalog.TaxIncluded is null)
        {
            throw Invalid(nameof(catalog.TaxIncluded), "A priced catalog must state whether tax is included.");
        }
    }

    private static void ValidateUsage(CostUsage usage)
    {
        var quantities = new[]
        {
            usage.ComputeHours,
            usage.StorageGibMonths,
            usage.EgressGib,
            usage.JobMillionRows,
            usage.SupportMonths,
        };

        if (quantities.Any(quantity => quantity < 0m))
        {
            throw Invalid(nameof(usage), "Cost driver quantities cannot be negative.");
        }
    }

    private static void ValidatePriceBand(UnitCostDriver driver)
    {
        var low = driver.Low!.Value;
        var baseCost = driver.Base!.Value;
        var high = driver.High!.Value;

        if (low < 0m || baseCost < 0m || high < 0m)
        {
            throw Invalid(nameof(driver), $"Cost driver '{driver.Id}' cannot contain negative prices.");
        }

        if (low > baseCost || baseCost > high)
        {
            throw Invalid(nameof(driver), $"Cost driver '{driver.Id}' must satisfy low <= base <= high.");
        }
    }

    private static CapacityPlanningException Invalid(string parameterName, string message) =>
        new(CapacityPlanningErrorCode.InvalidInput, parameterName, message);
}
