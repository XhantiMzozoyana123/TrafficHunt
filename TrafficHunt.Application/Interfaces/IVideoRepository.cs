using TrafficHunt.Domain.Entities;

namespace TrafficHunt.Application.Interfaces;

public interface IVideoRepository
{
    Task<Video?> GetByYouTubeIdAsync(int campaignId, string youTubeVideoId, CancellationToken ct = default);
    Task AddAsync(Video video, CancellationToken ct = default);
}
