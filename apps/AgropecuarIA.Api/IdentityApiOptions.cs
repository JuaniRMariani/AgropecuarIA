using Microsoft.Extensions.Options;

namespace AgropecuarIA.Api;

public sealed class OidcProviderOptions
{
    public const string SectionName = "Identity:Oidc";

    public string Authority { get; set; } = string.Empty;

    public string ClientId { get; set; } = string.Empty;

    public string ClientSecret { get; set; } = string.Empty;

    public string EmailConnection { get; set; } = "email";

    public string GoogleConnection { get; set; } = "google-oauth2";

    public bool EmailEnabled { get; set; } = true;

    public bool GoogleEnabled { get; set; } = true;

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(Authority) &&
        !string.IsNullOrWhiteSpace(ClientId) &&
        !string.IsNullOrWhiteSpace(ClientSecret);

    public string GetProviderConnection(string connection) => connection switch
    {
        "email" => EmailConnection,
        "google" => GoogleConnection,
        _ => throw new ArgumentOutOfRangeException(nameof(connection)),
    };

    public bool IsConnectionEnabled(string connection) => connection switch
    {
        "email" => EmailEnabled,
        "google" => GoogleEnabled,
        _ => false,
    };
}

public sealed class OidcProviderOptionsValidator(IHostEnvironment environment)
    : IValidateOptions<OidcProviderOptions>
{
    public ValidateOptionsResult Validate(string? name, OidcProviderOptions options)
    {
        if (environment.IsDevelopment() || environment.IsEnvironment("Test") || options.IsConfigured)
        {
            return ValidateOptionsResult.Success;
        }

        return ValidateOptionsResult.Fail(
            "Identity:Oidc:Authority, ClientId, and ClientSecret are required outside Development/Test.");
    }
}

public sealed class DevelopmentIdentityProviderOptions
{
    public const string SectionName = "Identity:DevelopmentProvider";

    public bool Enabled { get; set; }
}

public sealed class DevelopmentIdentityProviderOptionsValidator(IHostEnvironment environment)
    : IValidateOptions<DevelopmentIdentityProviderOptions>
{
    public ValidateOptionsResult Validate(string? name, DevelopmentIdentityProviderOptions options)
    {
        if (!options.Enabled || environment.IsDevelopment() || environment.IsEnvironment("Test"))
        {
            return ValidateOptionsResult.Success;
        }

        return ValidateOptionsResult.Fail(
            "Identity:DevelopmentProvider:Enabled is forbidden outside Development/Test.");
    }
}
