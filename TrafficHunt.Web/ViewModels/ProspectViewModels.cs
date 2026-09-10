using TrafficHunt.Domain.Entities;

namespace TrafficHunt.Web.ViewModels;

/// <summary>
/// Prospect list page with filters.
/// </summary>
public class ProspectIndexViewModel
{
    public int CampaignId { get; set; }
    public string CampaignName { get; set; } = string.Empty;
    public string? Status { get; set; }
    public int? MinIntentScore { get; set; }
    public List<Prospect> Prospects { get; set; } = new();
    public List<Campaign> Campaigns { get; set; } = new();
    public bool HasCampaign => CampaignId > 0;
}