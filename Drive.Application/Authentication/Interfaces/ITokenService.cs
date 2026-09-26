using Drive.Core.Common.Result;
using Drive.Core.Entities;

namespace Drive.Application.Authentication.Interfaces;

public interface ITokenService
{
    string GenerateAccessToken(User user);
    string GenerateRefreshToken();
    string HashRefreshToken(string token);

    Task<string> GenerateEmailVerificationTokenAsync(
        User user,
        CancellationToken cancellationToken = default);

    Task<Result<(Guid UserId, string Email)>> ValidateEmailVerificationTokenAsync(
        string token,
        CancellationToken cancellationToken = default);

    Task<string> GeneratePasswordResetTokenAsync(
        User user,
        CancellationToken cancellationToken = default);

    Task<Result<(Guid UserId, string Email)>> ValidatePasswordResetTokenAsync(
        string token,
        CancellationToken cancellationToken = default);
}