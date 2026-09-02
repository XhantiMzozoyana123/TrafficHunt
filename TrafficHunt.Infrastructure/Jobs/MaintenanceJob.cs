using Hangfire;
using Microsoft.Extensions.Logging;
using TrafficHunt.Application.Interfaces;

namespace TrafficHunt.Infrastructure.Jobs;

/// <summary>
/// Maintenance job — cleans up old data, generates reports.
/// Runs in the "maintenance" queue.
/// </summary>
[Queue("maintenance")]
public class MaintenanceJob
{
    private readonly IProspectRepository _prospects;
    private readonly ILogger<MaintenanceJob> _logger;

    public MaintenanceJob(
        IProspectRepository prospects,
        ILogger<MaintenanceJob> logger)
    {
        _prospects = prospects;
        _logger = logger;
    }

    /// <summary>
    /// Generate daily stats report and cleanup old rejected prospects.
    /// </summary>
    public async Task RunDailyCleanupAsync()
    {
        _logger.LogInformation("Running daily maintenance job");
        // Cleanup logic: remove old rejected prospects, generate stats, etc.
        await Task.CompletedTask;
    }
}
