namespace Drive.Application.Authentication.DTOs;


public record RegisterResponse(
    Guid UserId,
    string Email);