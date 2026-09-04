using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.WebUtilities;

namespace AgropecuarIA.ProductiveCore.Application;

public sealed record ProductionHistoryPosition(DateTimeOffset RecordedAtUtc, Guid Id);
public sealed record ProductionHistoryWindow(int Limit, ProductionHistoryPosition? Before);
public sealed record ProductionCyclePage(IReadOnlyList<ProductionCycleDto> Items, bool HasMore, string? NextCursor);
public sealed record ProductionTimelinePage(ProductionCycleDto Cycle, IReadOnlyList<ProductionEventDto> Events, bool HasMore, string? NextCursor);

internal static class ProductionHistoryPaging
{
    private static readonly JsonSerializerOptions JsonOptions = new() { MaxDepth = 8 };

    public static ProductionHistoryWindow Parse(int? limit, string? cursor, string kind, Guid organizationId, Guid resourceId)
    {
        int pageSize = limit ?? 20;
        if (pageSize is < 1 or > 100) throw InvalidQuery();
        if (cursor is null) return new(pageSize, null);
        if (cursor.Length is < 1 or > 512 || cursor.Any(c => !char.IsAsciiLetterOrDigit(c) && c != '-' && c != '_'))
            throw InvalidQuery();
        try
        {
            Cursor? decoded = JsonSerializer.Deserialize<Cursor>(WebEncoders.Base64UrlDecode(cursor), JsonOptions);
            if (decoded is null || decoded.Version != 1 || decoded.Kind != kind || decoded.OrganizationId != organizationId
                || decoded.ResourceId != resourceId || decoded.Id == Guid.Empty || decoded.RecordedAtUtc == default
                || decoded.RecordedAtUtc == DateTimeOffset.MaxValue || decoded.RecordedAtUtc.Offset != TimeSpan.Zero
                || decoded.RecordedAtUtc.Ticks % 10 != 0)
                throw InvalidQuery();
            return new(pageSize, new(decoded.RecordedAtUtc, decoded.Id));
        }
        catch (Exception exception) when (exception is JsonException or FormatException or ArgumentException)
        {
            throw InvalidQuery();
        }
    }

    public static string Encode(string kind, Guid organizationId, Guid resourceId, DateTimeOffset recordedAtUtc, Guid id) =>
        WebEncoders.Base64UrlEncode(JsonSerializer.SerializeToUtf8Bytes(
            new Cursor(1, kind, organizationId, resourceId, recordedAtUtc.ToUniversalTime(), id), JsonOptions));

    private static ProductiveCoreOperationException InvalidQuery() => new(
        "productive_core.invalid_history_query", 400, "The history page query is invalid.");

    // Context binding prevents accidental reuse across resources. This is an opaque
    // position, not a signed credential: every page independently reauthorizes access.
    [JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
    private sealed record Cursor(int Version, string Kind, Guid OrganizationId, Guid ResourceId, DateTimeOffset RecordedAtUtc, Guid Id);
}
