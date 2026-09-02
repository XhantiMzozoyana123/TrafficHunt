using Hangfire;
using Microsoft.AspNetCore.Mvc;
using TrafficHunt.Infrastructure.Jobs;

namespace TrafficHunt.Web.Controllers;

/// <summary>
/// Manages Hangfire background jobs for the discovery pipeline.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class JobsController : ControllerBase
{
    /// <summary>
    /// Enqueue a YouTube discovery job for a campaign.
    /// </summary>
    [HttpPost("discover/{campaignId}")]
    public IActionResult StartDiscovery(int campaignId, [FromQuery] int videosPerKeyword = 3)
    {
        var jobId = BackgroundJob.Enqueue<YouTubeDiscoveryJob>(
            job => job.RunAsync(campaignId, videosPerKeyword));

        return Ok(new { jobId, status = "queued", queue = "youtube" });
    }

    /// <summary>
    /// Schedule a recurring channel monitoring job.
    /// </summary>
    [HttpPost("monitor-channel")]
    public IActionResult MonitorChannel([FromQuery] int campaignId, [FromQuery] string channelId)
    {
        var jobId = $"monitor-{campaignId}-{channelId}";
        RecurringJob.AddOrUpdate<ChannelMonitoringJob>(
            jobId,
            job => job.RunAsync(campaignId, channelId),
            Cron.Hourly,
            new RecurringJobOptions { QueueName = "youtube" });

        return Ok(new { jobId, status = "scheduled", interval = "hourly" });
    }

    /// <summary>
    /// Get job status by Hangfire job ID.
    /// </summary>
    [HttpGet("{jobId}/status")]
    public IActionResult GetJobStatus(string jobId)
    {
        var monitoringApi = JobStorage.Current.GetMonitoringApi();
        var jobDetails = monitoringApi.JobDetails(jobId);

        if (jobDetails == null)
            return NotFound(new { jobId, status = "not_found" });

        return Ok(new
        {
            jobId,
            createdAt = jobDetails.CreatedAt,
            jobDetails.History
        });
    }
}
