using Microsoft.AspNetCore.Mvc;
using TrafficHunt.Application.Interfaces;
using TrafficHunt.Web.ViewModels;

namespace TrafficHunt.Web.Controllers;

public class ProspectsController : Controller
{
    private readonly IProspectService _prospects;
    private readonly ICampaignService _campaigns;

    public ProspectsController(IProspectService prospects, ICampaignService campaigns)
    {
        _prospects = prospects;
        _campaigns = campaigns;
    }

    public async Task<IActionResult> Index(
        int? campaignId,
        string? status,
        int? minIntentScore,
        CancellationToken ct)
    {
        var campaigns = await _campaigns.GetAllAsync(ct);

        var model = new ProspectIndexViewModel
        {
            CampaignId = campaignId ?? 0,
            Status = status,
            MinIntentScore = minIntentScore,
            Campaigns = campaigns
        };

        if (campaignId is > 0)
        {
            var campaign = campaigns.FirstOrDefault(c => c.Id == campaignId);
            model.CampaignName = campaign?.Name ?? string.Empty;
            model.Prospects = await _prospects.GetByCampaignAsync(
                campaignId.Value, status, minIntentScore, ct);
        }

        return View(model);
    }

    public async Task<IActionResult> Details(int id, CancellationToken ct)
    {
        var prospect = await _prospects.GetByIdAsync(id, ct);
        if (prospect is null) return NotFound();
        return View(prospect);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(int id, string status, int campaignId, string list, CancellationToken ct)
    {
        await _prospects.UpdateStatusAsync(id, status, ct);
        TempData["Success"] = "Prospect status updated.";

        if (list == "details")
            return RedirectToAction(nameof(Details), new { id });
        return RedirectToAction(nameof(Index), new { campaignId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, int campaignId, CancellationToken ct)
    {
        await _prospects.DeleteAsync(id, ct);
        TempData["Success"] = "Prospect deleted.";
        return RedirectToAction(nameof(Index), new { campaignId });
    }
}
