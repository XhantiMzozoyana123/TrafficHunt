using Microsoft.EntityFrameworkCore;
using TrafficHunt.Domain.Entities;

namespace TrafficHunt.Infrastructure.Persistence;

public class TrafficHuntDbContext : DbContext
{
    public TrafficHuntDbContext(DbContextOptions<TrafficHuntDbContext> options) : base(options)
    {
    }

    public DbSet<Campaign> Campaigns => Set<Campaign>();
    public DbSet<CampaignProblem> CampaignProblems => Set<CampaignProblem>();
    public DbSet<CampaignKeyword> CampaignKeywords => Set<CampaignKeyword>();
    public DbSet<Prospect> Prospects => Set<Prospect>();
    public DbSet<Video> Videos => Set<Video>();
    public DbSet<Comment> Comments => Set<Comment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Campaign>(e =>
        {
            e.HasKey(c => c.Id);
            e.Property(c => c.Name).HasMaxLength(200).IsRequired();
            e.Property(c => c.ProductName).HasMaxLength(200);
        });

        modelBuilder.Entity<CampaignProblem>(e =>
        {
            e.HasKey(p => p.Id);
            e.HasOne(p => p.Campaign)
                .WithMany(c => c.Problems)
                .HasForeignKey(p => p.CampaignId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<CampaignKeyword>(e =>
        {
            e.HasKey(k => k.Id);
            e.Property(k => k.Keyword).HasMaxLength(300).IsRequired();
            e.HasOne(k => k.Campaign)
                .WithMany(c => c.Keywords)
                .HasForeignKey(k => k.CampaignId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(k => new { k.CampaignId, k.Keyword }).IsUnique();
        });

        modelBuilder.Entity<Prospect>(e =>
        {
            e.HasKey(p => p.Id);
            e.Property(p => p.Status).HasMaxLength(50);
            e.HasIndex(p => new { p.CampaignId, p.CommentId }).IsUnique();
            e.HasIndex(p => p.IntentScore);
            e.HasOne(p => p.Campaign)
                .WithMany(c => c.Prospects)
                .HasForeignKey(p => p.CampaignId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Video>(e =>
        {
            e.HasKey(v => v.Id);
            e.HasIndex(v => new { v.CampaignId, v.YouTubeVideoId }).IsUnique();
            e.HasOne(v => v.Campaign)
                .WithMany()
                .HasForeignKey(v => v.CampaignId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Comment>(e =>
        {
            e.HasKey(c => c.Id);
            e.HasIndex(c => new { c.VideoId, c.YouTubeCommentId }).IsUnique();
            e.HasOne(c => c.Video)
                .WithMany(v => v.Comments)
                .HasForeignKey(c => c.VideoId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
