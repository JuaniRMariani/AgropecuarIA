namespace AgropecuarIA.ArchitectureFitness;

public static class TerritoryOpenApiContractGuard
{
    public static IReadOnlyList<ValidationIssue> Validate(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        string text = File.ReadAllText(path);
        var issues = new List<ValidationIssue>();

        if (!HasSupportedMajorVersion(text))
        {
            Add(issues, "territory-openapi.version.invalid", "Territory OpenAPI version must be semantic 1.x.");
        }

        Require(text, "/api/territory/search:", "territory-openapi.search.missing", issues);
        Require(text, "/api/territory/resolve:", "territory-openapi.resolve.missing", issues);
        Require(text, "        - SessionCookie: []", "territory-openapi.session-security.missing", issues);
        Require(text, "        status: { type: string, enum: [fresh, stale, unavailable] }", "territory-openapi.degradation.incomplete", issues);
        Require(text, "        searchAvailable: { type: boolean, const: true }", "territory-openapi.fallback.missing", issues);
        Require(text, "pattern: '^[0-9]+$'", "territory-openapi.parent-code.open", issues);
        Require(text, "hierarchyLabel: { type: string, minLength: 1, maxLength: 700 }", "territory-openapi.hierarchy-label.too-short", issues);

        string problemSection = ExtractIndentedSection(text, "    Problem:", 4);
        if (!problemSection.SplitLines().Any(line => line.Trim() == "additionalProperties: false"))
        {
            Add(issues, "territory-openapi.problem.open", "Territory Problem schema must be closed.");
        }
        if (!problemSection.Contains("retryable: { type: boolean }", StringComparison.Ordinal))
        {
            Add(issues, "territory-openapi.problem.retryable.missing", "Territory Problem schema must allow the bounded retryable flag.");
        }

        return issues;
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

    private static void Require(
        string text,
        string token,
        string code,
        ICollection<ValidationIssue> issues)
    {
        if (!text.Contains(token, StringComparison.Ordinal))
        {
            Add(issues, code, $"Territory OpenAPI must contain '{token.Trim()}'.");
        }
    }

    private static string ExtractIndentedSection(string text, string header, int indentation)
    {
        string[] lines = text.SplitLines();
        int start = Array.FindIndex(lines, line => string.Equals(line, header, StringComparison.Ordinal));
        if (start < 0)
        {
            return string.Empty;
        }

        int end = start + 1;
        while (end < lines.Length)
        {
            string line = lines[end];
            if (!string.IsNullOrWhiteSpace(line) && line.TakeWhile(character => character == ' ').Count() <= indentation)
            {
                break;
            }

            end++;
        }

        return string.Join(Environment.NewLine, lines[start..end]);
    }

    private static void Add(ICollection<ValidationIssue> issues, string code, string message) =>
        issues.Add(new ValidationIssue(code, message));

    private static string[] SplitLines(this string value) =>
        value.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');
}
