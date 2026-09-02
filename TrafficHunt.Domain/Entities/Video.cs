namespace TrafficHunt.Domain.Entities;

public class Video
{
    public int Id { get; set; }

    public int CampaignId { get; set; }
    public Campaign? Campaign { get; set; }

    public string YouTubeVideoId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string ChannelTitle { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;

    public DateTime DiscoveredAt { get; set; } = DateTime.UtcNow;

    public List<Comment> Comments { get; set; } = new();
}

public class Comment
{
    public int Id { get; set; }
    public int VideoId { get; set; }
    public Video? Video { get; set; }

    public string YouTubeCommentId { get; set; } = string.Empty;
    public string AuthorName { get; set; } = string.Empty;
    public string AuthorChannelId { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public int LikeCount { get; set; }
    public DateTime PublishedAt { get; set; }

    public DateTime CollectedAt { get; set; } = DateTime.UtcNow;
}
