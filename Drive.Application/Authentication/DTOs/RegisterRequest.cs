namespace Drive.Application.Authentication.DTOs;

public record RegisterRequest(
    string Username,
    string Email,
    string Password
);