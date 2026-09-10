using Microsoft.AspNetCore.Mvc;
using TrafficHunt.Application.Interfaces;
using TrafficHunt.Web.ViewModels;

namespace TrafficHunt.Web.Controllers;

public class HomeController : Controller
{
    private readonly ICampaignService _campaigns;
    private readonly IProspectService _prospects;
    private readonly IReplyCampaignService _replyCampaigns;

    public HomeController(
        ICampaignService campaigns,
        IProspectService prospects,
        IReplyCampaignService replyCampaigns)
    {
        _campaigns = campaigns;
        _prospects = prospects;
        _replyCampaigns = replyCampaigns;
    }

    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var global = await _prospects.GetGlobalStatsAsync(ct);
        var campaigns = await _campaigns.GetAllAsync(ct);

        var summaries = new List<CampaignSummary>();
        foreach (var campaign in campaigns)
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

        var replyCampaigns = await _replyCampaigns.GetAllAsync(ct);

        var model = new DashboardViewModel
        {
            TotalCampaigns = campaigns.Count,
            TotalProspects = global.TotalProspects,
            HighIntentProspects = global.HighIntent,
            ContactedProspects = global.Contacted,
            InterestedProspects = global.Interested,
            ConvertedProspects = global.Converted,
            RunningReplyCampaigns = replyCampaigns.Count(c =>
                c.Status == TrafficHunt.Domain.Entities.ReplyCampaignStatus.Running),
            RepliesSent = replyCampaigns.Sum(c => c.RepliesSent),
            Campaigns = summaries.OrderByDescending(s => s.CreatedAt).ToList(),
            RecentReplyCampaigns = replyCampaigns.Take(6).ToList()
        };

        return View(model);
    }

    public IActionResult Error() => View();
}