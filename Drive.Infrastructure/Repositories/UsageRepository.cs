using Drive.Application.Billing.Usage.Interfaces;
using Drive.Core.Entities;
using Drive.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Drive.Infrastructure.Repositories;

public class UsageRepository(DriveDbContext context) : IUsageRepository
{
    public async Task<Usage?> GetCurrentUsageAsync(
        Guid userId,
        DateTime periodStart,
        DateTime periodEnd,
        CancellationToken cancellationToken = default)
    {
        return await context.Usages
            .FirstOrDefaultAsync(
                u => u.UserId == userId && u.PeriodStart == periodStart && u.PeriodEnd == periodEnd,
                cancellationToken);
    }

    public async Task<Usage> GetOrCreateCurrentUsageAsync(
        Guid userId,
        DateTime periodStart,
        DateTime periodEnd,
        CancellationToken cancellationToken = default)
    {
        var existing = await context.Usages
            .FirstOrDefaultAsync(
                u => u.UserId == userId && u.PeriodStart == periodStart && u.PeriodEnd == periodEnd,
                cancellationToken);

        if (existing is not null)
        {
            return existing;
        }

        var newUsage = new Usage
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            PeriodStart = periodStart,
            PeriodEnd = periodEnd,
            StorageUsedBytes = 0,
            ApiRequests = 0,
            BandwidthUsedBytes = 0
        };

        try
        {
            await context.Usages.AddAsync(newUsage, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);
            return newUsage;
        }
        catch (DbUpdateException)
        {
            // Another concurrent request may have inserted the usage row first
            context.ChangeTracker.Clear();
            var reloaded = await context.Usages
                .FirstOrDefaultAsync(
                    u => u.UserId == userId && u.PeriodStart == periodStart && u.PeriodEnd == periodEnd,
                    cancellationToken);

            if (reloaded is not null)
            {
                return reloaded;
            }

            throw;
        }
    }

    public async Task AddAsync(Usage usage, CancellationToken cancellationToken = default)
    {
        await context.Usages.AddAsync(usage, cancellationToken);
    }

    public void Update(Usage usage)
    {
        context.Usages.Update(usage);
    }

    public async Task<bool> TryReserveStorageAsync(
        Guid usageId,
        long bytesToAdd,
        long storageLimitBytes,
        CancellationToken cancellationToken = default)
    {
        var rowsAffected = await context.Usages
            .Where(u => u.Id == usageId && (u.StorageUsedBytes + bytesToAdd) <= storageLimitBytes)
            .ExecuteUpdateAsync(s => s.SetProperty(u => u.StorageUsedBytes, u => u.StorageUsedBytes + bytesToAdd), cancellationToken);

        return rowsAffected > 0;
    }

    public async Task ReleaseStorageAsync(
        Guid usageId,
        long bytesToRelease,
        CancellationToken cancellationToken = default)
    {
        await context.Usages
            .Where(u => u.Id == usageId)
            .ExecuteUpdateAsync(
                s => s.SetProperty(u => u.StorageUsedBytes, u => u.StorageUsedBytes > bytesToRelease ? u.StorageUsedBytes - bytesToRelease : 0L),
                cancellationToken);
    }

    public async Task IncrementApiRequestsAsync(
        Guid usageId,
        CancellationToken cancellationToken = default)
    {
        await context.Usages
            .Where(u => u.Id == usageId)
            .ExecuteUpdateAsync(
                s => s.SetProperty(u => u.ApiRequests, u => u.ApiRequests + 1),
                cancellationToken);
    }
}
