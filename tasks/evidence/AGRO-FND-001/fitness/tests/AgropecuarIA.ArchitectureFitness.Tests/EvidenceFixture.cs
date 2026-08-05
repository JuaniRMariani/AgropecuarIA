using AgropecuarIA.ArchitectureFitness;

namespace AgropecuarIA.ArchitectureFitness.Tests;

internal static class EvidenceFixture
{
    internal static ModuleBoundaryDocument Boundaries() =>
        EvidenceLoader.LoadModuleBoundaries(PathFor("module-boundaries.json"));

    internal static ConsumerMapDocument ConsumerMap() =>
        EvidenceLoader.LoadConsumerMap(PathFor("consumer-map.json"));

    internal static ContractSnapshot Contract(string fileName) =>
        EvidenceLoader.LoadContractSnapshot(PathFor("fixtures", "contracts", fileName));

    internal static string ContractsDirectory() => PathFor("contracts");

    private static string PathFor(params string[] segments) =>
        segments.Aggregate(
            Path.Combine(AppContext.BaseDirectory, "evidence"),
            Path.Combine);
}
