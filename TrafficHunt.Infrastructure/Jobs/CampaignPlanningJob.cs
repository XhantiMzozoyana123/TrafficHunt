using Hangfire;
using Microsoft.Extensions.Logging;
using TrafficHunt.Application.Dtos;
using TrafficHunt.Application.Interfaces;

namespace TrafficHunt.Infrastructure.Jobs;

/// <summary>
/// AI campaign-planning job — turns a plain-English description into a full
/// campaign skeleton with ONE bounded LLM call, then stores the result as
/// PlanningResult so the UI can poll for it.
/// Why a job: the remote LLM cold-loads qwen3.5 (60s+ first hit), which exceeds
/// browser/proxy timeouts when done inline in the POST. Enqueue + poll keeps the
/// HTTP request instant and the single LLM call safely inside a background job.
/// Runs in the "default" queue (chunk-safe: exactly one LLM call per job).
/// </summary>
[Queue("default")]
public class CampaignPlanningJob
{
    private readonly IOllamaService _ollama;
    private readonly ICampaignPlanner _planner;
    private readonly ILogger<CampaignPlanningJob> _logger;

    /// <summary>In-memory result store (Hangfire is InMemory — same lifetime).</summary>
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, PlanningResult> _results = new();

    public CampaignPlanningJob(IOllamaService ollama, ICampaignPlanner planner, ILogger<CampaignPlanningJob> logger)
    {
        _ollama = ollama;
        _planner = planner;
        _logger = logger;
    }

    public static PlanningResult? GetResult(string planningId) =>
        _results.TryGetValue(planningId, out var r) ? r : null;

    /// <summary>Run the single planning LLM call and persist the campaign.</summary>
    public async Task RunAsync(string planningId, string description, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        _logger.LogInformation("AI planning started ({PlanningId})", planningId);
        try
        {
            var plan = await _planner.PlanAsync(description, ct);
            _results[planningId] = PlanningResult.Succeeded(plan.CampaignId, plan);
            _logger.LogInformation("AI planning succeeded ({PlanningId} -> campaign {CampaignId})", planningId, plan.CampaignId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AI planning failed ({PlanningId})", planningId);
            _results[planningId] = PlanningResult.Failed(ex.Message);
            throw; // Hangfire retry with default policy
        }
    }
}

/// <summary>Pollable outcome of a <see cref="CampaignPlanningJob"/>.</summary>
public class PlanningResult
{
    public bool IsComplete { get; set; }
    public bool IsSuccess { get; set; }
    public int? CampaignId { get; set; }
    public CampaignPlan? Plan { get; set; }
    public string? Error { get; set; }

    public static PlanningResult Succeeded(int campaignId, CampaignPlan plan) => new()
    {
        IsComplete = true, IsSuccess = true, CampaignId = campaignId, Plan = plan
    };

    public static PlanningResult Failed(string error) => new()
    {
        IsComplete = true, IsSuccess = false, Error = error
    };
}
