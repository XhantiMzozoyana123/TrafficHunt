namespace TrafficHunt.Application.Dtos;

public class CreateReplyCampaignRequest
{
    public int CampaignId { get; set; }
    public string Name { get; set; } = string.Empty;
    public List<string> TemplateBodies { get; set; } = new();
    public int MinDelaySeconds { get; set; } = 120;
    public int MaxDelaySeconds { get; set; } = 180;
    public int? MinIntentScore { get; set; } = 80;
}

public class ReplyCampaignDto
{
    public int Id { get; set; }
    public int CampaignId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int MinDelaySeconds { get; set; }
    public int MaxDelaySeconds { get; set; }
    public int RepliesSent { get; set; }
    public int RepliesFailed { get; set; }
    public int RepliesPending { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public List<ReplyTemplateDto> Templates { get; set; } = new();
}

public class ReplyTemplateDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public int Order { get; set; }
    public int TimesUsed { get; set; }
}

public class ReplyRecordDto
{
    public int Id { get; set; }
    public string AuthorName { get; set; } = string.Empty;
    public string CommentText { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? MessageSent { get; set; }
    public DateTime? SentAt { get; set; }
    public string? ErrorMessage { get; set; }
}

public class ReplyCampaignStats
{
    public int TotalProspects { get; set; }
    public int RepliesSent { get; set; }
    public int RepliesFailed { get; set; }
    public int RepliesPending { get; set; }
    public double SuccessRate { get; set; }
    public TimeSpan? AverageDelay { get; set; }
}

public class CampaignAnalytics
{
    public string Summary { get; set; } = string.Empty;
    public List<string> Insights { get; set; } = new();
    public List<string> Recommendations { get; set; } = new();
    public Dictionary<string, int> TemplatePerformance { get; set; } = new();
    public List<HourlyActivity> ActivityByHour { get; set; } = new();
}

public class HourlyActivity
{
    public int Hour { get; set; }
    public int RepliesSent { get; set; }
}
