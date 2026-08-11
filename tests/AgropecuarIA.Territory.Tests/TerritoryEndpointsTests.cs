using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading.RateLimiting;
using AgropecuarIA.Territory.Application;
using AgropecuarIA.Territory.Delivery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AgropecuarIA.Territory.Tests;

[TestClass]
public sealed class TerritoryEndpointsTests
{
    private static readonly DateTimeOffset Now =
        new(2026, 8, 11, 12, 0, 0, TimeSpan.Zero);

    [TestMethod]
    public async Task EndpointsRequireAuthentication()
    {
        await using TerritoryTestHost host = await TerritoryTestHost.StartAsync(
            new FakeReader(),
            new FakeProvider());

        HttpResponseMessage search = await host.Client.GetAsync("/api/territory/search?query=rio");
        HttpResponseMessage resolve = await host.Client.GetAsync(
            "/api/territory/resolve?latitude=-34&longitude=-58");

        Assert.AreEqual(HttpStatusCode.Unauthorized, search.StatusCode);
        Assert.AreEqual(HttpStatusCode.Unauthorized, resolve.StatusCode);
    }

    [TestMethod]
    public async Task SearchReturnsTheFrozenResponseShape()
    {
        FakeReader reader = new()
        {
            Result = new TerritoryReferenceSearchPage(
                new TerritoryReferenceSource("georef", "fixture-1", Now),
                [new("62", "Río Negro", "province", null, null, "Río Negro")]),
        };
        await using TerritoryTestHost host = await TerritoryTestHost.StartAsync(
            reader,
            new FakeProvider());
        host.Authorize();

        HttpResponseMessage response = await host.Client.GetAsync(
            "/api/territory/search?query=R%C3%8DO&level=province&limit=1");
        using JsonDocument body = JsonDocument.Parse(await response.Content.ReadAsByteArrayAsync());

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        Assert.AreEqual("fresh", body.RootElement.GetProperty("status").GetString());
        Assert.AreEqual("georef", body.RootElement.GetProperty("source").GetProperty("provider").GetString());
        JsonElement item = body.RootElement.GetProperty("items")[0];
        Assert.AreEqual("62", item.GetProperty("officialCode").GetString());
        Assert.AreEqual("Río Negro", item.GetProperty("hierarchyLabel").GetString());
        Assert.AreEqual("rio", reader.Criteria!.NormalizedQuery);
    }

    [TestMethod]
    public async Task SearchWithoutSnapshotReturnsTyped503Problem()
    {
        await using TerritoryTestHost host = await TerritoryTestHost.StartAsync(
            new FakeReader(),
            new FakeProvider());
        host.Authorize();

        HttpResponseMessage response = await host.Client.GetAsync(
            "/api/territory/search?query=rio");
        using JsonDocument problem = JsonDocument.Parse(await response.Content.ReadAsByteArrayAsync());

        Assert.AreEqual(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.AreEqual(
            "territory.reference_unavailable",
            problem.RootElement.GetProperty("code").GetString());
        Assert.IsTrue(problem.RootElement.GetProperty("retryable").GetBoolean());
    }

    [TestMethod]
    public async Task ResolveInvalidCoordinateReturnsTyped400WithoutCallingProvider()
    {
        FakeProvider provider = new();
        await using TerritoryTestHost host = await TerritoryTestHost.StartAsync(
            new FakeReader(),
            provider);
        host.Authorize();

        HttpResponseMessage response = await host.Client.GetAsync(
            "/api/territory/resolve?latitude=-90&longitude=-58");
        using JsonDocument problem = JsonDocument.Parse(await response.Content.ReadAsByteArrayAsync());

        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.AreEqual(
            "territory.invalid_coordinates",
            problem.RootElement.GetProperty("code").GetString());
        Assert.AreEqual(0, provider.Calls);
    }

    [TestMethod]
    public async Task ResolveUnavailableHasManualSearchFallbackAndRatePolicyIsEnforced()
    {
        await using TerritoryTestHost host = await TerritoryTestHost.StartAsync(
            new FakeReader(),
            new FakeProvider
            {
                Exception = new TerritoryProviderException("offline"),
            },
            permitLimit: 1);
        host.Authorize();

        HttpResponseMessage first = await host.Client.GetAsync(
            "/api/territory/resolve?latitude=-34&longitude=-58");
        TerritoryResolveResponse? body = await first.Content
            .ReadFromJsonAsync<TerritoryResolveResponse>();
        HttpResponseMessage second = await host.Client.GetAsync(
            "/api/territory/resolve?latitude=-34&longitude=-58");

        Assert.AreEqual(HttpStatusCode.OK, first.StatusCode);
        Assert.IsNotNull(body);
        Assert.AreEqual("unavailable", body.Status);
        Assert.IsTrue(body.Fallback.SearchAvailable);
        Assert.AreEqual(HttpStatusCode.TooManyRequests, second.StatusCode);
    }

    private sealed class TerritoryTestHost : IAsyncDisposable
    {
        private TerritoryTestHost(WebApplication application, HttpClient client)
        {
            Application = application;
            Client = client;
        }

        private WebApplication Application { get; }

        public HttpClient Client { get; }

        public static async Task<TerritoryTestHost> StartAsync(
            ITerritoryReferenceReader reader,
            ITerritoryCoordinateProvider provider,
            int permitLimit = 100)
        {
            WebApplicationBuilder builder = WebApplication.CreateBuilder(new WebApplicationOptions
            {
                EnvironmentName = "Test",
            });
            builder.WebHost.UseTestServer();
            builder.Services.AddProblemDetails();
            builder.Services.AddMetrics();
            builder.Services.AddExceptionHandler<TerritoryExceptionHandler>();
            builder.Services.AddAuthentication(TestAuthenticationHandler.AuthenticationSchemeName)
                .AddScheme<AuthenticationSchemeOptions, TestAuthenticationHandler>(
                    TestAuthenticationHandler.AuthenticationSchemeName,
                    _ => { });
            builder.Services.AddAuthorization();
            builder.Services.AddRateLimiter(options =>
            {
                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
                options.AddPolicy(TerritoryEndpoints.RateLimitPolicy, _ =>
                    RateLimitPartition.GetFixedWindowLimiter(
                        "territory-test",
                        _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = permitLimit,
                            QueueLimit = 0,
                            Window = TimeSpan.FromHours(1),
                            AutoReplenishment = false,
                        }));
            });
            TerritoryReferenceOptions options = new()
            {
                CoordinateResolutionEnabled = true,
            };
            builder.Services.AddSingleton<IOptions<TerritoryReferenceOptions>>(
                Options.Create(options));
            builder.Services.AddSingleton(new TerritoryResolutionCache(options));
            builder.Services.AddSingleton(TimeProvider.System);
            builder.Services.AddSingleton<TerritoryTelemetry>();
            builder.Services.AddSingleton(reader);
            builder.Services.AddSingleton(provider);
            builder.Services.AddScoped<TerritoryReferenceService>();

            WebApplication application = builder.Build();
            application.UseExceptionHandler();
            application.UseAuthentication();
            application.UseAuthorization();
            application.UseRateLimiter();
            application.MapTerritoryEndpoints();
            await application.StartAsync();
            return new TerritoryTestHost(application, application.GetTestClient());
        }

        public void Authorize() => Client.DefaultRequestHeaders.Add("X-Test-Auth", "true");

        public async ValueTask DisposeAsync()
        {
            Client.Dispose();
            await Application.DisposeAsync();
        }
    }

    private sealed class FakeReader : ITerritoryReferenceReader
    {
        public TerritoryReferenceSearchPage? Result { get; init; }

        public TerritoryReferenceSearchCriteria? Criteria { get; private set; }

        public Task<TerritoryReferenceSearchPage?> SearchAsync(
            TerritoryReferenceSearchCriteria criteria,
            CancellationToken cancellationToken)
        {
            Criteria = criteria;
            return Task.FromResult(Result);
        }
    }

    private sealed class FakeProvider : ITerritoryCoordinateProvider
    {
        public TerritoryProviderException? Exception { get; init; }

        public int Calls { get; private set; }

        public Task<ProviderTerritoryResolution?> ResolveAsync(
            double latitude,
            double longitude,
            CancellationToken cancellationToken)
        {
            Calls++;
            if (Exception is not null)
            {
                throw Exception;
            }

            return Task.FromResult<ProviderTerritoryResolution?>(null);
        }
    }

    private sealed class TestAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        public const string AuthenticationSchemeName = "territory-test";

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (!Request.Headers.ContainsKey("X-Test-Auth"))
            {
                return Task.FromResult(AuthenticateResult.NoResult());
            }

            ClaimsIdentity identity = new(
                [new Claim(ClaimTypes.NameIdentifier, "test-user")],
                AuthenticationSchemeName);
            ClaimsPrincipal principal = new(identity);
            AuthenticationTicket ticket = new(principal, AuthenticationSchemeName);
            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
    }
}
