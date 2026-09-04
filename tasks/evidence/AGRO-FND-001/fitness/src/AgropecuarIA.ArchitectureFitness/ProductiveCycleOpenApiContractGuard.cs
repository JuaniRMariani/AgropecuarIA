using System.Text.Json;
using static AgropecuarIA.ArchitectureFitness.CatalogEntryReferenceContractGuard;

namespace AgropecuarIA.ArchitectureFitness;

public static class ProductiveCycleOpenApiContractGuard
{
    public static IReadOnlyList<ValidationIssue> Validate(string text)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(text);
        string normalized = text.Replace("\r\n", "\n", StringComparison.Ordinal);
        List<ValidationIssue> issues = [];
        int start = normalized.IndexOf("    post:\n      operationId: StartProductionCycle\n", StringComparison.Ordinal);
        int end = start < 0 ? -1 : normalized.IndexOf("  /api/", start + 1, StringComparison.Ordinal);
        string operation = start < 0 ? string.Empty : end < 0 ? normalized[start..] : normalized[start..end];
        if (!operation.Contains("      security:\n        - SessionCookie: []\n          AntiforgeryCookie: []\n", StringComparison.Ordinal))
            Add(issues, "productive-cycle.security.invalid", "Cycle creation requires session and antiforgery in the same requirement.");
        if (!operation.Contains("{name: X-CSRF-TOKEN, in: header, required: true", StringComparison.Ordinal))
            Add(issues, "productive-cycle.csrf-header.missing", "Cycle creation requires the request antiforgery token header.");
        foreach (string expected in new[] { "not a historical selector", "before item lookup", "not a guarantee of active version at commit", "no idempotency ledger" })
        {
            if (!operation.Contains(expected, StringComparison.Ordinal))
                Add(issues, "productive-cycle.resolution-boundary.missing", $"Cycle creation must document '{expected}'.");
        }
        foreach (string code in new[] { "catalog_version_stale", "catalog_not_published", "catalog_item_not_found", "catalog_unavailable" })
        {
            if (!operation.Contains($"productive_core.{code}", StringComparison.Ordinal))
                Add(issues, "productive-cycle.resolution-error.missing", $"Cycle creation must document '{code}'.");
        }

        if (ReadSchema(normalized, "StartProductionCycleRequest", issues) is JsonElement request)
        {
            if (!IsClosedObject(request, ["catalogCode", "purpose", "system", "startDateUtc", "expectedCatalogVersionId"],
                ["catalogCode", "purpose", "system", "startDateUtc"]))
                Add(issues, "productive-cycle.request.invalid", "Strict cycle request cannot accept client display, support, snapshot or actor fields.");
            if (!TryProperty(request, "expectedCatalogVersionId", out JsonElement expectedVersion)
                || !HasStringSet(expectedVersion, "type", ["string", "null"]) || !HasString(expectedVersion, "format", "uuid"))
                Add(issues, "productive-cycle.precondition.invalid", "The optional expected active catalog version must be a nullable UUID.");
        }
        if (ReadSchema(normalized, "CatalogEntrySnapshot", issues) is JsonElement snapshot)
            issues.AddRange(CatalogEntryReferenceContractGuard.Validate(snapshot));
        if (ReadSchema(normalized, "ProductionCycle", issues) is JsonElement cycle)
        {
            string[] fields = ["id", "organizationId", "managementUnitId", "catalogCode", "catalogDisplayName", "purpose", "system",
                "supportLevel", "status", "startDateUtc", "endDateUtc", "createdAtUtc", "catalogReferenceStatus", "catalogSnapshot",
                "effectiveSupportLevel", "capabilities", "absentCapabilities"];
            if (!IsClosedObject(cycle, fields, fields))
                Add(issues, "productive-cycle.response.invalid", "Cycle response must retain historical labels and add the complete authoritative reference state.");
            if (!TryProperty(cycle, "catalogReferenceStatus", out JsonElement status)
                || !HasStringSet(status, "enum", ["resolved_publication", "legacy_unresolved"]))
                Add(issues, "productive-cycle.reference-status.invalid", "Unresolved legacy cycles cannot be relabeled as resolved publications.");
            if (!TryProperty(cycle, "effectiveSupportLevel", out JsonElement effective) || !HasString(effective, "const", "FLUJO_GENERICO")
                || !TryProperty(cycle, "capabilities", out JsonElement capabilities) || !HasInt(capabilities, "maxItems", 0)
                || !TryProperty(cycle, "absentCapabilities", out JsonElement absent)
                || !HasStringSet(absent, "const", ["specialized_rules", "specialized_kpis", "ai_recommendations"]))
                Add(issues, "productive-cycle.capabilities.invalid", "Neither old nor newly resolved cycles can inherit specialized execution.");
            if (!TryProperty(cycle, "supportLevel", out JsonElement storedSupport) || !HasString(storedSupport, "type", "string")
                || storedSupport.TryGetProperty("const", out _) || storedSupport.TryGetProperty("enum", out _))
                Add(issues, "productive-cycle.legacy-support.rewritten", "Historical stored support labels remain uninterpreted strings, distinct from effective support.");
            if (!cycle.TryGetProperty("oneOf", out JsonElement branches) || branches.ValueKind != JsonValueKind.Array
                || branches.GetArrayLength() != 2 || !HasCycleBranch(branches, "resolved_publication", resolved: true)
                || !HasCycleBranch(branches, "legacy_unresolved", resolved: false))
                Add(issues, "productive-cycle.snapshot.incoherent", "Resolved cycles require a snapshot; unresolved legacy cycles require null.");
        }
        return issues;
    }

    private static bool HasCycleBranch(JsonElement branches, string status, bool resolved) =>
        branches.EnumerateArray().Any(branch => TryProperty(branch, "catalogReferenceStatus", out JsonElement referenceStatus)
            && HasString(referenceStatus, "const", status) && TryProperty(branch, "catalogSnapshot", out JsonElement snapshot)
            && (resolved ? HasString(snapshot, "$ref", "#/components/schemas/CatalogEntrySnapshot") : HasString(snapshot, "type", "null")));

    private static JsonElement? ReadSchema(string text, string name, ICollection<ValidationIssue> issues)
    {
        string prefix = $"    {name}: ";
        string? line = text.Split('\n').SingleOrDefault(value => value.StartsWith(prefix, StringComparison.Ordinal));
        if (line is not null)
        {
            try
            {
                using JsonDocument document = JsonDocument.Parse(line[prefix.Length..]);
                if (document.RootElement.ValueKind == JsonValueKind.Object) return document.RootElement.Clone();
            }
            catch (JsonException)
            {
                // Invalid inline JSON is reported as schema drift below.
            }
        }
        Add(issues, "productive-cycle.schema.invalid", $"'{name}' must retain its reviewed inline JSON Schema.");
        return null;
    }

    private static void Add(ICollection<ValidationIssue> issues, string code, string message) => issues.Add(new(code, message));
}
