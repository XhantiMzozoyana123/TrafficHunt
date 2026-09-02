namespace TrafficHunt.Domain.Entities;

public class Campaign
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    // ---- Product ----
    public string ProductName { get; set; } = string.Empty;
    public string ProductDescription { get; set; } = string.Empty;
    public string ProductUrl { get; set; } = string.Empty;
    public string ValueProposition { get; set; } = string.Empty;

    // ---- Targeting ----
    public string TargetAudience { get; set; } = string.Empty;
    public string PrimaryProblem { get; set; } = string.Empty;

    public List<CampaignProblem> Problems { get; set; } = new();
    public List<CampaignKeyword> Keywords { get; set; } = new();

    // ---- AI instructions ----
    public string QualificationRules { get; set; } = string.Empty;
    public string OutreachInstructions { get; set; } = string.Empty;
    public string ReplyInstructions { get; set; } = string.Empty;

    // ---- Status ----
    public string Status { get; set; } = "Active";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public List<Prospect> Prospects { get; set; } = new();
}
