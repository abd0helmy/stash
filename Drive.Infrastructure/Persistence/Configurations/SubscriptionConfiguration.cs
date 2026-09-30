using Drive.Core.Entities;
using Drive.Core.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Drive.Infrastructure.Persistence.Configurations;

public class SubscriptionConfiguration : IEntityTypeConfiguration<Subscription>
{
    public void Configure(EntityTypeBuilder<Subscription> builder)
    {
        builder.ToTable("Subscriptions");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.UserId)
            .IsRequired();

        builder.Property(s => s.PlanId)
            .IsRequired();

        builder.Property(s => s.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.Property(s => s.StartedAt)
            .IsRequired();

        builder.Property(s => s.CurrentPeriodStart)
            .IsRequired();

        builder.Property(s => s.CurrentPeriodEnd)
            .IsRequired();

        builder.Property(s => s.CancelAtPeriodEnd)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(s => s.CanceledAt)
            .IsRequired(false);

        builder.Property(s => s.StripeSubscriptionId)
            .IsRequired(false)
            .HasMaxLength(100);

        builder.Property(s => s.StripeCustomerId)
            .IsRequired(false)
            .HasMaxLength(100);

        builder.HasIndex(s => s.StripeSubscriptionId);

        builder.HasOne(s => s.User)
            .WithMany(u => u.Subscriptions)
            .HasForeignKey(s => s.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(s => s.Plan)
            .WithMany()
            .HasForeignKey(s => s.PlanId)
            .OnDelete(DeleteBehavior.Restrict);

        // One active subscription per user
        builder.HasIndex(s => s.UserId)
            .IsUnique()
            .HasFilter($"\"Status\" = '{nameof(SubscriptionStatus.Active)}'");

        builder.HasIndex(s => new { s.UserId, s.Status });
        builder.HasIndex(s => s.PlanId);
    }
}
