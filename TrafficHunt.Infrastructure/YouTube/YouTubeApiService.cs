using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using TrafficHunt.Application.Dtos;
using TrafficHunt.Application.Interfaces;

namespace TrafficHunt.Infrastructure.YouTube;

/// <summary>
/// YouTube Data API v3 access: comment collection (commentThreads) now,
/// OAuth reply actions in Milestone 2. Requires "YouTube:ApiKey" in configuration.
/// </summary>
public class YouTubeApiService : IYouTubeApiService
{
    private readonly HttpClient _http;
    private readonly string _apiKey;

    public YouTubeApiService(HttpClient http, IConfiguration configuration)
    {
        _http = http;
        _apiKey = configuration["YouTube:ApiKey"]
            ?? throw new InvalidOperationException("YouTube:ApiKey not configured.");
    }

    public async Task<List<CollectedComment>> GetVideoCommentsAsync(
        string youTubeVideoId, int maxComments = 100, CancellationToken ct = default)
    {
        var comments = new List<CollectedComment>();
        var pageToken = string.Empty;

        while (comments.Count < maxComments)
        {
            var pageSize = Math.Min(100, maxComments - comments.Count);
            var url = $"commentThreads?part=snippet&videoId={youTubeVideoId}" +
                      $"&maxResults={pageSize}&order=relevance&textFormat=plainText" +
                      (string.IsNullOrEmpty(pageToken) ? string.Empty : $"&pageToken={pageToken}") +
                      $"&key={_apiKey}";

            var response = await _http.GetFromJsonAsync<JsonElement>(url, cancellationToken: ct);

            foreach (var item in response.GetProperty("items").EnumerateArray())
            {
                var snippet = item.GetProperty("snippet").GetProperty("topLevelComment").GetProperty("snippet");
                comments.Add(new CollectedComment
                {
                    YouTubeCommentId = item.GetProperty("id").GetString() ?? string.Empty,
                    AuthorName = snippet.GetProperty("authorDisplayName").GetString() ?? string.Empty,
                    AuthorChannelId = snippet.GetProperty("authorChannelId").GetProperty("value").GetString() ?? string.Empty,
                    Text = snippet.GetProperty("textDisplay").GetString() ?? string.Empty,
                    LikeCount = snippet.TryGetProperty("likeCount", out var likes) ? likes.GetInt32() : 0,
                    PublishedAt = snippet.TryGetProperty("publishedAt", out var pub)
                        ? DateTime.Parse(pub.GetString()!).ToUniversalTime()
                        : DateTime.UtcNow
                });
            }

            pageToken = response.TryGetProperty("nextPageToken", out var token)
                ? token.GetString() ?? string.Empty
                : string.Empty;
            if (string.IsNullOrEmpty(pageToken)) break;
        }

        return comments;
    }
}
