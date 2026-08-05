namespace AgropecuarIA.StorageRecoverySpike;

using System.Security.Cryptography;

public interface IClock
{
    DateTimeOffset UtcNow { get; }
}

public sealed class SystemClock : IClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}

public interface IObjectStore
{
    Task CreateAsync(string key, ReadOnlyMemory<byte> content, CancellationToken cancellationToken);

    Task<byte[]> ReadAsync(string key, CancellationToken cancellationToken);

    Task DeleteAsync(string key, CancellationToken cancellationToken);

    Task<bool> ExistsAsync(string key, CancellationToken cancellationToken);

    IAsyncEnumerable<string> ListKeysAsync(CancellationToken cancellationToken);
}

public interface IMalwareScanner
{
    Task<ScanVerdict> ScanAsync(ReadOnlyMemory<byte> content, CancellationToken cancellationToken);
}

public interface IResourceAuthorizer
{
    bool IsAllowed(ActorContext actor, LinkedResource resource, string action);
}

public interface IOperationsAuthorizer
{
    bool IsAllowed(OperatorContext operatorContext, string scope);
}

public interface ISafeTelemetry
{
    void Record(string eventName, string tenantRef, Guid fileId, params (string Name, object Value)[] measurements);
}

public sealed class InMemorySafeTelemetry : ISafeTelemetry
{
    private readonly List<string> entries = [];
    private readonly object gate = new();

    public IReadOnlyList<string> Entries
    {
        get
        {
            lock (gate)
            {
                return entries.ToArray();
            }
        }
    }

    public void Record(string eventName, string tenantRef, Guid fileId, params (string Name, object Value)[] measurements)
    {
        var fields = string.Join(',', measurements.Select(static item => $"{item.Name}={item.Value}"));
        var fileRef = Convert.ToHexStringLower(SHA256.HashData(fileId.ToByteArray()))[..16];
        lock (gate)
        {
            entries.Add($"event={eventName};tenant_ref={tenantRef};file_ref={fileRef};{fields}");
        }
    }
}
