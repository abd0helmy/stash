using Drive.Application.Interfaces;
using Drive.Application.Sharing.DTOs;
using Drive.Application.Sharing.Interfaces;
using Drive.Core.Common;
using Drive.Core.Common.Result;
using Drive.Core.Entities;
using Drive.Core.Enums;

namespace Drive.Application.Sharing.Services;

public class ItemShareService(IItemShareRepository repository, IUnitOfWork unitOfWork) : IItemShareService
{
    public async Task<Result<ShareResponse>> ShareAsync(
        Guid itemId,
        ShareableItemType itemType,
        Guid ownerId,
        Guid sharedWithUserId,
        SharePermission permission,
        DateTime? expiresAt,
        CancellationToken cancellationToken)
    {
        if (ownerId == sharedWithUserId)
        {
            return Result<ShareResponse>.Failure(
                Error.Validation(
                    "Share.CannotShareWithSelf",
                    "You cannot share an item with yourself."));
        }

        if (expiresAt.HasValue && expiresAt.Value <= DateTime.UtcNow)
        {
            return Result<ShareResponse>.Failure(
                Error.Validation(
                    "Share.InvalidExpiry",
                    "Expiration date must be in the future."));
        }

        var existing = await repository.GetByItemAndUserAsync(itemId, itemType, sharedWithUserId, cancellationToken);

        if (existing is not null)
        {
            if (!existing.IsRevoked)
            {
                return Result<ShareResponse>.Failure(
                    Error.Conflict(
                        "Share.AlreadyShared",
                        "This item is already shared with the specified user."));
            }

            // Re-activate a previously revoked share.
            existing.IsRevoked = false;
            existing.Permission = permission;
            existing.UpdatedAt = DateTime.UtcNow;
            existing.ExpiresAt = expiresAt;

            repository.Update(existing);

            if (await unitOfWork.SaveChangesAsync(cancellationToken) == 0)
                return Result<ShareResponse>.Failure(Error.InternalServerError);

            return Result<ShareResponse>.Success(MapToResponse(existing));
        }

        var share = new ItemShare
        {
            Id = Guid.NewGuid(),
            ItemId = itemId,
            ItemType = itemType,
            OwnerId = ownerId,
            SharedWithUserId = sharedWithUserId,
            Permission = permission,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            ExpiresAt = expiresAt,
            IsRevoked = false
        };

        await repository.AddAsync(share, cancellationToken);

        if (await unitOfWork.SaveChangesAsync(cancellationToken) == 0)
            return Result<ShareResponse>.Failure(Error.InternalServerError);

        return Result<ShareResponse>.Success(MapToResponse(share));
    }

    public async Task<Result> UpdatePermissionAsync(
        Guid shareId,
        Guid requestingUserId,
        SharePermission newPermission,
        CancellationToken cancellationToken)
    {
        var share = await repository.GetByIdAsync(shareId, cancellationToken);

        if (share is null)
        {
            return Result.Failure(
                Error.NotFound(
                    "Share.NotFound",
                    "The specified share was not found."));
        }

        if (share.OwnerId != requestingUserId)
        {
            return Result.Failure(
                Error.Forbidden(
                    "Share.Forbidden",
                    "You do not have permission to modify this share."));
        }

        if (share.IsRevoked)
        {
            return Result.Failure(
                Error.Validation(
                    "Share.Revoked",
                    "Cannot update permission on a revoked share."));
        }

        share.Permission = newPermission;
        share.UpdatedAt = DateTime.UtcNow;

        repository.Update(share);

        if (await unitOfWork.SaveChangesAsync(cancellationToken) == 0)
            return Result.Failure(Error.InternalServerError);

        return Result.Success();
    }

    public async Task<Result> RevokeAsync(
        Guid shareId,
        Guid requestingUserId,
        CancellationToken cancellationToken)
    {
        var share = await repository.GetByIdAsync(shareId, cancellationToken);

        if (share is null)
        {
            return Result.Failure(
                Error.NotFound(
                    "Share.NotFound",
                    "The specified share was not found."));
        }

        if (share.OwnerId != requestingUserId)
        {
            return Result.Failure(
                Error.Forbidden(
                    "Share.Forbidden",
                    "You do not have permission to revoke this share."));
        }

        if (share.IsRevoked)
            return Result.Success();

        share.IsRevoked = true;
        share.UpdatedAt = DateTime.UtcNow;

        repository.Update(share);

        if (await unitOfWork.SaveChangesAsync(cancellationToken) == 0)
            return Result.Failure(Error.InternalServerError);

        return Result.Success();
    }

    public async Task<Result<List<ShareResponse>>> GetSharedWithUserAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        var shares = await repository.GetSharedWithUserAsync(userId, cancellationToken);

        var activeShares = shares
            .Where(s => !s.IsRevoked && (s.ExpiresAt is null || s.ExpiresAt > DateTime.UtcNow))
            .Select(MapToResponse)
            .ToList();

        return Result<List<ShareResponse>>.Success(activeShares);
    }

    public async Task<Result<bool>> HasAccessAsync(
        Guid itemId,
        ShareableItemType itemType,
        Guid userId,
        CancellationToken cancellationToken)
    {
        var share = await repository.GetByItemAndUserAsync(itemId, itemType, userId, cancellationToken);

        if (share is null || share.IsRevoked)
            return Result<bool>.Success(false);

        if (share.ExpiresAt.HasValue && share.ExpiresAt <= DateTime.UtcNow)
            return Result<bool>.Success(false);

        return Result<bool>.Success(true);
    }

    private static ShareResponse MapToResponse(ItemShare share) =>
        new(
            share.Id,
            share.ItemId,
            share.ItemType,
            share.OwnerId,
            share.SharedWithUserId,
            share.Permission,
            share.CreatedAt,
            share.UpdatedAt,
            share.ExpiresAt,
            share.IsRevoked);
}