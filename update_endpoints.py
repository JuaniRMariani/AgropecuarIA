import os

endpoints_path = r'B:\Xenova\AgropecuarIA\src\AgropecuarIA.ProductiveCore\Delivery\ProductiveCoreEndpoints.cs'
with open(endpoints_path, 'r', encoding='utf-8-sig') as f:
    content = f.read()

new_endpoint = '''
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
'''
content = content.replace('        return endpoints;', new_endpoint + '\\n        return endpoints;')

new_response = '''
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
'''
content = content.replace('    public sealed record RenamedFieldResponse(', new_response + '\\n    public sealed record RenamedFieldResponse(')

with open(endpoints_path, 'w', encoding='utf-8') as f:
    f.write(content)
print("Updated Endpoints.")
