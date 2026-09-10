using Microsoft.Extensions.Logging;

namespace TrafficHunt.Web.Logging;

/// <summary>
/// ILogger that forwards records into the shared <see cref="LogStore"/>.
///
/// Scope state (e.g. <c>new Dictionary&lt;string,object&gt;{ ["ReplyCampaignId"] = id }</c>)
/// is captured per-async-flow via an <see cref="AsyncLocal{T}"/> stack and attached
/// to every record written within the scope, so the UI can correlate logs to a
/// specific reply campaign (or Hangfire job id) without parsing message text.
/// </summary>
public sealed class LogStoreLogger : ILogger
{
    private readonly string _category;
    private readonly LogStore _store;
    private readonly LogLevel _minLevel;
    private readonly Func<string, bool> _categoryFilter;
    private readonly AsyncLocal<Stack<ScopeEntry>?> _scopes = new();

    public LogStoreLogger(string category, LogStore store, LogLevel minLevel, Func<string, bool> categoryFilter)
    {
        _category = category;
        _store = store;
        _minLevel = minLevel;
        _categoryFilter = categoryFilter;
    }

    public bool IsEnabled(LogLevel logLevel) =>
        logLevel >= _minLevel && _categoryFilter(_category);

    public IDisposable BeginScope<TState>(TState state)
    {
        // If logging is disabled for this category, skip scope bookkeeping entirely.
        var enabled = _categoryFilter(_category);
        if (!enabled) return NullScope.Instance;

        _scopes.Value ??= new Stack<ScopeEntry>();
        _scopes.Value.Push(new ScopeEntry(ExtractProperties(state)));
        return new ScopeDisposable(this);
    }

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel)) return;

        var entry = new LogEntry
        {
            Level = logLevel,
            Category = _category,
            Message = FormatMessage(state, exception, formatter),
            Timestamp = DateTime.UtcNow,
        };

        // Pull structured context (ReplyCampaignId / HangfireJobId) from any active scope.
        if (_scopes.Value?.Count > 0)
        {
            foreach (var scope in _scopes.Value) // top of stack first
            {
                foreach (var kv in scope.Properties)
                {
                    if (kv.Key.Equals("ReplyCampaignId", StringComparison.OrdinalIgnoreCase) && entry.ReplyCampaignId == null)
                        entry.ReplyCampaignId = AsInt(kv.Value);
                    else if (kv.Key.Equals("HangfireJobId", StringComparison.OrdinalIgnoreCase) && entry.HangfireJobId == null)
                        entry.HangfireJobId = kv.Value?.ToString();
                }
            }
        }

        _store.Append(entry);
    }

    private string FormatMessage<TState>(TState state, Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        var msg = TryFormatStructured(state)
                  ?? formatter?.Invoke(state, null)
                  ?? state?.ToString()
                  ?? string.Empty;

        if (exception != null && !string.IsNullOrEmpty(exception.Message))
            msg += $" | {exception.GetType().Name}: {exception.Message}";

        return msg;
    }

    /// <summary>
    /// Best-effort render of the structured <c>FormattedLogValues</c> / LoggerMessage
    /// state (which carries a "{OriginalFormat}" key). Returns null when the state
    /// is not in the expected key/value shape, so the caller can fall back to its formatter.
    /// </summary>
    private static string? TryFormatStructured<TState>(TState state)
    {
        if (state is IReadOnlyList<KeyValuePair<string, object>> list)
        {
            var originalFormat = "";
            var args = new List<object>();
            foreach (var kv in list)
            {
                if (kv.Key == "{OriginalFormat}")
                    originalFormat = kv.Value?.ToString() ?? string.Empty;
                else
                    args.Add(kv.Value);
            }

            if (!string.IsNullOrEmpty(originalFormat))
            {
                try { return string.Format(originalFormat, [.. args]); }
                catch { return originalFormat; }
            }
        }
        return null;
    }

    private static int? AsInt(object? value)
    {
        if (value == null) return null;
        if (value is int i) return i;
        if (value is string s && int.TryParse(s, out var j)) return j;
        return null;
    }

    private static List<KeyValuePair<string, object>> ExtractProperties<TState>(TState state)
    {
        var props = new List<KeyValuePair<string, object>>();
        if (state is null) return props;
        if (state is IEnumerable<KeyValuePair<string, object>> kvList)
            props.AddRange(kvList);
        else if (state is KeyValuePair<string, object> kv)
            props.Add(kv);
        else
            props.Add(new KeyValuePair<string, object>("Scope", state));
        return props;
    }

    private sealed class ScopeEntry
    {
        public List<KeyValuePair<string, object>> Properties { get; }
        public ScopeEntry(List<KeyValuePair<string, object>> properties) => Properties = properties;
    }

    private sealed class ScopeDisposable : IDisposable
    {
        private readonly LogStoreLogger _logger;
        private bool _disposed;

        public ScopeDisposable(LogStoreLogger logger) => _logger = logger;

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            var stack = _logger._scopes.Value;
            if (stack?.Count > 0) stack.Pop();
        }
    }

    // Used when logging is disabled for a category — a no-op scope handle.
    private sealed class NullScope : IDisposable
    {
        public static readonly NullScope Instance = new();
        public void Dispose() { }
    }
}
