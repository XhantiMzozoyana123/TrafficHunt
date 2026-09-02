namespace TrafficHunt.Domain.Entities;

public static class ProspectStatus
{
    public const string New = "New";
    public const string Qualified = "Qualified";
    public const string Contacted = "Contacted";
    public const string Interested = "Interested";
    public const string Converted = "Converted";
    public const string Rejected = "Rejected";
}

public class Prospect
{
    public int Id { get; set; }
    public int CampaignId { get; set; }
    public Campaign? Campaign { get; set; }

    public string CommentId { get; set; } = string.Empty;
    public string VideoId { get; set; } = string.Empty;
    public string VideoTitle { get; set; } = string.Empty;

    public string AuthorName { get; set; } = string.Empty;
    public string YouTubeChannelId { get; set; } = string.Empty;
    public string YouTubeProfileUrl { get; set; } = string.Empty;

    public string CommentText { get; set; } = string.Empty;

    // ---- AI qualification ----
    public bool IsTargetAudience { get; set; }
    public bool HasRelevantProblem { get; set; }
    public int IntentScore { get; set; }
    public string PainPoint { get; set; } = string.Empty;
    public string AIReason { get; set; } = string.Empty;

    public string Status { get; set; } = ProspectStatus.New;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastContactedAt { get; set; }
}
