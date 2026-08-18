namespace AgropecuarIA.ArchitectureFitness;

public static class ProductiveCoreOpenApiContractGuard
{
    public static IReadOnlyList<ValidationIssue> Validate(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        string text = File.ReadAllText(path);
        var issues = new List<ValidationIssue>();

        if (!HasSupportedMajorVersion(text))
        {
            Add(issues, "productive-openapi.version.invalid", "Productive Core OpenAPI version must be semantic 1.x.");
        }

        Require(text, "/api/organizations/{organizationId}/fields:", "productive-openapi.collection.missing", issues);
        Require(text, "/api/organizations/{organizationId}/fields/{fieldId}:", "productive-openapi.detail.missing", issues);
        Require(text, "        - SessionCookie: []", "productive-openapi.session-security.missing", issues);
        if (!HasConjunctiveCreateSecurity(text))
        {
            Add(
                issues,
                "productive-openapi.create-security.conjunctive-required",
                "Productive Core field creation must require session cookie and antiforgery in the same OpenAPI security requirement object.");
        }

        if (!HasConjunctiveRenameSecurity(text))
        {
            Add(
                issues,
                "productive-openapi.rename-security.conjunctive-required",
                "Productive Core field rename must require session cookie and antiforgery in the same OpenAPI security requirement object.");
        }

        Require(text, "          name: Idempotency-Key", "productive-openapi.idempotency.missing", issues);
        Require(text, "    patch:", "productive-openapi.rename.missing", issues);
        Require(text, "          name: If-Match", "productive-openapi.if-match.missing", issues);
        if (!HasDetailResponseHeader(text, "get", "ETag"))
        {
            Add(issues, "productive-openapi.detail-etag.missing", "Productive Core field detail must return the strong current ETag.");
        }

        if (!HasDetailResponseHeader(text, "patch", "ETag"))
        {
            Add(issues, "productive-openapi.rename-etag.missing", "Productive Core field rename must return the resulting strong ETag.");
        }
        Require(text, "        '412': { $ref: '#/components/responses/PreconditionFailed' }", "productive-openapi.precondition.missing", issues);
        Require(text, "schema: { $ref: '#/components/schemas/RenameFieldRequest' }", "productive-openapi.rename-request.missing", issues);
        Require(text, "schema: { $ref: '#/components/schemas/RenamedField' }", "productive-openapi.rename-response.missing", issues);
        Require(text, "        revision: { type: integer, minimum: 2 }", "productive-openapi.rename-revision.invalid", issues);
        Require(text, "pattern: '^\"[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}\"$'", "productive-openapi.etag-pattern.invalid", issues);
        Require(text, "        displayName: { type: string, minLength: 2, maxLength: 120 }", "productive-openapi.name-bounds.invalid", issues);
        Require(text, "        type: { type: string, const: field }", "productive-openapi.type.open", issues);
        Require(text, "        status: { type: string, const: draft }", "productive-openapi.status.open", issues);
        Require(text, "        spatialStatus: { type: string, const: not_configured }", "productive-openapi.spatial-status.open", issues);
        Require(text, "it does not claim geometry, area, cadastral status or agronomic location", "productive-openapi.non-spatial-boundary.missing", issues);
        Require(text, "        retryable: { type: boolean }", "productive-openapi.problem.retryable.missing", issues);
        Require(text, "organization field capacity reached (productive_core.management_unit_capacity_reached)", "productive-openapi.capacity-conflict.missing", issues);
        Require(text, "                maxItems: 100", "productive-openapi.list-bound.invalid", issues);
        Require(text, "displayName trims leading and trailing Unicode White_Space plus U+FEFF, then normalizes to NFC, counts Unicode scalars and rejects controls or lone surrogates.", "productive-openapi.name-canonicalization.missing", issues);

        if (Count(text, "additionalProperties: false") < 6)
        {
            Add(issues, "productive-openapi.schemas.open", "Productive Core request, responses and Problem schemas must be closed.");
        }

        return issues;
    }

    private static bool HasConjunctiveRenameSecurity(string text)
    {
        string normalized = text.Replace("\r\n", "\n", StringComparison.Ordinal);
        int detailStart = normalized.IndexOf(
            "/api/organizations/{organizationId}/fields/{fieldId}:",
            StringComparison.Ordinal);
        if (detailStart < 0)
        {
            return false;
        }

        int patchStart = normalized.IndexOf("    patch:\n", detailStart, StringComparison.Ordinal);
        if (patchStart < 0)
        {
            return false;
        }

        int nextPath = normalized.IndexOf("  /api/", patchStart + 1, StringComparison.Ordinal);
        string patchOperation = nextPath < 0
            ? normalized[patchStart..]
            : normalized[patchStart..nextPath];
        return patchOperation.Contains(
            "      security:\n        - SessionCookie: []\n          AntiforgeryCookie: []\n",
            StringComparison.Ordinal);
    }

    private static bool HasDetailResponseHeader(string text, string method, string header)
    {
        string normalized = text.Replace("\r\n", "\n", StringComparison.Ordinal);
        int detailStart = normalized.IndexOf(
            "/api/organizations/{organizationId}/fields/{fieldId}:",
            StringComparison.Ordinal);
        if (detailStart < 0)
        {
            return false;
        }

        string methodToken = $"    {method}:\n";
        int operationStart = normalized.IndexOf(methodToken, detailStart, StringComparison.Ordinal);
        if (operationStart < 0)
        {
            return false;
        }

        int operationEnd = normalized.IndexOf("    patch:\n", operationStart + methodToken.Length, StringComparison.Ordinal);
        if (method == "patch" || operationEnd < 0)
        {
            operationEnd = normalized.IndexOf("  /api/", operationStart + methodToken.Length, StringComparison.Ordinal);
        }

        string operation = operationEnd < 0
            ? normalized[operationStart..]
            : normalized[operationStart..operationEnd];
        return operation.Contains($"            {header}:\n", StringComparison.Ordinal);
    }

    private static bool HasConjunctiveCreateSecurity(string text)
    {
        string normalized = text.Replace("\r\n", "\n", StringComparison.Ordinal);
        int collectionStart = normalized.IndexOf(
            "/api/organizations/{organizationId}/fields:",
            StringComparison.Ordinal);
        if (collectionStart < 0)
        {
            return false;
        }

        int postStart = normalized.IndexOf("    post:\n", collectionStart, StringComparison.Ordinal);
        if (postStart < 0)
        {
            return false;
        }

        int nextPath = normalized.IndexOf("  /api/", postStart + 1, StringComparison.Ordinal);
        string postOperation = nextPath < 0
            ? normalized[postStart..]
            : normalized[postStart..nextPath];
        return postOperation.Contains(
            "      security:\n        - SessionCookie: []\n          AntiforgeryCookie: []\n",
            StringComparison.Ordinal);
    }

    private static bool HasSupportedMajorVersion(string text)
    {
        int start = text.IndexOf("info:", StringComparison.Ordinal);
        string? versionLine = start < 0
            ? null
            : text[start..].SplitLines().Take(8)
                .SingleOrDefault(line => line.StartsWith("  version: ", StringComparison.Ordinal));
        return versionLine is not null &&
            Version.TryParse(versionLine["  version: ".Length..], out Version? version) &&
            version.Major == 1;
    }

    private static int Count(string text, string token)
    {
        int count = 0;
        int offset = 0;
        while ((offset = text.IndexOf(token, offset, StringComparison.Ordinal)) >= 0)
        {
            count++;
            offset += token.Length;
        }

        return count;
    }

    private static void Require(
        string text,
        string token,
        string code,
        ICollection<ValidationIssue> issues)
    {
        if (!text.Contains(token, StringComparison.Ordinal))
        {
            Add(issues, code, $"Productive Core OpenAPI must contain '{token.Trim()}'.");
        }
    }

    private static void Add(ICollection<ValidationIssue> issues, string code, string message) =>
        issues.Add(new ValidationIssue(code, message));

    private static string[] SplitLines(this string value) =>
        value.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');
}
