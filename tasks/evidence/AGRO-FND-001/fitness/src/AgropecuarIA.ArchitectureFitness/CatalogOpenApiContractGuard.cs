using System.Text.Json;

namespace AgropecuarIA.ArchitectureFitness;

/// <summary>Guards the reviewed Catalog v2 schema and authorization boundaries.</summary>
public static class CatalogOpenApiContractGuard
{
    public static IReadOnlyList<ValidationIssue> Validate(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        string text = File.ReadAllText(path).Replace("\r\n", "\n", StringComparison.Ordinal);
        List<ValidationIssue> issues = [];
        if (!text.Contains("  version: 2.0.0\n", StringComparison.Ordinal))
            Add(issues, "catalog-openapi.version.invalid", "Reviewed candidateHash breaking change requires Catalog API 2.0.0.");

        foreach (string route in new[] { "diff", "ingest", "publish", "rollback/{versionId}", "items", "items/{code}", "versions" })
        {
            string operation = Operation(text, route);
            if (operation.Length == 0)
                Add(issues, "catalog-openapi.route.missing", $"Catalog route '{route}' is missing.");
            if (route is "ingest" or "publish" or "rollback/{versionId}")
            {
                if (!operation.Contains("      security:\n        - SessionCookie: []\n          AntiforgeryCookie: []\n", StringComparison.Ordinal))
                    Add(issues, "catalog-openapi.mutation.security.invalid", $"'{route}' must require session and antiforgery conjunctively.");
                if (!operation.Contains("Platform editorial permission", StringComparison.Ordinal))
                    Add(issues, "catalog-openapi.editorial-boundary.missing", $"'{route}' must document platform editorial authority.");
            }
        }
        if (!Operation(text, "diff").Contains("Platform editorial permission required", StringComparison.Ordinal))
            Add(issues, "catalog-openapi.diff.editorial-required", "Staging diff is an editorial surface, not a general authenticated reader.");
        if (!text.Contains("No idempotency ledger or automatic retry", StringComparison.Ordinal))
            Add(issues, "catalog-openapi.retry-boundary.missing", "Uncertain publication outcomes require reconciliation, not automatic replay.");

        Dictionary<string, JsonElement> schemas = ReadSchemas(text, issues);
        CheckSchema(schemas, "PublishCatalog", ["versionTag", "candidateHash"], issues, exactProperties: true);
        CheckSchema(schemas, "IngestSource", ["sourceId", "contentBase64"], issues, exactProperties: true);
        CheckSchema(schemas, "RawCatalogEntry", ["code", "displayName"], issues);
        if (schemas.TryGetValue("RawCatalogEntry", out JsonElement raw)
            && raw.TryGetProperty("properties", out JsonElement rawProperties)
            && rawProperties.TryGetProperty("supportLevel", out _))
            Add(issues, "catalog-openapi.ingest-support.forbidden", "Source upload cannot claim specialized support.");

        CheckSchema(schemas, "CatalogItem",
            ["id", "code", "displayName", "jurisdiction", "supportLevel", "category", "synonyms", "versionId", "versionTag", "activeVersionId",
             "sourceSnapshotId", "sourceId", "sourceHash", "sourceIngestedAtUtc", "provenanceStatus", "capabilities", "absentCapabilities"], issues, exactProperties: true);
        CheckSchema(schemas, "CatalogSearch", ["versionId", "versionTag", "activeVersionId", "publishedAtUtc", "isHistorical", "totalCount", "items"], issues);
        CheckSchema(schemas, "CatalogVersions", ["activeVersionId", "totalCount", "hasMore", "versions"], issues);
        CheckSchema(schemas, "EditorialDiff", ["totalStaged", "added", "modified", "removed", "conflicts", "conflictDetails", "generatedAtUtc", "candidateHash", "activeVersionId", "selectedSnapshots"], issues);

        if (!TryProperty(schemas, "CatalogItem", "capabilities", out JsonElement capabilities)
            || !HasInt(capabilities, "maxItems", 0))
            Add(issues, "catalog-openapi.capabilities.open", "This delivery must not advertise executable specialized capabilities.");
        if (!TryProperty(schemas, "CatalogItem", "absentCapabilities", out JsonElement absentCapabilities)
            || !HasStringSet(absentCapabilities, "const", ["specialized_rules", "specialized_kpis", "ai_recommendations"]))
            Add(issues, "catalog-openapi.absent-capabilities.invalid", "Absent capabilities must remain explicit rather than implying specialized execution.");
        if (!TryProperty(schemas, "CatalogItem", "provenanceStatus", out JsonElement provenance)
            || !HasStringSet(provenance, "enum", ["verified_snapshot", "legacy_unavailable"]))
            Add(issues, "catalog-openapi.provenance-status.invalid", "Verified lineage and unavailable legacy lineage must remain distinct.");
        foreach (string property in new[] { "sourceSnapshotId", "sourceId", "sourceHash", "sourceIngestedAtUtc" })
        {
            if (!TryProperty(schemas, "CatalogItem", property, out JsonElement source)
                || !HasStringSet(source, "type", ["string", "null"]))
                Add(issues, "catalog-openapi.legacy-provenance.not-nullable", $"Legacy '{property}' cannot be invented or required nonnull.");
        }
        if (!TryProperty(schemas, "CatalogSearch", "items", out JsonElement items) || !HasInt(items, "maxItems", 100)
            || !TryProperty(schemas, "CatalogVersions", "versions", out JsonElement versions) || !HasInt(versions, "maxItems", 100))
            Add(issues, "catalog-openapi.read-bound.invalid", "Both catalog entries and version pages must be bounded at 100.");
        if (!schemas.TryGetValue("Sha256", out JsonElement hash)
            || !hash.TryGetProperty("pattern", out JsonElement pattern) || pattern.ValueKind != JsonValueKind.String
            || pattern.GetString() != "^[0-9a-f]{64}$")
            Add(issues, "catalog-openapi.candidate-hash.invalid", "Candidate precondition must use SHA-256 hexadecimal shape.");
        if (!TryProperty(schemas, "EditorialDiff", "selectedSnapshots", out JsonElement selectedSnapshots)
            || !HasInt(selectedSnapshots, "maxItems", 64))
            Add(issues, "catalog-openapi.candidate-bound.invalid", "A reviewed candidate is bounded to 64 source snapshots.");
        foreach (string route in new[] { "items", "items/{code}" })
        {
            if (!Operation(text, route).Contains("\"name\":\"versionId\",\"in\":\"query\"", StringComparison.Ordinal))
                Add(issues, "catalog-openapi.historical-version.missing", $"'{route}' must support explicit historical versions.");
        }
        return issues;
    }

    private static Dictionary<string, JsonElement> ReadSchemas(string text, ICollection<ValidationIssue> issues)
    {
        Dictionary<string, JsonElement> schemas = new(StringComparer.Ordinal);
        int start = text.IndexOf("  schemas:\n", StringComparison.Ordinal);
        if (start < 0)
        {
            Add(issues, "catalog-openapi.schemas.missing", "Catalog schemas are missing.");
            return schemas;
        }
        foreach (string line in text[start..].Split('\n'))
        {
            if (!line.StartsWith("    ", StringComparison.Ordinal) || line.StartsWith("     ", StringComparison.Ordinal)) continue;
            int separator = line.IndexOf(": ", StringComparison.Ordinal);
            if (separator < 0) continue;
            try
            {
                using JsonDocument document = JsonDocument.Parse(line[(separator + 2)..]);
                schemas.Add(line[4..separator], document.RootElement.Clone());
            }
            catch (JsonException)
            {
                Add(issues, "catalog-openapi.schema-json.invalid", "Catalog inline JSON schemas must parse as JSON Schema objects.");
            }
        }
        return schemas;
    }

    private static void CheckSchema(Dictionary<string, JsonElement> schemas, string name, string[] required,
        ICollection<ValidationIssue> issues, bool exactProperties = false)
    {
        if (!schemas.TryGetValue(name, out JsonElement schema))
        {
            Add(issues, "catalog-openapi.schema.missing", $"Schema '{name}' is missing.");
            return;
        }
        if (!schema.TryGetProperty("additionalProperties", out JsonElement closed) || closed.ValueKind != JsonValueKind.False)
            Add(issues, "catalog-openapi.schema.open", $"Schema '{name}' must reject unknown fields.");
        if (!HasStringSet(schema, "required", required))
            Add(issues, "catalog-openapi.required.invalid", $"Schema '{name}' must retain its reviewed required fields.");
        if (exactProperties && (!schema.TryGetProperty("properties", out JsonElement properties)
            || !properties.EnumerateObject().Select(property => property.Name).ToHashSet(StringComparer.Ordinal).SetEquals(required)))
            Add(issues, "catalog-openapi.properties.invalid", $"Schema '{name}' has unreviewed/missing properties.");
    }

    private static bool TryProperty(Dictionary<string, JsonElement> schemas, string schemaName, string propertyName, out JsonElement property)
    {
        property = default;
        return schemas.TryGetValue(schemaName, out JsonElement schema)
            && schema.TryGetProperty("properties", out JsonElement properties)
            && properties.TryGetProperty(propertyName, out property);
    }

    private static bool HasStringSet(JsonElement root, string property, string[] expected) =>
        root.TryGetProperty(property, out JsonElement value) && value.ValueKind == JsonValueKind.Array
        && value.EnumerateArray().All(item => item.ValueKind == JsonValueKind.String)
        && value.EnumerateArray().Select(item => item.GetString()).ToHashSet(StringComparer.Ordinal).SetEquals(expected);

    private static bool HasInt(JsonElement root, string property, int expected) =>
        root.TryGetProperty(property, out JsonElement value) && value.ValueKind == JsonValueKind.Number
        && value.TryGetInt32(out int actual) && actual == expected;

    private static string Operation(string text, string route)
    {
        int start = text.IndexOf($"  /api/catalog/{route}:\n", StringComparison.Ordinal);
        if (start < 0) return string.Empty;
        int next = text.IndexOf("  /api/", start + 1, StringComparison.Ordinal);
        return next < 0 ? text[start..] : text[start..next];
    }

    private static void Add(ICollection<ValidationIssue> issues, string code, string message) => issues.Add(new(code, message));
}
