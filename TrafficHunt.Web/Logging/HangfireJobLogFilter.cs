using Hangfire.Server;

namespace TrafficHunt.Web.Logging;

/// <summary>
/// Hangfire server filter that records job lifecycle events (started / completed /
/// failed) into the <see cref="LogStore"/>, tagged with the Hangfire job id and the
/// campaign id (when the job's first argument is an int id).
///
/// Together with <see cref="LogStoreLogger"/> (which captures the in-method ILogger
/// calls) this gives a complete, real-time timeline of background jobs in the UI.
/// </summary>
public sealed class HangfireJobLogFilter : IServerFilter
{
    private readonly LogStore _store;

    public HangfireJobLogFilter(LogStore store) => _store = store;

    public void OnPerforming(PerformingContext filterContext)
    {
        var (id, method, campaignId) = Describe(filterContext.BackgroundJob);
        _store.Append(new LogEntry
        {
            Level = LogLevel.Information,
            Category = "Hangfire",
            Message = $"Job started: {method}",
            HangfireJobId = id,
            ReplyCampaignId = campaignId
        });
    }

    public void OnPerformed(PerformedContext filterContext)
    {
        var (id, method, campaignId) = Describe(filterContext.BackgroundJob);
        var level = filterContext.Exception == null ? LogLevel.Information : LogLevel.Error;
        var message = filterContext.Exception == null
            ? $"Job completed: {method}"
            : $"Job failed: {method} — {filterContext.Exception?.Message}";

        _store.Append(new LogEntry
        {
            Level = level,
            Category = "Hangfire",
            Message = message,
            HangfireJobId = id,
            ReplyCampaignId = campaignId
        });
    }

    private static (string? id, string method, int? campaignId) Describe(
        Hangfire.BackgroundJob? job)
    {
        if (job?.Job is null) return (job?.Id, "unknown", null);

        var method = job.Job.Method?.Name ?? job.Job.ToString() ?? "job";
        int? campaignId = null;

        try
        {
            var args = job.Job.Args;
            if (args is { Count: > 0 } && args[0] is int cid)
                campaignId = cid;
        }
        catch { /* args access is best-effort */ }

        return (job.Id, method, campaignId);
    }
}
