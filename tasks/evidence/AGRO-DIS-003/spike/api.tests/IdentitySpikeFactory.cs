using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace AgropecuarIA.IdentitySpike.Tests;

internal sealed class IdentitySpikeFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        var connectionString = TestEnvironment.Require("ConnectionStrings__IdentitySpike");

        builder.UseEnvironment("Spike");
        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:IdentitySpike"] = connectionString,
                ["urls"] = "https://localhost"
            });
        });
    }

    internal HttpClient CreateBrowserClient() => CreateClient(
        new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost"),
            HandleCookies = true
        });
}

internal static class TestEnvironment
{
    internal static string Require(string name) =>
        Environment.GetEnvironmentVariable(name) is { Length: > 0 } value
            ? value
            : throw new InvalidOperationException($"Required test environment variable is absent: {name}");
}
