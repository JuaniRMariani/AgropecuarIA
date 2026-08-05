using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace AgropecuarIA.StorageRecoverySpike;

public sealed partial class LocalObjectStore : IObjectStore
{
    private readonly string root;

    public LocalObjectStore(string root)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(root);
        this.root = Path.GetFullPath(root);
        Directory.CreateDirectory(this.root);
    }

    public async Task CreateAsync(string key, ReadOnlyMemory<byte> content, CancellationToken cancellationToken)
    {
        var path = Resolve(key);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        await using var output = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, FileOptions.Asynchronous);
        await output.WriteAsync(content, cancellationToken);
        await output.FlushAsync(cancellationToken);
    }

    public async Task<byte[]> ReadAsync(string key, CancellationToken cancellationToken)
    {
        var path = Resolve(key);
        return await File.ReadAllBytesAsync(path, cancellationToken);
    }

    public Task DeleteAsync(string key, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        File.Delete(Resolve(key));
        return Task.CompletedTask;
    }

    public Task<bool> ExistsAsync(string key, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(File.Exists(Resolve(key)));
    }

    public async IAsyncEnumerable<string> ListKeysAsync([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        foreach (var path in Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories).Order(StringComparer.Ordinal))
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return Path.GetRelativePath(root, path).Replace('\\', '/');
            await Task.Yield();
        }
    }

    private string Resolve(string key)
    {
        if (!SafeObjectKey().IsMatch(key))
        {
            throw new ArgumentException("The object key is not valid.", nameof(key));
        }

        var path = Path.GetFullPath(Path.Combine(root, key.Replace('/', Path.DirectorySeparatorChar)));
        var expectedPrefix = root.EndsWith(Path.DirectorySeparatorChar)
            ? root
            : root + Path.DirectorySeparatorChar;
        if (!path.StartsWith(expectedPrefix, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("The object key escapes the storage root.", nameof(key));
        }

        return path;
    }

    [GeneratedRegex("^tenants/[a-f0-9]{16}/quarantine/[a-f0-9]{32}/v[1-9][0-9]*$", RegexOptions.CultureInvariant)]
    private static partial Regex SafeObjectKey();
}
