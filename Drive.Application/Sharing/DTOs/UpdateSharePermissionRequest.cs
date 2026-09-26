using Drive.Core.Enums;

namespace Drive.Application.Sharing.DTOs;

public record UpdateSharePermissionRequest(
    SharePermission Permission);
