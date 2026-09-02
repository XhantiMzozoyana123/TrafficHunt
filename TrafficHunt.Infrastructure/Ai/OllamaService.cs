using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using TrafficHunt.Application.Dtos;
using TrafficHunt.Application.Interfaces;

namespace TrafficHunt.Infrastructure.Ai;

/// <summary>
/// Talks to a local/remote Ollama server configured under "Ollama" in appsettings.json.
/// Sends requests to POST /api/generate with { model, prompt, stream:false, format:"json" }.
/// </summary>
public class OllamaService : IOllamaService
{
    private readonly HttpClient _http;
    private readonly string _model;

    public OllamaService(HttpClient http, IConfiguration configuration)
    {
        _http = http;
        _model = configuration["Ollama:Model"] ?? "llama3";
    }

    public async Task<QualificationResult> QualifyCommentAsync(
        string campaignContext, string videoTitle, string commentText, CancellationToken ct = default)
    {
        const string exampleJson =
            "{ \"is_target_audience\": true, \"has_relevant_problem\": true, " +
            "\"intent_score\": 94, \"pain_point\": \"...\", \"reason\": \"...\" }";

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
        try { return ParseQualification(json); }
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
        public async Task<CampaignDraft> GenerateCampaignAsync(string description, CancellationToken ct = default)
    {
        const string exampleJson =
            "{ \"name\":\"...\"," +
            "\"productName\":\"...\"," +
            "\"productUrl\":\"...\"," +
            "\"productDescription\":\"...\"," +
            "\"valueProposition\":\"...\"," +
            "\"targetAudience\":\"...\"," +
            "\"primaryProblem\":\"...\"," +
            "\"problems\":[\"...\"]," +
            "\"keywords\":[\"...\"],\"reasoning\":\".\"}";

        var prompt = $"""
            You are a growth-strategy planner for a private customer-acquisition tool.
            Convert the description below into a structured campaign for finding prospects on
            YouTube who are actively looking for the promoted solution.

            DESCRIPTION:
            {description}

            Produce two parts:
            1. A short list of 6-12 concrete YouTube search keywords (discovery terms real people would type).
            2. The campaign fields shown in the JSON example. Keep problems concise.

            Return ONLY the JSON object below (valid JSON, no markdown):
                        {exampleJson}
            """;

        var json = await GenerateAsync(prompt, ct);
        try { return ParseCampaignDraft(json); }
        catch (JsonException ex)
        {
            throw new InvalidOperationException("Ollama returned an invalid campaign plan response.", ex);
        }
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

    private static T ParseJson<T>(string json)
    {
        // Ollama may wrap JSON in markdown fences or preamble - locate the object.
        var start = json.IndexOf('{');
        var end = json.LastIndexOf('}');
        if (start < 0 || end <= start)
            throw new JsonException("No JSON object found in response.");

        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        return JsonSerializer.Deserialize<T>(json[start..(end + 1)], options)!;
    }

    private static QualificationResult ParseQualification(string json)
    {
        var root = ParseJson<JsonElement>(json);
        return new QualificationResult
        {
            IsTargetAudience = root.TryGetProperty("is_target_audience", out var t) && t.GetBoolean(),
            HasRelevantProblem = root.TryGetProperty("has_relevant_problem", out var p) && p.GetBoolean(),
            IntentScore = root.TryGetProperty("intent_score", out var s) ? s.GetInt32() : 0,
            PainPoint = root.TryGetProperty("pain_point", out var pp) ? pp.GetString() ?? string.Empty : string.Empty,
            Reason = root.TryGetProperty("reason", out var r) ? r.GetString() ?? string.Empty : string.Empty
        };
    }

    private static CampaignDraft ParseCampaignDraft(string json)
    {
        var root = ParseJson<JsonElement>(json);

        string[] ToStringArray(JsonElement el, string prop) =>
            el.TryGetProperty(prop, out var arr) ? arr.EnumerateArray().Select(x => x.GetString() ?? "").ToArray() : [];

        return new CampaignDraft
        {
            Name = root.TryGetProperty("name", out var n) ? n.GetString() ?? "" : "",
            ProductName = root.TryGetProperty("productName", out var pn) ? pn.GetString() ?? "" : "",
            ProductUrl = root.TryGetProperty("productUrl", out var pu) ? pu.GetString() ?? "" : "",
            ProductDescription = root.TryGetProperty("productDescription", out var pd) ? pd.GetString() ?? "" : "",
            ValueProposition = root.TryGetProperty("valueProposition", out var vp) ? vp.GetString() ?? "" : "",
            TargetAudience = root.TryGetProperty("targetAudience", out var ta) ? ta.GetString() ?? "" : "",
            PrimaryProblem = root.TryGetProperty("primaryProblem", out var pp) ? pp.GetString() ?? "" : "",
            Problems = ToStringArray(root, "problems").ToList(),
            Keywords = ToStringArray(root, "keywords").ToList()
        };
    }
}
