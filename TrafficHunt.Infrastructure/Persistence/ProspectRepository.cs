using Microsoft.EntityFrameworkCore;
using TrafficHunt.Application.Interfaces;
using TrafficHunt.Domain.Entities;

namespace TrafficHunt.Infrastructure.Persistence;

public class ProspectRepository : IProspectRepository
{
    private const int HighIntentThreshold = 80;

    private readonly TrafficHuntDbContext _db;

    public ProspectRepository(TrafficHuntDbContext db)
    {
        _db = db;
    }

    public Task<List<Prospect>> GetByCampaignAsync(
        int campaignId, string? status = null, int? minIntentScore = null, CancellationToken ct = default)
    {
        var query = _db.Prospects.AsNoTracking().Where(p => p.CampaignId == campaignId);

        if (!string.IsNullOrEmpty(status))
            query = query.Where(p => p.Status == status);

        if (minIntentScore is not null)
            query = query.Where(p => p.IntentScore >= minIntentScore);

        return query
            .OrderByDescending(p => p.IntentScore)
            .ThenByDescending(p => p.CreatedAt)
            .ToListAsync(ct);
    }

    public Task<Prospect?> GetByIdAsync(int id, CancellationToken ct = default) =>
        _db.Prospects.Include(p => p.Campaign).FirstOrDefaultAsync(p => p.Id == id, ct);

    public Task<Prospect?> GetByCommentIdAsync(int campaignId, string commentId, CancellationToken ct = default) =>
        _db.Prospects.FirstOrDefaultAsync(p => p.CampaignId == campaignId && p.CommentId == commentId, ct);

    public Task<bool> ExistsAsync(int campaignId, string commentId, CancellationToken ct = default) =>
        _db.Prospects.AnyAsync(p => p.CampaignId == campaignId && p.CommentId == commentId, ct);

    public async Task AddAsync(Prospect prospect, CancellationToken ct = default)
    {
        _db.Prospects.Add(prospect);
        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(Prospect prospect, CancellationToken ct = default)
    {
        _db.Prospects.Update(prospect);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken ct = default)
    {
        var prospect = await _db.Prospects.FirstOrDefaultAsync(p => p.Id == id, ct);
        if (prospect is null) return false;
        _db.Prospects.Remove(prospect);
        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<GlobalStats> GetGlobalStatsAsync(CancellationToken ct = default)
    {
        var prospects = _db.Prospects.AsNoTracking();
        return new GlobalStats
        {
            TotalProspects = await prospects.CountAsync(ct),
            HighIntent = await prospects.CountAsync(p => p.IntentScore >= HighIntentThreshold, ct),
            Contacted = await prospects.CountAsync(p => p.Status == ProspectStatus.Contacted, ct),
            Interested = await prospects.CountAsync(p => p.Status == ProspectStatus.Interested, ct),
            Converted = await prospects.CountAsync(p => p.Status == ProspectStatus.Converted, ct)
        };
    }

    public async Task<CampaignStats> GetCampaignStatsAsync(int campaignId, CancellationToken ct = default)
    {
        var prospects = _db.Prospects.Where(p => p.CampaignId == campaignId);
        return new CampaignStats
        {
            TotalProspects = await prospects.CountAsync(ct),
            HighIntent = await prospects.CountAsync(p => p.IntentScore >= HighIntentThreshold, ct),
            Contacted = await prospects.CountAsync(p => p.Status == ProspectStatus.Contacted, ct),
            Interested = await prospects.CountAsync(p => p.Status == ProspectStatus.Interested, ct),
            Converted = await prospects.CountAsync(p => p.Status == ProspectStatus.Converted, ct)
        };
    }
}
