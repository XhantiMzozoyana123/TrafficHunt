using TrafficHunt.Application.Dtos;

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
}

