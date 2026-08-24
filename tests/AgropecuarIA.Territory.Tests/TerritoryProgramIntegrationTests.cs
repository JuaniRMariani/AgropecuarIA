using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace AgropecuarIA.Territory.Tests;

[TestClass]
public sealed class TerritoryProgramIntegrationTests
{
    [TestMethod]
    public async Task TerritoryResponsesArePrivateNoStoreBeforeAuthentication()
    {
        const string connectionString =
            "Host=127.0.0.1;Port=1;Database=unused;Username=unused;Password=unused;Timeout=1";
        await using WebApplicationFactory<Program> factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Test");
                builder.UseSetting("ConnectionStrings:Identity", connectionString);
                builder.UseSetting("ConnectionStrings:Territory", connectionString);
                builder.UseSetting("ConnectionStrings:ProductiveCore", connectionString);
                builder.UseSetting("ConnectionStrings:Catalog", connectionString);
            });
        using HttpClient client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
        });

        using HttpResponseMessage response = await client.GetAsync(
            "/api/territory/search?query=rio");

        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.AreEqual("no-store", response.Headers.CacheControl?.ToString());
        Assert.IsTrue(response.Headers.Pragma.Any(value => value.Name == "no-cache"));
    }
}
