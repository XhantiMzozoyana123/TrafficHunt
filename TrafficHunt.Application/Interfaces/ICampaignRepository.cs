using TrafficHunt.Domain.Entities;

namespace TrafficHunt.Application.Interfaces;

public interface ICampaignRepository
{
    Task<List<Campaign>> GetAllAsync(CancellationToken ct = default);
    Task<Campaign?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<Campaign> AddAsync(Campaign campaign, CancellationToken ct = default);
    Task<Campaign?> UpdateAsync(Campaign campaign, CancellationToken ct = default);
    Task<bool> DeleteAsync(int id, CancellationToken ct = default);
            Task<CampaignKeyword> AddKeywordAsync(int campaignId, string keyword, CancellationToken ct = default);
    Task<bool> RemoveKeywordAsync(int keywordId, CancellationToken ct = default);
    Task<CampaignProblem> AddProblemAsync(int campaignId, string problem, CancellationToken ct = default);
}
