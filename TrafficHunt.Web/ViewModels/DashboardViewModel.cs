using TrafficHunt.Domain.Entities;

namespace TrafficHunt.Web.ViewModels;

/// <summary>
/// Data shown on the system dashboard (Home page).
/// </summary>
public class DashboardViewModel
{
    public int TotalCampaigns { get; set; }
    public int TotalProspects { get; set; }
    public int HighIntentProspects { get; set; }
    public int ContactedProspects { get; set; }
    public int InterestedProspects { get; set; }
    public int ConvertedProspects { get; set; }
    public int RunningReplyCampaigns { get; set; }
    public int RepliesSent { get; set; }
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    public List<CampaignSummary> Campaigns { get; set; } = new();
    public List<ReplyCampaign> RecentReplyCampaigns { get; set; } = new();
}

/// <summary>
/// A campaign with its quick metrics, re-used on the dashboard and campaign list.
/// </summary>
public class CampaignSummary
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public int KeywordCount { get; set; }
    public int ProspectCount { get; set; }
    public int HighIntent { get; set; }
}