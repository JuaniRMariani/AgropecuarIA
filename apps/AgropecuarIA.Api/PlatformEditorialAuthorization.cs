using System.Security.Claims;
using AgropecuarIA.Catalog.Delivery;
using AgropecuarIA.Weather.Delivery;
using Microsoft.AspNetCore.Authorization;

namespace AgropecuarIA.Api;

internal static class PlatformEditorialAuthorization
{
    public static IServiceCollection AddPlatformEditorialAuthorization(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        HashSet<Guid> catalogActors = ReadActors(configuration, "Catalog:EditorialActorUserIds");
        HashSet<Guid> weatherActors = ReadActors(configuration, "Weather:AlertIngestionActorUserIds");
        services.AddAuthorization(options =>
        {
            AddPolicy(options, CatalogEndpoints.EditorialPolicy, catalogActors);
            AddPolicy(options, WeatherEndpoints.AlertIngestionPolicy, weatherActors);
        });
        return services;
    }

    private static void AddPolicy(AuthorizationOptions options, string name, HashSet<Guid> actors) =>
        options.AddPolicy(name, policy => policy
            .RequireAuthenticatedUser()
            .RequireAssertion(context =>
                Guid.TryParse(context.User.FindFirstValue(ClaimTypes.NameIdentifier), out Guid actor) &&
                actors.Contains(actor)));

    private static HashSet<Guid> ReadActors(IConfiguration configuration, string section)
    {
        HashSet<Guid> actors = [];
        foreach (string value in configuration.GetSection(section).Get<string[]>() ?? [])
        {
            if (!Guid.TryParseExact(value, "D", out Guid actor) || actor == Guid.Empty)
            {
                throw new InvalidOperationException($"{section} must contain nonempty UUIDs.");
            }
            actors.Add(actor);
        }
        return actors;
    }
}
