namespace AgropecuarIA.ProductiveCore.Application;

using Microsoft.Extensions.Options;

public sealed class ManagementUnitCreationOptions
{
    public static string SectionName => "ProductiveCore:ManagementUnitCreation";

    public bool Enabled { get; set; }

    public string CurrentKeyVersion { get; set; } = string.Empty;

    public Dictionary<string, string> HmacKeys { get; set; } = new(StringComparer.Ordinal);

    public TimeSpan LeaseLifetime { get; set; } = TimeSpan.FromMinutes(1);
}

public sealed class ManagementUnitCreationOptionsValidator : IValidateOptions<ManagementUnitCreationOptions>
{
    public ValidateOptionsResult Validate(string? name, ManagementUnitCreationOptions options)
    {
        _ = name;
        ArgumentNullException.ThrowIfNull(options);
        if (!options.Enabled)
        {
            return ValidateOptionsResult.Success;
        }

        if (options.LeaseLifetime < TimeSpan.FromSeconds(10) ||
            options.LeaseLifetime > TimeSpan.FromMinutes(5) ||
            options.HmacKeys.Count is < 1 or > 8 ||
            string.IsNullOrWhiteSpace(options.CurrentKeyVersion) ||
            !options.HmacKeys.ContainsKey(options.CurrentKeyVersion))
        {
            return ValidateOptionsResult.Fail("Management unit creation configuration is invalid.");
        }

        HashSet<string> uniqueKeys = new(StringComparer.Ordinal);
        foreach ((string version, string encodedKey) in options.HmacKeys)
        {
            if (string.IsNullOrWhiteSpace(version) || version.Length > 32 ||
                string.IsNullOrWhiteSpace(encodedKey))
            {
                return ValidateOptionsResult.Fail("Management unit creation configuration is invalid.");
            }

            byte[] key;
            try
            {
                key = Convert.FromBase64String(encodedKey);
            }
            catch (FormatException)
            {
                return ValidateOptionsResult.Fail("Management unit creation configuration is invalid.");
            }

            if (key.Length < 32 || !uniqueKeys.Add(Convert.ToHexString(key)))
            {
                return ValidateOptionsResult.Fail("Management unit creation configuration is invalid.");
            }
        }

        return ValidateOptionsResult.Success;
    }
}
