using System.Text;
using TrafficHunt.Application.Dtos;
using TrafficHunt.Application.Interfaces;
using TrafficHunt.Domain.Entities;

namespace TrafficHunt.Application.Services;

/// <summary>
/// The discovery orchestrator. Neither the frontend nor the AI (MCP) needs to
/// understand how discovery works - they simply say "find prospects".
/// </summary>
public class DiscoveryService : IDiscoveryService
{
    private readonly ICampaignRepository _campaigns;
    private readonly IVideoRepository _videos;
    private readonly IProspectRepository _prospects;
    private readonly IYouTubeSearchService _youtubeSearch;
    private readonly IYouTubeApiService _youtubeApi;
    private readonly IOllamaService _ollama;

    public DiscoveryService(
        ICampaignRepository campaigns,
        IVideoRepository videos,
        IProspectRepository prospects,
        IYouTubeSearchService youtubeSearch,
        IYouTubeApiService youtubeApi,
        IOllamaService ollama)
    {
        _campaigns = campaigns;
        _videos = videos;
        _prospects = prospects;
        _youtubeSearch = youtubeSearch;
        _youtubeApi = youtubeApi;
        _ollama = ollama;
    }

    public async IAsyncEnumerable<DiscoveryProgress> RunDiscoveryAsync(
        int campaignId,
        int videosPerKeyword = 3,
        int commentsPerVideo = 50,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
    {
        var campaign = await _campaigns.GetByIdAsync(campaignId, ct)
            ?? throw new InvalidOperationException($"Campaign {campaignId} not found.");

        var keywords = campaign.Keywords.Where(k => k.Active).Select(k => k.Keyword).ToList();
        var progress = new DiscoveryProgress
        {
            CampaignId = campaignId,
            TotalKeywords = keywords.Count
        };

        var campaignContext = BuildCampaignContext(campaign);

        foreach (var keyword in keywords)
        {
            progress.CurrentKeyword = keyword;
            progress.IsComplete = false;
            yield return progress;

            List<DiscoveredVideo> videos;
            try
            {
                videos = await _youtubeSearch.SearchVideosAsync(keyword, videosPerKeyword, ct);
            }
            catch (Exception ex)
            {
                // Discovery is best-effort per keyword; log and continue.
                continue;
            }

            foreach (var video in videos)
            {
                var videoEntity = await EnsureVideoAsync(campaignId, video, ct);

                List<CollectedComment> comments;
                try
                {
                    comments = await _youtubeApi.GetVideoCommentsAsync(video.YouTubeVideoId, commentsPerVideo, ct);
                }
                catch (Exception)
                {
                    continue;
                }

                foreach (var comment in comments)
                {
                    // Skip comments already qualified for this campaign.
                    if (await _prospects.ExistsAsync(campaignId, comment.YouTubeCommentId, ct))
                        continue;

                    try
                    {
                        var qualification = await _ollama.QualifyCommentAsync(
                            campaignContext, video.Title, comment.Text, ct);

                        progress.CommentsAnalyzed++;

                        // Only strong matches become prospects.
                        if (qualification.IsTargetAudience && qualification.HasRelevantProblem)
                        {
                            await UpsertProspectAsync(campaignId, video, comment, qualification, ct);
                            progress.ProspectsFound++;
                        }
                    }
                    catch (Exception)
                    {
                        // A single failed qualification must not stop the run.
                    }

                    yield return progress;
                }

                progress.VideosScanned++;
                yield return progress;
            }

            progress.KeywordsProcessed++;
            yield return progress;
        }

        progress.IsComplete = true;
        yield return progress;
    }

    private async Task UpsertProspectAsync(
        int campaignId,
        DiscoveredVideo video,
        CollectedComment comment,
        QualificationResult qualification,
        CancellationToken ct)
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
            return;
        }

        await _prospects.AddAsync(new Prospect
        {
            CampaignId = campaignId,
            CommentId = comment.YouTubeCommentId,
            VideoId = video.YouTubeVideoId,
            VideoTitle = video.Title,
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
        }, ct);
    }

    private async Task<Video> EnsureVideoAsync(int campaignId, DiscoveredVideo video, CancellationToken ct)
    {
        var existing = await _videos.GetByYouTubeIdAsync(campaignId, video.YouTubeVideoId, ct);
        if (existing is not null) return existing;

        var entity = new Video
        {
            CampaignId = campaignId,
            YouTubeVideoId = video.YouTubeVideoId,
            Title = video.Title,
            ChannelTitle = video.ChannelTitle,
            Url = video.Url
        };
        await _videos.AddAsync(entity, ct);
        return entity;
    }

    private static string BuildCampaignContext(Campaign campaign)
    {
        var sb = new StringBuilder();
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

