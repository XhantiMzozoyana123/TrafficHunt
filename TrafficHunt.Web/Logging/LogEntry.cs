using Microsoft.Extensions.Logging;

namespace TrafficHunt.Web.Logging;

/// <summary>
/// A single log entry streamed into the <see cref="LogStore"/>.
///
/// One <see cref="LogEntry"/> is produced, in real time, for:
///  - every inbound HTTP request (Category == "HTTP Request"), and
///  - every captured ILogger record / Hangfire lifecycle event.
///
/// <see cref="ReplyCampaignId"/> and <see cref="HangfireJobId"/> let the UI
/// correlate records back to a particular reply campaign / Hangfire job.
/// </summary>
public sealed class LogEntry
{
    public long Id { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    public LogLevel Level { get; set; } = LogLevel.Information;
    public string Category { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;

    // HTTP request details (Category == "HTTP Request")
    public string? RequestMethod { get; set; }
    public string? RequestPath { get; set; }
    public int? StatusCode { get; set; }
    public long? DurationMs { get; set; }
    public string? CorrelationId { get; set; }

    // Background-job correlation (set via ILogger scopes / Hangfire filter)
    public int? ReplyCampaignId { get; set; }
    public string? HangfireJobId { get; set; }

    public bool IsRequest => Category == "HTTP Request";
    public bool IsHangfire => Category == "Hangfire";

    public string LevelName => Level switch
    {
        LogLevel.Critical => "CRITICAL",
        LogLevel.Error => "ERROR",
        LogLevel.Warning => "WARNING",
        LogLevel.Information => "INFO",
        LogLevel.Debug => "DEBUG",
        LogLevel.Trace => "TRACE",
        _ => Level.ToString().ToUpperInvariant()
    };

    public string LevelClass => Level switch
    {
        LogLevel.Critical => "level-critical",
        LogLevel.Error => "level-error",
        LogLevel.Warning => "level-warning",
        LogLevel.Information => "level-info",
        LogLevel.Debug => "level-debug",
        LogLevel.Trace => "level-debug",
        _ => "level-info"
    };
}
