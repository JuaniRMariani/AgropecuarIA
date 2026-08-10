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

        services.AddDbContext<IdentityDbContext>(options =>
            options.UseNpgsql(connectionString, npgsql =>
                npgsql.MigrationsAssembly(typeof(IdentityDbContext).Assembly.FullName)));
        services.TryAddSingleton(TimeProvider.System);
        services.AddSingleton<IdentityTelemetry>();
        services.AddSingleton<IdentityTokenService>();
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
}
