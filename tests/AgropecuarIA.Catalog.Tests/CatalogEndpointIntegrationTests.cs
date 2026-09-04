using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.RateLimiting;
using AgropecuarIA.Catalog.Application;
using AgropecuarIA.Catalog.Delivery;
using AgropecuarIA.Catalog.Infrastructure;
using AgropecuarIA.Identity.Delivery;
using AgropecuarIA.Identity.Tests.Infrastructure;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AgropecuarIA.Catalog.Tests;

[TestClass]
public sealed class CatalogEndpointIntegrationTests
{
    private static readonly Guid EditorId = Guid.Parse("b1521588-55b3-4bf7-a271-b7298bbc7209");
    private const string UnusedDatabase = "Host=127.0.0.1;Port=1;Database=unused;Username=unused;Timeout=1";

    [TestMethod]
    public async Task AllRoutesRequireAuthenticationAndMutationsRequireEditorialPolicy()
    {
        await using WebApplication app = await StartAppAsync(UnusedDatabase);
        RouteEndpoint[] routes = ((IEndpointRouteBuilder)app).DataSources
            .SelectMany(source => source.Endpoints)
            .OfType<RouteEndpoint>()
            .Where(route => route.RoutePattern.RawText?.StartsWith("/api/catalog", StringComparison.Ordinal) == true)
            .ToArray();

        Assert.AreEqual(6, routes.Length);
        foreach (RouteEndpoint route in routes)
        {
            Assert.IsTrue(route.Metadata.GetOrderedMetadata<IAuthorizeData>().Count > 0);
            Assert.AreEqual(CatalogEndpoints.RateLimitPolicy,
                route.Metadata.GetMetadata<EnableRateLimitingAttribute>()?.PolicyName);
            bool mutation = route.Metadata.GetMetadata<IHttpMethodMetadata>()!.HttpMethods.Contains("POST");
            Assert.AreEqual(mutation, route.Metadata.GetOrderedMetadata<IAuthorizeData>()
                .Any(policy => policy.Policy == CatalogEndpoints.EditorialPolicy));
        }
    }

    [TestMethod]
    public async Task AnonymousReadersAndOrdinaryAuthenticatedWritersAreRejectedBeforeDatabaseAccess()
    {
        await using WebApplication app = await StartAppAsync(UnusedDatabase);
        using HttpClient client = app.GetTestClient();
        using HttpResponseMessage anonymous = await client.GetAsync("/api/catalog/items");
        Assert.AreEqual(HttpStatusCode.Unauthorized, anonymous.StatusCode);

        foreach (string route in MutationRoutes())
        {
            using HttpResponseMessage anonymousWrite = await client.PostAsJsonAsync(route, new { });
            Assert.AreEqual(HttpStatusCode.Unauthorized, anonymousWrite.StatusCode);
        }

        client.DefaultRequestHeaders.Add("X-Test-Actor", Guid.NewGuid().ToString("D"));
        foreach (string route in MutationRoutes())
        {
            using HttpResponseMessage forbidden = await client.PostAsJsonAsync(route, new { });
            Assert.AreEqual(HttpStatusCode.Forbidden, forbidden.StatusCode);
        }
    }

    [TestMethod]
    public async Task EditorialWritesRequireAntiforgeryAndRejectClientSuppliedPublicationActor()
    {
        await using WebApplication app = await StartAppAsync(UnusedDatabase);
        using HttpClient client = app.GetTestClient();
        client.DefaultRequestHeaders.Add("X-Test-Actor", EditorId.ToString("D"));

        foreach (string route in MutationRoutes())
        {
            object payload = route switch
            {
                "/api/catalog/ingest" => new IngestSourceCommand("source", "W10="),
                "/api/catalog/publish" => new CatalogEndpoints.PublishCatalogRequest("v1"),
                _ => new { },
            };
            using HttpResponseMessage missingToken = await client.PostAsJsonAsync(route, payload);
            Assert.AreEqual(HttpStatusCode.BadRequest, missingToken.StatusCode);
        }

        await AddAntiforgeryAsync(client);
        using HttpResponseMessage spoofedActor = await client.PostAsJsonAsync("/api/catalog/publish", new
        {
            versionTag = "spoofed",
            publishedBy = "another-editor",
        });
        Assert.AreEqual(HttpStatusCode.BadRequest, spoofedActor.StatusCode);
    }

    [TestMethod]
    public async Task ForwardMigrationPreservesStagingAndSupportsAuthenticatedPublicationSearchAndRollback()
    {
        PostgreSqlTestServer server = IdentityTestAssembly.PostgreSql
            ?? throw new InvalidOperationException("PostgreSQL test server failed to start.", IdentityTestAssembly.StartupError);
        string connectionString = await server.CreateDatabaseAsync(CancellationToken.None);
        try
        {
            await using WebApplication app = await StartAppAsync(connectionString);
            await using (AsyncServiceScope scope = app.Services.CreateAsyncScope())
            {
                CatalogDbContext database = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
                await database.GetService<IMigrator>().MigrateAsync("20260823021753_InitialCatalog");
                bool ingested = await scope.ServiceProvider.GetRequiredService<CatalogIngestionApplicationService>()
                    .IngestAsync(new IngestSourceCommand("verified-source", Convert.ToBase64String(
                        Encoding.UTF8.GetBytes("[{\"code\":\"MAIZ\",\"displayName\":\"Maíz\",\"jurisdiction\":\"AR\"}]"))),
                        CancellationToken.None);
                Assert.IsTrue(ingested);
                await database.Database.MigrateAsync();
                Assert.IsFalse(database.Database.HasPendingModelChanges());
                Assert.AreEqual(1, await database.CatalogStagingEntries.CountAsync());
            }

            using HttpClient client = app.GetTestClient();
            client.DefaultRequestHeaders.Add("X-Test-Actor", EditorId.ToString("D"));
            await AddAntiforgeryAsync(client);

            using HttpResponseMessage publish = await client.PostAsJsonAsync("/api/catalog/publish", new { versionTag = "v1" });
            Assert.AreEqual(HttpStatusCode.OK, publish.StatusCode);
            CatalogPublishResult first = (await publish.Content.ReadFromJsonAsync<CatalogPublishResult>())!;
            using HttpResponseMessage secondPublish = await client.PostAsJsonAsync("/api/catalog/publish", new { versionTag = "v2" });
            Assert.AreEqual(HttpStatusCode.OK, secondPublish.StatusCode);
            using HttpResponseMessage rollback = await client.PostAsJsonAsync($"/api/catalog/rollback/{first.VersionId:D}", new { });
            Assert.AreEqual(HttpStatusCode.OK, rollback.StatusCode);

            CatalogSearchResult search = (await client.GetFromJsonAsync<CatalogSearchResult>("/api/catalog/items?query=maiz"))!;
            Assert.AreEqual(first.VersionId, search.VersionId);
            Assert.AreEqual(1, search.TotalCount);
            Assert.AreEqual("MAIZ", search.Items.Single().Code);

            await using AsyncServiceScope verification = app.Services.CreateAsyncScope();
            CatalogDbContext persisted = verification.ServiceProvider.GetRequiredService<CatalogDbContext>();
            Assert.IsTrue(await persisted.CatalogPublishedVersions.AllAsync(version => version.PublishedBy == EditorId.ToString("D")));
            Assert.AreEqual(1, await persisted.CatalogPublishedVersions.CountAsync(version => version.IsActive));
            Assert.AreEqual(2, await persisted.CatalogPublishedItems.CountAsync());

            IngestSourceCommand additionalSource = new("another-source", Convert.ToBase64String(
                Encoding.UTF8.GetBytes("[{\"code\":\"SOJA\",\"displayName\":\"Soja\",\"jurisdiction\":\"AR\"}]")));
            using HttpResponseMessage ingest = await client.PostAsJsonAsync("/api/catalog/ingest", additionalSource);
            Assert.AreEqual(HttpStatusCode.OK, ingest.StatusCode);
            using HttpResponseMessage duplicateIngest = await client.PostAsJsonAsync("/api/catalog/ingest", additionalSource);
            Assert.AreEqual(HttpStatusCode.Conflict, duplicateIngest.StatusCode);
        }
        finally
        {
            await server.DropDatabaseAsync(connectionString, CancellationToken.None);
        }
    }

    private static string[] MutationRoutes() =>
        ["/api/catalog/ingest", "/api/catalog/publish", $"/api/catalog/rollback/{Guid.NewGuid():D}"];

    private static async Task AddAntiforgeryAsync(HttpClient client)
    {
        using HttpResponseMessage response = await client.GetAsync("/test/antiforgery");
        response.EnsureSuccessStatusCode();
        client.DefaultRequestHeaders.Add("Cookie", string.Join("; ", response.Headers.GetValues("Set-Cookie")
            .Select(value => value.Split(';')[0])));
        string token = (await response.Content.ReadFromJsonAsync<AntiforgeryToken>())!.RequestToken;
        client.DefaultRequestHeaders.Add("X-CSRF-TOKEN", token);
    }

    private static async Task<WebApplication> StartAppAsync(string connectionString)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Test" });
        builder.WebHost.UseTestServer();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:Catalog"] = connectionString,
        });
        builder.Services.AddCatalogModule(builder.Configuration);
        builder.Services.AddAuthentication("catalog-test")
            .AddScheme<AuthenticationSchemeOptions, TestAuthenticationHandler>("catalog-test", _ => { });
        builder.Services.AddAuthorization(options => options.AddPolicy(CatalogEndpoints.EditorialPolicy, policy =>
            policy.RequireAuthenticatedUser().RequireClaim(ClaimTypes.NameIdentifier, EditorId.ToString("D"))));
        builder.Services.AddAntiforgery(options => options.HeaderName = "X-CSRF-TOKEN");
        builder.Services.AddRateLimiter(options => options.AddPolicy(CatalogEndpoints.RateLimitPolicy,
            _ => RateLimitPartition.GetNoLimiter("catalog-test")));

        WebApplication app = builder.Build();
        app.Use(async (context, next) =>
        {
            try { await next(context); }
            catch (AntiforgeryValidationException) { context.Response.StatusCode = StatusCodes.Status400BadRequest; }
        });
        app.UseRateLimiter();
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseAntiforgery();
        app.MapCatalogEndpoints();
        app.MapGet("/test/antiforgery", (HttpContext context, IAntiforgery antiforgery) =>
            new AntiforgeryToken(antiforgery.GetAndStoreTokens(context).RequestToken!)).RequireAuthorization();
        await app.StartAsync();
        return app;
    }

    private sealed record AntiforgeryToken(string RequestToken);

    private sealed class TestAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (!Guid.TryParse(Request.Headers["X-Test-Actor"], out Guid actor))
                return Task.FromResult(AuthenticateResult.NoResult());

            ClaimsIdentity identity = new([
                new Claim(ClaimTypes.NameIdentifier, actor.ToString("D")),
                new Claim(IdentityAuthenticationDefaults.SessionIdClaim, Guid.NewGuid().ToString("D")),
                new Claim(IdentityAuthenticationDefaults.SessionVersionClaim, Guid.NewGuid().ToString("D")),
                new Claim("auth_time", DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture)),
            ], Scheme.Name);
            return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name)));
        }
    }
}
