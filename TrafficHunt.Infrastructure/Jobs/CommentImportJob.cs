using Hangfire;
using Microsoft.Extensions.Logging;
using TrafficHunt.Application.Dtos;
using TrafficHunt.Application.Interfaces;
using TrafficHunt.Domain.Entities;

namespace TrafficHunt.Infrastructure.Jobs;

/// <summary>
/// Comment import job — fetches comments from a YouTube video via the Data API,
/// then enqueues them for AI qualification in SMALL CHUNKS (default 5 per job)
/// on the "ai" queue instead of one job per comment.
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
    /// Fetch comments for a video and enqueue AI analysis in small chunks.
    /// Chunking bounds each downstream job to a few LLM calls so no single job
    /// can time out, and Hangfire retries stay cheap (re-runs one chunk, not all).
    /// </summary>
    public async Task RunAsync(int campaignId, string youTubeVideoId, string videoTitle, int commentsPerVideo, CancellationToken ct, int batchSize = CommentAnalysisJob.DefaultBatchSize)
    {
        ct.ThrowIfCancellationRequested();
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

        // Pre-filter already-qualified comments so chunks contain only real work.
        var fresh = new List<CollectedComment>(comments.Count);
        foreach (var comment in comments)
        {
            ct.ThrowIfCancellationRequested();
            if (await _prospects.ExistsAsync(campaignId, comment.YouTubeCommentId))
                continue;
            fresh.Add(comment);
        }

        // Enqueue one ai-queue job per chunk (strictly serial: ai queue = 1 worker).
        var size = batchSize <= 0 ? CommentAnalysisJob.DefaultBatchSize : batchSize;
        for (var i = 0; i < fresh.Count; i += size)
        {
            ct.ThrowIfCancellationRequested();
            var chunk = fresh.GetRange(i, Math.Min(size, fresh.Count - i));
            BackgroundJob.Enqueue<CommentAnalysisJob>(
                job => job.RunBatchAsync(campaignId, youTubeVideoId, videoTitle, chunk, default(CancellationToken)));
        }

        _logger.LogInformation("Imported {Total} comments for video {VideoId} -> {Chunks} analysis chunk(s) ({Fresh} new)",
            comments.Count, youTubeVideoId, (fresh.Count + size - 1) / size, fresh.Count);
    }
}
