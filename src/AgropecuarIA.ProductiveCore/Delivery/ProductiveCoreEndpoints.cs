using AgropecuarIA.Identity.Application;
using AgropecuarIA.Identity.Delivery;
using AgropecuarIA.ProductiveCore.Application;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Primitives;

namespace AgropecuarIA.ProductiveCore.Delivery;

public static class ProductiveCoreEndpoints
{
    public const string RateLimitPolicy = "productive-core";

    public static IEndpointRouteBuilder MapProductiveCoreEndpoints(this IEndpointRouteBuilder endpoints)
    {
        RouteGroupBuilder organizations = endpoints
            .MapGroup("/api/organizations/{organizationId:guid}")
            .RequireAuthorization()
            .RequireRateLimiting(RateLimitPolicy);

        organizations.MapPost("/fields", async (
            Guid organizationId,
            CreateFieldRequest request,
            HttpContext context,
            IAntiforgery antiforgery,
            ProductiveCoreApplicationService service,
            CancellationToken cancellationToken) =>
        {
            SetPrivateResponseHeaders(context.Response);
            await antiforgery.ValidateRequestAsync(context);
            string idempotencyKey = ReadSingleIdempotencyKey(context.Request.Headers);
            AuthenticatedSession session = AuthenticatedSessionClaims.Read(context.User);
            CreatedManagementUnitResult created = await service.CreateFieldAsync(
                new CreateFieldCommand(organizationId, request.DisplayName, idempotencyKey),
                RequestContext(context, session, organizationId),
                cancellationToken);
            context.Response.Headers.Location =
                $"/api/organizations/{created.OrganizationId:D}/fields/{created.FieldId:D}";
            return Results.Json(
                ToCreatedResponse(created),
                statusCode: StatusCodes.Status201Created);
        });

        organizations.MapGet("/fields", async (
            Guid organizationId,
            HttpContext context,
            ProductiveCoreApplicationService service,
            CancellationToken cancellationToken) =>
        {
            SetPrivateResponseHeaders(context.Response);
            AuthenticatedSession session = AuthenticatedSessionClaims.Read(context.User);
            IReadOnlyList<ManagementUnitResult> result = await service.ListFieldsAsync(
                organizationId,
                RequestContext(context, session, organizationId),
                cancellationToken);
            return Results.Ok(result.Select(ToResponse).ToArray());
        });

        organizations.MapGet("/fields/{fieldId:guid}", async (
            Guid organizationId,
            Guid fieldId,
            HttpContext context,
            ProductiveCoreApplicationService service,
            CancellationToken cancellationToken) =>
        {
            SetPrivateResponseHeaders(context.Response);
            AuthenticatedSession session = AuthenticatedSessionClaims.Read(context.User);
            ManagementUnitResult result = await service.GetFieldAsync(
                organizationId,
                fieldId,
                RequestContext(context, session, organizationId),
                cancellationToken);
            return Results.Ok(ToResponse(result));
        });

        return endpoints;
    }

    private static ProductiveRequestContext RequestContext(
        HttpContext context,
        AuthenticatedSession session,
        Guid organizationId) =>
        new(
            context.TraceIdentifier,
            session.UserId,
            session.SessionId,
            organizationId);

    private static string ReadSingleIdempotencyKey(IHeaderDictionary headers)
    {
        if (!headers.TryGetValue("Idempotency-Key", out StringValues values) || values.Count != 1)
        {
            throw ProductiveCoreErrors.InvalidIdempotencyKey();
        }

        return values[0] ?? string.Empty;
    }

    private static void SetPrivateResponseHeaders(HttpResponse response)
    {
        response.Headers.CacheControl = "no-store";
        response.Headers.Pragma = "no-cache";
    }

    private static FieldResponse ToResponse(ManagementUnitResult field) =>
        new(
            field.FieldId,
            field.OrganizationId,
            field.DisplayName,
            field.Type,
            field.Status,
            field.SpatialStatus,
            field.CreatedAtUtc,
            field.Version);

    private static CreatedFieldResponse ToCreatedResponse(CreatedManagementUnitResult field) =>
        new(
            field.FieldId,
            field.OrganizationId,
            field.DisplayName,
            field.Type,
            field.Status,
            field.SpatialStatus,
            field.CreatedAtUtc,
            field.Version,
            field.IsReplay);

    public sealed record CreateFieldRequest(string DisplayName);

    public sealed record FieldResponse(
        Guid FieldId,
        Guid OrganizationId,
        string DisplayName,
        string Type,
        string Status,
        string SpatialStatus,
        DateTimeOffset CreatedAtUtc,
        Guid Version);

    public sealed record CreatedFieldResponse(
        Guid FieldId,
        Guid OrganizationId,
        string DisplayName,
        string Type,
        string Status,
        string SpatialStatus,
        DateTimeOffset CreatedAtUtc,
        Guid Version,
        bool IsReplay);
}
