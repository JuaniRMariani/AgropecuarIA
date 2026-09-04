using System.Text.Json;
using AgropecuarIA.ProductiveCore.Application;

namespace AgropecuarIA.ArchitectureFitness.Tests;

[TestClass]
public sealed class ProductionHistoryContractTests
{
    private static readonly JsonSerializerOptions WebJson = new(JsonSerializerDefaults.Web);
    private static readonly string[] CyclePageFields = ["items", "hasMore", "nextCursor"];
    private static readonly string[] TimelinePageFields = ["cycle", "events", "hasMore", "nextCursor"];

    [TestMethod]
    [DataRow("ProductionCyclePage", "items", "ProductionCycle")]
    [DataRow("ProductionTimelinePage", "events", "ProductionEvent")]
    public void HistoryPageContractsBoundRowsAndKeepNullableContinuation(string schema, string collection, string itemSchema)
    {
        string block = SchemaBlock(schema);
        StringAssert.Contains(block, "      additionalProperties: false");
        StringAssert.Contains(block, $"        {collection}: {{ type: array, maxItems: 100, items: {{ $ref: '#/components/schemas/{itemSchema}' }} }}");
        StringAssert.Contains(block, "nextCursor: { type: [string, 'null'], minLength: 1, maxLength: 512");
        StringAssert.Contains(block, "hasMore: { const: true }, nextCursor: { type: string }");
        StringAssert.Contains(block, "hasMore: { const: false }, nextCursor: { type: 'null' }");
        string text = File.ReadAllText(EvidenceFixture.ProductiveCoreOpenApiPath());
        StringAssert.Contains(text, "default: 20, minimum: 1, maximum: 100");
        StringAssert.Contains(text, "productive_core.invalid_history_query");
    }

    [TestMethod]
    public void HistoryPageRuntimeSerializesTheExactAdditiveEnvelope()
    {
        JsonElement cycles = JsonSerializer.SerializeToElement(new ProductionCyclePage([], false, null), WebJson);
        CollectionAssert.AreEquivalent(CyclePageFields, cycles.EnumerateObject().Select(property => property.Name).ToArray());
        Assert.AreEqual(JsonValueKind.Null, cycles.GetProperty("nextCursor").ValueKind);
        Assert.IsFalse(cycles.GetProperty("hasMore").GetBoolean());
        DateTimeOffset now = DateTimeOffset.UtcNow;
        var cycle = new ProductionCycleDto(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "MAIZ", "Synthetic maize",
            "grano", "secano", "FLUJO_GENERICO", "active", now, null, now, "legacy_unresolved", null,
            "FLUJO_GENERICO", [], ["specialized_rules", "specialized_kpis", "ai_recommendations"]);
        JsonElement timeline = JsonSerializer.SerializeToElement(new ProductionTimelinePage(cycle, [], false, null), WebJson);
        CollectionAssert.AreEquivalent(TimelinePageFields, timeline.EnumerateObject().Select(property => property.Name).ToArray());
        Assert.AreEqual(JsonValueKind.Object, timeline.GetProperty("cycle").ValueKind);
    }

    [TestMethod]
    [DataRow("ListProductionCyclePage", "createdAtUtc DESC then id DESC")]
    [DataRow("GetProductionTimelinePage", "recordedAtUtc DESC then id DESC, not effectiveDateUtc")]
    public void HistoryPageOperationsExposeReauthorizationOrderingAndLegacyMigration(string operationId, string ordering)
    {
        string text = File.ReadAllText(EvidenceFixture.ProductiveCoreOpenApiPath()).Replace("\r\n", "\n", StringComparison.Ordinal);
        int start = text.IndexOf("      operationId: " + operationId + "\n", StringComparison.Ordinal);
        Assert.IsGreaterThanOrEqualTo(0, start);
        int end = text.IndexOf("\n  /api/", start, StringComparison.Ordinal);
        if (end < 0) end = text.IndexOf("\ncomponents:", start, StringComparison.Ordinal);
        string operation = text[start..end];
        StringAssert.Contains(operation, ordering);
        StringAssert.Contains(operation, "        - SessionCookie: []");
        StringAssert.Contains(operation, "#/components/parameters/HistoryLimit");
        StringAssert.Contains(operation, "#/components/parameters/HistoryCursor");
        StringAssert.Contains(operation, "resource-bound position, not authorization");
        StringAssert.Contains(operation, "Separate requests do not share an MVCC snapshot");
        StringAssert.Contains(operation, "legacy route is deprecated but unchanged");
        StringAssert.Contains(operation, "private, no-store");
    }

    private static string SchemaBlock(string name)
    {
        string text = File.ReadAllText(EvidenceFixture.ProductiveCoreOpenApiPath()).Replace("\r\n", "\n", StringComparison.Ordinal);
        int start = text.IndexOf("    " + name + ":\n", StringComparison.Ordinal);
        Assert.IsGreaterThanOrEqualTo(0, start);
        string[] lines = text[start..].Split('\n');
        return string.Join('\n', lines.Take(1).Concat(lines.Skip(1).TakeWhile(line =>
            line.Length == 0 || !line.StartsWith("    ", StringComparison.Ordinal) || line.StartsWith("     ", StringComparison.Ordinal))));
    }
}
