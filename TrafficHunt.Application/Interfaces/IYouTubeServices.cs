using TrafficHunt.Application.Dtos;

namespace TrafficHunt.Application.Interfaces;

/// <summary>
/// Persists a string setting (API keys, OAuth tokens) to the app's settings store
/// (appsettings.json by default). Implemented in the composition root.
/// </summary>
public interface ISettingsStore
{
    string? Get(string key);
    void Set(string key, string? value);
    void Save();
}

public interface ISettingsStoreFactory
{
    ISettingsStore Create();
}

/// <summary>
/// YouTube discovery via YoutubeExplode (video search only - comments moved to the Data API).
/// </summary>
public interface IYouTubeSearchService
{
    Task<List<DiscoveredVideo>> SearchVideosAsync(string keyword, int maxResults = 10, CancellationToken ct = default);

    /// <summary>
    /// Search for videos from a specific channel. Used by channel monitoring.
    /// </summary>
    Task<List<DiscoveredVideo>> SearchChannelVideosAsync(string channelId, int maxResults = 10, CancellationToken ct = default);
}

/// <summary>
/// YouTube Data API v3 access: comment collection for discovery.
/// </summary>
public interface IYouTubeApiService
{
    Task<List<CollectedComment>> GetVideoCommentsAsync(string youTubeVideoId, int maxComments = 100, CancellationToken ct = default);
}

/// <summary>
/// Publishes a reply to a YouTube comment via the official Data API v3
/// (POST /youtube/v3/comments?part=snippet with an OAuth2 Bearer token).
/// </summary>
public interface IYouTubeReplySender
{
    /// <summary>
    /// Sends a reply to the given parent comment on the given video.
    /// Returns the new reply's comment id.
    /// </summary>
    Task<string> SendReplyAsync(string videoId, string parentCommentId, string textOriginal, CancellationToken ct = default);

    /// <summary>True when reply credentials (session or OAuth) are configured.</summary>
    bool IsConnected { get; }
}
