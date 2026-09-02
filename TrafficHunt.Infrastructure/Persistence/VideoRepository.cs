using Microsoft.EntityFrameworkCore;
using TrafficHunt.Application.Interfaces;
using TrafficHunt.Domain.Entities;

namespace TrafficHunt.Infrastructure.Persistence;

public class VideoRepository : IVideoRepository
{
    private readonly TrafficHuntDbContext _db;

    public VideoRepository(TrafficHuntDbContext db)
    {
        _db = db;
    }

    public Task<Video?> GetByYouTubeIdAsync(int campaignId, string youTubeVideoId, CancellationToken ct = default) =>
        _db.Videos.FirstOrDefaultAsync(v => v.CampaignId == campaignId && v.YouTubeVideoId == youTubeVideoId, ct);

    public async Task AddAsync(Video video, CancellationToken ct = default)
    {
        _db.Videos.Add(video);
        await _db.SaveChangesAsync(ct);
    }
}
