using TrafficHunt.Application.Dtos;
using TrafficHunt.Domain.Entities;

namespace TrafficHunt.Application.Interfaces;

public interface ICampaignService
{
    Task<List<Campaign>> GetAllAsync(CancellationToken ct = default);
    Task<Campaign?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<Campaign> CreateAsync(Campaign campaign, CancellationToken ct = default);
    Task<Campaign?> UpdateAsync(Campaign campaign, CancellationToken ct = default);
    Task<bool> DeleteAsync(int id, CancellationToken ct = default);
    Task<CampaignKeyword> AddKeywordAsync(int campaignId, string keyword, CancellationToken ct = default);
    Task<bool> RemoveKeywordAsync(int keywordId, CancellationToken ct = default);
    Task<CampaignStats> GetStatsAsync(int campaignId, CancellationToken ct = default);
}
