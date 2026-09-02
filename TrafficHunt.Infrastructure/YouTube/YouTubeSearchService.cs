using TrafficHunt.Application.Dtos;
using TrafficHunt.Application.Interfaces;
using YoutubeExplode;
using YoutubeExplode.Common;

namespace TrafficHunt.Infrastructure.YouTube;

/// <summary>
/// YoutubeExplode = DISCOVERY (video search only). Sending messages is never done here.
/// </summary>
public class YouTubeSearchService : IYouTubeSearchService
{
    private readonly YoutubeClient _youtube = new();

    public async Task<List<DiscoveredVideo>> SearchVideosAsync(string keyword, int maxResults = 10, CancellationToken ct = default)
    {
        var results = await _youtube.Search.GetVideosAsync(keyword, ct).CollectAsync(maxResults);

        return results.Select(v => new DiscoveredVideo
        {
            YouTubeVideoId = v.Id.Value,
            Title = v.Title,
            ChannelTitle = v.Author?.ChannelTitle ?? string.Empty,
            Url = v.Url
        }).ToList();
    }
}
