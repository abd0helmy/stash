using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Drive.Application.Authentication.Interfaces;
using Drive.Application.Authentication.Options;
using Drive.Application.Interfaces;
using Drive.Core.Common;
using Drive.Core.Common.Result;
using Drive.Core.Entities;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Drive.Infrastructure.Identity;

public class TokenService(
    IOptions<JwtOptions> options,
    ICacheService cacheService) : ITokenService
{
    public string GenerateAccessToken(User user)
    {
        var settings = options.Value;

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(ClaimTypes.Role, user.Role.ToString())
        };

        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(settings.SecretKey));

        var credentials = new SigningCredentials(
            key,
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: settings.Issuer,
            audience: settings.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(
                settings.ExpirationInMinutes),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string GenerateRefreshToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(64);
        return WebEncoders.Base64UrlEncode(bytes);
    }

    public string HashRefreshToken(string token)
    {
        var hashBytes = SHA256.HashData(
            Encoding.UTF8.GetBytes(token));

        return Convert.ToHexString(hashBytes);
    }

    public async Task<string> GenerateEmailVerificationTokenAsync(
        User user,
        CancellationToken cancellationToken = default)
    {
        var userKey = $"user_verification:{user.Id}";
        var existingToken = await cacheService.GetAsync<string>(userKey, cancellationToken);
        if (!string.IsNullOrEmpty(existingToken))
        {
            await cacheService.RemoveAsync($"email_verification:{existingToken}", cancellationToken);
        }

        var tokenBytes = RandomNumberGenerator.GetBytes(32);
        var token = WebEncoders.Base64UrlEncode(tokenBytes);

        var tokenData = new VerificationTokenData(user.Id, user.Email);
        var expiry = TimeSpan.FromHours(24);

        await cacheService.SetAsync($"email_verification:{token}", tokenData, expiry, cancellationToken);
        await cacheService.SetAsync(userKey, token, expiry, cancellationToken);

        return token;
    }

    public async Task<Result<(Guid UserId, string Email)>> ValidateEmailVerificationTokenAsync(
        string token,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return Result<(Guid UserId, string Email)>.Failure(
                Error.Validation("Auth.InvalidToken", "Token is required."));
        }

        var tokenKey = $"email_verification:{token}";
        var tokenData = await cacheService.GetAndDeleteAsync<VerificationTokenData>(tokenKey, cancellationToken);

        if (tokenData is null)
        {
            return Result<(Guid UserId, string Email)>.Failure(
                Error.Validation("Auth.InvalidToken", "The token is invalid or has expired."));
        }

        await cacheService.RemoveAsync($"user_verification:{tokenData.UserId}", cancellationToken);

        return Result<(Guid UserId, string Email)>.Success((tokenData.UserId, tokenData.Email));
    }

    public async Task<string> GeneratePasswordResetTokenAsync(
        User user,
        CancellationToken cancellationToken = default)
    {
        var userKey = $"user_password_reset:{user.Id}";
        var existingToken = await cacheService.GetAsync<string>(userKey, cancellationToken);
        if (!string.IsNullOrEmpty(existingToken))
        {
            await cacheService.RemoveAsync($"password_reset:{existingToken}", cancellationToken);
        }

        var tokenBytes = RandomNumberGenerator.GetBytes(32);
        var token = WebEncoders.Base64UrlEncode(tokenBytes);

        var tokenData = new PasswordResetTokenData(user.Id, user.Email);
        var expiry = TimeSpan.FromHours(1);

        await cacheService.SetAsync($"password_reset:{token}", tokenData, expiry, cancellationToken);
        await cacheService.SetAsync(userKey, token, expiry, cancellationToken);

        return token;
    }

    public async Task<Result<(Guid UserId, string Email)>> ValidatePasswordResetTokenAsync(
        string token,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return Result<(Guid UserId, string Email)>.Failure(
                Error.Validation("Auth.InvalidToken", "Token is required."));
        }

        var tokenKey = $"password_reset:{token}";
        var tokenData = await cacheService.GetAndDeleteAsync<PasswordResetTokenData>(tokenKey, cancellationToken);

        if (tokenData is null)
        {
            return Result<(Guid UserId, string Email)>.Failure(
                Error.Validation("Auth.InvalidToken", "The token is invalid or has expired."));
        }

        await cacheService.RemoveAsync($"user_password_reset:{tokenData.UserId}", cancellationToken);

        return Result<(Guid UserId, string Email)>.Success((tokenData.UserId, tokenData.Email));
    }
}

public record VerificationTokenData(Guid UserId, string Email);
public record PasswordResetTokenData(Guid UserId, string Email);