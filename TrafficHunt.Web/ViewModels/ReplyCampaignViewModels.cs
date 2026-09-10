using TrafficHunt.Application.Dtos;
using TrafficHunt.Domain.Entities;

namespace TrafficHunt.Web.ViewModels;

/// <summary>
/// Reply campaign list page.
/// </summary>
public class ReplyCampaignIndexViewModel
{
    public List<ReplyCampaign> ReplyCampaigns { get; set; } = new();
    public int TotalRepliesSent { get; set; }
}

/// <summary>
/// Reply campaign create form.
/// </summary>
public class ReplyCampaignCreateViewModel
{
    public int CampaignId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int MinIntentScore { get; set; } = 80;
    public int MinDelaySeconds { get; set; } = 120;
    public int MaxDelaySeconds { get; set; } = 180;

    /// <summary>Website URL the AI replies will drive traffic to (saved to the source campaign).</summary>
    public string WebsiteUrl { get; set; } = string.Empty;

    public List<Campaign> Campaigns { get; set; } = new();
}

/// <summary>
/// Reply campaign edit form (name + anti-spam delay range).
/// </summary>
public class ReplyCampaignEditViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int MinDelaySeconds { get; set; } = 120;
    public int MaxDelaySeconds { get; set; } = 180;
    public string Status { get; set; } = string.Empty;
}

/// <summary>
/// Reply campaign detail page (records + stats + AI analytics).
/// </summary>
public class ReplyCampaignDetailViewModel
{
    public ReplyCampaign Campaign { get; set; } = null!;
    public string CampaignName { get; set; } = string.Empty;
    public List<ReplyRecord> Records { get; set; } = new();
    public CampaignAnalytics? Analytics { get; set; }
    public double SentPercent { get; set; }
    public double PendingPercent { get; set; }
    public double FailedPercent { get; set; }
}