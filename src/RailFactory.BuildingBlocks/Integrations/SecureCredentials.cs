using System.Security.Cryptography;
using System.Text;

namespace RailFactory.BuildingBlocks.Integrations;

/// <summary>
/// Holds credential values as UTF-8 byte arrays that are zeroed on Dispose,
/// preventing plaintext strings from lingering on the managed heap.
/// </summary>
public sealed class SecureCredentials : IDisposable
{
    private readonly Dictionary<string, byte[]> _data;
    private bool _disposed;

    private SecureCredentials(Dictionary<string, byte[]> data) => _data = data;

    public static SecureCredentials FromStrings(IReadOnlyDictionary<string, string> source)
    {
        var data = new Dictionary<string, byte[]>(source.Count, StringComparer.Ordinal);
        foreach (var (k, v) in source)
            data[k] = Encoding.UTF8.GetBytes(v);
        return new SecureCredentials(data);
    }

    public IReadOnlyCollection<string> Keys => _data.Keys;

    public bool TryGetString(string key, out string value)
    {
        if (_data.TryGetValue(key, out var bytes))
        {
            value = Encoding.UTF8.GetString(bytes);
            return true;
        }
        value = string.Empty;
        return false;
    }

    public string GetString(string key) =>
        _data.TryGetValue(key, out var bytes)
            ? Encoding.UTF8.GetString(bytes)
            : throw new KeyNotFoundException($"Credential key '{key}' not found.");

    /// <summary>Returns a snapshot as strings for JSON serialization. Caller must not persist the result.</summary>
    public IReadOnlyDictionary<string, string> ToStringDictionary()
    {
        var result = new Dictionary<string, string>(_data.Count, StringComparer.Ordinal);
        foreach (var (k, v) in _data)
            result[k] = Encoding.UTF8.GetString(v);
        return result;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        foreach (var bytes in _data.Values)
            CryptographicOperations.ZeroMemory(bytes);
        _data.Clear();
    }
}
