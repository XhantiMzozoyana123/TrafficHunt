using Hangfire;
using Microsoft.AspNetCore.Mvc;
using TrafficHunt.Application.Interfaces;
using TrafficHunt.Domain.Entities;
using TrafficHunt.Infrastructure.Jobs;
using TrafficHunt.Web.ViewModels;

namespace TrafficHunt.Web.Controllers;

public class CampaignsController : Controller
{
    private readonly ICampaignService _campaigns;
    private readonly ICampaignPlanner _planner;
    private readonly IProspectService _prospects;
    private readonly IReplyCampaignService _replyCampaigns;

    public CampaignsController(
        ICampaignService campaigns,
        ICampaignPlanner planner,
        IProspectService prospects,
        IReplyCampaignService replyCampaigns)
    {
        _campaigns = campaigns;
        _planner = planner;
        _prospects = prospects;
        _replyCampaigns = replyCampaigns;
    }

    // ---- List ----
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var all = await _campaigns.GetAllAsync(ct);
        var totalProspects = (await _prospects.GetGlobalStatsAsync(ct)).TotalProspects;

        var summaries = new List<CampaignSummary>();
        foreach (var campaign in all)
        {
            var stats = await _campaigns.GetStatsAsync(campaign.Id, ct);
            summaries.Add(new CampaignSummary
            {
                Id = campaign.Id,
                Name = campaign.Name,
                ProductName = campaign.ProductName,
                Status = campaign.Status,
                CreatedAt = campaign.CreatedAt,
                KeywordCount = campaign.Keywords.Count,
                ProspectCount = stats.TotalProspects,
                HighIntent = stats.HighIntent
            });
        }

        return View(new CampaignIndexViewModel
        {
            Campaigns = summaries,
            TotalProspects = totalProspects
        });
    }

    // ---- Detail ----
    public async Task<IActionResult> Details(int id, CancellationToken ct)
    {
        var campaign = await _campaigns.GetByIdAsync(id, ct);
        if (campaign is null) return NotFound();

        var topProspects = (await _prospects.GetByCampaignAsync(id, ct: ct))
            .OrderByDescending(p => p.IntentScore)
            .Take(10)
            .ToList();

        var replyCampaigns = await _replyCampaigns.GetByCampaignAsync(id, ct);

        var model = new CampaignDetailViewModel
        {
            Campaign = campaign,
            Stats = await _campaigns.GetStatsAsync(id, ct),
            TopProspects = topProspects,
            ReplyCampaigns = replyCampaigns
        };

        return View(model);
    }

    // ---- Create ----
    [HttpGet]
    public IActionResult Create() => View(new CampaignFormViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CampaignFormViewModel model, CancellationToken ct)
    {
        if (!ModelState.IsValid) return View(model);

        var campaign = await _campaigns.CreateAsync(new Campaign
        {
            Name = model.Name,
            ProductName = model.ProductName,
            ProductDescription = model.ProductDescription,
            ProductUrl = model.ProductUrl,
            ValueProposition = model.ValueProposition,
            TargetAudience = model.TargetAudience,
            PrimaryProblem = model.PrimaryProblem,
            QualificationRules = model.QualificationRules,
            OutreachInstructions = model.OutreachInstructions,
            ReplyInstructions = model.ReplyInstructions,
            Status = model.Status
        }, ct);

        await AddProblemsFromTextAsync(campaign.Id, model.ProblemsText, ct);
        await AddKeywordsFromTextAsync(campaign.Id, model.KeywordsText, ct);

        TempData["Success"] = $"Campaign \"{campaign.Name}\" created.";
        return RedirectToAction(nameof(Details), new { id = campaign.Id });
    }

    // ---- Edit ----
    [HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken ct)
    {
        var campaign = await _campaigns.GetByIdAsync(id, ct);
        if (campaign is null) return NotFound();

        return View(new CampaignFormViewModel
        {
            Id = campaign.Id,
            Name = campaign.Name,
            ProductName = campaign.ProductName,
            ProductDescription = campaign.ProductDescription,
            ProductUrl = campaign.ProductUrl,
            ValueProposition = campaign.ValueProposition,
            TargetAudience = campaign.TargetAudience,
            PrimaryProblem = campaign.PrimaryProblem,
            QualificationRules = campaign.QualificationRules,
            OutreachInstructions = campaign.OutreachInstructions,
            ReplyInstructions = campaign.ReplyInstructions,
            Status = campaign.Status
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(CampaignFormViewModel model, CancellationToken ct)
    {
        if (!ModelState.IsValid) return View(model);

        var campaign = await _campaigns.GetByIdAsync(model.Id, ct);
        if (campaign is null) return NotFound();

        var updated = await _campaigns.UpdateAsync(new Campaign
        {
            Id = campaign.Id,
            Name = model.Name,
            ProductName = model.ProductName,
            ProductDescription = model.ProductDescription,
            ProductUrl = model.ProductUrl,
            ValueProposition = model.ValueProposition,
            TargetAudience = model.TargetAudience,
            PrimaryProblem = model.PrimaryProblem,
            QualificationRules = model.QualificationRules,
            OutreachInstructions = model.OutreachInstructions,
            ReplyInstructions = model.ReplyInstructions,
            Status = model.Status
        }, ct);

        if (updated is null) return NotFound();

        TempData["Success"] = "Campaign updated.";
        return RedirectToAction(nameof(Details), new { id = campaign.Id });
    }

    // ---- Delete ----
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var deleted = await _campaigns.DeleteAsync(id, ct);
        if (!deleted) return NotFound();
        TempData["Success"] = "Campaign deleted.";
        return RedirectToAction(nameof(Index));
    }

    // ---- AI-assisted planning (form post → AI plan) ----
    [HttpGet]
    public IActionResult Plan() => View(new CampaignPlanViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Plan(CampaignPlanViewModel model, CancellationToken ct)
    {
        if (!ModelState.IsValid) return View(model);

        try
        {
            var plan = await _planner.PlanAsync(model.Description, ct);
            model.Plan = plan;
            model.VideosPerKeyword ??= 3;
            return View(model);
        }
        catch (Exception ex)
        {
            model.Error = $"AI planning failed: {ex.Message}";
            return View(model);
        }
    }

    /// <summary>
    /// Kick off discovery as a Hangfire background job and return to the campaign detail.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult RunDiscovery(int id, int? videosPerKeyword, CancellationToken ct)
    {
        var videos = videosPerKeyword ?? 3;
        var jobId = BackgroundJob.Enqueue<YouTubeDiscoveryJob>(job => job.RunAsync(id, videos, default(CancellationToken)));

        TempData["Success"] = $"Extraction queued (job {jobId}). Refresh the campaign to see results.";
        return RedirectToAction(nameof(Details), new { id });
    }

    // ---- Keywords ----
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddKeyword(int campaignId, string keyword, CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(keyword))
            await _campaigns.AddKeywordAsync(campaignId, keyword, ct);
        TempData["Success"] = "Keyword added.";
        return RedirectToAction(nameof(Details), new { id = campaignId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveKeyword(int keywordId, int campaignId, CancellationToken ct)
    {
        await _campaigns.RemoveKeywordAsync(keywordId, ct);
        TempData["Success"] = "Keyword removed.";
        return RedirectToAction(nameof(Details), new { id = campaignId });
    }

    // ---- Helpers ----
    private async Task AddProblemsFromTextAsync(int campaignId, string text, CancellationToken ct)
    {
        var problems = SplitLines(text);
        if (problems.Count > 0)
            await _campaigns.AddProblemsAsync(campaignId, problems, ct);
    }

    private async Task AddKeywordsFromTextAsync(int campaignId, string text, CancellationToken ct)
    {
        foreach (var keyword in SplitLines(text))
            await _campaigns.AddKeywordAsync(campaignId, keyword, ct);
    }

    private static List<string> SplitLines(string? text) =>
        (text ?? string.Empty)
            .Split('\n')
            .Select(s => s.Trim())
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .ToList();
}