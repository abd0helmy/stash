using Drive.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Drive.Infrastructure.Persistence.Configurations;

public class UsageConfiguration : IEntityTypeConfiguration<Usage>
{
    public void Configure(EntityTypeBuilder<Usage> builder)
    {
        builder.ToTable("Usages");

        builder.HasKey(u => u.Id);

        builder.Property(u => u.UserId)
            .IsRequired();

        builder.Property(u => u.PeriodStart)
            .IsRequired();

        builder.Property(u => u.PeriodEnd)
            .IsRequired();

        builder.Property(u => u.StorageUsedBytes)
            .IsRequired()
            .HasDefaultValue(0L);

        builder.Property(u => u.ApiRequests)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(u => u.BandwidthUsedBytes)
            .IsRequired()
            .HasDefaultValue(0L);

        builder.HasOne(u => u.User)
            .WithMany(u => u.Usages)
            .HasForeignKey(u => u.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Unique usage per user + billing period
        builder.HasIndex(u => new { u.UserId, u.PeriodStart, u.PeriodEnd })
            .IsUnique();

        builder.HasIndex(u => new { u.UserId, u.PeriodStart });
    }
}
