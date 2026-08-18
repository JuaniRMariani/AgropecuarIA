namespace AgropecuarIA.ProductiveCore.Application;

using Microsoft.Extensions.Options;

public abstract class ManagementUnitIdempotencyOptions
{
    public bool Enabled { get; set; }

    public string CurrentKeyVersion { get; set; } = string.Empty;

    public Dictionary<string, string> HmacKeys { get; set; } = new(StringComparer.Ordinal);

    public TimeSpan LeaseLifetime { get; set; } = TimeSpan.FromMinutes(1);
}

public sealed class ManagementUnitCreationOptions : ManagementUnitIdempotencyOptions
{
    public static string SectionName => "ProductiveCore:ManagementUnitCreation";
}

public sealed class ManagementUnitRenameOptions : ManagementUnitIdempotencyOptions
{
    public static string SectionName => "ProductiveCore:ManagementUnitRename";
}

public sealed class ManagementUnitCreationOptionsValidator : IValidateOptions<ManagementUnitCreationOptions>
{
    public ValidateOptionsResult Validate(string? name, ManagementUnitCreationOptions options)
    {
        _ = name;
        return ManagementUnitIdempotencyOptionsValidation.Validate(
            options,
            "Management unit creation configuration is invalid.");
    }
}

public sealed class ManagementUnitRenameOptionsValidator : IValidateOptions<ManagementUnitRenameOptions>
{
    public ValidateOptionsResult Validate(string? name, ManagementUnitRenameOptions options)
    {
        _ = name;
        return ManagementUnitIdempotencyOptionsValidation.Validate(
            options,
            "Management unit rename configuration is invalid.");
    }
}

internal static class ManagementUnitIdempotencyOptionsValidation
{
    public static ValidateOptionsResult Validate(
        ManagementUnitIdempotencyOptions options,
        string failureMessage)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentException.ThrowIfNullOrWhiteSpace(failureMessage);
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
            return ValidateOptionsResult.Fail(failureMessage);
        }

        HashSet<string> uniqueKeys = new(StringComparer.Ordinal);
        foreach ((string version, string encodedKey) in options.HmacKeys)
        {
            if (string.IsNullOrWhiteSpace(version) || version.Length > 32 ||
                string.IsNullOrWhiteSpace(encodedKey))
            {
                return ValidateOptionsResult.Fail(failureMessage);
            }

            byte[] key;
            try
            {
                key = Convert.FromBase64String(encodedKey);
            }
            catch (FormatException)
            {
                return ValidateOptionsResult.Fail(failureMessage);
            }

            if (key.Length < 32 || !uniqueKeys.Add(Convert.ToHexString(key)))
            {
                return ValidateOptionsResult.Fail(failureMessage);
            }
        }

        return ValidateOptionsResult.Success;
    }
}
