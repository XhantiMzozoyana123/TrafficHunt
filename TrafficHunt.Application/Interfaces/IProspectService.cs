using TrafficHunt.Application.Dtos;
using TrafficHunt.Domain.Entities;

namespace TrafficHunt.Application.Interfaces;

public interface IProspectService
{
    Task<List<Prospect>> GetByCampaignAsync(int campaignId, string? status = null, int? minIntentScore = null, CancellationToken ct = default);
    Task<Prospect?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<Prospect> UpsertFromQualificationAsync(
        int campaignId,
        string videoId,
        string videoTitle,
        CollectedComment comment,
        QualificationResult qualification,
        CancellationToken ct = default);
    Task<Prospect?> UpdateStatusAsync(int id, string status, CancellationToken ct = default);
    Task<bool> DeleteAsync(int id, CancellationToken ct = default);
    Task<GlobalStats> GetGlobalStatsAsync(CancellationToken ct = default);
}
