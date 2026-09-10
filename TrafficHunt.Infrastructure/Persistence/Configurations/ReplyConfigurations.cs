using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TrafficHunt.Domain.Entities;

namespace TrafficHunt.Infrastructure.Persistence.Configurations;

public class ReplyCampaignConfiguration : IEntityTypeConfiguration<ReplyCampaign>
{
    public void Configure(EntityTypeBuilder<ReplyCampaign> builder)
    {
        builder.ToTable("ReplyCampaigns");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(x => x.MinDelaySeconds).HasDefaultValue(120);
        builder.Property(x => x.MaxDelaySeconds).HasDefaultValue(180);

        builder.HasOne(x => x.Campaign)
            .WithMany()
            .HasForeignKey(x => x.CampaignId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.Templates)
            .WithOne(t => t.ReplyCampaign)
            .HasForeignKey(t => t.ReplyCampaignId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.ReplyRecords)
            .WithOne(r => r.ReplyCampaign)
            .HasForeignKey(r => r.ReplyCampaignId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class ReplyTemplateConfiguration : IEntityTypeConfiguration<ReplyTemplate>
{
    public void Configure(EntityTypeBuilder<ReplyTemplate> builder)
    {
        builder.ToTable("ReplyTemplates");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Body).IsRequired();
    }
}

public class ReplyRecordConfiguration : IEntityTypeConfiguration<ReplyRecord>
{
    public void Configure(EntityTypeBuilder<ReplyRecord> builder)
    {
        builder.ToTable("ReplyRecords");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(x => x.MessageSent).IsRequired();
        builder.Property(x => x.ErrorMessage).HasMaxLength(1000);

        builder.HasIndex(x => new { x.ReplyCampaignId, x.Status });
        builder.HasIndex(x => x.ProspectId);

        builder.HasOne(x => x.Template)
            .WithMany()
            .HasForeignKey(x => x.TemplateId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
