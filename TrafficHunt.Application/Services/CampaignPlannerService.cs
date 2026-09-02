using TrafficHunt.Application.Dtos;
using TrafficHunt.Application.Interfaces;
using TrafficHunt.Domain.Entities;

namespace TrafficHunt.Application.Services;

/// <summary>
/// Turns a plain-English description of what's being promoted into a full campaign,
/// delegating the AI work to <see cref="IOllamaService"/>.
/// </summary>
public class CampaignPlannerService : ICampaignPlanner
{
    private readonly IOllamaService _ollama;
    private readonly ICampaignService _campaigns;

    public CampaignPlannerService(IOllamaService ollama, ICampaignService campaigns)
    {
        _ollama = ollama;
        _campaigns = campaigns;
    }

    public async Task<CampaignPlan> PlanAsync(string description, CancellationToken ct = default)
    {
        var draft = await _ollama.GenerateCampaignAsync(description, ct);

        var plan = new CampaignPlan
        {
            RawPlanJson = System.Text.Json.JsonSerializer.Serialize(draft),
            Draft = draft
        };

        var campaign = await _campaigns.CreateAsync(new Campaign
        {
            Name = draft.Name,
            ProductName = draft.ProductName,
            ProductDescription = draft.ProductDescription,
            ProductUrl = draft.ProductUrl,
            ValueProposition = draft.ValueProposition,
            TargetAudience = draft.TargetAudience,
            PrimaryProblem = draft.PrimaryProblem
        }, ct);

        plan.CampaignId = campaign.Id;

        // Seed problems (used as AI campaign context when qualifying prospects).
        if (draft.Problems?.Any() == true)
            await _campaigns.AddProblemsAsync(campaign.Id, draft.Problems, ct);

        // Seed discovery keywords.
        if (draft.Keywords?.Any() == true)
        {
            foreach (var keyword in draft.Keywords.Distinct(StringComparer.OrdinalIgnoreCase))
                await _campaigns.AddKeywordAsync(campaign.Id, keyword, ct);
        }

        return plan;
    }

    public async Task<int> PlanAndCreateAsync(string description, CancellationToken ct = default) =>
        (await PlanAsync(description, ct)).CampaignId;
}

