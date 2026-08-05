namespace AgropecuarIA.ArchitectureFitness;

public sealed record CompatibilityResult(IReadOnlyList<ValidationIssue> Issues)
{
    public bool IsCompatible => Issues.Count == 0;
}

public static class ContractCompatibility
{
    public static CompatibilityResult Evaluate(ContractSnapshot previous, ContractSnapshot current)
    {
        ArgumentNullException.ThrowIfNull(previous);
        ArgumentNullException.ThrowIfNull(current);

        var issues = new List<ValidationIssue>();
        if (!string.Equals(previous.Name, current.Name, StringComparison.Ordinal))
        {
            Add(issues, "contract-name.changed", "N and N-1 snapshots must describe the same contract.");
        }

        if (!TryParseVersion(previous.Version, out var previousVersion)
            || !TryParseVersion(current.Version, out var currentVersion))
        {
            Add(issues, "contract-version.invalid", "Contract versions must use numeric semantic version syntax.");
        }
        else
        {
            if (previousVersion.Major != currentVersion.Major)
            {
                Add(issues, "contract-version.major-changed", "A major version change is outside the N/N-1 additive window.");
            }

            if (currentVersion <= previousVersion)
            {
                Add(issues, "contract-version.not-incremented", "Version N must be newer than N-1.");
            }
        }

        if (!previous.ToleratesUnknownFields)
        {
            Add(issues, "n-1.unknown-fields-rejected", "The N-1 consumer must tolerate unknown fields for additive evolution.");
        }

        var previousFields = IndexFields(previous.Fields, "N-1", issues);
        var currentFields = IndexFields(current.Fields, "N", issues);

        foreach (var previousField in previousFields.Values)
        {
            if (!currentFields.TryGetValue(previousField.Name, out var currentField))
            {
                Add(issues, "field.removed", $"Field '{previousField.Name}' was removed.");
                continue;
            }

            if (!string.Equals(previousField.Type, currentField.Type, StringComparison.Ordinal))
            {
                Add(issues, "field.type-changed", $"Field '{previousField.Name}' changed type from '{previousField.Type}' to '{currentField.Type}'.");
            }

            if (!previousField.Required && currentField.Required)
            {
                Add(issues, "field.became-required", $"Optional field '{previousField.Name}' became required.");
            }

            if (previousField.Required && !currentField.Required)
            {
                Add(issues, "field.became-optional", $"Required response field '{previousField.Name}' became optional and may disappear for N-1 consumers.");
            }

            ValidateEnum(previousField, currentField, issues);
        }

        foreach (var addedField in currentFields.Values.Where(field => !previousFields.ContainsKey(field.Name)))
        {
            if (addedField.Required)
            {
                Add(issues, "field.required-added", $"New field '{addedField.Name}' is required.");
            }
        }

        return new CompatibilityResult(
            issues
                .OrderBy(issue => issue.Code, StringComparer.Ordinal)
                .ThenBy(issue => issue.Message, StringComparer.Ordinal)
                .ToArray());
    }

    private static Dictionary<string, ContractField> IndexFields(
        IReadOnlyList<ContractField>? fields,
        string snapshot,
        ICollection<ValidationIssue> issues)
    {
        var index = new Dictionary<string, ContractField>(StringComparer.Ordinal);
        foreach (var field in fields ?? [])
        {
            if (string.IsNullOrWhiteSpace(field.Name) || string.IsNullOrWhiteSpace(field.Type))
            {
                Add(issues, "field.invalid", $"Snapshot {snapshot} contains a field without name or type.");
                continue;
            }

            if (!index.TryAdd(field.Name, field))
            {
                Add(issues, "field.duplicate", $"Snapshot {snapshot} duplicates field '{field.Name}'.");
            }
        }

        return index;
    }

    private static void ValidateEnum(
        ContractField previous,
        ContractField current,
        ICollection<ValidationIssue> issues)
    {
        var previousValues = (previous.EnumValues ?? []).ToHashSet(StringComparer.Ordinal);
        var currentValues = (current.EnumValues ?? []).ToHashSet(StringComparer.Ordinal);
        if (previous.ExtensibleEnum && !current.ExtensibleEnum)
        {
            Add(issues, "enum.closed", $"Extensible enum field '{previous.Name}' became closed.");
        }

        foreach (var removed in previousValues.Except(currentValues).Order(StringComparer.Ordinal))
        {
            Add(issues, "enum.value-removed", $"Enum field '{previous.Name}' removed value '{removed}'.");
        }

        if (!previous.ExtensibleEnum && currentValues.Except(previousValues).Any())
        {
            Add(issues, "enum.closed-value-added", $"Closed enum field '{previous.Name}' added values that N-1 cannot accept.");
        }
    }

    private static bool TryParseVersion(string value, out Version version)
    {
        if (Version.TryParse(value, out var parsed) && parsed.Build >= 0)
        {
            version = parsed;
            return true;
        }

        version = new Version();
        return false;
    }

    private static void Add(ICollection<ValidationIssue> issues, string code, string message) =>
        issues.Add(new ValidationIssue(code, message));
}
