using System.Text.Json;
using AgropecuarIA.ProductiveCore.Application;
using AgropecuarIA.ProductiveCore.Delivery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Primitives;
using System.Reflection;
using System.Threading.RateLimiting;

namespace AgropecuarIA.ProductiveCore.Tests;

[TestClass]
public sealed class ProductiveCoreEndpointContractTests
{
    private static readonly JsonSerializerOptions WebJson = new(JsonSerializerDefaults.Web);

    [TestMethod]
    public async Task RoutesMatchFrozenContractAndRequireAuthorizationAndRateLimiting()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.Services.AddAuthorization();
        builder.Services.AddAntiforgery();
        builder.Services.AddScoped<ProductiveCoreApplicationService>(_ => null!);
        builder.Services.AddScoped<ProductiveCoreGeometryApplicationService>(_ => null!);
        builder.Services.AddScoped<ProductiveCoreRenameApplicationService>(_ => null!);
        builder.Services.AddScoped<ProductiveCoreArchiveApplicationService>(_ => null!);
        builder.Services.AddScoped<ProductionCycleApplicationService>(_ => null!);
        builder.Services.AddRateLimiter(options => options.AddPolicy(
            ProductiveCoreEndpoints.RateLimitPolicy,
            _ => RateLimitPartition.GetNoLimiter("test")));
        await using WebApplication app = builder.Build();
        app.MapProductiveCoreEndpoints();

        RouteEndpoint[] routes = ((IEndpointRouteBuilder)app).DataSources
            .SelectMany(source => source.Endpoints)
            .OfType<RouteEndpoint>()
            .OrderBy(endpoint => endpoint.RoutePattern.RawText, StringComparer.Ordinal)
            .ThenBy(endpoint => string.Join(',', endpoint.Metadata.GetMetadata<IHttpMethodMetadata>()?.HttpMethods ?? []))
            .ToArray();

        Assert.AreEqual(11, routes.Length);
        AssertRoute(routes, "/api/organizations/{organizationId:guid}/fields", "GET");
        AssertRoute(routes, "/api/organizations/{organizationId:guid}/fields", "POST");
        AssertRoute(routes, "/api/organizations/{organizationId:guid}/fields/{fieldId:guid}", "GET");
        AssertRoute(routes, "/api/organizations/{organizationId:guid}/fields/{fieldId:guid}", "PATCH");
        AssertRoute(routes, "/api/organizations/{organizationId:guid}/fields/{fieldId:guid}/archive", "POST");
        AssertRoute(routes, "/api/organizations/{organizationId:guid}/fields/{fieldId:guid}/geometry", "POST");
        AssertRoute(routes, "/api/organizations/{organizationId:guid}/fields/{fieldId:guid}/geometry", "GET");
        AssertRoute(routes, "/api/organizations/{organizationId:guid}/fields/{fieldId:guid}/cycles", "GET");
        AssertRoute(routes, "/api/organizations/{organizationId:guid}/fields/{fieldId:guid}/cycles", "POST");
        AssertRoute(routes, "/api/organizations/{organizationId:guid}/cycles/{cycleId:guid}/events", "POST");
        AssertRoute(routes, "/api/organizations/{organizationId:guid}/cycles/{cycleId:guid}/timeline", "GET");
        Assert.IsTrue(routes.All(route => route.Metadata.GetMetadata<IAuthorizeData>() is not null));
        Assert.IsTrue(routes.All(route =>
            route.Metadata.GetMetadata<EnableRateLimitingAttribute>()?.PolicyName ==
            ProductiveCoreEndpoints.RateLimitPolicy));
    }

    [TestMethod]
    public void ResponseDtosSerializeWithExactFlatShapes()
    {
        Guid fieldId = Guid.NewGuid();
        Guid organizationId = Guid.NewGuid();
        DateTimeOffset createdAtUtc = new(2026, 8, 18, 18, 0, 0, TimeSpan.Zero);
        ProductiveCoreEndpoints.FieldResponse detail = new(
            fieldId,
            organizationId,
            "Campo Norte",
            "field",
            "draft",
            "not_configured",
            createdAtUtc,
            Guid.NewGuid());
        ProductiveCoreEndpoints.CreatedFieldResponse created = new(
            detail.FieldId,
            detail.OrganizationId,
            detail.DisplayName,
            detail.Type,
            detail.Status,
            detail.SpatialStatus,
            detail.CreatedAtUtc,
            detail.Version,
            true);
        ProductiveCoreEndpoints.RenamedFieldResponse renamed = new(
            detail.FieldId,
            detail.OrganizationId,
            "Campo Sur",
            detail.Type,
            detail.Status,
            detail.SpatialStatus,
            detail.CreatedAtUtc,
            2,
            Guid.NewGuid(),
            false);

        using JsonDocument detailJson = JsonDocument.Parse(JsonSerializer.Serialize(detail, WebJson));
        using JsonDocument createdJson = JsonDocument.Parse(JsonSerializer.Serialize(created, WebJson));
        using JsonDocument renamedJson = JsonDocument.Parse(JsonSerializer.Serialize(renamed, WebJson));

        Assert.AreEqual(fieldId, detailJson.RootElement.GetProperty("fieldId").GetGuid());
        Assert.AreEqual("field", detailJson.RootElement.GetProperty("type").GetString());
        Assert.AreEqual("not_configured", detailJson.RootElement.GetProperty("spatialStatus").GetString());
        Assert.IsFalse(detailJson.RootElement.TryGetProperty("isReplay", out _));
        Assert.IsTrue(createdJson.RootElement.GetProperty("isReplay").GetBoolean());
        Assert.AreEqual(9, createdJson.RootElement.EnumerateObject().Count());
        Assert.AreEqual(2, renamedJson.RootElement.GetProperty("revision").GetInt64());
        Assert.IsFalse(renamedJson.RootElement.GetProperty("isReplay").GetBoolean());
        Assert.AreEqual(10, renamedJson.RootElement.EnumerateObject().Count());
    }

    [TestMethod]
    public void RenameAcceptsOnlyASingleStrongQuotedUuidEntityTag()
    {
        Guid version = Guid.NewGuid();
        var validHeaders = new HeaderDictionary
        {
            ["If-Match"] = $"\"{version:D}\"",
        };

        Assert.AreEqual(version, InvokeStrongVersionParser(validHeaders));

        foreach (string invalidValue in new[]
        {
            version.ToString("D"),
            $"W/\"{version:D}\"",
            "\"00000000-0000-0000-0000-000000000000\"",
            "\"not-a-uuid\"",
        })
        {
            var invalidHeaders = new HeaderDictionary { ["If-Match"] = invalidValue };
            AssertInvalidFieldVersion(invalidHeaders);
        }

        var repeatedHeaders = new HeaderDictionary
        {
            ["If-Match"] = new StringValues([$"\"{version:D}\"", $"\"{Guid.NewGuid():D}\""]),
        };
        AssertInvalidFieldVersion(repeatedHeaders);
        AssertInvalidFieldVersion(new HeaderDictionary());
    }

    [TestMethod]
    public void DetailAndRenameEntityTagWriterUsesAStrongUuidVersion()
    {
        Guid version = Guid.NewGuid();
        var context = new DefaultHttpContext();
        MethodInfo writer = typeof(ProductiveCoreEndpoints).GetMethod(
            "SetEntityTag",
            BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new InvalidOperationException("The entity-tag writer is missing.");

        _ = writer.Invoke(null, [context.Response, version]);

        Assert.AreEqual($"\"{version:D}\"", context.Response.Headers.ETag.ToString());
    }

    private static void AssertRoute(
        IEnumerable<RouteEndpoint> routes,
        string pattern,
        string method)
    {
        Assert.IsTrue(routes.Any(route =>
            string.Equals(
                route.RoutePattern.RawText?.TrimStart('/'),
                pattern.TrimStart('/'),
                StringComparison.Ordinal) &&
            route.Metadata.GetMetadata<IHttpMethodMetadata>()?.HttpMethods.Contains(
                method,
                StringComparer.Ordinal) == true),
            string.Join(
                "; ",
                routes.Select(route => string.Concat(
                    route.RoutePattern.RawText,
                    " [",
                    string.Join(',', route.Metadata.GetMetadata<IHttpMethodMetadata>()?.HttpMethods ?? []),
                    "]"))));
    }

    private static Guid InvokeStrongVersionParser(IHeaderDictionary headers)
    {
        MethodInfo parser = typeof(ProductiveCoreEndpoints).GetMethod(
            "ReadStrongVersion",
            BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new InvalidOperationException("The strong entity-tag parser is missing.");
        return (Guid)(parser.Invoke(null, [headers])
            ?? throw new InvalidOperationException("The strong entity-tag parser returned no version."));
    }

    private static void AssertInvalidFieldVersion(IHeaderDictionary headers)
    {
        TargetInvocationException wrapper = Assert.ThrowsExactly<TargetInvocationException>(
            () => InvokeStrongVersionParser(headers));
        ProductiveCoreOperationException error =
            wrapper.InnerException as ProductiveCoreOperationException
            ?? throw new InvalidOperationException("The endpoint returned an unexpected error.");
        Assert.AreEqual("productive_core.invalid_field_version", error.Code);
        Assert.AreEqual(400, error.StatusCode);
    }
}
