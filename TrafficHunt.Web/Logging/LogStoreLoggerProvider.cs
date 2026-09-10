using Microsoft.Extensions.Logging;

namespace TrafficHunt.Web.Logging;

/// <summary>
/// ILoggerProvider that mirrors log records into the shared <see cref="LogStore"/>
/// so background-job logs can be streamed live to the operator console.
///
/// Only categories matching <see cref="CategoryFilter"/> are captured (defaults to
/// the "TrafficHunt" namespace) so Microsoft/EntityFramework noise stays out of the
/// console. Add this provider alongside the default ones (via
/// <c>builder.Logging.AddProvider(...)</c>) so the existing console output is kept.
/// </summary>
public sealed class LogStoreLoggerProvider : ILoggerProvider
{
    private readonly LogStore _store;
    private readonly LogLevel _minLevel;
    private readonly Func<string, bool> _categoryFilter;

    public LogStoreLoggerProvider(LogStore store, LogLevel minLevel, Func<string, bool>? categoryFilter = null)
    {
        _store = store;
        _minLevel = minLevel;
        _categoryFilter = categoryFilter ?? DefaultCategoryFilter;
    }

    public ILogger CreateLogger(string categoryName) =>
        new LogStoreLogger(categoryName, _store, _minLevel, _categoryFilter);

    public void Dispose() { /* snapshot is GC'd when the host shuts down */ }

    private static bool DefaultCategoryFilter(string category) =>
        category.StartsWith("TrafficHunt", StringComparison.OrdinalIgnoreCase);
}
