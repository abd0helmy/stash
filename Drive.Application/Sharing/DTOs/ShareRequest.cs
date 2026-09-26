using Drive.Core.Enums;

namespace Drive.Application.Sharing.DTOs;

public record ShareRequest(
    Guid ItemId,
    ShareableItemType ItemType,
    Guid SharedWithUserId,
    SharePermission Permission,
    DateTime? ExpiresAt);
