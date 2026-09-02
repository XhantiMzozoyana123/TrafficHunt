using Hangfire;
using Hangfire.Storage;
using Microsoft.Extensions.Logging;
using TrafficHunt.Application.Dtos;
using TrafficHunt.Application.Interfaces;
using TrafficHunt.Domain.Entities;

namespace TrafficHunt.Infrastructure.Jobs;

/// <summary>
/// YouTube discovery job — searches for videos per campaign keyword.
/// Runs in the "youtube" queue.
/// </summary>
[Queue("youtube")]
public class YouTubeDiscoveryJob
{
    private readonly ICampaignRepository _campaigns;
    private readonly IVideoRepository _videos;
    private readonly IYouTubeSearchService _youtubeSearch;
    private readonly ILogger<YouTubeDiscoveryJob> _logger;

    public YouTubeDiscoveryJob(
        ICampaignRepository campaigns,
        IVideoRepository videos,
        IYouTubeSearchService youtubeSearch,
        ILogger<YouTubeDiscoveryJob> logger)
    {
        _campaigns = campaigns;
        _videos = videos;
        _youtubeSearch = youtubeSearch;
        _logger = logger;
    }

    /// <summary>
    /// Search YouTube for videos matching the campaign's keywords.
    /// Enqueues a CommentImportJob for each video found.
    /// </summary>
    public async Task RunAsync(int campaignId, int videosPerKeyword = 3)
    {
        var campaign = await _campaigns.GetByIdAsync(campaignId)
            ?? throw new InvalidOperationException($"Campaign {campaignId} not found.");

        var keywords = campaign.Keywords.Where(k => k.Active).Select(k => k.Keyword).ToList();
        _logger.LogInformation("YouTube discovery started for campaign {CampaignId} with {Count} keywords", campaignId, keywords.Count);

        foreach (var keyword in keywords)
        {
            _logger.LogInformation("Searching YouTube for keyword: {Keyword}", keyword);

            List<DiscoveredVideo> videos;
            try
            {
                videos = await _youtubeSearch.SearchVideosAsync(keyword, videosPerKeyword);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to search YouTube for keyword: {Keyword}", keyword);
                continue;
            }

            foreach (var video in videos)
            {
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
                    job => job.RunAsync(campaignId, video.YouTubeVideoId, video.Title, 50));
            }
        }

        _logger.LogInformation("YouTube discovery completed for campaign {CampaignId}", campaignId);
    }
}
