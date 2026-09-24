namespace Drive.Application.Authentication.DTOs;

public record LoginRequest(
    string Identifier,
    string Password
);