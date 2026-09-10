using TrafficHunt.Domain.Entities;

namespace TrafficHunt.Web.ViewModels;

/// <summary>
/// Background job system page (Hangfire overview).
/// </summary>
public class JobsIndexViewModel
{
    public long ServerCount { get; set; }
    public long Enqueued { get; set; }
    public long Scheduled { get; set; }
    public long Processing { get; set; }
    public long Succeeded { get; set; }
    public long Failed { get; set; }
    public int RecurringJobCount { get; set; }
    public IList<QueueCount> Queues { get; set; } = new List<QueueCount>();
    public IList<RecurringJobRow> RecurringJobs { get; set; } = new List<RecurringJobRow>();
    public IList<RecentJobRow> RecentJobs { get; set; } = new List<RecentJobRow>();
    public IList<ActiveJobRow> ActiveJobs { get; set; } = new List<ActiveJobRow>();

    /// <summary>Optional simple action result message shown as a banner.</summary>
    public string? Message { get; set; }
}

public class QueueCount
{
    public string Queue { get; set; } = string.Empty;
    public long Length { get; set; }
}

public class RecurringJobRow
{
    public string Id { get; set; } = string.Empty;
    public string Cron { get; set; } = string.Empty;
    public string Next { get; set; } = "â€”";
}

public class RecentJobRow
{
    public string JobId { get; set; } = string.Empty;
    public string Method { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string TotalDuration { get; set; } = "â€”";
    public string Ended { get; set; } = "â€”";
}

/// <summary>A job that is enqueued, processing, scheduled or failed â€” i.e. can be killed/deleted.</summary>
public class ActiveJobRow
{
    public string JobId { get; set; } = string.Empty;
    public string Method { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string Detail { get; set; } = string.Empty;
}

/// <summary>Settings page â€” API keys and service endpoints.</summary>
public class SettingsViewModel
{
    public string YouTubeApiKey { get; set; } = string.Empty;
    public string OllamaBaseUrl { get; set; } = string.Empty;
    public string OllamaModel { get; set; } = string.Empty;
    public string ConnectionString { get; set; } = string.Empty;
    public string OAuthClientId { get; set; } = string.Empty;
    public string OAuthClientSecret { get; set; } = string.Empty;
    public bool OAuthConnected { get; set; }
    public string RedirectUri { get; set; } = string.Empty;
}
