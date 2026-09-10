using Hangfire;
using Hangfire.Storage;
using Microsoft.AspNetCore.Mvc;
using TrafficHunt.Infrastructure.Jobs;
using TrafficHunt.Web.ViewModels;

namespace TrafficHunt.Web.Controllers;

/// <summary>
/// Background job system page (Hangfire overview + discovery enqueue).
/// </summary>
public class JobsController : Controller
{
    public IActionResult Index()
    {
        var monitoring = JobStorage.Current.GetMonitoringApi();
        var stats = monitoring.GetStatistics();

        var recurring = JobStorage.Current.GetConnection().GetRecurringJobs();
        var succeeded = monitoring.SucceededJobs(0, 15);

        var active = new List<ActiveJobRow>();
        foreach (var q in monitoring.Queues())
        {
            foreach (var kvp in monitoring.EnqueuedJobs(q.Name ?? string.Empty, 0, 50))
                active.Add(ActiveRow(kvp.Key, kvp.Value, "Enqueued", $"queue: {q.Name}"));
        }
        foreach (var kvp in monitoring.ProcessingJobs(0, 50))
            active.Add(ActiveRow(kvp.Key, kvp.Value, "Processing", $"server: {kvp.Value?.ServerId ?? "—"}"));
        foreach (var kvp in monitoring.ScheduledJobs(0, 50))
            active.Add(ActiveRow(kvp.Key, kvp.Value, "Scheduled",
                $"starts: {kvp.Value?.ScheduledAt?.ToLocalTime().ToString("g") ?? "—"}"));
        foreach (var kvp in monitoring.FailedJobs(0, 50))
            active.Add(ActiveRow(kvp.Key, kvp.Value, "Failed",
                kvp.Value?.ExceptionMessage is string msg ? msg : "—"));

        var model = new JobsIndexViewModel
        {
            ServerCount = stats.Servers,
            Enqueued = stats.Enqueued,
            Scheduled = stats.Scheduled,
            Processing = stats.Processing,
            Succeeded = stats.Succeeded,
            Failed = stats.Failed,
            RecurringJobCount = (int)recurring.Count(),
            Queues = monitoring.Queues()
                .Select(q => new QueueCount { Queue = q.Name ?? string.Empty, Length = q.Length })
                .ToList(),
            RecurringJobs = recurring
                .Select(r => new RecurringJobRow
                {
                    Id = r.Id,
                    Cron = r.Cron,
                    Next = r.NextExecution?.ToLocalTime().ToString("g") ?? "—"
                })
                .ToList(),
            ActiveJobs = active,
            RecentJobs = succeeded
                .Select(kvp => new RecentJobRow
                {
                    JobId = kvp.Key,
                    Method = kvp.Value?.Job?.ToString() ?? "Unknown job",
                    State = "Succeeded",
                    TotalDuration = kvp.Value?.TotalDuration?.ToString("g") ?? "—",
                    Ended = kvp.Value?.SucceededAt?.ToLocalTime().ToString("g") ?? "—"
                })
                .ToList()
        };

        return View(model);
    }

    /// <summary>JSON endpoint for live job-status polling (every 5s from the UI).</summary>
    [HttpGet]
    public IActionResult GetStatus()
    {
        var monitoring = JobStorage.Current.GetMonitoringApi();
        var stats = monitoring.GetStatistics();

        var active = new List<object>();
        foreach (var q in monitoring.Queues())
        {
            foreach (var kvp in monitoring.EnqueuedJobs(q.Name ?? string.Empty, 0, 50))
                active.Add(new { id = kvp.Key, method = kvp.Value?.Job?.ToString() ?? "Unknown", state = "Enqueued", detail = $"queue: {q.Name}" });
        }
        foreach (var kvp in monitoring.ProcessingJobs(0, 50))
            active.Add(new { id = kvp.Key, method = kvp.Value?.Job?.ToString() ?? "Unknown", state = "Processing", detail = $"server: {kvp.Value?.ServerId ?? "—"}" });
        foreach (var kvp in monitoring.ScheduledJobs(0, 50))
            active.Add(new { id = kvp.Key, method = kvp.Value?.Job?.ToString() ?? "Unknown", state = "Scheduled", detail = $"starts: {kvp.Value?.ScheduledAt?.ToLocalTime().ToString("g") ?? "—"}" });
        foreach (var kvp in monitoring.FailedJobs(0, 50))
            active.Add(new { id = kvp.Key, method = kvp.Value?.Job?.ToString() ?? "Unknown", state = "Failed", detail = (kvp.Value?.ExceptionMessage as string) ?? "—" });

        return Json(new
        {
            serverCount = stats.Servers,
            enqueued = stats.Enqueued,
            scheduled = stats.Scheduled,
            processing = stats.Processing,
            succeeded = stats.Succeeded,
            failed = stats.Failed,
            active
        });
    }

    private static ActiveJobRow ActiveRow(string id, dynamic? job, string state, string detail) =>
        new()
        {
            JobId = id,
            Method = job?.Job?.ToString() ?? "Unknown job",
            State = state,
            Detail = detail
        };

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult RunDiscovery(int campaignId, int videosPerKeyword, CancellationToken ct)
    {
        var videos = videosPerKeyword <= 0 ? 3 : videosPerKeyword;
        var jobId = BackgroundJob.Enqueue<YouTubeDiscoveryJob>(job => job.RunAsync(campaignId, videos, default(CancellationToken)));
        TempData["Success"] = $"Extraction queued for campaign {campaignId} (job {jobId}).";
        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// Kill (interrupt + remove) or delete a background job. For jobs already executing,
    /// Delete signals the cancellation token and the worker stops the job at its next
    /// checkpoint; queued/scheduled/failed jobs are removed immediately.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult KillJob(string jobId, string? backToState)
    {
        var deleted = BackgroundJob.Delete(jobId);
        TempData[deleted ? "Success" : "Error"] = deleted
            ? $"Job {jobId} killed/deleted."
            : $"Job {jobId} could not be deleted (already finished or unknown).";
        return RedirectToAction(nameof(Index));
    }

    /// <summary>Requeue a failed or deleted job so it runs again.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult RequeueJob(string jobId)
    {
        var ok = BackgroundJob.Requeue(jobId);
        TempData[ok ? "Success" : "Error"] = ok
            ? $"Job {jobId} requeued."
            : $"Job {jobId} could not be requeued.";
        return RedirectToAction(nameof(Index));
    }

    /// <summary>Remove all succeeded jobs from storage (history cleanup).</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult ClearSucceeded()
    {
        var monitoring = JobStorage.Current.GetMonitoringApi();
        var removed = 0;
        foreach (var id in monitoring.SucceededJobs(0, 1000).Select(kvp => kvp.Key))
            if (BackgroundJob.Delete(id)) removed++;
        TempData["Success"] = $"Cleared {removed} succeeded jobs.";
        return RedirectToAction(nameof(Index));
    }
}
