using System.Text.Json;
using AgropecuarIA.ProductiveCore.Application;
using AgropecuarIA.ProductiveCore.Delivery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
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

        Assert.AreEqual(3, routes.Length);
        AssertRoute(routes, "/api/organizations/{organizationId:guid}/fields", "GET");
        AssertRoute(routes, "/api/organizations/{organizationId:guid}/fields", "POST");
        AssertRoute(routes, "/api/organizations/{organizationId:guid}/fields/{fieldId:guid}", "GET");
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

        using JsonDocument detailJson = JsonDocument.Parse(JsonSerializer.Serialize(detail, WebJson));
        using JsonDocument createdJson = JsonDocument.Parse(JsonSerializer.Serialize(created, WebJson));

        Assert.AreEqual(fieldId, detailJson.RootElement.GetProperty("fieldId").GetGuid());
        Assert.AreEqual("field", detailJson.RootElement.GetProperty("type").GetString());
        Assert.AreEqual("not_configured", detailJson.RootElement.GetProperty("spatialStatus").GetString());
        Assert.IsFalse(detailJson.RootElement.TryGetProperty("isReplay", out _));
        Assert.IsTrue(createdJson.RootElement.GetProperty("isReplay").GetBoolean());
        Assert.AreEqual(9, createdJson.RootElement.EnumerateObject().Count());
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
}
