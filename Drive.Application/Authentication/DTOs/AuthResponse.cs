namespace Drive.Application.Authentication.DTOs;

public record AuthResponse(
    string AccessToken,
    string RefreshToken
);