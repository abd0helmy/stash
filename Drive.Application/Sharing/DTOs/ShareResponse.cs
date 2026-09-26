using Drive.Core.Enums;

namespace Drive.Application.Sharing.DTOs;

public record ShareResponse(
    Guid Id,
    Guid ItemId,
    ShareableItemType ItemType,
    Guid OwnerId,
    Guid SharedWithUserId,
    SharePermission Permission,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    DateTime? ExpiresAt,
    bool IsRevoked);
