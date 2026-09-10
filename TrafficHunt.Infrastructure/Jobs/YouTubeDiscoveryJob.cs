using Hangfire;
using Microsoft.Extensions.Logging;
using TrafficHunt.Application.Dtos;
using TrafficHunt.Application.Interfaces;
using TrafficHunt.Domain.Entities;

namespace TrafficHunt.Infrastructure.Jobs;

/// <summary>
/// YouTube discovery FAN-OUT job — searches for videos per campaign keyword.
/// Chunked: one keyword per KeywordDiscoveryJob on the "youtube" queue, so a
/// campaign with 20 keywords becomes 20 short jobs instead of one long job
/// that can time out. Each keyword job fans out to one CommentImportJob per
/// video, which in turn fans out to small CommentAnalysisJob chunks on "ai".
/// Full chain: this job (1) -> keyword jobs (K) -> import jobs (V) -> ai chunks.
/// Runs in the "youtube" queue.
/// </summary>
[Queue("youtube")]
public class YouTubeDiscoveryJob
{
    private readonly ICampaignRepository _campaigns;
    private readonly ILogger<YouTubeDiscoveryJob> _logger;

    public YouTubeDiscoveryJob(
        ICampaignRepository campaigns,
        ILogger<YouTubeDiscoveryJob> logger)
    {
        _campaigns = campaigns;
        _logger = logger;
    }

    /// <summary>
    /// Fan-out: enqueue one short keyword job per active keyword and return
    /// immediately. Never calls YouTube or the LLM itself, so it cannot time out.
    /// </summary>
    public async Task RunAsync(int campaignId, int videosPerKeyword, CancellationToken ct)
    {
        var campaign = await _campaigns.GetByIdAsync(campaignId)
            ?? throw new InvalidOperationException($"Campaign {campaignId} not found.");

        var keywords = campaign.Keywords.Where(k => k.Active).Select(k => k.Keyword).ToList();
        _logger.LogInformation("YouTube discovery fan-out for campaign {CampaignId}: {Count} keyword job(s)",
            campaignId, keywords.Count);

        foreach (var keyword in keywords)
        {
            ct.ThrowIfCancellationRequested();
            BackgroundJob.Enqueue<KeywordDiscoveryJob>(
                job => job.RunAsync(campaignId, keyword, videosPerKeyword, default(CancellationToken)));
        }

        _logger.LogInformation("YouTube discovery fan-out completed for campaign {CampaignId}", campaignId);
    }
}

/// <summary>
/// Single-keyword discovery chunk — searches YouTube for ONE keyword, stores new
/// videos, and enqueues one CommentImportJob per video.
/// Bounded work per job (one search + a few DB writes + enqueues), so it cannot
/// time out no matter how many keywords the campaign has.
/// Runs in the "youtube" queue.
/// </summary>
[Queue("youtube")]
public class KeywordDiscoveryJob
{
    private readonly ICampaignRepository _campaigns;
    private readonly IVideoRepository _videos;
    private readonly IYouTubeSearchService _youtubeSearch;
    private readonly ILogger<KeywordDiscoveryJob> _logger;

    public KeywordDiscoveryJob(
        ICampaignRepository campaigns,
        IVideoRepository videos,
        IYouTubeSearchService youtubeSearch,
        ILogger<KeywordDiscoveryJob> logger)
    {
        _campaigns = campaigns;
        _videos = videos;
        _youtubeSearch = youtubeSearch;
        _logger = logger;
    }

    /// <summary>
    /// Search YouTube for a single keyword. Enqueues a CommentImportJob per video.
    /// Hangfire injects a CancellationToken so the job can be killed from the UI.
    /// </summary>
    public async Task RunAsync(int campaignId, string keyword, int videosPerKeyword, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        _logger.LogInformation("Searching YouTube for keyword: {Keyword} (campaign {CampaignId})", keyword, campaignId);

        List<DiscoveredVideo> videos;
        try
        {
            videos = await _youtubeSearch.SearchVideosAsync(keyword, videosPerKeyword);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to search YouTube for keyword: {Keyword}", keyword);
            return; // Best-effort per keyword; a retry would hit the same failure.
        }

        foreach (var video in videos)
        {
            ct.ThrowIfCancellationRequested();
            // Store video if not already present
            var existing = await _videos.GetByYouTubeIdAsync(campaignId, video.YouTubeVideoId);
            if (existing is null)
            {
                await _videos.AddAsync(new Video
                {
                    CampaignId = campaignId,
                    YouTubeVideoId = video.YouTubeVideoId,
                    Title = video.Title,
                    ChannelTitle = video.ChannelTitle,
                    Url = video.Url
                });
            }

            // Enqueue comment import for this video
            BackgroundJob.Enqueue<CommentImportJob>(
                job => job.RunAsync(campaignId, video.YouTubeVideoId, video.Title, 50, default(CancellationToken), CommentAnalysisJob.DefaultBatchSize));
        }

        _logger.LogInformation("Keyword discovery done: {Keyword} -> {Count} video(s)", keyword, videos.Count);
    }
}
