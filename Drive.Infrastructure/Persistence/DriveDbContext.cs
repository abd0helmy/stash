using Drive.Core.Entities;
using Microsoft.EntityFrameworkCore;
using File = Drive.Core.Entities.File;

namespace Drive.Infrastructure.Persistence;

public class DriveDbContext(DbContextOptions<DriveDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Folder> Folders => Set<Folder>();
    public DbSet<File> Files => Set<File>();
    public DbSet<ItemShare> ItemShares => Set<ItemShare>();
    public DbSet<Plan> Plans => Set<Plan>();
    public DbSet<Subscription> Subscriptions => Set<Subscription>();
    public DbSet<Usage> Usages => Set<Usage>();


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(DriveDbContext).Assembly);
    }
}