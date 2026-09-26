using Drive.Application.Interfaces;
using Drive.Application.Sharing.Interfaces;
using Drive.Core.Entities;
using Drive.Core.Enums;
using Drive.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Drive.Infrastructure.Repositories;

public class ItemShareRepository(DriveDbContext context) : IItemShareRepository
{
    public async Task<ItemShare?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await context.ItemShares.FindAsync([id], cancellationToken);
    }

    public async Task<ItemShare?> GetByItemAndUserAsync(Guid itemId, ShareableItemType itemType, Guid sharedWithUserId,
        CancellationToken cancellationToken = default)
    {
        return await context.ItemShares.FirstOrDefaultAsync(itemShare =>
                itemShare.ItemId == itemId && itemShare.ItemType == itemType &&
                itemShare.SharedWithUserId == sharedWithUserId,
            cancellationToken);
    }

    public async Task<List<ItemShare>> GetByItemAsync(Guid itemId, ShareableItemType itemType,
        CancellationToken cancellationToken = default)
    {
        return await context.ItemShares.Where(itemShare => itemShare.ItemId == itemId && itemShare.ItemType == itemType)
            .ToListAsync<ItemShare>(cancellationToken);
    }

    public async Task<List<ItemShare>> GetSharedWithUserAsync(Guid userId,
        CancellationToken cancellationToken = default)
    {
        return await context.ItemShares.Where(itemShare => itemShare.SharedWithUserId == userId)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(ItemShare itemShare, CancellationToken cancellationToken = default)
    {
        await context.ItemShares.AddAsync(itemShare, cancellationToken);
    }

    public void Update(ItemShare itemShare)
    {
        context.Update(itemShare);
    }

    public void Remove(ItemShare itemShare)
    {
        context.Remove(itemShare);
    }
}