using Hangfire;
using Microsoft.Extensions.Logging;
using TrafficHunt.Application.Interfaces;
using TrafficHunt.Domain.Entities;

namespace TrafficHunt.Infrastructure.Jobs;

/// <summary>
/// Opportunity detection job — scans high-intent prospects and generates
/// outreach recommendations. Runs in the "ai" queue.
/// </summary>
[Queue("ai")]
public class OpportunityDetectionJob
{
    private readonly IProspectRepository _prospects;
    private readonly IOllamaService _ollama;
    private readonly ILogger<OpportunityDetectionJob> _logger;

    public OpportunityDetectionJob(
        IProspectRepository prospects,
        IOllamaService ollama,
        ILogger<OpportunityDetectionJob> logger)
    {
        _prospects = prospects;
        _ollama = ollama;
        _logger = logger;
    }

    /// <summary>
    /// Find high-intent prospects for a campaign and generate outreach recommendations.
    /// </summary>
    public async Task RunAsync(int campaignId, int minIntentScore = 80)
    {
        var prospects = await _prospects.GetByCampaignAsync(campaignId, minIntentScore: minIntentScore);
        _logger.LogInformation("Opportunity detection: {Count} high-intent prospects for campaign {CampaignId}", prospects.Count, campaignId);

        // Could generate recommendations, group by pain point, etc.
        // For now, this is a placeholder for future AI-powered opportunity scoring.
    }
}
