using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using File = Drive.Core.Entities.File;

namespace Drive.Infrastructure.Persistence.Configurations;

public class FileConfiguration : IEntityTypeConfiguration<File>
{
    public void Configure(EntityTypeBuilder<File> builder)
    {
        builder.HasKey(f => f.Id);

        builder.Property(f => f.Name)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(f => f.ObjectKey)
            .IsRequired()
            .HasMaxLength(1024);

        builder.Property(f => f.MimeType)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(f => f.Size)
            .IsRequired();

        builder.Property(f => f.OwnerId)
            .IsRequired();

        builder.Property(f => f.CreatedAt)
            .IsRequired();

        builder.Property(f => f.UpdatedAt)
            .IsRequired();

        builder.HasOne(f => f.Owner)
            .WithMany()
            .HasForeignKey(f => f.OwnerId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(f => f.Folder)
            .WithMany()
            .HasForeignKey(f => f.FolderId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Property(f => f.DeletedAt)
            .IsRequired(false);

        builder.HasQueryFilter(f => f.DeletedAt == null);

        builder.HasIndex(f => new { f.OwnerId, f.FolderId });

        builder.HasIndex(f => f.ObjectKey)
            .IsUnique();
    }
}
