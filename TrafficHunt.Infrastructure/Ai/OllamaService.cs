using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using TrafficHunt.Application.Dtos;
using TrafficHunt.Application.Interfaces;

namespace TrafficHunt.Infrastructure.Ai;

/// <summary>
/// Talks to a local Ollama server (default http://localhost:11434).
/// Configuration lives in appsettings.json under "Ollama".
/// </summary>
public class OllamaService : IOllamaService
{
    private readonly HttpClient _http;
    private readonly string _model;

    public OllamaService(HttpClient http, IConfiguration configuration)
    {
        _http = http;
        _model = configuration["Ollama:Model"] ?? "llama3.1";
    }

    public async Task<QualificationResult> QualifyCommentAsync(
        string campaignContext, string videoTitle, string commentText, CancellationToken ct = default)
    {
        const string exampleJson = "{ \"is_target_audience\": true, \"has_relevant_problem\": true, \"intent_score\": 94, \"pain_point\": \"...\", \"reason\": \"...\" }";

        var prompt = $"""
            You are a customer-acquisition analyst. Analyze the YouTube comment below.

            CAMPAIGN CONTEXT:
            {campaignContext}

            VIDEO TITLE: {videoTitle}

            COMMENT:
            {commentText}

            Determine:
            1. is_target_audience: is the commenter part of the campaign's target audience?
               (Someone LOOKING TO HIRE the target audience, e.g. "my client needs a video editor",
               is NOT the target audience.)
            2. has_relevant_problem: does the commenter express a problem the promoted product solves?
            3. intent_score: 0-100, how strongly they express that problem.
            4. pain_point: short description of their problem.
            5. reason: one sentence explaining your analysis.

            Respond with ONLY a JSON object, no markdown, no extra text. Use exactly this shape:
            {exampleJson}
            """;

        var json = await GenerateAsync(prompt, ct);

        try
        {
            return ParseQualification(json);
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException("Ollama returned an invalid qualification response.", ex);
        }
    }

    public async Task<string> GenerateReplyAsync(
        string campaignContext, string commentText, string painPoint, CancellationToken ct = default)
    {
        var prompt = $"""
            You write YouTube comment replies for customer outreach. Reply as a helpful human,
            not a marketer. Never use hashtags or emojis. Keep it under 80 words.

            CAMPAIGN CONTEXT:
            {campaignContext}

            PROSPECT COMMENT:
            {commentText}

            PROSPECT PAIN POINT:
            {painPoint}

            Write one personalized reply that acknowledges their specific problem and naturally
            mentions the promoted product as a way to solve it.
            """;

        return (await GenerateAsync(prompt, ct)).Trim();
    }

    private async Task<string> GenerateAsync(string prompt, CancellationToken ct)
    {
        var payload = JsonSerializer.Serialize(new
        {
            model = _model,
            prompt,
            stream = false,
            format = "json",
            options = new { temperature = 0.4 }
        });

        using var content = new StringContent(payload, Encoding.UTF8, "application/json");
        var response = await _http.PostAsync("/api/generate", content, ct);
        response.EnsureSuccessStatusCode();

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
        return doc.RootElement.GetProperty("response").GetString() ?? string.Empty;
    }

    private static QualificationResult ParseQualification(string json)
    {
        // Ollama can wrap JSON in markdown fences or preamble - find the object.
        var start = json.IndexOf('{');
        var end = json.LastIndexOf('}');
        if (start < 0 || end <= start)
            throw new JsonException("No JSON object found in response.");

        using var doc = JsonDocument.Parse(json[start..(end + 1)]);
        var root = doc.RootElement;

        return new QualificationResult
        {
            IsTargetAudience = root.TryGetProperty("is_target_audience", out var t) && t.GetBoolean(),
            HasRelevantProblem = root.TryGetProperty("has_relevant_problem", out var p) && p.GetBoolean(),
            IntentScore = root.TryGetProperty("intent_score", out var s) ? s.GetInt32() : 0,
            PainPoint = root.TryGetProperty("pain_point", out var pp) ? pp.GetString() ?? string.Empty : string.Empty,
            Reason = root.TryGetProperty("reason", out var r) ? r.GetString() ?? string.Empty : string.Empty
        };
    }
}
