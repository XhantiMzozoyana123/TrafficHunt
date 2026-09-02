using TrafficHunt.Application.Interfaces;
using TrafficHunt.Application.Dtos;
using TrafficHunt.Domain.Entities;

namespace TrafficHunt.Application.Services;

public class CampaignService : ICampaignService
{
    private const int HighIntentThreshold = 80;

    private readonly ICampaignRepository _campaigns;
    private readonly IProspectRepository _prospects;

    public CampaignService(ICampaignRepository campaigns, IProspectRepository prospects)
    {
        _campaigns = campaigns;
        _prospects = prospects;
    }

    public Task<List<Campaign>> GetAllAsync(CancellationToken ct = default) =>
        _campaigns.GetAllAsync(ct);

    public Task<Campaign?> GetByIdAsync(int id, CancellationToken ct = default) =>
        _campaigns.GetByIdAsync(id, ct);

    public async Task<Campaign> CreateAsync(Campaign campaign, CancellationToken ct = default)
    {
        campaign.CreatedAt = DateTime.UtcNow;
        campaign.UpdatedAt = DateTime.UtcNow;
        return await _campaigns.AddAsync(campaign, ct);
    }

    public async Task<Campaign?> UpdateAsync(Campaign campaign, CancellationToken ct = default)
    {
        campaign.UpdatedAt = DateTime.UtcNow;
        return await _campaigns.UpdateAsync(campaign, ct);
    }

    public Task<bool> DeleteAsync(int id, CancellationToken ct = default) =>
        _campaigns.DeleteAsync(id, ct);

    public Task<CampaignKeyword> AddKeywordAsync(int campaignId, string keyword, CancellationToken ct = default) =>
        _campaigns.AddKeywordAsync(campaignId, keyword.Trim(), ct);

    public Task<bool> RemoveKeywordAsync(int keywordId, CancellationToken ct = default) =>
        _campaigns.RemoveKeywordAsync(keywordId, ct);

    public Task<CampaignStats> GetStatsAsync(int campaignId, CancellationToken ct = default) =>
        _prospects.GetCampaignStatsAsync(campaignId, ct);
}
