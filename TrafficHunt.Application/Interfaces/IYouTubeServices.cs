using TrafficHunt.Application.Dtos;

namespace TrafficHunt.Application.Interfaces;

/// <summary>
/// YouTube discovery via YoutubeExplode (video search only - comments moved to the Data API).
/// </summary>
public interface IYouTubeSearchService
{
    Task<List<DiscoveredVideo>> SearchVideosAsync(string keyword, int maxResults = 10, CancellationToken ct = default);
}

/// <summary>
/// YouTube Data API v3 access: comment collection now, OAuth reply actions in Milestone 2.
/// </summary>
public interface IYouTubeApiService
{
    Task<List<CollectedComment>> GetVideoCommentsAsync(string youTubeVideoId, int maxComments = 100, CancellationToken ct = default);
}
