namespace Drive.Application.Authentication.DTOs;

public record ResetPasswordRequest(
    string Token,
    string NewPassword
);
