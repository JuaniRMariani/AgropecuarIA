using AgropecuarIA.Identity.Application;
using AgropecuarIA.Identity.Delivery;
using AgropecuarIA.ProductiveCore.Application;
using AgropecuarIA.ProductiveCore.Domain;
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
            SetEntityTag(context.Response, result.Version);
            return Results.Ok(ToResponse(result));
        });

        organizations.MapPatch("/fields/{fieldId:guid}", async (
            Guid organizationId,
            Guid fieldId,
            RenameFieldDraftRequest request,
            HttpContext context,
            IAntiforgery antiforgery,
            ProductiveCoreRenameApplicationService service,
            CancellationToken cancellationToken) =>
        {
            SetPrivateResponseHeaders(context.Response);
            await antiforgery.ValidateRequestAsync(context);
            string idempotencyKey = ReadSingleIdempotencyKey(context.Request.Headers);
            Guid expectedVersion = ReadStrongVersion(context.Request.Headers);
            AuthenticatedSession session = AuthenticatedSessionClaims.Read(context.User);
            RenamedManagementUnitResult renamed = await service.RenameFieldDraftAsync(
                new RenameFieldDraftCommand(
                    organizationId,
                    fieldId,
                    request.DisplayName,
                    expectedVersion,
                    idempotencyKey),
                RequestContext(context, session, organizationId),
                cancellationToken);
            SetEntityTag(context.Response, renamed.Version);
            return Results.Ok(ToRenamedResponse(renamed));
        });


        organizations.MapPost("/fields/{fieldId:guid}/archive", async (
            Guid organizationId,
            Guid fieldId,
            HttpContext context,
            IAntiforgery antiforgery,
            ProductiveCoreArchiveApplicationService service,
            CancellationToken cancellationToken) =>
        {
            SetPrivateResponseHeaders(context.Response);
            await antiforgery.ValidateRequestAsync(context);
            string idempotencyKey = ReadSingleIdempotencyKey(context.Request.Headers);
            Guid expectedVersion = ReadStrongVersion(context.Request.Headers);
            AuthenticatedSession session = AuthenticatedSessionClaims.Read(context.User);
            ArchivedManagementUnitResult archived = await service.ArchiveFieldDraftAsync(
                new ArchiveFieldDraftCommand(
                    organizationId,
                    fieldId,
                    expectedVersion,
                    idempotencyKey),
                RequestContext(context, session, organizationId),
                cancellationToken);
            SetEntityTag(context.Response, archived.Version);
            return Results.Ok(ToArchivedResponse(archived));
        });

        organizations.MapPost("/fields/{fieldId:guid}/geometry", async (
            Guid organizationId,
            Guid fieldId,
            ConfigureFieldGeometryRequest request,
            HttpContext context,
            IAntiforgery antiforgery,
            ProductiveCoreGeometryApplicationService service,
            CancellationToken cancellationToken) =>
        {
            SetPrivateResponseHeaders(context.Response);
            await antiforgery.ValidateRequestAsync(context);
            Guid expectedVersion = ReadStrongVersion(context.Request.Headers);
            AuthenticatedSession session = AuthenticatedSessionClaims.Read(context.User);
            var result = await service.ConfigureGeometryAsync(
                new ConfigureFieldGeometryCommand(
                    organizationId,
                    fieldId,
                    request.BoundaryGeoJson,
                    request.DeclaredAreaHectares,
                    expectedVersion),
                RequestContext(context, session, organizationId),
                cancellationToken);
            SetEntityTag(context.Response, result.Version);
            return Results.Ok(result);
        });

        organizations.MapGet("/fields/{fieldId:guid}/geometry", async (
            Guid organizationId,
            Guid fieldId,
            HttpContext context,
            ProductiveCoreGeometryApplicationService service,
            CancellationToken cancellationToken) =>
        {
            SetPrivateResponseHeaders(context.Response);
            AuthenticatedSession session = AuthenticatedSessionClaims.Read(context.User);
            ConfiguredFieldGeometryResult result = await service.GetGeometryAsync(
                organizationId, fieldId, RequestContext(context, session, organizationId), cancellationToken);
            SetEntityTag(context.Response, result.Version);
            return Results.Ok(result);
        });

        organizations.MapPost("/fields/{fieldId:guid}/cycles", async (
            Guid organizationId,
            Guid fieldId,
            StartProductionCycleRequest request,
            HttpContext context,
            IAntiforgery antiforgery,
            ProductionCycleApplicationService service,
            CancellationToken cancellationToken) =>
        {
            SetPrivateResponseHeaders(context.Response);
            await antiforgery.ValidateRequestAsync(context);
            AuthenticatedSession session = AuthenticatedSessionClaims.Read(context.User);
            var cycle = await service.StartCycleAsync(
                new StartProductionCycleCommand(
                    organizationId,
                    fieldId,
                    request.CatalogCode,
                    request.CatalogDisplayName,
                    request.Purpose,
                    request.System,
                    request.SupportLevel,
                    request.StartDateUtc),
                RequestContext(context, session, organizationId),
                cancellationToken);
            return Results.Created(
                $"/api/organizations/{organizationId:D}/cycles/{cycle.Id:D}",
                cycle);
        });

        organizations.MapGet("/fields/{fieldId:guid}/cycles", async (
            Guid organizationId,
            Guid fieldId,
            HttpContext context,
            ProductionCycleApplicationService service,
            CancellationToken cancellationToken) =>
        {
            SetPrivateResponseHeaders(context.Response);
            AuthenticatedSession session = AuthenticatedSessionClaims.Read(context.User);
            var cycles = await service.ListCyclesAsync(organizationId, fieldId,
                RequestContext(context, session, organizationId), cancellationToken);
            return Results.Ok(cycles);
        });

        organizations.MapPost("/cycles/{cycleId:guid}/events", async (
            Guid organizationId,
            Guid cycleId,
            RecordProductionEventRequest request,
            HttpContext context,
            IAntiforgery antiforgery,
            ProductionCycleApplicationService service,
            CancellationToken cancellationToken) =>
        {
            SetPrivateResponseHeaders(context.Response);
            await antiforgery.ValidateRequestAsync(context);
            AuthenticatedSession session = AuthenticatedSessionClaims.Read(context.User);
            var evt = await service.RecordEventAsync(
                new RecordProductionEventCommand(
                    organizationId,
                    cycleId,
                    request.EventType,
                    request.EffectiveDateUtc,
                    request.Quantity,
                    request.Unit,
                    request.Notes,
                    request.Origin ?? ProductionOrigins.Manual),
                RequestContext(context, session, organizationId),
                cancellationToken);
            return Results.Created(
                $"/api/organizations/{organizationId:D}/cycles/{cycleId:D}/events/{evt.Id:D}",
                evt);
        });

        organizations.MapGet("/cycles/{cycleId:guid}/timeline", async (
            Guid organizationId,
            Guid cycleId,
            HttpContext context,
            ProductionCycleApplicationService service,
            CancellationToken cancellationToken) =>
        {
            SetPrivateResponseHeaders(context.Response);
            AuthenticatedSession session = AuthenticatedSessionClaims.Read(context.User);
            var timeline = await service.GetTimelineAsync(organizationId, cycleId,
                RequestContext(context, session, organizationId), cancellationToken);
            return timeline is not null
                ? Results.Ok(timeline)
                : Results.NotFound(new { status = "cycle_not_found" });
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

    private static Guid ReadStrongVersion(IHeaderDictionary headers)
    {
        if (!headers.TryGetValue("If-Match", out StringValues values) || values.Count != 1)
        {
            throw ProductiveCoreErrors.InvalidFieldVersion();
        }

        string value = values[0] ?? string.Empty;
        if (value.Length != 38 || value[0] != '"' || value[^1] != '"' ||
            !Guid.TryParseExact(value[1..^1], "D", out Guid version) ||
            version == Guid.Empty)
        {
            throw ProductiveCoreErrors.InvalidFieldVersion();
        }

        return version;
    }

    private static void SetPrivateResponseHeaders(HttpResponse response)
    {
        response.Headers.CacheControl = "no-store";
        response.Headers.Pragma = "no-cache";
    }

    private static void SetEntityTag(HttpResponse response, Guid version) =>
        response.Headers.ETag = $"\"{version:D}\"";

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

    private static RenamedFieldResponse ToRenamedResponse(RenamedManagementUnitResult field) =>
        new(
            field.FieldId,
            field.OrganizationId,
            field.DisplayName,
            field.Type,
            field.Status,
            field.SpatialStatus,
            field.CreatedAtUtc,
            field.Revision,
            field.Version,
            field.IsReplay);

    public sealed record CreateFieldRequest(string DisplayName);

    public sealed record RenameFieldDraftRequest(string DisplayName);

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


    private static ArchivedFieldResponse ToArchivedResponse(ArchivedManagementUnitResult field) =>
        new(
            field.FieldId,
            field.OrganizationId,
            field.DisplayName,
            field.Type,
            field.Status,
            field.SpatialStatus,
            field.CreatedAtUtc,
            field.Revision,
            field.Version,
            field.IsReplay);

    public sealed record ArchivedFieldResponse(
        Guid FieldId,
        Guid OrganizationId,
        string DisplayName,
        string Type,
        string Status,
        string SpatialStatus,
        DateTimeOffset CreatedAtUtc,
        long Revision,
        Guid Version,
        bool IsReplay);

    public sealed record RenamedFieldResponse(
        Guid FieldId,
        Guid OrganizationId,
        string DisplayName,
        string Type,
        string Status,
        string SpatialStatus,
        DateTimeOffset CreatedAtUtc,
        long Revision,
        Guid Version,
        bool IsReplay);
}
