using Hangfire;
using Microsoft.Extensions.Logging;
using TrafficHunt.Application.Dtos;
using TrafficHunt.Application.Interfaces;
using TrafficHunt.Domain.Entities;

namespace TrafficHunt.Infrastructure.Jobs;

/// <summary>
/// Comment import job — fetches comments from a YouTube video via the Data API.
/// Runs in the "youtube" queue.
/// </summary>
[Queue("youtube")]
public class CommentImportJob
{
    private readonly IYouTubeApiService _youtubeApi;
    private readonly IProspectRepository _prospects;
    private readonly ILogger<CommentImportJob> _logger;

    public CommentImportJob(
        IYouTubeApiService youtubeApi,
        IProspectRepository prospects,
        ILogger<CommentImportJob> logger)
    {
        _youtubeApi = youtubeApi;
        _prospects = prospects;
        _logger = logger;
    }

    /// <summary>
    /// Fetch comments for a video and enqueue analysis for each new comment.
    /// </summary>
    public async Task RunAsync(int campaignId, string youTubeVideoId, string videoTitle, int commentsPerVideo = 50)
    {
        _logger.LogInformation("Importing comments for video {VideoId}", youTubeVideoId);

        List<CollectedComment> comments;
        try
        {
            comments = await _youtubeApi.GetVideoCommentsAsync(youTubeVideoId, commentsPerVideo);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to import comments for video {VideoId}", youTubeVideoId);
            throw; // Let Hangfire retry
        }

        foreach (var comment in comments)
        {
            // Skip if already qualified
            if (await _prospects.ExistsAsync(campaignId, comment.YouTubeCommentId))
                continue;

            // Enqueue AI analysis for this comment
            BackgroundJob.Enqueue<CommentAnalysisJob>(
                job => job.RunAsync(campaignId, youTubeVideoId, videoTitle, comment));
        }

        _logger.LogInformation("Imported {Count} comments for video {VideoId}", comments.Count, youTubeVideoId);
    }
}
