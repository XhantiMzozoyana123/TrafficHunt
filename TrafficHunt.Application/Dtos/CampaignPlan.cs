namespace TrafficHunt.Application.Dtos;

/// <summary>
/// Raw campaign skeleton produced by the AI from a plain-English description.
/// </summary>
public class CampaignDraft
{
    public string Name { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string ProductUrl { get; set; } = string.Empty;
    public string ProductDescription { get; set; } = string.Empty;
    public string ValueProposition { get; set; } = string.Empty;
    public string TargetAudience { get; set; } = string.Empty;
    public string PrimaryProblem { get; set; } = string.Empty;
    public List<string> Problems { get; set; } = new();
    public List<string> Keywords { get; set; } = new();
}

/// <summary>
/// Result of planning a campaign from text.
/// </summary>
public class CampaignPlan
{
    public int CampaignId { get; set; } = -1;
    public CampaignDraft Draft { get; set; } = new();
    public string RawPlanJson { get; set; } = string.Empty;
}
