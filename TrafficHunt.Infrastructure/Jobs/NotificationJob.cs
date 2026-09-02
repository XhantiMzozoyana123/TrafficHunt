using Hangfire;
using Microsoft.Extensions.Logging;
using TrafficHunt.Application.Interfaces;

namespace TrafficHunt.Infrastructure.Jobs;

/// <summary>
/// Notification job — sends notifications to the operator.
/// Runs in the "notifications" queue.
/// </summary>
[Queue("notifications")]
public class NotificationJob
{
    private readonly ILogger<NotificationJob> _logger;

    public NotificationJob(ILogger<NotificationJob> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Notify the operator that a discovery job has completed.
    /// </summary>
    public async Task RunAsync(string message, string? jobId = null)
    {
        _logger.LogInformation("NOTIFICATION: {Message} [Job: {JobId}]", message, jobId);
        // Future: send email, in-app notification, etc.
        await Task.CompletedTask;
    }
}
