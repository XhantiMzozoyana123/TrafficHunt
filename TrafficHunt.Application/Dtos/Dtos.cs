namespace TrafficHunt.Application.Dtos;

/// <summary>
/// A video found by a discovery keyword search.
/// </summary>
public class DiscoveredVideo
{
    public string YouTubeVideoId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string ChannelTitle { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
}

/// <summary>
/// A comment collected from a discovered video.
/// </summary>
public class CollectedComment
{
    public string YouTubeCommentId { get; set; } = string.Empty;
    public string AuthorName { get; set; } = string.Empty;
    public string AuthorChannelId { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public int LikeCount { get; set; }
    public DateTime PublishedAt { get; set; }
}

/// <summary>
/// AI qualification result for a single comment against a campaign.
/// </summary>
public class QualificationResult
{
    public bool IsTargetAudience { get; set; }
    public bool HasRelevantProblem { get; set; }
    public int IntentScore { get; set; }
    public string PainPoint { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
}

/// <summary>
/// Streaming progress of a discovery run.
/// </summary>
public class DiscoveryProgress
{
    public int CampaignId { get; set; }
    public string CurrentKeyword { get; set; } = string.Empty;
    public int KeywordsProcessed { get; set; }
    public int TotalKeywords { get; set; }
    public int VideosScanned { get; set; }
    public int CommentsAnalyzed { get; set; }
    public int ProspectsFound { get; set; }
    public bool IsComplete { get; set; }
}
