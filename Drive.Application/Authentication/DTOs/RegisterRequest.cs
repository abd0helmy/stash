namespace Drive.Application.Authentication.DTOs;

public record RegisterRequest(
    string Email,
    string Password
);