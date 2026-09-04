using System.Text;
using System.Text.Json;
using AgropecuarIA.Catalog.Domain;

namespace AgropecuarIA.Catalog.Application;

internal sealed record ParsedCatalogEntry(string Code, string DisplayName, string Jurisdiction, string Category, IReadOnlyList<string> Synonyms);
internal sealed record ParsedCatalogSource(string SourceId, byte[] Content, IReadOnlyList<ParsedCatalogEntry> Entries);

internal static class CatalogSourceParser
{
    public const int MaximumBytes = 1024 * 1024;
    public const int MaximumRows = 10000;
    private static readonly HashSet<string> AllowedProperties = new(StringComparer.OrdinalIgnoreCase)
        { "code","displayName","jurisdiction","category","synonyms" };

    public static ParsedCatalogSource Parse(IngestSourceCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        string sourceId = command.SourceId?.Trim().ToLowerInvariant() ?? string.Empty;
        if (sourceId.Length is < 1 or > 128 || sourceId.Any(c => !char.IsAsciiLetterOrDigit(c) && c is not ('_' or '-' or '.' or ':')) ||
            !char.IsAsciiLetterOrDigit(sourceId[0]) || string.IsNullOrWhiteSpace(command.ContentBase64))
            throw CatalogErrors.InvalidSource();
        if (command.ContentBase64.Length > ((MaximumBytes + 2) / 3) * 4)
            throw CatalogErrors.TooLarge();
        try
        {
            byte[] content = Convert.FromBase64String(command.ContentBase64);
            if (content.Length > MaximumBytes) throw CatalogErrors.TooLarge();
            _ = new UTF8Encoding(false, true).GetString(content);
            using JsonDocument document = JsonDocument.Parse(content, new JsonDocumentOptions { MaxDepth = 6 });
            JsonElement root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Array) throw CatalogErrors.InvalidSource();
            if (root.GetArrayLength() > MaximumRows) throw CatalogErrors.TooLarge();
            var entries = new List<ParsedCatalogEntry>(root.GetArrayLength());
            var identifiers = new HashSet<string>(StringComparer.Ordinal);
            foreach (JsonElement element in root.EnumerateArray())
            {
                if (element.ValueKind != JsonValueKind.Object) throw CatalogErrors.InvalidSource();
                var properties = new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase);
                foreach (JsonProperty property in element.EnumerateObject())
                    if (!AllowedProperties.Contains(property.Name) || !properties.TryAdd(property.Name, property.Value)) throw CatalogErrors.InvalidSource();
                string code = Text(properties, "code", 64, null).ToUpperInvariant();
                string name = Text(properties, "displayName", 256, null);
                string jurisdiction = Text(properties, "jurisdiction", 64, "AR").ToUpperInvariant();
                string category = Text(properties, "category", 64, CatalogCategories.Otros).ToUpperInvariant();
                if (!CatalogCategories.IsValid(category)) throw CatalogErrors.InvalidSource();
                var synonyms = new List<string>();
                if (properties.TryGetValue("synonyms", out JsonElement aliases))
                {
                    if (aliases.ValueKind != JsonValueKind.Array || aliases.GetArrayLength() > 20) throw CatalogErrors.InvalidSource();
                    foreach (JsonElement alias in aliases.EnumerateArray()) synonyms.Add(ReadText(alias, 256));
                }

                if (!identifiers.Add(CatalogNameNormalizer.Normalize(code))) throw CatalogErrors.InvalidSource();
                foreach (string synonym in synonyms)
                    if (!identifiers.Add(CatalogNameNormalizer.Normalize(synonym))) throw CatalogErrors.InvalidSource();
                entries.Add(new(code, name, jurisdiction, category, synonyms));
            }

            return new(sourceId, content, entries);
        }
        catch (Exception exception) when (exception is FormatException or JsonException or ArgumentException or InvalidOperationException)
        {
            throw CatalogErrors.InvalidSource();
        }
    }

    private static string Text(Dictionary<string, JsonElement> properties, string name, int maximum, string? fallback) =>
        properties.TryGetValue(name, out JsonElement value) ? ReadText(value, maximum) : fallback ?? throw CatalogErrors.InvalidSource();

    private static string ReadText(JsonElement value, int maximum)
    {
        if (value.ValueKind != JsonValueKind.String) throw CatalogErrors.InvalidSource();
        string text = value.GetString()!.Trim();
        if (text.Length is 0 || text.Length > maximum || text.Any(char.IsControl) || CatalogNameNormalizer.Normalize(text).Length is 0)
            throw CatalogErrors.InvalidSource();
        return text;
    }
}
