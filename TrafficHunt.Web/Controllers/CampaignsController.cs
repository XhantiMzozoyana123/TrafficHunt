using Microsoft.AspNetCore.Mvc;
using TrafficHunt.Application.Interfaces;
using TrafficHunt.Domain.Entities;

namespace TrafficHunt.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CampaignsController : ControllerBase
{
    private readonly ICampaignService _campaigns;

    public CampaignsController(ICampaignService campaigns)
    {
        _campaigns = campaigns;
    }

    [HttpGet]
    public async Task<ActionResult<List<Campaign>>> GetAll(CancellationToken ct) =>
        Ok(await _campaigns.GetAllAsync(ct));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Campaign>> GetById(int id, CancellationToken ct)
    {
        var campaign = await _campaigns.GetByIdAsync(id, ct);
        return campaign is null ? NotFound() : Ok(campaign);
    }

    [HttpPost]
    public async Task<ActionResult<Campaign>> Create(Campaign campaign, CancellationToken ct)
    {
        var created = await _campaigns.CreateAsync(campaign, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<Campaign>> Update(int id, Campaign campaign, CancellationToken ct)
    {
        if (id != campaign.Id) return BadRequest();
        var updated = await _campaigns.UpdateAsync(campaign, ct);
        return updated is null ? NotFound() : Ok(updated);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct) =>
        await _campaigns.DeleteAsync(id, ct) ? NoContent() : NotFound();

    [HttpPost("{id:int}/keywords")]
    public async Task<ActionResult<CampaignKeyword>> AddKeyword(int id, [FromBody] KeywordRequest request, CancellationToken ct) =>
        Ok(await _campaigns.AddKeywordAsync(id, request.Keyword, ct));

    [HttpDelete("keywords/{keywordId:int}")]
    public async Task<IActionResult> RemoveKeyword(int keywordId, CancellationToken ct) =>
        await _campaigns.RemoveKeywordAsync(keywordId, ct) ? NoContent() : NotFound();

    [HttpGet("{id:int}/stats")]
    public async Task<ActionResult<CampaignStats>> GetStats(int id, CancellationToken ct) =>
        Ok(await _campaigns.GetStatsAsync(id, ct));

    public record KeywordRequest(string Keyword);
}
