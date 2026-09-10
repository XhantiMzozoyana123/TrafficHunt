using TrafficHunt.Application.Dtos;
using TrafficHunt.Domain.Entities;

namespace TrafficHunt.Application.Interfaces;

/// <summary>
/// Local AI reasoning (Ollama) - comment qualification and reply generation.
/// </summary>
public interface IOllamaService
{
    Task<QualificationResult> QualifyCommentAsync(
        string campaignContext,
        string videoTitle,
        string commentText,
        CancellationToken ct = default);

    Task<string> GenerateReplyAsync(
        string campaignContext,
        string commentText,
        string painPoint,
        CancellationToken ct = default);

    /// <summary>
    /// From a plain-English description of what's being promoted, generate a full campaign
    /// skeleton (product, audience, problems, discovery keywords) as structured JSON.
    /// </summary>
    Task<CampaignDraft> GenerateCampaignAsync(string description, CancellationToken ct = default);

    /// <summary>
    /// Generate reply message templates for a reply campaign, grounded in real
    /// prospect comments so responses address what prospects actually say.
    /// </summary>
    Task<List<string>> GenerateReplyTemplatesAsync(string campaignContext, List<string> sampleComments, CancellationToken ct = default);

    /// <summary>
    /// Generate an AI-powered analytics summary for a completed reply campaign.
    /// </summary>
    Task<string> GenerateAnalyticsSummaryAsync(ReplyCampaign campaign, List<ReplyRecord> records, CancellationToken ct = default);
}

