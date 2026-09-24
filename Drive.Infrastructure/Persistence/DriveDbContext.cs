using Drive.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace Drive.Infrastructure.Persistence;

public class DriveDbContext(DbContextOptions<DriveDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Folder> Folders => Set<Folder>();


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(DriveDbContext).Assembly);
    }
}