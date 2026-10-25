using System.Collections.Concurrent;

namespace Orders.Api.Infrastructure;

// Remembers the idempotency keys of the last 24 hours. One instance per process: a retry that reaches
// another instance is not detected. The webshop retries on the same connection, so this is enough for now.
public sealed class IdempotencyStore(TimeProvider time)
{
    private static readonly TimeSpan s_retention = TimeSpan.FromHours(24);
    private readonly ConcurrentDictionary<string, DateTimeOffset> _keys = new(StringComparer.Ordinal);

    public bool TryRegister(string key)
    {
        var now = time.GetUtcNow();
        foreach (var old in _keys.Where(entry => now - entry.Value > s_retention))
        {
            _keys.TryRemove(old.Key, out _);
        }

        return _keys.TryAdd(key, now);
    }
}
