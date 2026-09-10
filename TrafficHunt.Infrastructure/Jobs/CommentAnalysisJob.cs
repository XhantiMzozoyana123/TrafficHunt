using Hangfire;
using Microsoft.Extensions.Logging;
using TrafficHunt.Application.Dtos;
using TrafficHunt.Application.Interfaces;
using TrafficHunt.Domain.Entities;

namespace TrafficHunt.Infrastructure.Jobs;

/// <summary>
/// Comment analysis chunk job — qualifies a SMALL BATCH of comments with the LLM
/// (default 5 per job) instead of one job per comment.
/// Why: the remote Ollama host cold-loads qwen3.5 (60s+ first hit) and OOM-kills
/// under concurrency — 1-comment-per-job floods the ai queue with hundreds of
/// jobs that time out, retry, and pile up. Small sequential chunks keep each job
/// short, serial (ai queue = 1 worker), idempotent, and independently retryable.
/// Each job only ever makes a bounded number of LLM calls, so no single HTTP
/// request or Hangfire job can time out on a large comment backlog.
/// </summary>
[Queue("ai")]
[AutomaticRetry(Attempts = 3, DelaysInSeconds = new[] { 60, 300, 900 }, OnAttemptsExceeded = AttemptsExceededAction.Delete)]
public class CommentAnalysisJob
{
    /// <summary>Max LLM calls per job execution — keeps every chunk short.</summary>
    public const int DefaultBatchSize = 5;
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
    /// Analyze ONE comment (legacy single-comment entry point — kept so already
    /// enqueued jobs from before the chunking change still execute). Delegates to
    /// the batch overload with a single item.
    /// </summary>
    public Task RunAsync(int campaignId, string youTubeVideoId, string videoTitle, CollectedComment comment, CancellationToken ct) =>
        RunBatchAsync(campaignId, youTubeVideoId, videoTitle, new List<CollectedComment> { comment }, ct);

    /// <summary>
    /// Analyze a small batch of comments against the campaign context, storing
    /// each as a prospect. Skips already-qualified comments (idempotent, safe to
    /// retry). One LLM call per comment, strictly sequential.
    /// </summary>
    public async Task RunBatchAsync(
        int campaignId, string youTubeVideoId, string videoTitle, List<CollectedComment> comments, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        if (comments.Count == 0) return;

        var campaign = await _campaigns.GetByIdAsync(campaignId)
            ?? throw new InvalidOperationException($"Campaign {campaignId} not found.");

        var campaignContext = BuildCampaignContext(campaign);

        var done = 0;
        foreach (var comment in comments)
        {
            ct.ThrowIfCancellationRequested();
            // Skip if already qualified (double-check after dequeue — idempotent retry).
            if (await _prospects.ExistsAsync(campaignId, comment.YouTubeCommentId))
                continue;

            _logger.LogInformation("Analyzing comment {CommentId} for campaign {CampaignId} ({Done}/{Total})",
                comment.YouTubeCommentId, campaignId, done + 1, comments.Count);

            QualificationResult qualification;
            try
            {
                qualification = await _ollama.QualifyCommentAsync(campaignContext, videoTitle, comment.Text);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to analyze comment {CommentId}", comment.YouTubeCommentId);
                throw; // Let Hangfire retry with backoff (AutomaticRetry above)
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
            done++;
        }
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
