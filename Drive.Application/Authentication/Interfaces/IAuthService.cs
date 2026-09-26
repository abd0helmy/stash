using Drive.Application.Authentication.DTOs;
using Drive.Core.Common.Result;
namespace Drive.Application.Authentication.Interfaces;

public interface IAuthService
{
    Task<Result<RegisterResponse>> RegisterAsync(
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

    Task<Result> VerifyEmailAsync(
        VerifyEmailRequest request,
        CancellationToken cancellationToken = default);

    Task<Result> ResendVerificationEmailAsync(
        ResendVerificationEmailRequest request,
        CancellationToken cancellationToken = default);

    Task<Result> ForgotPasswordAsync(
        ForgotPasswordRequest request,
        CancellationToken cancellationToken = default);

    Task<Result> ResetPasswordAsync(
        ResetPasswordRequest request,
        CancellationToken cancellationToken = default);
}