using Microsoft.EntityFrameworkCore;
using TrafficHunt.Application.Interfaces;
using TrafficHunt.Domain.Entities;
using TrafficHunt.Infrastructure.Persistence;

namespace TrafficHunt.Infrastructure.Persistence.Repositories;

public class ReplyCampaignRepository : IReplyCampaignRepository
{
    private readonly TrafficHuntDbContext _context;

    public ReplyCampaignRepository(TrafficHuntDbContext context)
    {
        _context = context;
    }

    public async Task<List<ReplyCampaign>> GetAllAsync(CancellationToken ct = default)
    {
        return await _context.ReplyCampaigns
            .Include(c => c.Campaign)
            .Include(c => c.Templates)
            .Include(c => c.ReplyRecords)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task<ReplyCampaign?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        return await _context.ReplyCampaigns
            .Include(c => c.Campaign)
            .Include(c => c.Templates)
            .Include(c => c.ReplyRecords)
                .ThenInclude(r => r.Prospect)
            .FirstOrDefaultAsync(c => c.Id == id, ct);
    }

    public async Task<List<ReplyCampaign>> GetByCampaignAsync(int campaignId, CancellationToken ct = default)
    {
        return await _context.ReplyCampaigns
            .Include(c => c.Templates)
            .Include(c => c.ReplyRecords)
            .Where(c => c.CampaignId == campaignId)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task<ReplyCampaign> AddAsync(ReplyCampaign campaign, CancellationToken ct = default)
    {
        _context.ReplyCampaigns.Add(campaign);
        await _context.SaveChangesAsync(ct);
        return campaign;
    }

    public async Task UpdateAsync(ReplyCampaign campaign, CancellationToken ct = default)
    {
        _context.ReplyCampaigns.Update(campaign);
        await _context.SaveChangesAsync(ct);
    }

    public async Task<ReplyTemplate> AddTemplateAsync(ReplyTemplate template, CancellationToken ct = default)
    {
        _context.ReplyTemplates.Add(template);
        await _context.SaveChangesAsync(ct);
        return template;
    }

    public async Task<ReplyRecord> AddRecordAsync(ReplyRecord record, CancellationToken ct = default)
    {
        _context.ReplyRecords.Add(record);
        await _context.SaveChangesAsync(ct);
        return record;
    }

    public async Task<ReplyCampaign?> GetByRecordIdAsync(int replyRecordId, CancellationToken ct = default)
    {
        var campaignId = await _context.ReplyRecords
            .Where(r => r.Id == replyRecordId)
            .Select(r => r.ReplyCampaignId)
            .FirstOrDefaultAsync(ct);
        if (campaignId == 0) return null;

        return await _context.ReplyCampaigns
            .Include(c => c.Templates)
            .Include(c => c.ReplyRecords)
                .ThenInclude(r => r.Prospect)
            .FirstOrDefaultAsync(c => c.Id == campaignId, ct);
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken ct = default)
    {
        // The campaign (with templates + records) may already be tracked in this
        // request's DbContext. Mixing tracked graph deletion with raw ExecuteDelete*
        // makes EF try to delete already-removed rows -> DbUpdateConcurrencyException.
        // Detach everything first so the delete is pure SQL and row counts line up.
        _context.ChangeTracker.Clear();

        var exists = await _context.ReplyCampaigns.AnyAsync(c => c.Id == id, ct);
        if (!exists) return false;

        await _context.ReplyRecords
            .Where(r => r.ReplyCampaignId == id)
            .ExecuteDeleteAsync(ct);
        await _context.ReplyTemplates
            .Where(t => t.ReplyCampaignId == id)
            .ExecuteDeleteAsync(ct);
        await _context.ReplyCampaigns
            .Where(c => c.Id == id)
            .ExecuteDeleteAsync(ct);
        return true;
    }
}
