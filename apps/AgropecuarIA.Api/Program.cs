using System.Net;
using System.Globalization;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using AgropecuarIA.Api;
using AgropecuarIA.Identity;
using AgropecuarIA.Identity.Delivery;
using AgropecuarIA.Identity.Infrastructure;
using AgropecuarIA.ProductiveCore;
using AgropecuarIA.ProductiveCore.Delivery;
using AgropecuarIA.ProductiveCore.Infrastructure;
using AgropecuarIA.Territory;
using AgropecuarIA.Territory.Delivery;
using AgropecuarIA.Territory.Infrastructure;
using AgropecuarIA.Catalog;
using AgropecuarIA.Catalog.Delivery;
using AgropecuarIA.Catalog.Infrastructure;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
bool applyIdentityMigrations = builder.Configuration.GetValue<bool>("Identity:ApplyMigrations");
bool applyTerritoryMigrations = builder.Configuration.GetValue<bool>("Territory:ApplyMigrations");
bool applyProductiveCoreMigrations = builder.Configuration.GetValue<bool>("ProductiveCore:ApplyMigrations");
if (applyIdentityMigrations &&
    !builder.Environment.IsDevelopment() &&
    !builder.Environment.IsEnvironment("Test"))
{
    throw new InvalidOperationException(
        "Identity:ApplyMigrations can only be enabled in Development/Test. Use an explicit migrator in shared environments.");
}
if (applyTerritoryMigrations &&
    !builder.Environment.IsDevelopment() &&
    !builder.Environment.IsEnvironment("Test"))
{
    throw new InvalidOperationException(
        "Territory:ApplyMigrations can only be enabled in Development/Test. Use an explicit migrator in shared environments.");
}
if (applyProductiveCoreMigrations &&
    !builder.Environment.IsDevelopment() &&
    !builder.Environment.IsEnvironment("Test"))
{
    throw new InvalidOperationException(
        "ProductiveCore:ApplyMigrations can only be enabled in Development/Test. Use an explicit migrator in shared environments.");
}

builder.Services.AddProblemDetails(options =>
{
    options.CustomizeProblemDetails = context =>
        context.ProblemDetails.Extensions.Remove("traceId");
});
builder.Services.AddTerritoryModule(builder.Configuration);
builder.Services.AddProductiveCoreModule(builder.Configuration);
builder.Services.AddCatalogModule(builder.Configuration);
builder.Services.AddExceptionHandler<IdentityExceptionHandler>();
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow;
});

builder.Services.AddOptions<OidcProviderOptions>()
    .Bind(builder.Configuration.GetSection(OidcProviderOptions.SectionName))
    .ValidateOnStart();
builder.Services.AddSingleton<IValidateOptions<OidcProviderOptions>, OidcProviderOptionsValidator>();
builder.Services.AddOptions<StrongAuthenticationOptions>()
    .Bind(builder.Configuration.GetSection(StrongAuthenticationOptions.SectionName))
    .ValidateOnStart();
builder.Services.AddSingleton<
    IValidateOptions<StrongAuthenticationOptions>,
    StrongAuthenticationOptionsValidator>();
builder.Services.AddOptions<DevelopmentIdentityProviderOptions>()
    .Bind(builder.Configuration.GetSection(DevelopmentIdentityProviderOptions.SectionName))
    .ValidateOnStart();
builder.Services.AddSingleton<
    IValidateOptions<DevelopmentIdentityProviderOptions>,
    DevelopmentIdentityProviderOptionsValidator>();
builder.Services.AddSingleton<DevelopmentIdentityFixtureAllocator>();
builder.Services.AddAgropecuariaIdentity(builder.Configuration);

OidcProviderOptions oidcConfiguration =
    builder.Configuration.GetSection(OidcProviderOptions.SectionName).Get<OidcProviderOptions>() ?? new();
int perIpPermitLimit = builder.Configuration.GetValue("Identity:RateLimits:PerIpPerMinute", 120);
int perSessionPermitLimit = builder.Configuration.GetValue("Identity:RateLimits:PerSessionPerMinute", 30);
int stepUpPermitLimit = builder.Configuration.GetValue(
    "Identity:RateLimits:StepUpPerSessionPerFiveMinutes",
    5);
int territoryPermitLimit = builder.Configuration.GetValue(
    "Territory:RateLimits:PerSessionPerMinute",
    60);
int productiveCorePermitLimit = builder.Configuration.GetValue(
    "ProductiveCore:RateLimits:PerSessionPerMinute",
    30);
if (perIpPermitLimit <= 0 ||
    perSessionPermitLimit <= 0 ||
    perSessionPermitLimit > perIpPermitLimit ||
    stepUpPermitLimit <= 0 ||
    stepUpPermitLimit > perSessionPermitLimit ||
    territoryPermitLimit <= 0 ||
    territoryPermitLimit > perIpPermitLimit ||
    productiveCorePermitLimit <= 0 ||
    productiveCorePermitLimit > perIpPermitLimit)
{
    throw new InvalidOperationException(
        "Rate limits must be positive; step-up cannot exceed the identity per-session limit and session policies cannot exceed per-IP.");
}
AuthenticationBuilder externalAuthentication = builder.Services.AddAuthentication()
    .AddCookie(IdentityAuthenticationDefaults.ExternalScheme, options =>
    {
        options.Cookie.Name = "__Host-agro-oidc";
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.Path = "/";
        options.ExpireTimeSpan = TimeSpan.FromMinutes(10);
        options.SlidingExpiration = false;
    });

if (oidcConfiguration.IsConfigured)
{
    externalAuthentication.AddOpenIdConnect(OpenIdConnectDefaults.AuthenticationScheme, options =>
    {
        options.Authority = oidcConfiguration.Authority;
        options.ClientId = oidcConfiguration.ClientId;
        options.ClientSecret = oidcConfiguration.ClientSecret;
        options.SignInScheme = IdentityAuthenticationDefaults.ExternalScheme;
        IdentityEndpoints.ConfigureOidc(options);
    });
}

builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-CSRF-TOKEN";
    bool localEnvironment = builder.Environment.IsDevelopment() || builder.Environment.IsEnvironment("Test");
    options.Cookie.Name = localEnvironment ? "agro-xsrf" : "__Host-agro-xsrf";
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = localEnvironment
        ? CookieSecurePolicy.SameAsRequest
        : CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.Cookie.Path = "/";
});

builder.Services.AddRateLimiter(options =>
{
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
        RateLimitPartition.GetFixedWindowLimiter(
            $"ip:{context.Connection.RemoteIpAddress?.ToString() ?? "unknown"}",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = perIpPermitLimit,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                AutoReplenishment = true,
            }));
    options.AddPolicy("identity", context =>
    {
        string partitionKey = context.Request.Cookies.TryGetValue(
            IdentityAuthenticationDefaults.SessionCookieName,
            out string? sessionToken)
            ? $"session:{Convert.ToHexString(IdentityTokenService.HashToken(sessionToken))}"
            : $"anonymous:{context.Connection.RemoteIpAddress?.ToString() ?? "unknown"}";
        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey,
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = perSessionPermitLimit,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                AutoReplenishment = true,
            });
    });
    options.AddPolicy("identity-step-up", context =>
    {
        string partitionKey = context.Request.Cookies.TryGetValue(
            IdentityAuthenticationDefaults.SessionCookieName,
            out string? sessionToken)
            ? $"step-up:{Convert.ToHexString(IdentityTokenService.HashToken(sessionToken))}"
            : $"step-up-anonymous:{context.Connection.RemoteIpAddress?.ToString() ?? "unknown"}";
        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey,
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = stepUpPermitLimit,
                Window = TimeSpan.FromMinutes(5),
                QueueLimit = 0,
                AutoReplenishment = true,
            });
    });
    options.AddPolicy(TerritoryEndpoints.RateLimitPolicy, context =>
    {
        string partitionKey = context.Request.Cookies.TryGetValue(
            IdentityAuthenticationDefaults.SessionCookieName,
            out string? sessionToken)
            ? $"territory:{Convert.ToHexString(IdentityTokenService.HashToken(sessionToken))}"
            : $"territory-anonymous:{context.Connection.RemoteIpAddress?.ToString() ?? "unknown"}";
        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey,
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = territoryPermitLimit,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                AutoReplenishment = true,
            });
    });
    options.AddPolicy(ProductiveCoreEndpoints.RateLimitPolicy, context =>
    {
        string partitionKey = context.Request.Cookies.TryGetValue(
            IdentityAuthenticationDefaults.SessionCookieName,
            out string? sessionToken)
            ? $"productive-core:{Convert.ToHexString(IdentityTokenService.HashToken(sessionToken))}"
            : $"productive-core-anonymous:{context.Connection.RemoteIpAddress?.ToString() ?? "unknown"}";
        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey,
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = productiveCorePermitLimit,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                AutoReplenishment = true,
            });
    });
    options.OnRejected = async (context, cancellationToken) =>
    {
        context.HttpContext.RequestServices
            .GetRequiredService<IdentityTelemetry>()
            .Record("request", "rate_limited");
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        context.HttpContext.Response.ContentType = "application/problem+json";
        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out TimeSpan retryAfter))
        {
            context.HttpContext.Response.Headers.RetryAfter = Math.Max(
                1,
                (int)Math.Ceiling(retryAfter.TotalSeconds)).ToString(
                    CultureInfo.InvariantCulture);
        }
        await context.HttpContext.Response.WriteAsJsonAsync(
            new
            {
                type = "https://agropecuaria.local/problems/request.rate_limited",
                title = "Too many requests.",
                status = StatusCodes.Status429TooManyRequests,
                code = "request.rate_limited",
                correlationId = context.HttpContext.TraceIdentifier,
            },
            options: null,
            contentType: "application/problem+json",
            cancellationToken);
    };
});

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.ForwardLimit = 1;
    foreach (string configuredProxy in builder.Configuration
        .GetSection("ReverseProxy:KnownProxies")
        .Get<string[]>() ?? [])
    {
        if (!IPAddress.TryParse(configuredProxy, out IPAddress? knownProxy))
        {
            throw new InvalidOperationException(
                $"ReverseProxy:KnownProxies contains an invalid IP address: {configuredProxy}");
        }

        options.KnownProxies.Add(knownProxy);
    }
});

builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation()
        .AddSource(IdentityTelemetry.SourceName)
        .AddSource(ProductiveCoreTelemetry.SourceName))
    .WithMetrics(metrics => metrics
        .AddAspNetCoreInstrumentation()
        .AddMeter(IdentityTelemetry.SourceName)
        .AddMeter(TerritoryTelemetry.SourceName)
        .AddMeter(ProductiveCoreTelemetry.SourceName));

bool applyCatalogMigrations = builder.Configuration.GetValue<bool>("Catalog:ApplyMigrations");

WebApplication app = builder.Build();

if (applyIdentityMigrations)
{
    await using AsyncServiceScope scope = app.Services.CreateAsyncScope();
    IdentityDbContext identityDbContext = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
    await identityDbContext.Database.MigrateAsync();
}
if (applyTerritoryMigrations)
{
    await using AsyncServiceScope scope = app.Services.CreateAsyncScope();
    TerritoryDbContext territoryDbContext = scope.ServiceProvider.GetRequiredService<TerritoryDbContext>();
    await territoryDbContext.Database.MigrateAsync();
}
if (applyProductiveCoreMigrations)
{
    await using AsyncServiceScope scope = app.Services.CreateAsyncScope();
    ProductiveCoreDbContext productiveCoreDbContext =
        scope.ServiceProvider.GetRequiredService<ProductiveCoreDbContext>();
    await productiveCoreDbContext.Database.MigrateAsync();
}

if (applyCatalogMigrations)
{
    await using AsyncServiceScope scope = app.Services.CreateAsyncScope();
    CatalogDbContext catalogDbContext =
        scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
    await catalogDbContext.Database.MigrateAsync();
}

app.UseExceptionHandler();
app.UseForwardedHeaders();
app.Use(async (context, next) =>
{
    if (context.Request.Path.StartsWithSegments("/api/identity") ||
        context.Request.Path.StartsWithSegments("/api/development/identity") ||
        context.Request.Path.StartsWithSegments("/api/territory") ||
        context.Request.Path.StartsWithSegments("/api/organizations"))
    {
        context.Response.Headers.CacheControl = "no-store";
        context.Response.Headers.Pragma = "no-cache";
    }

    await next(context);
});
if (!app.Environment.IsEnvironment("Test"))
{
    app.UseHttpsRedirection();
}

app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapIdentityEndpoints();
app.MapTerritoryEndpoints();
app.MapProductiveCoreEndpoints();
app.MapCatalogEndpoints();

app.Run();

public partial class Program;
