using Microsoft.AspNetCore.Mvc;
using TrafficHunt.Web.Logging;
using TrafficHunt.Web.ViewModels;

namespace TrafficHunt.Web.Controllers;

/// <summary>
/// Live log viewer. Streams HTTP request logs and background-job logs (including
/// Reply Campaign Hangfire jobs) into the browser via lightweight AJAX polling,
/// consistent with the rest of the console.
/// </summary>
public sealed class LogsController : Controller
{
    private readonly LogStore _store;

    public LogsController(LogStore store) => _store = store;

    public IActionResult Index()
    {
        var entries = _store.GetRecent(LogsIndexViewModel.DefaultPageSize); // chronological
        return View(new LogsIndexViewModel { Entries = entries });
    }

    /// <summary>
    /// Polling endpoint. Returns only entries newer than <paramref name="sinceId"/>
    /// (so the browser only receives deltas), optionally filtered by reply campaign
    /// or category.
    /// </summary>
    [HttpGet]
    public IActionResult Recent(long sinceId = 0, int? campaignId = null, string? category = null, int limit = 200)
    {
        var entries = _store.GetSince(sinceId, limit);
        if (campaignId.HasValue) entries = entries.Where(e => e.ReplyCampaignId == campaignId).ToArray();
        if (!string.IsNullOrWhiteSpace(category)) entries = entries.Where(e => e.Category == category).ToArray();

        return Json(new
        {
            entries = entries.Select(e => new
            {
                id = e.Id,
                time = e.Timestamp.ToUniversalTime().ToString("o"),
                level = e.LevelName,
                levelClass = e.LevelClass,
                category = e.Category,
                message = e.Message,
                method = e.RequestMethod,
                path = e.RequestPath,
                status = e.StatusCode,
                durationMs = e.DurationMs,
                correlationId = e.CorrelationId,
                campaignId = e.ReplyCampaignId,
                jobId = e.HangfireJobId
            })
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Clear()
    {
        _store.Clear();
        TempData["Success"] = "Live logs cleared.";
        return RedirectToAction(nameof(Index));
    }
}
