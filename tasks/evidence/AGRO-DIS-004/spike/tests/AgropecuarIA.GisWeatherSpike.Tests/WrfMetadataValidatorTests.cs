namespace AgropecuarIA.GisWeatherSpike.Tests;

[TestClass]
public sealed class WrfMetadataValidatorTests
{
    private static readonly string[] RequiredVariables = ["PP", "T2", "HR2", "dirViento10", "magViento10", "lat", "lon", "time"];
    private readonly WrfMetadataValidator _validator = new(WrfValidationLimits.SpikeDefaults);

    [TestMethod]
    public void ValidateValidMetadataReturnsRequiredDimensionsAndVariables()
    {
        using var payload = FixtureFiles.Open(Path.Combine("wrf", "valid-metadata.json"));

        var result = _validator.Validate(payload);

        Assert.IsTrue(result.IsSuccess, result.Error?.SafeMessage);
        var metadata = result.Value ?? throw new AssertFailedException("WRF metadata is required.");
        Assert.AreEqual("NETCDF4", metadata.Format);
        Assert.AreEqual(1, metadata.TimeSteps);
        Assert.AreEqual(1249, metadata.Y);
        Assert.AreEqual(999, metadata.X);
        CollectionAssert.IsSubsetOf(
            RequiredVariables,
            metadata.Variables.ToArray());
    }

    [TestMethod]
    [DataRow("truncated-metadata.json", ProviderErrorCode.SchemaInvalid)]
    [DataRow("missing-variable-metadata.json", ProviderErrorCode.RunMissing)]
    [DataRow("dimension-bomb-metadata.json", ProviderErrorCode.PayloadTooLarge)]
    [DataRow("shape-bomb-metadata.json", ProviderErrorCode.SchemaInvalid)]
    public void ValidateInvalidMetadataReturnsTypedError(string fixture, ProviderErrorCode expected)
    {
        using var payload = FixtureFiles.Open(Path.Combine("wrf", fixture));

        var result = _validator.Validate(payload);

        Assert.IsFalse(result.IsSuccess);
        Assert.IsNull(result.Value);
        Assert.AreEqual(expected, result.Error?.Code);
    }
}
