using System.Collections.Concurrent;

namespace SideGigPh.Maui.Services.Api;

/// <summary>
/// Answers the API gave recently, kept in memory for <see cref="FreshFor"/> so moving between screens doesn't ask
/// again. Only successful reads go in (a failed read is never cached), and a write that changes the data removes it.
/// </summary>
public sealed class TimedCache<T>(TimeSpan freshFor)
{
    private readonly ConcurrentDictionary<string, (T Value, DateTimeOffset At)> _entries = new();

    public TimeSpan FreshFor { get; } = freshFor;

    public bool TryGet(string key, out T value)
    {
        if (_entries.TryGetValue(key, out var entry) && DateTimeOffset.UtcNow - entry.At < FreshFor)
        {
            value = entry.Value;
            return true;
        }

        value = default!;
        return false;
    }

    public void Set(string key, T value) => _entries[key] = (value, DateTimeOffset.UtcNow);

    public void Remove(string key) => _entries.TryRemove(key, out _);

    public void Clear() => _entries.Clear();
}
