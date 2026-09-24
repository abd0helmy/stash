using Drive.Application.Interfaces;
using Drive.Core.Entities;
using Drive.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Drive.Infrastructure.Repositories;

public class RefreshTokenRepository(DriveDbContext dbContext)
    : IRefreshTokenRepository
{
    public async Task<RefreshToken?> GetByHashAsync(
        string tokenHash,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.RefreshTokens
            .FirstOrDefaultAsync(
                x => x.TokenHash == tokenHash,
                cancellationToken);
    }

    public async Task AddAsync(
        RefreshToken refreshToken,
        CancellationToken cancellationToken = default)
    {
        await dbContext.RefreshTokens.AddAsync(
            refreshToken,
            cancellationToken);
    }

    public Task RevokeAsync(
        RefreshToken refreshToken,
        CancellationToken cancellationToken = default)
    {
        refreshToken.RevokedAt = DateTime.UtcNow;

        dbContext.RefreshTokens.Update(refreshToken);

        return Task.CompletedTask;
    }
}