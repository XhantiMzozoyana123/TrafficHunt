using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using TrafficHunt.Application.Dtos;
using TrafficHunt.Application.Interfaces;
using TrafficHunt.Domain.Entities;

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
            You write YouTube comment replies for traffic outreach. Reply as a helpful human,
            not a marketer. Never use hashtags or emojis. Keep it under 80 words.

            CAMPAIGN CONTEXT:
            {campaignContext}

            PROSPECT COMMENT:
            {commentText}

            PROSPECT PAIN POINT:
            {painPoint}

            GOAL: drive traffic to our website. The ONLY promotional element allowed in the reply
            is our website link. Rules:
            - First genuinely respond to what they said (answer, empathize, or add a useful tip).
            - Then point them to the website link for the full solution — phrase it casually,
              e.g. "I wrote up how I fixed this on my site" or "the full guide is on my site".
            - No selling language, no features list, no "check out our product" — just the link
              as a natural next step. Include the link once, at the end.
            """;

        // format: null — a reply is free text. With format:"json" Ollama wrapped
        // replies in JSON (stored as "{ }" or {"comment": "reply"}), which then
        // failed / posted garbage on YouTube.
        return (await GenerateAsync(prompt, ct, format: null)).Trim();
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

    public async Task<List<string>> GenerateReplyTemplatesAsync(string campaignContext, List<string> sampleComments, CancellationToken ct = default)
    {
        var commentsBlock = sampleComments.Count > 0
            ? string.Join("\n- ", sampleComments.Take(15).Select(c => c.Length > 220 ? c[..220] + "…" : c))
            : "(no prospect comments collected yet — use general pain points)";

        var placeholderNote = "- Where personalization helps use the placeholders {name}, {pain_point}, {channel}";
        var linkNote = "- End every template with the website link — the link is the ONLY promotional element. " +
                       "Introduce it casually (\"full breakdown on my site:\") so the reply reads as helpful, not ads.";
        var prompt = $"""
            You are a YouTube outreach specialist. Generate 5 different reply message templates
            whose goal is to drive traffic to our website. They must read as genuine answers to
            the prospect's comment, not advertisements.

            CAMPAIGN CONTEXT (includes the website URL to use):
            {campaignContext}

            REAL COMMENTS PROSPECTS HAVE LEFT (study their wording, tone and specific struggles —
            the replies must respond correctly to comments like these):
            - {commentsBlock}

            Each template should:
            - Be under 80 words
            - Sound human and helpful (not salesy)
            - Directly acknowledge and respond to the kind of problem shown in the real comments above
            - Be unique in tone/angle (casual, professional, empathetic, direct, story-based)
            {placeholderNote}
            {linkNote}

            Return ONLY a JSON array of strings, no markdown:
            ["template 1", "template 2", "template 3", "template 4", "template 5"]
            """;

        var json = await GenerateAsync(prompt, ct);
        try
        {
            var root = ParseJson<JsonElement>(json);
            return root.EnumerateArray().Select(x => x.GetString() ?? "").Where(s => !string.IsNullOrWhiteSpace(s)).ToList();
        }
        catch
        {
            return new List<string>
            {
                "Hey {name}! Saw your comment about {pain_point} — I built a tool that handles exactly that. Happy to walk you through it!",
                "That {pain_point} struggle is super common. I made something that solves it — want me to share the details?",
                "I ran into the same issue with {pain_point} and ended up building a fix. It might be exactly what you're looking for!",
                "Totally get the frustration around {pain_point}. There's a tool I made that automates the whole thing — worth a look?",
                "Your comment about {pain_point} hit home — I've been there. I built a solution for precisely this; let me know and I'll send it over!"
            };
        }
    }

    public async Task<string> GenerateAnalyticsSummaryAsync(ReplyCampaign campaign, List<ReplyRecord> records, CancellationToken ct = default)
    {
        var prompt = $"""
            You are a marketing analytics AI. Analyze the following reply campaign performance
            and provide a concise, actionable summary.

            Campaign: {campaign.Name}
            Status: {campaign.Status}
            Replies Sent: {campaign.RepliesSent}
            Replies Failed: {campaign.RepliesFailed}
            Replies Pending: {campaign.RepliesPending}
            Templates Used: {campaign.Templates.Count}
            Delay Range: {campaign.MinDelaySeconds}-{campaign.MaxDelaySeconds} seconds

            Provide a 2-3 sentence summary of the campaign's performance and what to improve.
            Be specific and actionable.
            """;

        return (await GenerateAsync(prompt, ct, format: null)).Trim();
    }

    /// <summary>
    /// Calls Ollama. Pass <paramref name="format"/> = <c>"json"</c> only when the prompt
    /// is expected to return a JSON object that we parse (qualification, campaign draft,
    /// templates). Replies and free-text analytics must leave it null so Ollama does not
    /// wrap them in JSON (which produced stored replies like <c>{"price wars": "..."}</c>).
    /// </summary>
    private async Task<string> GenerateAsync(string prompt, CancellationToken ct, string? format = "json")
    {
        var payload = JsonSerializer.Serialize(new
        {
            model = _model,
            prompt,
            stream = false,
            format,
            // Keep the model resident on the server (never unload) — the reverse proxy
            // in front of the LLM host cuts slow connections, and a cold model load
            // regularly exceeded it, producing 504s / client timeouts.
            keep_alive = -1,
            options = new { temperature = 0.4 }
        });

        using var content = new StringContent(payload, Encoding.UTF8, "application/json");

        try
        {
            var response = await _http.PostAsync("/api/generate", content, ct);
            response.EnsureSuccessStatusCode();

            using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
            return doc.RootElement.GetProperty("response").GetString() ?? string.Empty;
        }
        catch (Exception ex) when (
            ex is HttpRequestException or TaskCanceledException or TimeoutException
            && ex is not OperationCanceledException { CancellationToken.IsCancellationRequested: true })
        {
            // One immediate retry: timeouts are usually the model cold-loading on the
            // server (the load continues server-side even after the connection is cut),
            // so the second attempt — hitting the now-warm model — typically succeeds.
            var response = await _http.PostAsync("/api/generate", content, ct);
            response.EnsureSuccessStatusCode();

            using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
            return doc.RootElement.GetProperty("response").GetString() ?? string.Empty;
        }
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
