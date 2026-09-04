using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
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

        Assert.AreEqual(7, routes.Length);
        foreach (RouteEndpoint route in routes)
        {
            Assert.IsTrue(route.Metadata.GetOrderedMetadata<IAuthorizeData>().Count > 0);
            Assert.AreEqual(CatalogEndpoints.RateLimitPolicy,
                route.Metadata.GetMetadata<EnableRateLimitingAttribute>()?.PolicyName);
            bool mutation = route.Metadata.GetMetadata<IHttpMethodMetadata>()!.HttpMethods.Contains("POST");
            bool editorial = mutation || route.RoutePattern.RawText == "/api/catalog/diff";
            Assert.AreEqual(editorial, route.Metadata.GetOrderedMetadata<IAuthorizeData>()
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
        using HttpResponseMessage forbiddenDiff = await client.GetAsync("/api/catalog/diff");
        Assert.AreEqual(HttpStatusCode.Forbidden, forbiddenDiff.StatusCode);
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
                "/api/catalog/publish" => new CatalogEndpoints.PublishCatalogRequest("v1", new string('a', 64)),
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
                byte[] legacyHash = System.Security.Cryptography.SHA256.HashData("legacy-unverified"u8);
                await database.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO catalog.catalog_source_snapshots (\"Id\",\"SourceId\",\"ContentHash\",\"CreatedAtUtc\") VALUES ({Guid.NewGuid()},{"legacy-source"},{legacyHash},{DateTimeOffset.UtcNow})");
                await database.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO catalog.catalog_staging_entries (\"Id\",\"SourceId\",\"SourceHash\",\"Code\",\"DisplayName\",\"Jurisdiction\",\"CreatedAtUtc\") VALUES ({Guid.NewGuid()},{"legacy-source"},{legacyHash},{"OLD"},{"Legacy"},{"AR"},{DateTimeOffset.UtcNow})");
                await database.Database.MigrateAsync();
                Assert.IsFalse(database.Database.HasPendingModelChanges());
                Assert.AreEqual(1, await database.CatalogStagingEntries.CountAsync());
            }

            using HttpClient client = app.GetTestClient();
            client.DefaultRequestHeaders.Add("X-Test-Actor", EditorId.ToString("D"));
            await AddAntiforgeryAsync(client);

            using HttpResponseMessage source = await client.PostAsJsonAsync("/api/catalog/ingest", new IngestSourceCommand("verified-source", Convert.ToBase64String(Encoding.UTF8.GetBytes("[{\"code\":\"MAIZ\",\"displayName\":\"Maíz\",\"jurisdiction\":\"AR\"}]"))));
            Assert.AreEqual(HttpStatusCode.OK, source.StatusCode, await source.Content.ReadAsStringAsync());
            CatalogEditorialDiffResult diff = (await client.GetFromJsonAsync<CatalogEditorialDiffResult>("/api/catalog/diff"))!;
            using HttpResponseMessage publish = await client.PostAsJsonAsync("/api/catalog/publish", new { versionTag = "v1", candidateHash = diff.CandidateHash });
            Assert.AreEqual(HttpStatusCode.OK, publish.StatusCode);
            CatalogPublishResult first = (await publish.Content.ReadFromJsonAsync<CatalogPublishResult>())!;
            diff = (await client.GetFromJsonAsync<CatalogEditorialDiffResult>("/api/catalog/diff"))!;
            using HttpResponseMessage secondPublish = await client.PostAsJsonAsync("/api/catalog/publish", new { versionTag = "v2", candidateHash = diff.CandidateHash });
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

    [TestMethod]
    public async Task ReviewedCandidatePublicationAndHistoricalReaderExposeCanonicalVersionAndProvenance()
    {
        await using CatalogDatabaseScenario scenario = await CatalogDatabaseScenario.CreateAsync();
        await using WebApplication app = await StartAppAsync(scenario.ConnectionString);
        using HttpClient client = app.GetTestClient();
        client.DefaultRequestHeaders.Add("X-Test-Actor", EditorId.ToString("D"));
        await AddAntiforgeryAsync(client);
        using HttpResponseMessage ingested = await client.PostAsJsonAsync("/api/catalog/ingest", new IngestSourceCommand("http-fixture",
            Convert.ToBase64String("[{\"code\":\"MAIZ\",\"displayName\":\"Maiz original\",\"synonyms\":[\"Choclo\"]}]"u8)));
        Assert.AreEqual(HttpStatusCode.OK, ingested.StatusCode);
        using HttpResponseMessage missingHash = await client.PostAsJsonAsync("/api/catalog/publish", new { versionTag = "missing" });
        Assert.AreEqual(HttpStatusCode.BadRequest, missingHash.StatusCode);
        CatalogEditorialDiffResult reviewed = (await client.GetFromJsonAsync<CatalogEditorialDiffResult>("/api/catalog/diff"))!;
        using HttpResponseMessage published = await client.PostAsJsonAsync("/api/catalog/publish", new { versionTag = "http-v1", candidateHash = reviewed.CandidateHash });
        Assert.AreEqual(HttpStatusCode.OK, published.StatusCode);
        CatalogPublishResult first = (await published.Content.ReadFromJsonAsync<CatalogPublishResult>())!;
        using HttpResponseMessage stale = await client.PostAsJsonAsync("/api/catalog/publish", new { versionTag = "stale", candidateHash = reviewed.CandidateHash });
        await AssertProblemAsync(stale, HttpStatusCode.Conflict, "catalog.candidate_stale");
        using HttpResponseMessage invalid = await client.PostAsJsonAsync("/api/catalog/ingest", new IngestSourceCommand("http-fixture",
            Convert.ToBase64String("[{\"code\":\"X\"}]"u8)));
        await AssertProblemAsync(invalid, HttpStatusCode.BadRequest, "catalog.invalid_source");
        await scenario.IngestAsync("http-fixture", "[{\"code\":\"MAIZ\",\"displayName\":\"Maiz revised\",\"synonyms\":[\"Choclo\"]}]");
        CatalogPublishResult second = await scenario.PublishAsync("http-v2");
        client.DefaultRequestHeaders.Remove("X-Test-Actor");
        client.DefaultRequestHeaders.Add("X-Test-Actor", Guid.NewGuid().ToString("D"));
        CatalogSearchResult historical = (await client.GetFromJsonAsync<CatalogSearchResult>($"/api/catalog/items?query=choclo&versionId={first.VersionId:D}"))!;
        Assert.IsTrue(historical.IsHistorical);
        Assert.AreEqual(second.VersionId, historical.ActiveVersionId);
        Assert.AreEqual("Maiz original", historical.Items.Single().DisplayName);
        CatalogPublishedItemDto detail = (await client.GetFromJsonAsync<CatalogPublishedItemDto>($"/api/catalog/items/maiz?versionId={first.VersionId:D}"))!;
        Assert.AreEqual(historical.Items.Single().Id, detail.Id);
        Assert.AreEqual(first.VersionId, detail.VersionId);
        Assert.AreEqual("verified_snapshot", detail.ProvenanceStatus);
        Assert.AreEqual(reviewed.SelectedSnapshots.Single().SnapshotId, detail.SourceSnapshotId);
        Assert.AreEqual(reviewed.SelectedSnapshots.Single().ContentHash, detail.SourceHash);
        Assert.IsNotNull(detail.SourceIngestedAtUtc);
        Assert.AreEqual("http-fixture", detail.SourceId);
        Assert.AreEqual("FLUJO_GENERICO", detail.SupportLevel);
        Assert.HasCount(0, detail.Capabilities);
        CatalogVersionsResult versions = (await client.GetFromJsonAsync<CatalogVersionsResult>("/api/catalog/versions?limit=1&offset=0"))!;
        Assert.AreEqual(2, versions.TotalCount);
        Assert.IsTrue(versions.HasMore);
        Assert.AreEqual(second.VersionId, versions.ActiveVersionId);
        Assert.AreEqual(second.VersionId, versions.Versions.Single().Id);
        using HttpResponseMessage missing = await client.GetAsync($"/api/catalog/items?versionId={Guid.NewGuid():D}");
        await AssertProblemAsync(missing, HttpStatusCode.NotFound, "catalog.version_not_found");
        using HttpResponseMessage bounds = await client.GetAsync("/api/catalog/versions?limit=101");
        await AssertProblemAsync(bounds, HttpStatusCode.BadRequest, "catalog.invalid_request");
    }

    [TestMethod]
    public async Task EditorialDatabaseFailureReturnsSafeUnavailableAndPreservesAllCommittedSinks()
    {
        await using CatalogDatabaseScenario scenario = await CatalogDatabaseScenario.CreateAsync();
        await scenario.IngestAsync("fixture", "[{\"code\":\"X\",\"displayName\":\"Synthetic\"}]");
        await scenario.PublishAsync("before");
        CatalogEditorialDiffResult diff = await scenario.DiffAsync();
        long[] before = await scenario.CountsAsync();
        await scenario.ExecuteAsync("""
            CREATE FUNCTION catalog.http_fault() RETURNS trigger LANGUAGE plpgsql AS $$ BEGIN RAISE EXCEPTION 'PRIVATE_SQL_PAYLOAD_PATH'; END $$;
            CREATE TRIGGER http_fault BEFORE INSERT ON catalog.catalog_outbox_messages FOR EACH ROW EXECUTE FUNCTION catalog.http_fault();
            """);
        await using WebApplication app = await StartAppAsync(scenario.ConnectionString);
        using HttpClient client = app.GetTestClient();
        client.DefaultRequestHeaders.Add("X-Test-Actor", EditorId.ToString("D"));
        await AddAntiforgeryAsync(client);
        using HttpResponseMessage failed = await client.PostAsJsonAsync("/api/catalog/publish", new { versionTag = "must-rollback", candidateHash = diff.CandidateHash });
        await AssertProblemAsync(failed, HttpStatusCode.ServiceUnavailable, "catalog.unavailable");
        string body = await failed.Content.ReadAsStringAsync();
        Assert.DoesNotContain("PRIVATE_SQL_PAYLOAD_PATH", body);
        using JsonDocument problem = JsonDocument.Parse(body);
        Assert.IsFalse(problem.RootElement.GetProperty("retryable").GetBoolean());
        CollectionAssert.AreEqual(before, await scenario.CountsAsync());
    }

    private static async Task AssertProblemAsync(HttpResponseMessage response, HttpStatusCode status, string code)
    {
        Assert.AreEqual(status, response.StatusCode, await response.Content.ReadAsStringAsync());
        Assert.AreEqual("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        using JsonDocument problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.AreEqual(code, problem.RootElement.GetProperty("code").GetString());
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
