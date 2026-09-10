namespace TrafficHunt.Domain.Entities;

public class ReplyCampaign
{
    public int Id { get; set; }
    public int CampaignId { get; set; }
    public Campaign Campaign { get; set; } = null!;

    public string Name { get; set; } = string.Empty;
    public ReplyCampaignStatus Status { get; set; } = ReplyCampaignStatus.Draft;

    // Delay configuration (in seconds)
    public int MinDelaySeconds { get; set; } = 120;  // 2 minutes
    public int MaxDelaySeconds { get; set; } = 180;  // 3 minutes

    // Tracking
    public int RepliesSent { get; set; }
    public int RepliesFailed { get; set; }
    public int RepliesPending { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }

    // Navigation
    public List<ReplyTemplate> Templates { get; set; } = new();
    public List<ReplyRecord> ReplyRecords { get; set; } = new();
}

public class ReplyTemplate
{
    public int Id { get; set; }
    public int ReplyCampaignId { get; set; }
    public ReplyCampaign ReplyCampaign { get; set; } = null!;

    public string Name { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public int Order { get; set; }
    public int TimesUsed { get; set; }
}

public class ReplyRecord
{
    public int Id { get; set; }
    public int ReplyCampaignId { get; set; }
    public ReplyCampaign ReplyCampaign { get; set; } = null!;

    public int ProspectId { get; set; }
    public Prospect Prospect { get; set; } = null!;

    public ReplyStatus Status { get; set; } = ReplyStatus.Pending;
    public int? TemplateId { get; set; }
    public ReplyTemplate? Template { get; set; }

    public string? MessageSent { get; set; }
    public string? ErrorMessage { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ScheduledAt { get; set; }
    public DateTime? SentAt { get; set; }
}

public enum ReplyCampaignStatus
{
    Draft,
    Running,
    Paused,
    Completed,
    Cancelled
}

public enum ReplyStatus
{
    Pending,
    Sent,
    Failed,
    Skipped,
    Approved
}
