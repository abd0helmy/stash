using Drive.Core.Entities;

namespace Drive.Core.Interfaces;

public interface IRefreshTokenRepository
{
    Task<RefreshToken?> GetByHashAsync(
        string tokenHash,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        RefreshToken refreshToken,
        CancellationToken cancellationToken = default);

    Task RevokeAsync(
        RefreshToken refreshToken,
        CancellationToken cancellationToken = default);
}