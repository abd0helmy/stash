using Drive.Core.Entities;
using Drive.Core.Enums;

namespace Drive.Application.Sharing.Interfaces;

public interface IItemShareRepository
{
    Task<ItemShare?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<ItemShare?> GetByItemAndUserAsync(Guid itemId, ShareableItemType itemType, Guid sharedWithUserId,
        CancellationToken cancellationToken = default);

    Task<List<ItemShare>> GetByItemAsync(Guid itemId, ShareableItemType itemType,
        CancellationToken cancellationToken = default);

    Task<List<ItemShare>> GetSharedWithUserAsync(Guid userId, CancellationToken cancellationToken = default);

    Task AddAsync(ItemShare itemShare, CancellationToken cancellationToken = default);

    void Update(ItemShare itemShare);

    void Remove(ItemShare itemShare);
}