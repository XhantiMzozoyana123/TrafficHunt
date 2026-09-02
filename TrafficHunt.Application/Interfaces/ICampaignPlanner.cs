using TrafficHunt.Application.Dtos;

namespace TrafficHunt.Application.Interfaces;

public interface ICampaignPlanner
{
    /// <summary>
    /// From a plain-English description of what's being promoted, generate a full campaign
    /// (product, audience, problems, keywords) and persist it.
    /// </summary>
    Task<int> PlanAndCreateAsync(string description, CancellationToken ct = default);

    Task<CampaignPlan> PlanAsync(string description, CancellationToken ct = default);
}
