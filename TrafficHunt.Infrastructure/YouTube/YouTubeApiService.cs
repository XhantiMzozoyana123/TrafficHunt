using System.Text;
using System.Text.Json;
using TrafficHunt.Application.Dtos;
using TrafficHunt.Application.Interfaces;

namespace TrafficHunt.Infrastructure.YouTube;

/// <summary>
/// Comment collection via YouTube's internal (innertube) API — the same technique
/// YoutubeExplode used before it dropped comment support in v6. No API key required.
/// </summary>
public class YouTubeApiService : IYouTubeApiService
{
    private readonly HttpClient _http;

    public YouTubeApiService(HttpClient http) => _http = http;

    public async Task<List<CollectedComment>> GetVideoCommentsAsync(
        string youTubeVideoId, int maxComments = 100, CancellationToken ct = default)
    {
        var comments = new List<CollectedComment>();

        // Step 1: resolve the comments continuation token for this video.
        var token = await GetCommentsTokenAsync(youTubeVideoId, ct);
        if (token is null)
            return comments;

        // Step 2: follow comment continuations until we have enough comments.
        while (!string.IsNullOrEmpty(token) && comments.Count < maxComments)
        {
            ct.ThrowIfCancellationRequested();

            var payload = await CallAsync(new { context = WebContext(), continuation = token }, ct);

            foreach (var c in ExtractComments(payload))
            {
                comments.Add(c);
                if (comments.Count >= maxComments)
                    return comments;
            }

            token = FindNextContinuation(payload, token);
        }

        return comments;
    }

    private async Task<string?> GetCommentsTokenAsync(string videoId, CancellationToken ct)
    {
        var payload = await CallAsync(new { context = WebContext(), videoId }, ct);
        var raw = JsonSerializer.Serialize(payload);

        // Prefer the token that appears after the comments engagement panel marker.
        var panelIdx = raw.IndexOf("engagement-panel-comments-section", StringComparison.Ordinal);
        if (panelIdx >= 0)
        {
            var m = System.Text.RegularExpressions.Regex.Match(
                raw[panelIdx..], @"""token"":\s*""([^""]+)""");
            if (m.Success)
                return m.Groups[1].Value.Replace("\\u0026", "&");
        }

        // Fall back to the first continuation token in the response.
        var any = System.Text.RegularExpressions.Regex.Match(raw, @"""token"":\s*""([^""]+)""");
        return any.Success ? any.Groups[1].Value.Replace("\\u0026", "&") : null;
    }

    private static object WebContext() => new
    {
        client = new
        {
            clientName = "WEB",
            clientVersion = "2.20240401.00.00",
            hl = "en",
            gl = "US"
        }
    };

    private async Task<JsonElement> CallAsync(object body, CancellationToken ct)
    {
        var json = JsonSerializer.Serialize(body);
        using var content = new StringContent(json, Encoding.UTF8, "application/json");
        using var resp = await _http.PostAsync(
            "https://www.youtube.com/youtubei/v1/next?prettyPrint=false", content, ct);
        resp.EnsureSuccessStatusCode();
        var stream = await resp.Content.ReadAsStreamAsync(ct);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
        return doc.RootElement.Clone();
    }
    private static IEnumerable<CollectedComment> ExtractComments(JsonElement root)
    {
        var list = new List<CollectedComment>();
        Collect(root, list);
        return list;
    }

    private static void Collect(JsonElement el, List<CollectedComment> list)
    {
        switch (el.ValueKind)
        {
            case JsonValueKind.Object:
                if (el.TryGetProperty("commentEntityPayload", out var cep) && cep.ValueKind == JsonValueKind.Object)
                {
                    var c = ParseEntity(cep);
                    if (c is not null) list.Add(c);
                }
                if (el.TryGetProperty("commentRenderer", out var cr) && cr.ValueKind == JsonValueKind.Object)
                {
                    var c = ParseRenderer(cr);
                    if (c is not null) list.Add(c);
                }
                foreach (var p in el.EnumerateObject())
                    Collect(p.Value, list);
                break;
            case JsonValueKind.Array:
                foreach (var item in el.EnumerateArray())
                    Collect(item, list);
                break;
        }
    }

    private static CollectedComment? ParseEntity(JsonElement cep)
    {
        try
        {
            var commentId = cep.GetProperty("properties").GetProperty("commentId").GetString() ?? string.Empty;
            var text = cep.GetProperty("properties").GetProperty("content").GetProperty("content").GetString() ?? string.Empty;
            var author = cep.GetProperty("author");
            var likeCount = 0;
            if (cep.TryGetProperty("toolbar", out var tb) &&
                tb.TryGetProperty("likeCountNotliked", out var lc) &&
                int.TryParse(lc.GetString(), out var parsed))
                likeCount = parsed;

            return new CollectedComment
            {
                YouTubeCommentId = commentId,
                AuthorName = author.GetProperty("displayName").GetString() ?? string.Empty,
                AuthorChannelId = author.TryGetProperty("channelId", out var ch) ? ch.GetString() ?? string.Empty : string.Empty,
                Text = text,
                LikeCount = likeCount
            };
        }
        catch (KeyNotFoundException) { return null; }
    }

    private static CollectedComment? ParseRenderer(JsonElement cr)
    {
        try
        {
            static string GetText(JsonElement el)
            {
                var sb = new StringBuilder();
                if (el.ValueKind == JsonValueKind.Array)
                    foreach (var r in el.EnumerateArray())
                        if (r.TryGetProperty("text", out var t))
                            sb.Append(t.GetString());
                return sb.ToString();
            }

            var commentId = cr.TryGetProperty("commentId", out var cid) ? cid.GetString() ?? string.Empty : string.Empty;
            var text = GetText(cr.GetProperty("contentText").GetProperty("runs"));
            var author = cr.GetProperty("authorText").GetProperty("simpleText").GetString() ?? string.Empty;
            string channelId = string.Empty;
            if (cr.TryGetProperty("authorEndpoint", out var ae) &&
                ae.TryGetProperty("browseEndpoint", out var be) &&
                be.TryGetProperty("browseId", out var bid))
                channelId = bid.GetString() ?? string.Empty;
            var likeCount = 0;
            if (cr.TryGetProperty("likeCount", out var lk) &&
                lk.TryGetProperty("simpleText", out var lkt) &&
                int.TryParse(lkt.GetString(), out var parsed))
                likeCount = parsed;

            return new CollectedComment
            {
                YouTubeCommentId = commentId,
                AuthorName = author,
                AuthorChannelId = channelId,
                Text = text,
                LikeCount = likeCount
            };
        }
        catch (KeyNotFoundException) { return null; }
    }

    private static string? FindNextContinuation(JsonElement root, string? excludeToken)
    {
        using var doc = JsonDocument.Parse(JsonSerializer.Serialize(root));
        foreach (var t in FindAllTokens(doc.RootElement))
            if (!string.IsNullOrEmpty(t) && t != excludeToken)
                return t;
        return null;
    }

    private static IEnumerable<string> FindAllTokens(JsonElement el)
    {
        switch (el.ValueKind)
        {
            case JsonValueKind.Object:
                if (el.TryGetProperty("continuationItemRenderer", out var cir) &&
                    cir.TryGetProperty("continuationEndpoint", out var ce) &&
                    ce.TryGetProperty("continuationCommand", out var cc) &&
                    cc.TryGetProperty("token", out var tk))
                    yield return tk.GetString() ?? string.Empty;
                foreach (var p in el.EnumerateObject())
                    foreach (var t in FindAllTokens(p.Value))
                        yield return t;
                break;
            case JsonValueKind.Array:
                foreach (var item in el.EnumerateArray())
                    foreach (var t in FindAllTokens(item))
                        yield return t;
                break;
        }
    }
}
