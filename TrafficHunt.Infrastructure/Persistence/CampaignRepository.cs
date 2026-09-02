using Microsoft.EntityFrameworkCore;
using TrafficHunt.Application.Interfaces;
using TrafficHunt.Domain.Entities;

namespace TrafficHunt.Infrastructure.Persistence;

public class CampaignRepository : ICampaignRepository
{
    private readonly TrafficHuntDbContext _db;

    public CampaignRepository(TrafficHuntDbContext db)
    {
        _db = db;
    }

    public Task<List<Campaign>> GetAllAsync(CancellationToken ct = default) =>
        _db.Campaigns
            .Include(c => c.Keywords)
            .Include(c => c.Problems)
            .AsNoTracking()
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync(ct);

    public Task<Campaign?> GetByIdAsync(int id, CancellationToken ct = default) =>
        _db.Campaigns
            .Include(c => c.Keywords)
            .Include(c => c.Problems)
            .FirstOrDefaultAsync(c => c.Id == id, ct);

    public async Task<Campaign> AddAsync(Campaign campaign, CancellationToken ct = default)
    {
        _db.Campaigns.Add(campaign);
        await _db.SaveChangesAsync(ct);
        return campaign;
    }

    public async Task<Campaign?> UpdateAsync(Campaign campaign, CancellationToken ct = default)
    {
        var existing = await _db.Campaigns
            .Include(c => c.Keywords)
            .Include(c => c.Problems)
            .FirstOrDefaultAsync(c => c.Id == campaign.Id, ct);

        if (existing is null) return null;

        existing.Name = campaign.Name;
        existing.ProductName = campaign.ProductName;
        existing.ProductDescription = campaign.ProductDescription;
        existing.ProductUrl = campaign.ProductUrl;
        existing.ValueProposition = campaign.ValueProposition;
        existing.TargetAudience = campaign.TargetAudience;
        existing.PrimaryProblem = campaign.PrimaryProblem;
        existing.QualificationRules = campaign.QualificationRules;
        existing.OutreachInstructions = campaign.OutreachInstructions;
        existing.ReplyInstructions = campaign.ReplyInstructions;
        existing.Status = campaign.Status;
        existing.UpdatedAt = campaign.UpdatedAt;

        await _db.SaveChangesAsync(ct);
        return existing;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken ct = default)
    {
        var campaign = await _db.Campaigns.FirstOrDefaultAsync(c => c.Id == id, ct);
        if (campaign is null) return false;
        _db.Campaigns.Remove(campaign);
        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<CampaignKeyword> AddKeywordAsync(int campaignId, string keyword, CancellationToken ct = default)
    {
        var keywordEntity = new CampaignKeyword
        {
            CampaignId = campaignId,
            Keyword = keyword,
            Active = true
        };
        _db.CampaignKeywords.Add(keywordEntity);
                await _db.SaveChangesAsync(ct);
        return keywordEntity;
    }

    public async Task<bool> RemoveKeywordAsync(int keywordId, CancellationToken ct = default)
    {
        var keyword = await _db.CampaignKeywords.FirstOrDefaultAsync(k => k.Id == keywordId, ct);
        if (keyword is null) return false;
        _db.CampaignKeywords.Remove(keyword);
        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<CampaignProblem> AddProblemAsync(int campaignId, string problem, CancellationToken ct = default)
    {
        var entity = new CampaignProblem { CampaignId = campaignId, Text = problem };
        _db.CampaignProblems.Add(entity);
        await _db.SaveChangesAsync(ct);
        return entity;
    }
}
