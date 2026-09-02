using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using TrafficHunt.Application.Dtos;
using TrafficHunt.Application.Interfaces;

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

    public record DiscoveryRequest(int? VideosPerKeyword, int? CommentsPerVideo);
}
