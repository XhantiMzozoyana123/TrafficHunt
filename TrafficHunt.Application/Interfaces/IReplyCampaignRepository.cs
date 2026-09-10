using TrafficHunt.Domain.Entities;

namespace TrafficHunt.Application.Interfaces;

public interface IReplyCampaignRepository
{
    Task<List<ReplyCampaign>> GetAllAsync(CancellationToken ct = default);
    Task<ReplyCampaign?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<List<ReplyCampaign>> GetByCampaignAsync(int campaignId, CancellationToken ct = default);
    Task<ReplyCampaign> AddAsync(ReplyCampaign campaign, CancellationToken ct = default);
    Task UpdateAsync(ReplyCampaign campaign, CancellationToken ct = default);
    Task<ReplyTemplate> AddTemplateAsync(ReplyTemplate template, CancellationToken ct = default);
    Task<ReplyRecord> AddRecordAsync(ReplyRecord record, CancellationToken ct = default);
    Task<ReplyCampaign?> GetByRecordIdAsync(int replyRecordId, CancellationToken ct = default);

    /// <summary>Deletes a reply campaign and ALL its templates + reply records.
    /// Returns false when the campaign does not exist.</summary>
    Task<bool> DeleteAsync(int id, CancellationToken ct = default);
}
