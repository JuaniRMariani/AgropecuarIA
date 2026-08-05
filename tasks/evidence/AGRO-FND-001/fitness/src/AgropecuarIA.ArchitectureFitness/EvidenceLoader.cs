using System.Text.Json;

namespace AgropecuarIA.ArchitectureFitness;

public static class EvidenceLoader
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public static ModuleBoundaryDocument LoadModuleBoundaries(string path) =>
        Load<ModuleBoundaryDocument>(path);

    public static ConsumerMapDocument LoadConsumerMap(string path) =>
        Load<ConsumerMapDocument>(path);

    public static ContractSnapshot LoadContractSnapshot(string path) =>
        Load<ContractSnapshot>(path);

    private static T Load<T>(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        try
        {
            using var stream = File.OpenRead(path);
            return JsonSerializer.Deserialize<T>(stream, SerializerOptions)
                ?? throw new InvalidDataException($"Evidence file '{path}' contains JSON null.");
        }
        catch (JsonException exception)
        {
            throw new InvalidDataException($"Evidence file '{path}' is not a valid {typeof(T).Name} document.", exception);
        }
    }
}

