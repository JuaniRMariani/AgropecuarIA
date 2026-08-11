using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AgropecuarIA.Identity.Tests.Infrastructure;

internal sealed class IdentityApiScenario : IAsyncDisposable
{
    private readonly PostgreSqlTestServer _postgresql;
    private readonly IdentityApiFactory _factory;

    private IdentityApiScenario(
        PostgreSqlTestServer postgresql,
        string connectionString,
        IdentityApiFactory factory)
    {
        _postgresql = postgresql;
        ConnectionString = connectionString;
        _factory = factory;
    }

    public string ConnectionString { get; }

    public TestLogSink Logs => _factory.Logs;

    public static async Task<IdentityApiScenario> CreateAsync(
        string environment = "Test",
        IReadOnlyDictionary<string, string?>? configuration = null,
        Action<IServiceCollection, string>? configureServices = null,
        CancellationToken cancellationToken = default)
    {
        if (IdentityTestAssembly.PostgreSql is null)
        {
            Assert.Fail(
                "PostgreSQL integration fixture could not start: "
                + IdentityTestAssembly.StartupError?.Message);
        }

        var postgresql = IdentityTestAssembly.PostgreSql;
        var connectionString = await postgresql.CreateDatabaseAsync(cancellationToken);
        var factory = new IdentityApiFactory(
            connectionString,
            environment,
            configuration,
            configureServices);

        try
        {
            _ = factory.Server;
            return new IdentityApiScenario(postgresql, connectionString, factory);
        }
        catch
        {
            factory.Dispose();
            await postgresql.DropDatabaseAsync(connectionString, cancellationToken);
            throw;
        }
    }

    public BrowserSession CreateBrowser(IReadOnlyDictionary<string, string>? cookies = null)
    {
        return new BrowserSession(
            _factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false,
                BaseAddress = new Uri("https://localhost"),
                HandleCookies = false,
            }),
            cookies);
    }

    public async ValueTask DisposeAsync()
    {
        _factory.Dispose();
        await _postgresql.DropDatabaseAsync(ConnectionString, CancellationToken.None);
    }

    private sealed class IdentityApiFactory(
        string connectionString,
        string environment,
        IReadOnlyDictionary<string, string?>? configuration,
        Action<IServiceCollection, string>? configureServices) : WebApplicationFactory<Program>
    {
        public TestLogSink Logs { get; } = new();

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment(environment);
            builder.UseSetting("ConnectionStrings:Identity", connectionString);
            builder.UseSetting(
                "Identity:DevelopmentProvider:Enabled",
                (environment is "Development" or "Test").ToString());
            builder.UseSetting("Identity:DevelopmentProvider:SyntheticProfileCount", "1");
            builder.UseSetting(
                "Identity:StrongAuthentication:Enabled",
                (environment is "Development" or "Test").ToString());
            builder.UseSetting(
                "Identity:ApplyMigrations",
                (environment is "Development" or "Test").ToString());
            builder.ConfigureAppConfiguration((_, configurationBuilder) =>
            {
                var values = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
                {
                    ["ConnectionStrings:Identity"] = connectionString,
                    ["Identity:DevelopmentProvider:Enabled"] =
                        (environment is "Development" or "Test").ToString(),
                    ["Identity:DevelopmentProvider:SyntheticProfileCount"] = "1",
                    ["Identity:StrongAuthentication:Enabled"] =
                        (environment is "Development" or "Test").ToString(),
                    ["Identity:ApplyMigrations"] =
                        (environment is "Development" or "Test").ToString(),
                    ["Identity:OrganizationBootstrap:Enabled"] =
                        (environment is "Development" or "Test").ToString(),
                    ["Identity:OrganizationBootstrap:CurrentKeyVersion"] = "test-v1",
                    ["Identity:OrganizationBootstrap:IdempotencyHmacKeys:test-v1"] =
                        "dGVzdC1vbmx5LWlkZW1wb3RlbmN5LWhtYWMta2V5LTMyaW4=",
                };

                if (configuration is not null)
                {
                    foreach (var item in configuration)
                    {
                        values[item.Key] = item.Value;
                    }
                }

                configurationBuilder.AddInMemoryCollection(values);
            });
            builder.ConfigureServices(services =>
            {
                services.AddSingleton<ILoggerProvider>(Logs);
                configureServices?.Invoke(services, connectionString);
            });
        }
    }
}
