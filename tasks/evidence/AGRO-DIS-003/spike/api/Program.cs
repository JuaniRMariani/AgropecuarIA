using AgropecuarIA.IdentitySpike.Api.Auditing;
using AgropecuarIA.IdentitySpike.Api.Common;
using AgropecuarIA.IdentitySpike.Api.Data;
using AgropecuarIA.IdentitySpike.Api.Fixtures;
using AgropecuarIA.IdentitySpike.Api.Linking;
using AgropecuarIA.IdentitySpike.Api.Recovery;
using AgropecuarIA.IdentitySpike.Api.Sessions;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails(options =>
{
    options.CustomizeProblemDetails = context =>
    {
        context.ProblemDetails.Extensions["correlationId"] =
            CorrelationIdAccessor.Get(context.HttpContext);
    };
});
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = AntiforgeryEndpoints.HeaderName;
    options.Cookie.Name = "__Host-agro-dis003-antiforgery";
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Lax;
});
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton(_ => new FixtureIdentityDirectory());
builder.Services.AddSingleton<SessionStore>();
builder.Services.AddSingleton<IMembershipDiscoveryRepository>(services =>
{
    var configuration = services.GetRequiredService<IConfiguration>();
    var connectionString = configuration["IdentitySpike:DiscoveryConnectionString"];

    if (string.IsNullOrWhiteSpace(connectionString))
    {
        throw new InvalidOperationException(
            "IdentitySpike:DiscoveryConnectionString is required at runtime. " +
            "Membership discovery must not reuse the tenant application principal.");
    }

    return new PostgresMembershipDiscoveryRepository(connectionString);
});
builder.Services.AddSingleton<SessionContextService>();
builder.Services.AddSingleton<ReauthenticationProofStore>();
builder.Services.AddSingleton<LinkAttemptService>();
builder.Services.AddSingleton<RecoveryRequestService>();
builder.Services.AddSingleton<RecoveryChallengeStore>();
builder.Services.AddSingleton<NpgsqlDataSource>(services =>
{
    var configuration = services.GetRequiredService<IConfiguration>();
    var connectionString = configuration.GetConnectionString("IdentitySpike");

    if (string.IsNullOrWhiteSpace(connectionString))
    {
        throw new InvalidOperationException(
            "ConnectionStrings:IdentitySpike is required at runtime. The R0 spike does not contain credentials.");
    }

    return NpgsqlDataSource.Create(connectionString);
});
builder.Services.AddSingleton<TenantRecordRepository>();
builder.Services.AddSingleton<AuditEventRepository>();

var app = builder.Build();

await app.Services
    .GetRequiredService<IMembershipDiscoveryRepository>()
    .ValidateConfigurationAsync(CancellationToken.None);

app.UseExceptionHandler();
app.Use(async (context, next) =>
{
    var correlationId = CorrelationIdAccessor.Get(context);
    context.Response.Headers[CorrelationIdAccessor.HeaderName] = correlationId.ToString("D");
    if (context.Request.Path.StartsWithSegments("/api/spike") ||
        context.Request.Path.StartsWithSegments("/__fixtures"))
    {
        context.Response.Headers.CacheControl = "no-store";
    }

    await next(context);
});

var spikeApi = app.MapGroup("/api/spike");
AntiforgeryEndpoints.Map(spikeApi);
SessionEndpoints.Map(spikeApi);
LinkingEndpoints.Map(spikeApi);
RecoveryEndpoints.Map(spikeApi);
TenantRecordEndpoints.Map(spikeApi);

#if DEBUG
if (app.Environment.IsEnvironment("Spike"))
{
    SpikeFixtureSafety.EnsureLoopbackOnly(app.Configuration);
    FixtureEndpoints.Map(app.MapGroup("/__fixtures"));
}
#endif

app.Run();

public partial class Program;
