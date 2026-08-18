using AgropecuarIA.ProductiveCore.Application;
using Microsoft.Extensions.Options;

namespace AgropecuarIA.ProductiveCore.Tests;

[TestClass]
public sealed class ManagementUnitCreationOptionsValidatorTests
{
    [TestMethod]
    public void DisabledFeatureDoesNotRequireSecrets()
    {
        ManagementUnitCreationOptionsValidator validator = new();

        ValidateOptionsResult result = validator.Validate(
            null,
            new ManagementUnitCreationOptions { Enabled = false });

        Assert.IsTrue(result.Succeeded);
    }

    [TestMethod]
    public void EnabledFeatureRequiresAValidUniqueVersionedKeyRingAndLease()
    {
        ManagementUnitCreationOptionsValidator validator = new();
        string key = Convert.ToBase64String(Enumerable.Range(1, 32).Select(value => (byte)value).ToArray());
        ManagementUnitCreationOptions valid = new()
        {
            Enabled = true,
            CurrentKeyVersion = "v2",
            LeaseLifetime = TimeSpan.FromMinutes(1),
            HmacKeys = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["v1"] = Convert.ToBase64String(Enumerable.Range(33, 32).Select(value => (byte)value).ToArray()),
                ["v2"] = key,
            },
        };

        Assert.IsTrue(validator.Validate(null, valid).Succeeded);

        valid.HmacKeys["v1"] = key;
        Assert.IsTrue(validator.Validate(null, valid).Failed);

        valid.HmacKeys.Remove("v2");
        Assert.IsTrue(validator.Validate(null, valid).Failed);

        valid.LeaseLifetime = TimeSpan.FromSeconds(1);
        Assert.IsTrue(validator.Validate(null, valid).Failed);
    }
}
