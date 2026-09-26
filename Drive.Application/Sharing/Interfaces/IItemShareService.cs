using Drive.Application.Sharing.DTOs;
using Drive.Core.Common.Result;
using Drive.Core.Enums;

namespace Drive.Application.Sharing.Interfaces;

public interface IItemShareService
{
    Task<Result<ShareResponse>> ShareAsync(
        Guid itemId,
        ShareableItemType itemType,
        Guid ownerId,
        Guid sharedWithUserId,
        SharePermission permission,
        DateTime? expiresAt,
        CancellationToken cancellationToken);

    Task<Result> UpdatePermissionAsync(Guid shareId, Guid requestingUserId, SharePermission newPermission,
        CancellationToken cancellationToken);

    Task<Result> RevokeAsync(Guid shareId, Guid requestingUserId, CancellationToken cancellationToken);

    Task<Result<List<ShareResponse>>> GetSharedWithUserAsync(Guid userId, CancellationToken cancellationToken);

    Task<Result<bool>> HasAccessAsync(Guid itemId, ShareableItemType itemType, Guid userId, CancellationToken
        cancellationToken);
}