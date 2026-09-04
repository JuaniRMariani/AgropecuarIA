using System.Text.Json;

namespace AgropecuarIA.ArchitectureFitness;

public static class CatalogEntryReferenceContractGuard
{
    private static readonly string[] SourceFields = ["sourceSnapshotId", "sourceId", "sourceHash", "sourceIngestedAtUtc"];
    private static readonly string[] Fields = ["versionId", "itemId", "versionTag", "code", "displayName", "declaredCatalogSupportLevel",
        "sourceSnapshotId", "sourceId", "sourceHash", "sourceIngestedAtUtc", "provenanceStatus", "resolvedAtUtc"];

    public static IReadOnlyList<ValidationIssue> Validate(JsonElement schema)
    {
        List<ValidationIssue> issues = [];
        if (schema.ValueKind != JsonValueKind.Object)
        {
            Add(issues, "catalog-reference.shape.invalid", "CatalogEntryRef must be an object schema.");
            return issues;
        }
        if (!IsClosedObject(schema, Fields, Fields))
            Add(issues, "catalog-reference.shape.invalid", "CatalogEntryRef must contain exactly the 12 reviewed platform reference fields.");
        foreach (string name in new[] { "versionId", "itemId" })
            CheckProperty(schema, name, "string", "uuid", issues);
        CheckProperty(schema, "resolvedAtUtc", "string", "date-time", issues);
        foreach ((string name, int maximum) in new[] { ("versionTag", 64), ("code", 64), ("displayName", 256), ("declaredCatalogSupportLevel", 64) })
        {
            CheckProperty(schema, name, "string", null, issues);
            if (!TryProperty(schema, name, out JsonElement property) || !HasInt(property, "maxLength", maximum))
                Add(issues, "catalog-reference.bounds.invalid", $"'{name}' must preserve its published storage bound.");
        }
        foreach (string name in SourceFields)
        {
            if (!TryProperty(schema, name, out JsonElement property) || !HasStringSet(property, "type", ["string", "null"]))
                Add(issues, "catalog-reference.legacy.nullability", $"'{name}' must permit honest unavailable legacy lineage.");
        }
        if (!TryProperty(schema, "sourceSnapshotId", out JsonElement sourceId) || !HasString(sourceId, "format", "uuid")
            || !TryProperty(schema, "sourceIngestedAtUtc", out JsonElement sourceTime) || !HasString(sourceTime, "format", "date-time")
            || !TryProperty(schema, "sourceHash", out JsonElement hash) || !HasString(hash, "pattern", "^[0-9a-f]{64}$")
            || !TryProperty(schema, "sourceId", out JsonElement sourceName) || !HasInt(sourceName, "maxLength", 128))
            Add(issues, "catalog-reference.source-shape.invalid", "Verified lineage must retain its source UUID, timestamp, bounded identity and SHA-256 hash.");
        if (!TryProperty(schema, "provenanceStatus", out JsonElement status)
            || !HasStringSet(status, "enum", ["verified_snapshot", "legacy_unavailable"]))
            Add(issues, "catalog-reference.provenance.invalid", "Verified and unavailable legacy provenance must remain distinct.");
        if (!schema.TryGetProperty("oneOf", out JsonElement branches) || branches.ValueKind != JsonValueKind.Array
            || branches.GetArrayLength() != 2
            || !HasLineageBranch(branches, "verified_snapshot", "string")
            || !HasLineageBranch(branches, "legacy_unavailable", "null"))
            Add(issues, "catalog-reference.lineage.incoherent", "Verified lineage requires all four values; legacy unavailable lineage requires all four null.");
        return issues;
    }

    internal static bool IsClosedObject(JsonElement schema, string[] properties, string[] required) =>
        HasString(schema, "type", "object") && schema.TryGetProperty("additionalProperties", out JsonElement closed)
        && closed.ValueKind == JsonValueKind.False && HasStringSet(schema, "required", required)
        && schema.TryGetProperty("properties", out JsonElement values) && values.ValueKind == JsonValueKind.Object
        && values.EnumerateObject().Select(property => property.Name).ToHashSet(StringComparer.Ordinal).SetEquals(properties);

    internal static bool TryProperty(JsonElement schema, string name, out JsonElement property)
    {
        property = default;
        return schema.ValueKind == JsonValueKind.Object && schema.TryGetProperty("properties", out JsonElement properties)
            && properties.ValueKind == JsonValueKind.Object && properties.TryGetProperty(name, out property);
    }

    internal static bool HasString(JsonElement value, string property, string expected) =>
        value.ValueKind == JsonValueKind.Object && value.TryGetProperty(property, out JsonElement actual)
        && actual.ValueKind == JsonValueKind.String && actual.GetString() == expected;

    internal static bool HasStringSet(JsonElement value, string property, string[] expected) =>
        value.ValueKind == JsonValueKind.Object && value.TryGetProperty(property, out JsonElement actual)
        && actual.ValueKind == JsonValueKind.Array && actual.EnumerateArray().All(item => item.ValueKind == JsonValueKind.String)
        && actual.EnumerateArray().Select(item => item.GetString()).ToHashSet(StringComparer.Ordinal).SetEquals(expected);

    internal static bool HasInt(JsonElement value, string property, int expected) =>
        value.ValueKind == JsonValueKind.Object && value.TryGetProperty(property, out JsonElement actual)
        && actual.ValueKind == JsonValueKind.Number && actual.TryGetInt32(out int number) && number == expected;

    private static bool HasLineageBranch(JsonElement branches, string status, string sourceType) =>
        branches.EnumerateArray().Any(branch => TryProperty(branch, "provenanceStatus", out JsonElement provenance)
            && HasString(provenance, "const", status)
            && SourceFields.All(name => TryProperty(branch, name, out JsonElement source) && HasString(source, "type", sourceType)));

    private static void CheckProperty(JsonElement schema, string name, string type, string? format, ICollection<ValidationIssue> issues)
    {
        if (!TryProperty(schema, name, out JsonElement property) || !HasString(property, "type", type)
            || (format is not null && !HasString(property, "format", format)))
            Add(issues, "catalog-reference.property.invalid", $"'{name}' must retain its reviewed type and format.");
    }

    private static void Add(ICollection<ValidationIssue> issues, string code, string message) => issues.Add(new(code, message));
}
