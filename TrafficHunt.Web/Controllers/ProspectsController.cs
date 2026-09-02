using Microsoft.AspNetCore.Mvc;
using TrafficHunt.Application.Interfaces;
using TrafficHunt.Domain.Entities;

namespace TrafficHunt.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProspectsController : ControllerBase
{
    private readonly IProspectService _prospects;

    public ProspectsController(IProspectService prospects)
    {
        _prospects = prospects;
    }

    [HttpGet]
    public async Task<ActionResult<List<Prospect>>> GetByCampaign(
        [FromQuery] int campaignId,
        [FromQuery] string? status,
        [FromQuery] int? minIntentScore,
        CancellationToken ct) =>
        Ok(await _prospects.GetByCampaignAsync(campaignId, status, minIntentScore, ct));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Prospect>> GetById(int id, CancellationToken ct)
    {
        var prospect = await _prospects.GetByIdAsync(id, ct);
        return prospect is null ? NotFound() : Ok(prospect);
    }

    [HttpPatch("{id:int}/status")]
    public async Task<ActionResult<Prospect>> UpdateStatus(
        int id, [FromBody] StatusRequest request, CancellationToken ct)
    {
        var updated = await _prospects.UpdateStatusAsync(id, request.Status, ct);
        return updated is null ? NotFound() : Ok(updated);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct) =>
        await _prospects.DeleteAsync(id, ct) ? NoContent() : NotFound();

    [HttpGet("stats/global")]
    public async Task<ActionResult<GlobalStats>> GetGlobalStats(CancellationToken ct) =>
        Ok(await _prospects.GetGlobalStatsAsync(ct));

    public record StatusRequest(string Status);
}
