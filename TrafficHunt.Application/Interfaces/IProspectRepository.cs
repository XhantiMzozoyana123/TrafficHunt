using TrafficHunt.Domain.Entities;

namespace TrafficHunt.Application.Interfaces;

public interface IProspectRepository
{
    Task<List<Prospect>> GetByCampaignAsync(int campaignId, string? status = null, int? minIntentScore = null, CancellationToken ct = default);
    Task<Prospect?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<Prospect?> GetByCommentIdAsync(int campaignId, string commentId, CancellationToken ct = default);
    Task<bool> ExistsAsync(int campaignId, string commentId, CancellationToken ct = default);
    Task AddAsync(Prospect prospect, CancellationToken ct = default);
    Task UpdateAsync(Prospect prospect, CancellationToken ct = default);
    Task<bool> DeleteAsync(int id, CancellationToken ct = default);

    Task<GlobalStats> GetGlobalStatsAsync(CancellationToken ct = default);
    Task<CampaignStats> GetCampaignStatsAsync(int campaignId, CancellationToken ct = default);
}

public class GlobalStats
{
    public int TotalProspects { get; set; }
    public int HighIntent { get; set; }
    public int Contacted { get; set; }
    public int Interested { get; set; }
    public int Converted { get; set; }
}

public class CampaignStats
{
    public int TotalProspects { get; set; }
    public int HighIntent { get; set; }
    public int Contacted { get; set; }
    public int Interested { get; set; }
    public int Converted { get; set; }
}
