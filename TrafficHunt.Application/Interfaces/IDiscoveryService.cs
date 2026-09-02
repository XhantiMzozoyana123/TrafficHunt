using TrafficHunt.Application.Dtos;

namespace TrafficHunt.Application.Interfaces;

public interface IDiscoveryService
{
    /// <summary>
    /// Run the full discovery pipeline for a campaign:
    /// keywords -> YouTube search -> collect comments -> AI qualification -> prospects.
    /// </summary>
    IAsyncEnumerable<DiscoveryProgress> RunDiscoveryAsync(
        int campaignId,
        int videosPerKeyword = 3,
        int commentsPerVideo = 50,
        CancellationToken ct = default);
}
