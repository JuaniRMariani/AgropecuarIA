using AgropecuarIA.Identity.Application;
using AgropecuarIA.Identity.Delivery;
using AgropecuarIA.Identity.Infrastructure;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace AgropecuarIA.Identity;

public static class IdentityServiceCollectionExtensions
{
    public static IServiceCollection AddAgropecuariaIdentity(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        string connectionString = configuration.GetConnectionString("Identity")
            ?? throw new InvalidOperationException(
                "ConnectionStrings:Identity must be configured for the identity module.");

        services.AddOptions<IdentityRuntimeOptions>()
            .Bind(configuration.GetSection(IdentityRuntimeOptions.SectionName))
            .Validate(
                options => options.SessionLifetime > TimeSpan.Zero &&
                    options.LinkAttemptLifetime > TimeSpan.Zero &&
                    options.StepUpAttemptLifetime > TimeSpan.Zero &&
                    options.StrongAuthenticationWindow > TimeSpan.Zero &&
                    options.RecentAuthenticationWindow > TimeSpan.Zero,
                "Identity runtime durations must be positive.")
            .ValidateOnStart();

        services.AddOptions<OrganizationBootstrapOptions>()
            .Bind(configuration.GetSection(OrganizationBootstrapOptions.SectionName))
            .Validate(
                IsValidOrganizationBootstrap,
                "Enabled organization bootstrap requires a current idempotency HMAC key and a bounded key ring of Base64 keys containing at least 32 bytes each.")
            .ValidateOnStart();

        services.AddOptions<OrganizationOwnerInvitationOptions>()
            .Bind(configuration.GetSection(OrganizationOwnerInvitationOptions.SectionName))
            .Validate(
                IsValidOrganizationOwnerInvitations,
                "Enabled owner invitations require a 1 hour to 30 day lifetime, a current HMAC key, and a bounded key ring of Base64 keys containing at least 32 bytes each.")
            .ValidateOnStart();

        services.AddDbContext<IdentityDbContext>(options =>
            options.UseNpgsql(connectionString, npgsql =>
                npgsql.MigrationsAssembly(typeof(IdentityDbContext).Assembly.FullName)));
        services.TryAddSingleton(TimeProvider.System);
        services.AddSingleton<IdentityTelemetry>();
        services.AddSingleton<IdentityTokenService>();
        services.TryAddSingleton<IOrganizationCreationCommitBoundary,
            OrganizationCreationCommitBoundary>();
        services.TryAddScoped<IOrganizationCreationRecoveryContextFactory,
            OrganizationCreationRecoveryContextFactory>();
        services.AddScoped<IdentityApplicationService>();
        services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = IdentityAuthenticationDefaults.SessionScheme;
                options.DefaultChallengeScheme = IdentityAuthenticationDefaults.SessionScheme;
            })
            .AddScheme<AuthenticationSchemeOptions, SessionAuthenticationHandler>(
                IdentityAuthenticationDefaults.SessionScheme,
                _ => { });
        services.AddAuthorization();

        return services;
    }

    private static bool IsValidOrganizationBootstrap(OrganizationBootstrapOptions options)
    {
        if (!options.Enabled)
        {
            return true;
        }

        if (string.IsNullOrWhiteSpace(options.CurrentKeyVersion) ||
            options.CurrentKeyVersion.Length > 32 ||
            options.IdempotencyHmacKeys.Count is < 1 or > 8 ||
            !options.IdempotencyHmacKeys.ContainsKey(options.CurrentKeyVersion))
        {
            return false;
        }

        foreach ((string version, string encodedKey) in options.IdempotencyHmacKeys)
        {
            if (string.IsNullOrWhiteSpace(version) || version.Length > 32)
            {
                return false;
            }

            try
            {
                if (Convert.FromBase64String(encodedKey).Length < 32)
                {
                    return false;
                }
            }
            catch (FormatException)
            {
                return false;
            }
        }

        return true;
    }

    private static bool IsValidOrganizationOwnerInvitations(
        OrganizationOwnerInvitationOptions options)
    {
        if (!options.Enabled)
        {
            return true;
        }

        if (options.Lifetime < TimeSpan.FromHours(1) ||
            options.Lifetime > TimeSpan.FromDays(30) ||
            string.IsNullOrWhiteSpace(options.CurrentKeyVersion) ||
            options.CurrentKeyVersion.Length > 32 ||
            options.HmacKeys.Count is < 1 or > 8 ||
            !options.HmacKeys.ContainsKey(options.CurrentKeyVersion))
        {
            return false;
        }

        var uniqueKeys = new HashSet<string>(StringComparer.Ordinal);
        foreach ((string version, string encodedKey) in options.HmacKeys)
        {
            if (string.IsNullOrWhiteSpace(version) || version.Length > 32)
            {
                return false;
            }

            try
            {
                byte[] key = Convert.FromBase64String(encodedKey);
                if (key.Length < 32 || !uniqueKeys.Add(Convert.ToHexString(key)))
                {
                    return false;
                }
            }
            catch (FormatException)
            {
                return false;
            }
        }

        return true;
    }
}
