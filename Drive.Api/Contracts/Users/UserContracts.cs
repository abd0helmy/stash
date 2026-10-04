using System.ComponentModel.DataAnnotations;

namespace Drive.Api.Contracts.Users;

public record UserProfileResponse(
    Guid Id,
    string Email,
    string Role,
    bool IsEmailVerified,
    DateTime CreatedAt
);

public record ChangePasswordRequest(
    [Required] string CurrentPassword,
    [Required][MinLength(8)] string NewPassword
);
