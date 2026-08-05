namespace AgropecuarIA.IdentitySpike.Api.Fixtures;

internal static class SpikeFixtureSafety
{
    internal static void EnsureLoopbackOnly(IConfiguration configuration)
    {
        var configuredUrls = new List<string>();
        if (configuration["urls"] is { Length: > 0 } urls)
        {
            configuredUrls.AddRange(urls.Split(';', StringSplitOptions.RemoveEmptyEntries));
        }

        foreach (var endpoint in configuration.GetSection("Kestrel:Endpoints").GetChildren())
        {
            if (endpoint["Url"] is { Length: > 0 } endpointUrl)
            {
                configuredUrls.Add(endpointUrl);
            }
        }

        if (configuredUrls.Count == 0)
        {
            throw new InvalidOperationException(
                "The Spike environment requires an explicit loopback URL configuration.");
        }

        foreach (var value in configuredUrls)
        {
            if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) || !uri.IsLoopback)
            {
                throw new InvalidOperationException(
                    "The Spike fixture endpoints may only bind to an explicit loopback URL.");
            }
        }
    }
}
