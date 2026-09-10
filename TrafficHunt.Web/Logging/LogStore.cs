using System.Collections.Concurrent;

namespace TrafficHunt.Web.Logging;

/// <summary>
/// Thread-safe, bounded, in-memory ring buffer that powers the live operator console.
///
/// It is a singleton shared by three producers:
///   - <see cref="RequestLoggingMiddleware"/> (HTTP request logs),
///   - <see cref="LogStoreLoggerProvider"/> (ILogger records from TrafficHunt jobs/services),
///   - <see cref="HangfireJobLogFilter"/> (Hangfire job lifecycle events).
///
/// Consumers: <see cref="Controllers.LogsController"/>.
/// </summary>
public sealed class LogStore
{
    public const int MaxEntries = 5000;

    private readonly ConcurrentQueue<LogEntry> _entries = new();
    private long _counter;

    /// <summary>Append an entry, assigning a monotonic id + UTC timestamp if unset.</summary>
    public void Append(LogEntry entry)
    {
        if (entry.Timestamp == default) entry.Timestamp = DateTime.UtcNow;
        entry.Id = Interlocked.Increment(ref _counter);
        _entries.Enqueue(entry);

        // Bound the buffer: evict oldest entries once we exceed MaxEntries.
        while (_entries.Count > MaxEntries && _entries.TryDequeue(out _)) { }
    }

    /// <summary>Snapshot of the most recent <paramref name="limit"/> entries, oldest-first (chronological).</summary>
    public LogEntry[] GetRecent(int limit = 200)
    {
        var arr = _entries.ToArray(); // ConcurrentQueue.ToArray is enqueue order == chronological
        var start = Math.Max(0, arr.Length - limit);
        var count = arr.Length - start;
        var result = new LogEntry[count];
        Array.Copy(arr, start, result, 0, count);
        return result;
    }

    /// <summary>Entries with Id &gt; <paramref name="sinceId"/>, oldest-first, capped at <paramref name="limit"/>.</summary>
    public LogEntry[] GetSince(long sinceId, int limit = 200)
    {
        var arr = _entries.ToArray(); // chronological
        var result = new List<LogEntry>(Math.Min(limit, arr.Length));
        foreach (var e in arr)
        {
            if (e.Id <= sinceId) continue;
            result.Add(e);
            if (result.Count == limit) break;
        }
        return result.ToArray();
    }

    /// <summary>Remove all buffered entries and reset the id counter.</summary>
    public void Clear()
    {
        while (_entries.TryDequeue(out _)) { }
        Interlocked.Exchange(ref _counter, 0);
    }

    public int Count => _entries.Count;
}
