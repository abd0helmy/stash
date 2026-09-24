using Drive.Application.Authentication.DTOs;
using Drive.Core.Common.Result;
namespace Drive.Application.Authentication.Interfaces;

public interface IAuthService
{
    Task<Result<AuthResponse>> RegisterAsync(
        RegisterRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<AuthResponse>> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<AuthResponse>> RefreshTokenAsync(
        string refreshToken,
        CancellationToken cancellationToken = default);

    Task<Result> LogoutAsync(
        string refreshToken,
        CancellationToken cancellationToken = default);
}