using Drive.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Drive.Infrastructure.Persistence.Configurations;

public class PlanConfiguration : IEntityTypeConfiguration<Plan>
{
    public void Configure(EntityTypeBuilder<Plan> builder)
    {
        builder.ToTable("Plans");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Name)
            .IsRequired()
            .HasMaxLength(50);

        builder.HasIndex(p => p.Name)
            .IsUnique();

        builder.Property(p => p.Price)
            .IsRequired()
            .HasPrecision(18, 2);

        builder.Property(p => p.Currency)
            .IsRequired()
            .HasMaxLength(10);

        builder.Property(p => p.BillingInterval)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(p => p.StorageLimitBytes)
            .IsRequired();

        builder.Property(p => p.MonthlyRequestLimit)
            .IsRequired();

        builder.Property(p => p.MaxFileSizeBytes)
            .IsRequired();

        builder.Property(p => p.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(p => p.CreatedAt)
            .IsRequired();

        builder.Property(p => p.UpdatedAt)
            .IsRequired(false);

        builder.Property(p => p.StripeProductId)
            .IsRequired(false)
            .HasMaxLength(100);

        builder.Property(p => p.StripePriceId)
            .IsRequired(false)
            .HasMaxLength(100);

        // Seed initial plans
        builder.HasData(DefaultPlans.All);
    }
}
