using Hangfire;
using Microsoft.Extensions.Logging;
using TrafficHunt.Application.Interfaces;

namespace TrafficHunt.Infrastructure.Jobs;

/// <summary>
/// AI reply-template chunk job — see ReplyDraftingJob.cs for the drafting chunk.
/// Template generation runs here as ONE bounded LLM call so HTTP never blocks.
/// </summary>
[Queue("default")]
public class ReplyTemplateJob
{
    public const int MaxSampleComments = 12;
    private readonly IReplyCampaignRepository _replyCampaigns;
    private readonly ICampaignRepository _campaigns;
    private readonly IProspectService _prospects;
    private readonly IOllamaService _ollama;
    private readonly ILogger<ReplyTemplateJob> _logger;

    public ReplyTemplateJob(
        IReplyCampaignRepository replyCampaigns,
        ICampaignRepository campaigns,
        IProspectService prospects,
        IOllamaService ollama,
        ILogger<ReplyTemplateJob> logger)
    {
        _replyCampaigns = replyCampaigns;
        _campaigns = campaigns;
        _prospects = prospects;
        _ollama = ollama;
        _logger = logger;
    }

    public async Task RunAsync(int replyCampaignId, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var campaign = await _replyCampaigns.GetByIdAsync(replyCampaignId, ct)
            ?? throw new InvalidOperationException($"Reply campaign {replyCampaignId} not found.");
        if (campaign.Templates.Count > 0) return; // idempotent

        var sourceCampaign = await _campaigns.GetByIdAsync(campaign.CampaignId, ct);
        var prospects = await _prospects.GetByCampaignAsync(campaign.CampaignId, minIntentScore: 60, ct: ct);
        var sampleComments = prospects
            .Where(p => !string.IsNullOrWhiteSpace(p.CommentText))
            .OrderByDescending(p => p.IntentScore)
            .Take(MaxSampleComments)
            .Select(p => p.CommentText.Length > 500 ? p.CommentText[..500] : p.CommentText)
            .ToList();

        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"Product: {sourceCampaign?.ProductName}");
        sb.AppendLine($"Website link (the ONLY promotional element — end every reply with this exact URL): {sourceCampaign?.ProductUrl}");
        if (!string.IsNullOrWhiteSpace(sourceCampaign?.ProductDescription)) sb.AppendLine($"What it does: {sourceCampaign.ProductDescription}");
        if (!string.IsNullOrWhiteSpace(sourceCampaign?.ValueProposition)) sb.AppendLine($"Value proposition: {sourceCampaign.ValueProposition}");
        if (!string.IsNullOrWhiteSpace(sourceCampaign?.TargetAudience)) sb.AppendLine($"Target audience: {sourceCampaign.TargetAudience}");

        List<string> templates;
        try
        {
            _logger.LogInformation("Generating reply templates for reply campaign {Id}", replyCampaignId);
            templates = await _ollama.GenerateReplyTemplatesAsync(sb.ToString(), sampleComments, ct);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or TimeoutException or IOException or System.Net.Sockets.SocketException)
        {
            _logger.LogWarning(ex, "LLM unreachable for reply campaign {Id} — fallback templates", replyCampaignId);
            templates = new()
            {
                "Hey! I know how tough that can be — I ran into the same thing. I wrote up how I handled it on my site if you want the full breakdown: {product_url}",
                "This hit close to home. I put together a guide on my site that covers exactly this — might save you some time: {product_url}",
                "Totally understand where you're coming from. I've shared what worked for me on my site — feel free to check it out: {product_url}",
                "Great point! I actually wrote about this on my site — the full write-up is here if you're interested: {product_url}"
            };
        }

        var productUrl = sourceCampaign?.ProductUrl?.Trim() ?? string.Empty;
        var order = 0;
        foreach (var body in templates.Select(t =>
            productUrl.Length == 0 ? t.Replace("{product_url}", string.Empty).Trim() : t.Replace("{product_url}", productUrl)))
        {
            campaign.Templates.Add(new Domain.Entities.ReplyTemplate
            {
                Name = $"Template {++order}",
                Body = body,
                Order = order - 1
            });
        }

        await _replyCampaigns.UpdateAsync(campaign, ct);
        _logger.LogInformation("Attached {Count} templates to reply campaign {Id}", order, replyCampaignId);
    }
}
