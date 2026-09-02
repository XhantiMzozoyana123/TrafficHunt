using Hangfire;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using TrafficHunt.Application.Dtos;
using TrafficHunt.Application.Interfaces;
using TrafficHunt.Infrastructure.Jobs;

namespace TrafficHunt.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DiscoveryController : ControllerBase
{
    private readonly IDiscoveryService _discovery;
    private readonly ILogger<DiscoveryController> _logger;

    public DiscoveryController(IDiscoveryService discovery, ILogger<DiscoveryController> logger)
    {
        _discovery = discovery;
        _logger = logger;
    }

    /// <summary>
    /// Runs the full discovery pipeline and streams progress to the caller (SSE).
    /// Good for small campaigns / real-time feedback.
    /// </summary>
    [HttpPost("{campaignId:int}/run")]
    public async Task Run(int campaignId, [FromBody] DiscoveryRequest? request, CancellationToken ct)
    {
        Response.ContentType = "text/event-stream";

        try
        {
            await foreach (var progress in _discovery.RunDiscoveryAsync(
                campaignId,
                request?.VideosPerKeyword ?? 3,
                request?.CommentsPerVideo ?? 50,
                ct))
            {
                var json = JsonSerializer.Serialize(progress);
                await Response.WriteAsync($"data: {json}\n\n", ct);
                await Response.Body.FlushAsync(ct);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Discovery for campaign {CampaignId} cancelled", campaignId);
        }
    }

    /// <summary>
    /// Enqueues discovery as a Hangfire background job.
    /// Returns immediately with a job ID for tracking.
    /// Use this for large campaigns or when you want fire-and-forget processing.
    /// </summary>
    [HttpPost("{campaignId:int}/run-background")]
    public IActionResult RunBackground(int campaignId, [FromQuery] int videosPerKeyword = 3)
    {
        var jobId = BackgroundJob.Enqueue<YouTubeDiscoveryJob>(
            job => job.RunAsync(campaignId, videosPerKeyword));

        return Ok(new { jobId, status = "queued", queue = "youtube", campaignId });
    }

    /// <summary>
    /// Schedule recurring discovery for a campaign.
    /// Useful for monitoring a niche over time.
    /// </summary>
    [HttpPost("{campaignId:int}/schedule")]
    public IActionResult ScheduleRecurring(int campaignId, [FromQuery] string cron = "0 */6 * * *")
    {
        var jobId = $"discovery-{campaignId}";
        RecurringJob.AddOrUpdate<YouTubeDiscoveryJob>(
            jobId,
            job => job.RunAsync(campaignId),
            cron,
            new RecurringJobOptions { QueueName = "youtube" });

        return Ok(new { jobId, status = "scheduled", cron, campaignId });
    }

    public record DiscoveryRequest(int? VideosPerKeyword, int? CommentsPerVideo);
}
