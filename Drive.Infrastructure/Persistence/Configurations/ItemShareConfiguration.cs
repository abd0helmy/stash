using Drive.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Drive.Infrastructure.Persistence.Configurations;

public class ItemShareConfiguration : IEntityTypeConfiguration<ItemShare>
{
    public void Configure(EntityTypeBuilder<ItemShare> builder)
    {
        builder.ToTable("ItemShares");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.ItemType)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(x => x.Permission)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.Property(x => x.UpdatedAt)
            .IsRequired();

        // Fast lookup: "who has access to this item"
        builder.HasIndex(x => new { x.ItemId, x.ItemType });

        // Fast lookup: "what's shared with this user"
        builder.HasIndex(x => x.SharedWithUserId);

        // Prevent sharing the same item with the same user twice
        builder.HasIndex(x => new { x.ItemId, x.ItemType, x.SharedWithUserId })
            .IsUnique();
    }
}