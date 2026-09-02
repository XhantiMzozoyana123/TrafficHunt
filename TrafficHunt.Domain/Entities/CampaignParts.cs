namespace TrafficHunt.Domain.Entities;

public class CampaignProblem
{
    public int Id { get; set; }
    public int CampaignId { get; set; }
    public Campaign? Campaign { get; set; }
    public string Text { get; set; } = string.Empty;
}

public class CampaignKeyword
{
    public int Id { get; set; }
    public int CampaignId { get; set; }
    public Campaign? Campaign { get; set; }
    public string Keyword { get; set; } = string.Empty;
    public bool Active { get; set; } = true;
}
