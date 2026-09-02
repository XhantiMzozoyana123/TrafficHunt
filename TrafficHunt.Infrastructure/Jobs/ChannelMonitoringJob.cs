using Hangfire;
using Microsoft.Extensions.Logging;
using TrafficHunt.Application.Dtos;
using TrafficHunt.Application.Interfaces;
using TrafficHunt.Domain.Entities;

namespace TrafficHunt.Infrastructure.Jobs;

/// <summary>
/// Channel monitoring job — checks a YouTube channel for new videos and
/// triggers discovery. Designed to run as a recurring job.
/// </summary>
[Queue("youtube")]
public class ChannelMonitoringJob
{
    private readonly IYouTubeSearchService _youtubeSearch;
    private readonly IVideoRepository _videos;
    private readonly ILogger<ChannelMonitoringJob> _logger;

    public ChannelMonitoringJob(
        IYouTubeSearchService youtubeSearch,
        IVideoRepository videos,
        ILogger<ChannelMonitoringJob> logger)
    {
        _youtubeSearch = youtubeSearch;
        _videos = videos;
        _logger = logger;
    }

    /// <summary>
    /// Check a channel for new videos since the last check.
    /// </summary>
    public async Task RunAsync(int campaignId, string channelId, int maxVideos = 10)
    {
        _logger.LogInformation("Monitoring channel {ChannelId} for campaign {CampaignId}", channelId, campaignId);

        List<DiscoveredVideo> videos;
        try
        {
            videos = await _youtubeSearch.SearchChannelVideosAsync(channelId, maxVideos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to monitor channel {ChannelId}", channelId);
            throw;
        }

        foreach (var video in videos)
        {
            // Only process new videos
            var existing = await _videos.GetByYouTubeIdAsync(campaignId, video.YouTubeVideoId);
            if (existing is not null) continue;

            await _videos.AddAsync(new Video
            {
                CampaignId = campaignId,
                YouTubeVideoId = video.YouTubeVideoId,
                Title = video.Title,
                ChannelTitle = video.ChannelTitle,
                Url = video.Url
            });

            // Enqueue comment import
            BackgroundJob.Enqueue<CommentImportJob>(
                job => job.RunAsync(campaignId, video.YouTubeVideoId, video.Title, 50));
        }

        _logger.LogInformation("Channel monitoring found {Count} new videos for channel {ChannelId}", videos.Count, channelId);
    }
}
