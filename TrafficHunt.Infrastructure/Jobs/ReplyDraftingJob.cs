using Hangfire;
using Microsoft.Extensions.Logging;
using TrafficHunt.Application.Interfaces;

namespace TrafficHunt.Infrastructure.Jobs;

/// <summary>
/// Per-record AI drafting chunk (5 records/job). See ReplyTemplateJob for docs.
/// </summary>
[Queue("ai")]
[AutomaticRetry(Attempts = 3, DelaysInSeconds = new[] { 60, 300, 900 }, OnAttemptsExceeded = AttemptsExceededAction.Delete)]
public class ReplyDraftingJob
{
    public const int DefaultBatchSize = 5;
    private readonly IReplyCampaignRepository _replyCampaigns;
    private readonly IOllamaService _ollama;
    private readonly ILogger<ReplyDraftingJob> _logger;

    public ReplyDraftingJob(
        IReplyCampaignRepository replyCampaigns,
        IOllamaService ollama,
        ILogger<ReplyDraftingJob> logger)
    {
        _replyCampaigns = replyCampaigns;
        _ollama = ollama;
        _logger = logger;
    }

    public async Task RunAsync(int replyCampaignId, CancellationToken ct, int batchSize = DefaultBatchSize)
    {
        ct.ThrowIfCancellationRequested();
        var size = batchSize <= 0 ? DefaultBatchSize : batchSize;

        var campaign = await _replyCampaigns.GetByIdAsync(replyCampaignId, ct)
            ?? throw new InvalidOperationException($"Reply campaign {replyCampaignId} not found.");

        var pending = campaign.ReplyRecords
            .Where(r => r.Status == Domain.Entities.ReplyStatus.Pending)
            .OrderBy(r => r.Id)
            .Take(size)
            .ToList();

        if (pending.Count == 0) return;

        var ctx = new System.Text.StringBuilder();
        var c = campaign.Campaign;
        if (c == null) ctx.AppendLine("Product outreach: no campaign details available.");
        else
        {
            ctx.AppendLine($"Product: {c.ProductName}");
            ctx.AppendLine($"Website link (the ONLY promotional element — end every reply with this exact URL): {c.ProductUrl}");
            if (!string.IsNullOrWhiteSpace(c.ProductDescription)) ctx.AppendLine($"What it does: {c.ProductDescription}");
            if (!string.IsNullOrWhiteSpace(c.ValueProposition)) ctx.AppendLine($"Value proposition: {c.ValueProposition}");
            if (!string.IsNullOrWhiteSpace(c.TargetAudience)) ctx.AppendLine($"Target audience: {c.TargetAudience}");
        }
        var context = ctx.ToString();

        foreach (var record in pending)
        {
            ct.ThrowIfCancellationRequested();
            if (record.Status != Domain.Entities.ReplyStatus.Pending) continue; // idempotent

            string message;
            try
            {
                message = await _ollama.GenerateReplyAsync(
                    context,
                    record.Prospect?.CommentText ?? string.Empty,
                    record.Prospect?.PainPoint ?? string.Empty,
                    ct);
                message = ReplyText.Clean(message);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "LLM draft failed for record {RecordId} — template fallback", record.Id);
                var template = campaign.Templates.OrderBy(t => t.TimesUsed).ThenBy(t => t.Order).FirstOrDefault();
                if (template == null)
                    throw new InvalidOperationException("AI reply failed and reply campaign has no templates to fall back on.");
                var author = record.Prospect?.AuthorName ?? "";
                message = template.Body
                    .Replace("{name}", author.Split(' ')[0])
                    .Replace("{pain_point}", record.Prospect?.PainPoint ?? "")
                    .Replace("{channel}", author);
                record.TemplateId = template.Id;
            }

            message = ReplyText.EnsureCampaignLink(message, campaign.Campaign?.ProductUrl);
            record.MessageSent = message;
            record.Status = Domain.Entities.ReplyStatus.Approved;
            record.ScheduledAt = DateTime.UtcNow;
            record.ErrorMessage = null;
        }

        await _replyCampaigns.UpdateAsync(campaign, ct);
        _logger.LogInformation("Drafted {Count} replies for reply campaign {Id}", pending.Count, replyCampaignId);

        var remaining = campaign.ReplyRecords.Count(r => r.Status == Domain.Entities.ReplyStatus.Pending);
        if (remaining > 0)
            BackgroundJob.Enqueue<ReplyDraftingJob>(job => job.RunAsync(replyCampaignId, default(CancellationToken), size));
    }
}
