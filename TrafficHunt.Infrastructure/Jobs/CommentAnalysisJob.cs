using Hangfire;
using Microsoft.Extensions.Logging;
using TrafficHunt.Application.Dtos;
using TrafficHunt.Application.Interfaces;
using TrafficHunt.Domain.Entities;

namespace TrafficHunt.Infrastructure.Jobs;

/// <summary>
/// Comment analysis job — sends a comment to Ollama for qualification.
/// Runs in the "ai" queue (limited workers to avoid overwhelming Ollama).
/// </summary>
[Queue("ai")]
public class CommentAnalysisJob
{
    private readonly ICampaignRepository _campaigns;
    private readonly IProspectRepository _prospects;
    private readonly IOllamaService _ollama;
    private readonly ILogger<CommentAnalysisJob> _logger;

    public CommentAnalysisJob(
        ICampaignRepository campaigns,
        IProspectRepository prospects,
        IOllamaService ollama,
        ILogger<CommentAnalysisJob> logger)
    {
        _campaigns = campaigns;
        _prospects = prospects;
        _ollama = ollama;
        _logger = logger;
    }

    /// <summary>
    /// Analyze a comment against the campaign context and store as a prospect if qualified.
    /// </summary>
    public async Task RunAsync(int campaignId, string youTubeVideoId, string videoTitle, CollectedComment comment)
    {
        // Skip if already exists (double-check after dequeue)
        if (await _prospects.ExistsAsync(campaignId, comment.YouTubeCommentId))
            return;

        var campaign = await _campaigns.GetByIdAsync(campaignId)
            ?? throw new InvalidOperationException($"Campaign {campaignId} not found.");

        var campaignContext = BuildCampaignContext(campaign);

        _logger.LogInformation("Analyzing comment {CommentId} for campaign {CampaignId}", comment.YouTubeCommentId, campaignId);

        QualificationResult qualification;
        try
        {
            qualification = await _ollama.QualifyCommentAsync(campaignContext, videoTitle, comment.Text);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to analyze comment {CommentId}", comment.YouTubeCommentId);
            throw; // Let Hangfire retry
        }

        // Store the prospect
        await _prospects.AddAsync(new Prospect
        {
            CampaignId = campaignId,
            CommentId = comment.YouTubeCommentId,
            VideoId = youTubeVideoId,
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
        });

        _logger.LogInformation("Stored prospect from comment {CommentId} with intent score {Score}", comment.YouTubeCommentId, qualification.IntentScore);
    }

    private static string BuildCampaignContext(Campaign campaign)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"Product: {campaign.ProductName} - {campaign.ProductUrl}");
        sb.AppendLine($"Description: {campaign.ProductDescription}");
        sb.AppendLine($"Value proposition: {campaign.ValueProposition}");
        sb.AppendLine($"Target audience: {campaign.TargetAudience}");
        sb.AppendLine($"Primary problem solved: {campaign.PrimaryProblem}");

        if (campaign.Problems.Count > 0)
            sb.AppendLine($"Secondary problems solved: {string.Join("; ", campaign.Problems.Select(p => p.Text))}");

        if (!string.IsNullOrWhiteSpace(campaign.QualificationRules))
            sb.AppendLine($"Qualification rules: {campaign.QualificationRules}");

        return sb.ToString();
    }
}
