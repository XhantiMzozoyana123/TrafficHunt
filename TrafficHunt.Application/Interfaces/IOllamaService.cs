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
}
