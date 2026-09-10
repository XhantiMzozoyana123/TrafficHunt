using Hangfire;
using System.Collections.Concurrent;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TrafficHunt.Application.Dtos;
using TrafficHunt.Application.Interfaces;
using TrafficHunt.Infrastructure.Jobs;
using TrafficHunt.Web.ViewModels;

namespace TrafficHunt.Web.Controllers;

public class ReplyCampaignsController : Controller
{
    private readonly IReplyCampaignService _service;
    private readonly ICampaignService _campaigns;
    private readonly IServiceScopeFactory _scopeFactory;

    // Per-process guard so two requests/pollers never run the same campaign's send
    // loop concurrently (which could double-send a reply). Removed when the run ends.
    private static readonly ConcurrentDictionary<int, byte> _activeRuns = new();

    public ReplyCampaignsController(
        IReplyCampaignService service,
        ICampaignService campaigns,
        IServiceScopeFactory scopeFactory)
    {
        _service = service;
        _campaigns = campaigns;
        _scopeFactory = scopeFactory;
    }

    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var campaigns = await _service.GetAllAsync(ct);
        return View(new ReplyCampaignIndexViewModel
        {
            ReplyCampaigns = campaigns,
            TotalRepliesSent = campaigns.Sum(c => c.RepliesSent)
        });
    }

    public async Task<IActionResult> Details(int id, CancellationToken ct)
    {
        var campaign = await _service.GetByIdAsync(id, ct);
        if (campaign is null) return NotFound();

        // Self-healing: if the campaign is marked Running but still has unsent work and
        // nothing is actively processing it, kick off the background run. This recovers
        // campaigns left "Running" by the old Hangfire-based flow (which never actually
        // executed) so they resume instead of sitting stuck at Sent 0. The guard in
        // RunInBackground prevents starting a second loop if one is already going.
        if (campaign.Status == TrafficHunt.Domain.Entities.ReplyCampaignStatus.Running &&
            campaign.ReplyRecords.Any(r =>
                r.Status == TrafficHunt.Domain.Entities.ReplyStatus.Approved ||
                r.Status == TrafficHunt.Domain.Entities.ReplyStatus.Pending))
        {
            RunInBackground(id);
        }

        var total = campaign.ReplyRecords.Count;
        var model = new ReplyCampaignDetailViewModel
        {
            Campaign = campaign,
            CampaignName = campaign.Campaign?.Name ?? string.Empty,
            Records = campaign.ReplyRecords.OrderByDescending(r => r.CreatedAt).ToList(),
            SentPercent = total == 0 ? 0 : campaign.RepliesSent * 100.0 / total,
            PendingPercent = total == 0 ? 0 : campaign.RepliesPending * 100.0 / total,
            FailedPercent = total == 0 ? 0 : campaign.RepliesFailed * 100.0 / total
        };

        // Generate AI analytics summary for running/completed campaigns (best-effort).
        try
        {
            model.Analytics = await _service.GetAnalyticsAsync(id, ct);
        }
        catch
        {
            model.Analytics = null;
        }

        return View(model);
    }

    /// <summary>JSON endpoint for live campaign progress polling (every 5s from the UI).</summary>
    [HttpGet]
    public async Task<IActionResult> GetStatus(int id, CancellationToken ct)
    {
        var campaign = await _service.GetByIdAsync(id, ct);
        if (campaign is null) return NotFound();

        var records = campaign.ReplyRecords.OrderByDescending(r => r.CreatedAt).Select(r => new
        {
            r.Id,
            prospect = r.Prospect?.AuthorName ?? "—",
            comment = r.Prospect?.CommentText ?? "",
            r.MessageSent,
            status = r.Status.ToString(),
            sentAt = r.SentAt?.ToLocalTime().ToString("g"),
            r.ErrorMessage
        });

        return Json(new
        {
            status = campaign.Status.ToString(),
            sent = campaign.RepliesSent,
            pending = campaign.RepliesPending,
            failed = campaign.RepliesFailed,
            total = campaign.ReplyRecords.Count,
            startedAt = campaign.StartedAt?.ToLocalTime().ToString("g"),
            completedAt = campaign.CompletedAt?.ToLocalTime().ToString("g"),
            records
        });
    }

    [HttpGet]
    public async Task<IActionResult> Create(CancellationToken ct)
    {
        return View(new ReplyCampaignCreateViewModel
        {
            Campaigns = await _campaigns.GetAllAsync(ct)
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ReplyCampaignCreateViewModel model, CancellationToken ct)
    {
        model.Campaigns = await _campaigns.GetAllAsync(ct);
        if (!ModelState.IsValid) return View(model);

        // Persist the website URL onto the source campaign — the AI template generator
        // and per-prospect replies use it as the ONLY promotional element.
        var source = await _campaigns.GetByIdAsync(model.CampaignId, ct);
        if (source == null)
        {
            ModelState.AddModelError(nameof(model.CampaignId), "Source campaign not found.");
            return View(model);
        }
        if (!string.IsNullOrWhiteSpace(model.WebsiteUrl))
        {
            source.ProductUrl = model.WebsiteUrl.Trim();
            await _campaigns.UpdateAsync(source, ct);
        }

        var request = new CreateReplyCampaignRequest
        {
            CampaignId = model.CampaignId,
            Name = model.Name,
            TemplateBodies = new List<string>(), // templates are always AI-generated
            MinDelaySeconds = model.MinDelaySeconds,
            MaxDelaySeconds = model.MaxDelaySeconds,
            MinIntentScore = model.MinIntentScore
        };

        var campaign = await _service.CreateAsync(request, ct);
        // Template generation is ONE bounded LLM call on the "default" queue —
        // never inline: the remote LLM cold-loads (60s+) and would time out HTTP.
        BackgroundJob.Enqueue<ReplyTemplateJob>(job => job.RunAsync(campaign.Id, default(CancellationToken)));
        TempData["Success"] = $"Reply campaign \"{campaign.Name}\" created as a draft. AI templates are generating in the background.";
        return RedirectToAction(nameof(Details), new { id = campaign.Id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Start(int id, CancellationToken ct)
    {
        await _service.StartAsync(id, ct);
        RunInBackground(id);
        TempData["Success"] = "Reply campaign started. Sending is running in the background — watch it here.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Pause(int id, CancellationToken ct)
    {
        await _service.PauseAsync(id, ct);
        TempData["Success"] = "Reply campaign paused.";
        return RedirectToAction(nameof(Details), new { id });
    }

    /// <summary>
    /// Unpauses a campaign and kicks off sending in the background (no Hangfire job).
    /// ResumeAsync just flips the status back to Running; the AI drafting/sending is
    /// handled by the background run started here.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Resume(int id, CancellationToken ct)
    {
        await _service.ResumeAsync(id, ct);
        RunInBackground(id);
        TempData["Success"] = "Reply campaign resumed. Sending is running in the background — watch it here.";
        return RedirectToAction(nameof(Details), new { id });
    }

    /// <summary>Drafts + approves one pending reply (human approval gate).</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(int recordId, int replyCampaignId, CancellationToken ct)
    {
        var record = await _service.ApproveAsync(recordId, ct);
        TempData[record == null ? "Error" : "Success"] = record == null
            ? "Reply record not found or already handled."
            : "Reply drafted and approved. It will be sent while the campaign is running.";
        return RedirectToAction(nameof(Details), new { id = replyCampaignId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult ApproveAll(int id, CancellationToken ct)
    {
        // Chunked: enqueue a 5-record drafting job on the "ai" queue instead of
        // drafting inline — a cold LLM makes inline approval time out HTTP.
        // The job self-chains until no Pending records remain.
        BackgroundJob.Enqueue<ReplyDraftingJob>(job => job.RunAsync(id, default(CancellationToken), ReplyDraftingJob.DefaultBatchSize));
        TempData["Success"] = "AI drafting queued — replies will be drafted + approved in small batches. Refresh to watch progress.";
        return RedirectToAction(nameof(Details), new { id });
    }

    /// <summary>
    /// Resets all failed replies back to Pending (they will be re-drafted with a fresh
    /// AI message) and restarts the background send loop.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RetryFailed(int id, CancellationToken ct)
    {
        var count = await _service.RetryFailedAsync(id, ct);
        TempData[count == 0 ? "Error" : "Success"] = count == 0
            ? "No failed replies to retry."
            : $"{count} failed replies reset to pending — retrying in the background.";
        if (count > 0) RunInBackground(id);
        return RedirectToAction(nameof(Details), new { id });
    }

    /// <summary>GET: edit a reply campaign's name + delay range.</summary>
    [HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken ct)
    {
        var campaign = await _service.GetByIdAsync(id, ct);
        if (campaign == null) return NotFound();

        return View(new ReplyCampaignEditViewModel
        {
            Id = campaign.Id,
            Name = campaign.Name,
            MinDelaySeconds = campaign.MinDelaySeconds,
            MaxDelaySeconds = campaign.MaxDelaySeconds,
            Status = campaign.Status.ToString()
        });
    }

    /// <summary>POST: save reply campaign edits.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(ReplyCampaignEditViewModel model, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return View(model);

        try
        {
            var updated = await _service.UpdateAsync(
                model.Id, model.Name, model.MinDelaySeconds, model.MaxDelaySeconds, ct);

            if (!updated)
            {
                TempData["Error"] = "Reply campaign not found.";
                return RedirectToAction(nameof(Index));
            }

            TempData["Success"] = "Reply campaign updated.";
            return RedirectToAction(nameof(Details), new { id = model.Id });
        }
        catch (ArgumentException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(model);
        }
    }

    /// <summary>
    /// Deletes a reply campaign and ALL its records. A running campaign is paused
    /// first so the background send loop stops cleanly.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var deleted = await _service.DeleteAsync(id, ct);
        TempData[deleted ? "Success" : "Error"] = deleted
            ? "Reply campaign deleted (including all its reply records)."
            : "Reply campaign not found — it may have been deleted already.";
        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// Runs a reply campaign in the background (fire-and-forget), so the HTTP
    /// request returns immediately and the browser isn't left "loading" while the
    /// campaign sends its replies. Each reply is sent directly against YouTube —
    /// no Hangfire job — inside a dedicated DI scope so the DbContext is safe.
    /// The details page polls <see cref="GetStatus"/> to show live progress.
    /// </summary>
    private void RunInBackground(int replyCampaignId)
    {
        // Guard against concurrent runs of the same campaign (a reload/poll must not
        // start a second loop which could double-send a reply).
        if (!_activeRuns.TryAdd(replyCampaignId, 0)) return;

        _ = Task.Run(async () =>
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var service = scope.ServiceProvider.GetRequiredService<IReplyCampaignService>();
                await service.RunAllAsync(replyCampaignId, CancellationToken.None);
            }
            catch (Exception ex)
            {
                try
                {
                    var logScope = _scopeFactory.CreateScope();
                    using var _ = logScope;
                    var logger = logScope.ServiceProvider.GetRequiredService<ILogger<ReplyCampaignsController>>();
                    logger.LogError(ex, "Background reply-campaign run failed for campaign {ReplyCampaignId}", replyCampaignId);
                }
                catch { /* logging is best-effort */ }
            }
            finally
            {
                _activeRuns.TryRemove(replyCampaignId, out _);
            }
        });
    }
}
