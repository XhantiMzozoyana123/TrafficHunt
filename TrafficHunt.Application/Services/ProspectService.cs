using TrafficHunt.Application.Dtos;
using TrafficHunt.Application.Interfaces;
using TrafficHunt.Domain.Entities;

namespace TrafficHunt.Application.Services;

public class ProspectService : IProspectService
{
    private readonly IProspectRepository _prospects;

    public ProspectService(IProspectRepository prospects)
    {
        _prospects = prospects;
    }

    public Task<List<Prospect>> GetByCampaignAsync(
        int campaignId, string? status = null, int? minIntentScore = null, CancellationToken ct = default) =>
        _prospects.GetByCampaignAsync(campaignId, status, minIntentScore, ct);

    public Task<Prospect?> GetByIdAsync(int id, CancellationToken ct = default) =>
        _prospects.GetByIdAsync(id, ct);

    public async Task<Prospect> UpsertFromQualificationAsync(
        int campaignId,
        string videoId,
        string videoTitle,
        CollectedComment comment,
        QualificationResult qualification,
        CancellationToken ct = default)
    {
        var existing = await _prospects.GetByCommentIdAsync(campaignId, comment.YouTubeCommentId, ct);

        if (existing is not null)
        {
            existing.IsTargetAudience = qualification.IsTargetAudience;
            existing.HasRelevantProblem = qualification.HasRelevantProblem;
            existing.IntentScore = qualification.IntentScore;
            existing.PainPoint = qualification.PainPoint;
            existing.AIReason = qualification.Reason;
            await _prospects.UpdateAsync(existing, ct);
            return existing;
        }

        var prospect = new Prospect
        {
            CampaignId = campaignId,
            CommentId = comment.YouTubeCommentId,
            VideoId = videoId,
            VideoTitle = videoTitle,
            AuthorName = comment.AuthorName,
            YouTubeChannelId = comment.AuthorChannelId,
            YouTubeProfileUrl = string.IsNullOrEmpty(comment.AuthorChannelId)
                ? string.Empty
                : $"https://www.youtube.com/channel/{comment.AuthorChannelId}",
            CommentText = comment.Text,
            IsTargetAudience = qualification.IsTargetAudience,
            HasRelevantProblem = qualification.HasRelevantProblem,
            IntentScore = qualification.IntentScore,
            PainPoint = qualification.PainPoint,
            AIReason = qualification.Reason,
            Status = ProspectStatus.New
        };

        await _prospects.AddAsync(prospect, ct);
        return prospect;
    }

    public async Task<Prospect?> UpdateStatusAsync(int id, string status, CancellationToken ct = default)
    {
        var prospect = await _prospects.GetByIdAsync(id, ct);
        if (prospect is null) return null;

        prospect.Status = status;
        if (status == ProspectStatus.Contacted)
            prospect.LastContactedAt = DateTime.UtcNow;

        await _prospects.UpdateAsync(prospect, ct);
        return prospect;
    }

    public Task<bool> DeleteAsync(int id, CancellationToken ct = default) =>
        _prospects.DeleteAsync(id, ct);

    public Task<GlobalStats> GetGlobalStatsAsync(CancellationToken ct = default) =>
        _prospects.GetGlobalStatsAsync(ct);
}
