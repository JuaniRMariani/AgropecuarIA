using System.Text.Json;

namespace AgropecuarIA.ArchitectureFitness;

public static class PublishedSchemaValidator
{
    private const string SchemaDialect = "https://json-schema.org/draft/2020-12/schema";

    private static readonly HashSet<string> ExpectedFiles = new(StringComparer.Ordinal)
    {
        "cursor-page.v1.schema.json",
        "event-envelope.v1.schema.json",
        "identity-linked.v1.schema.json",
        "identity-step-up-completed.v1.schema.json",
        "organization-created.v1.schema.json",
        "organization-owner-invited.v1.schema.json",
        "organization-owner-invitation-accepted.v1.schema.json",
        "organization-owner-invitation-revoked.v1.schema.json",
        "organization-owner-membership-removed.v1.schema.json",
        "problem-details.v1.schema.json",
        "request-scope.v1.schema.json",
    };

    private static readonly HashSet<string> ForbiddenPublicProperties = new(StringComparer.OrdinalIgnoreCase)
    {
        "tenantId",
        "userId",
        "resourceId",
        "cuit",
        "coordinates",
        "stack",
        "sql",
        "payload",
        "detail",
    };

    public static IReadOnlyList<ValidationIssue> ValidateDirectory(string directory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(directory);
        var issues = new List<ValidationIssue>();
        var files = Directory.GetFiles(directory, "*.schema.json")
            .Select(path => Path.GetFileName(path))
            .ToHashSet(StringComparer.Ordinal);

        foreach (var missing in ExpectedFiles.Except(files).Order(StringComparer.Ordinal))
        {
            Add(issues, "schema.missing", $"Published schema '{missing}' is missing.");
        }

        foreach (var unexpected in files.Except(ExpectedFiles).Order(StringComparer.Ordinal))
        {
            Add(issues, "schema.unregistered", $"Published schema '{unexpected}' is not registered.");
        }

        foreach (var file in files.Intersect(ExpectedFiles).Order(StringComparer.Ordinal))
        {
            var path = Path.Combine(directory, file);
            try
            {
                issues.AddRange(Validate(file, File.ReadAllText(path)));
            }
            catch (JsonException exception)
            {
                Add(issues, "schema.json.invalid", $"Schema '{file}' is invalid JSON: {exception.Message}");
            }
        }

        return Order(issues);
    }

    public static IReadOnlyList<ValidationIssue> Validate(string fileName, string json)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
        ArgumentException.ThrowIfNullOrWhiteSpace(json);
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        var issues = new List<ValidationIssue>();

        RequireString(root, "$schema", SchemaDialect, fileName, issues);
        RequireNonBlankString(root, "$id", fileName, issues);
        RequireNonBlankString(root, "title", fileName, issues);

        switch (fileName)
        {
            case "request-scope.v1.schema.json":
                ValidateRequestScope(root, issues);
                break;
            case "problem-details.v1.schema.json":
                ValidateClosedObject(root, fileName, ["type", "title", "status", "code", "correlationId"], issues);
                ValidateNoSensitiveProperties(root, fileName, allowPayload: false, issues);
                break;
            case "cursor-page.v1.schema.json":
                ValidateClosedObject(root, fileName, ["items", "hasMore"], issues);
                if (!TryProperty(root, "properties", "items", out var items)
                    || !items.TryGetProperty("maxItems", out var maxItems)
                    || !maxItems.TryGetInt32(out var limit)
                    || limit is < 1 or > 200)
                {
                    Add(issues, "schema.cursor.limit-invalid", "Cursor page must cap items between 1 and 200.");
                }
                break;
            case "event-envelope.v1.schema.json":
                ValidateClosedObject(
                    root,
                    fileName,
                    ["eventId", "eventType", "schemaVersion", "source", "scope", "occurredAt", "recordedAt", "correlationId", "aggregateId", "aggregateVersion", "payload"],
                    issues);
                ValidateNoSensitiveProperties(root, fileName, allowPayload: true, issues);
                if (!TryProperty(root, "properties", "scope", out var scope)
                    || !scope.TryGetProperty("$ref", out var reference)
                    || !string.Equals(reference.GetString(), "request-scope.v1.schema.json", StringComparison.Ordinal))
                {
                    Add(issues, "schema.event.scope-invalid", "Event scope must reference the discriminated request scope contract.");
                }
                break;
            case "identity-linked.v1.schema.json":
                ValidateClosedObject(root, fileName, ["userId", "identityId", "connection", "linkedAtUtc"], issues);
                ValidateExactProperties(root, fileName, ["userId", "identityId", "connection", "linkedAtUtc"], issues);
                ValidateStringProperty(root, fileName, "userId", "uuid", issues);
                ValidateStringProperty(root, fileName, "identityId", "uuid", issues);
                ValidateStringProperty(root, fileName, "connection", null, issues);
                ValidateStringProperty(root, fileName, "linkedAtUtc", "date-time", issues);
                break;
            case "identity-step-up-completed.v1.schema.json":
                ValidateClosedObject(root, fileName, ["userId", "previousSessionId", "sessionId", "purpose", "completedAtUtc"], issues);
                ValidateExactProperties(root, fileName, ["userId", "previousSessionId", "sessionId", "purpose", "completedAtUtc"], issues);
                ValidateStringProperty(root, fileName, "userId", "uuid", issues);
                ValidateStringProperty(root, fileName, "previousSessionId", "uuid", issues);
                ValidateStringProperty(root, fileName, "sessionId", "uuid", issues);
                ValidateStringProperty(root, fileName, "purpose", null, issues);
                ValidateStringProperty(root, fileName, "completedAtUtc", "date-time", issues);
                break;
            case "organization-created.v1.schema.json":
                ValidateClosedObject(root, fileName, ["organizationId", "ownerMembershipId", "createdAtUtc"], issues);
                ValidateExactProperties(root, fileName, ["organizationId", "ownerMembershipId", "createdAtUtc"], issues);
                ValidateStringProperty(root, fileName, "organizationId", "uuid", issues);
                ValidateStringProperty(root, fileName, "ownerMembershipId", "uuid", issues);
                ValidateStringProperty(root, fileName, "createdAtUtc", "date-time", issues);
                break;
            case "organization-owner-invited.v1.schema.json":
                ValidateClosedObject(root, fileName, ["organizationId", "invitationId", "expiresAtUtc", "invitedAtUtc"], issues);
                ValidateExactProperties(root, fileName, ["organizationId", "invitationId", "expiresAtUtc", "invitedAtUtc"], issues);
                ValidateStringProperty(root, fileName, "organizationId", "uuid", issues);
                ValidateStringProperty(root, fileName, "invitationId", "uuid", issues);
                ValidateStringProperty(root, fileName, "expiresAtUtc", "date-time", issues);
                ValidateStringProperty(root, fileName, "invitedAtUtc", "date-time", issues);
                break;
            case "organization-owner-invitation-accepted.v1.schema.json":
                ValidateClosedObject(root, fileName, ["organizationId", "invitationId", "membershipId", "acceptedAtUtc"], issues);
                ValidateExactProperties(root, fileName, ["organizationId", "invitationId", "membershipId", "acceptedAtUtc"], issues);
                ValidateStringProperty(root, fileName, "organizationId", "uuid", issues);
                ValidateStringProperty(root, fileName, "invitationId", "uuid", issues);
                ValidateStringProperty(root, fileName, "membershipId", "uuid", issues);
                ValidateStringProperty(root, fileName, "acceptedAtUtc", "date-time", issues);
                break;
            case "organization-owner-invitation-revoked.v1.schema.json":
                ValidateClosedObject(root, fileName, ["organizationId", "invitationId", "revokedAtUtc"], issues);
                ValidateExactProperties(root, fileName, ["organizationId", "invitationId", "revokedAtUtc"], issues);
                ValidateStringProperty(root, fileName, "organizationId", "uuid", issues);
                ValidateStringProperty(root, fileName, "invitationId", "uuid", issues);
                ValidateStringProperty(root, fileName, "revokedAtUtc", "date-time", issues);
                break;
            case "organization-owner-membership-removed.v1.schema.json":
                ValidateClosedObject(
                    root,
                    fileName,
                    ["organizationId", "membershipId", "authorizationVersion", "revokedInvitationCount", "removedAtUtc"],
                    issues);
                ValidateExactProperties(
                    root,
                    fileName,
                    ["organizationId", "membershipId", "authorizationVersion", "revokedInvitationCount", "removedAtUtc"],
                    issues);
                ValidateStringProperty(root, fileName, "organizationId", "uuid", issues);
                ValidateStringProperty(root, fileName, "membershipId", "uuid", issues);
                ValidateIntegerProperty(root, fileName, "authorizationVersion", 2, issues);
                ValidateIntegerProperty(root, fileName, "revokedInvitationCount", 0, issues);
                ValidateStringProperty(root, fileName, "removedAtUtc", "date-time", issues);
                break;
            default:
                Add(issues, "schema.unregistered", $"Schema '{fileName}' is not registered.");
                break;
        }

        return Order(issues);
    }

    private static void ValidateRequestScope(JsonElement root, ICollection<ValidationIssue> issues)
    {
        if (!root.TryGetProperty("oneOf", out var branches)
            || branches.ValueKind != JsonValueKind.Array
            || branches.GetArrayLength() != 2)
        {
            Add(issues, "schema.scope.union-invalid", "Request scope must contain exactly platform and tenant branches.");
            return;
        }

        var kinds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var branch in branches.EnumerateArray())
        {
            if (!IsClosedObject(branch)
                || !TryProperty(branch, "properties", "kind", out var kindProperty)
                || !kindProperty.TryGetProperty("const", out var kindValue)
                || string.IsNullOrWhiteSpace(kindValue.GetString()))
            {
                Add(issues, "schema.scope.branch-invalid", "Each scope branch must be closed and declare a kind constant.");
                continue;
            }

            var kind = kindValue.GetString()!;
            _ = kinds.Add(kind);
            var required = RequiredNames(branch);
            if (!required.IsSupersetOf(["kind", "actorId", "correlationId"]))
            {
                Add(issues, "schema.scope.required-invalid", $"Scope branch '{kind}' misses common required fields.");
            }

            var hasTenant = required.Contains("tenantId");
            if ((kind == "tenant") != hasTenant)
            {
                Add(issues, "schema.scope.tenant-invalid", "Only tenant scope must require tenantId.");
            }
        }

        if (!kinds.SetEquals(["platform", "tenant"]))
        {
            Add(issues, "schema.scope.kinds-invalid", "Request scope kinds must be exactly platform and tenant.");
        }
    }

    private static void ValidateClosedObject(
        JsonElement root,
        string fileName,
        IReadOnlyCollection<string> requiredNames,
        ICollection<ValidationIssue> issues)
    {
        if (!IsClosedObject(root))
        {
            Add(issues, "schema.root.open", $"Producer schema '{fileName}' must reject undeclared root properties.");
        }

        var required = RequiredNames(root);
        foreach (var missing in requiredNames.Except(required).Order(StringComparer.Ordinal))
        {
            Add(issues, "schema.required.missing", $"Schema '{fileName}' must require '{missing}'.");
        }
    }

    private static void ValidateNoSensitiveProperties(
        JsonElement root,
        string fileName,
        bool allowPayload,
        ICollection<ValidationIssue> issues)
    {
        if (!root.TryGetProperty("properties", out var properties))
        {
            return;
        }

        foreach (var property in properties.EnumerateObject())
        {
            if (ForbiddenPublicProperties.Contains(property.Name)
                && !(allowPayload && property.NameEquals("payload")))
            {
                Add(issues, "schema.sensitive-property", $"Schema '{fileName}' exposes forbidden property '{property.Name}'.");
            }
        }
    }

    private static void ValidateExactProperties(
        JsonElement root,
        string fileName,
        IReadOnlyCollection<string> expectedNames,
        ICollection<ValidationIssue> issues)
    {
        var actual = root.TryGetProperty("properties", out var properties)
            && properties.ValueKind == JsonValueKind.Object
            ? properties.EnumerateObject().Select(property => property.Name).ToHashSet(StringComparer.Ordinal)
            : [];
        var expected = expectedNames.ToHashSet(StringComparer.Ordinal);
        if (!actual.SetEquals(expected))
        {
            Add(
                issues,
                "schema.properties.invalid",
                $"Schema '{fileName}' properties must be exactly: {string.Join(", ", expected.Order(StringComparer.Ordinal))}.");
        }
    }

    private static void ValidateStringProperty(
        JsonElement root,
        string fileName,
        string propertyName,
        string? expectedFormat,
        ICollection<ValidationIssue> issues)
    {
        if (!TryProperty(root, "properties", propertyName, out var property)
            || !property.TryGetProperty("type", out var type)
            || !string.Equals(type.GetString(), "string", StringComparison.Ordinal)
            || (expectedFormat is not null
                && (!property.TryGetProperty("format", out var format)
                    || !string.Equals(format.GetString(), expectedFormat, StringComparison.Ordinal))))
        {
            Add(
                issues,
                "schema.property-shape.invalid",
                $"Schema '{fileName}' property '{propertyName}' must be a string{(expectedFormat is null ? string.Empty : $" with format '{expectedFormat}'")}.");
        }
    }

    private static void ValidateIntegerProperty(
        JsonElement root,
        string fileName,
        string propertyName,
        int expectedMinimum,
        ICollection<ValidationIssue> issues)
    {
        if (!TryProperty(root, "properties", propertyName, out var property)
            || !property.TryGetProperty("type", out var type)
            || !string.Equals(type.GetString(), "integer", StringComparison.Ordinal)
            || !property.TryGetProperty("minimum", out var minimum)
            || !minimum.TryGetInt32(out int actualMinimum)
            || actualMinimum != expectedMinimum)
        {
            Add(
                issues,
                "schema.property-shape.invalid",
                $"Schema '{fileName}' property '{propertyName}' must be an integer with minimum {expectedMinimum}.");
        }
    }

    private static bool IsClosedObject(JsonElement element) =>
        element.TryGetProperty("additionalProperties", out var additional)
        && additional.ValueKind == JsonValueKind.False;

    private static HashSet<string> RequiredNames(JsonElement element) =>
        element.TryGetProperty("required", out var required) && required.ValueKind == JsonValueKind.Array
            ? required.EnumerateArray().Select(item => item.GetString() ?? string.Empty).ToHashSet(StringComparer.Ordinal)
            : new HashSet<string>(StringComparer.Ordinal);

    private static bool TryProperty(
        JsonElement root,
        string containerName,
        string propertyName,
        out JsonElement value)
    {
        if (root.TryGetProperty(containerName, out var container)
            && container.ValueKind == JsonValueKind.Object
            && container.TryGetProperty(propertyName, out value))
        {
            return true;
        }

        value = default;
        return false;
    }

    private static void RequireString(
        JsonElement root,
        string propertyName,
        string expected,
        string fileName,
        ICollection<ValidationIssue> issues)
    {
        if (!root.TryGetProperty(propertyName, out var property)
            || !string.Equals(property.GetString(), expected, StringComparison.Ordinal))
        {
            Add(issues, "schema.dialect.invalid", $"Schema '{fileName}' must use JSON Schema 2020-12.");
        }
    }

    private static void RequireNonBlankString(
        JsonElement root,
        string propertyName,
        string fileName,
        ICollection<ValidationIssue> issues)
    {
        if (!root.TryGetProperty(propertyName, out var property)
            || string.IsNullOrWhiteSpace(property.GetString()))
        {
            Add(issues, "schema.metadata.missing", $"Schema '{fileName}' requires non-empty '{propertyName}'.");
        }
    }

    private static ValidationIssue[] Order(IEnumerable<ValidationIssue> issues) =>
        issues.OrderBy(issue => issue.Code, StringComparer.Ordinal)
            .ThenBy(issue => issue.Message, StringComparer.Ordinal)
            .ToArray();

    private static void Add(ICollection<ValidationIssue> issues, string code, string message) =>
        issues.Add(new ValidationIssue(code, message));
}
