using Drive.Application.Authentication.DTOs;
using Drive.Application.Authentication.Interfaces;
using Drive.Core.Common;
using Drive.Core.Common.Result;
using Drive.Core.Entities;
using Drive.Application.Interfaces;

namespace Drive.Application.Authentication.Services;

public class AuthService(
    ITokenService tokenService,
    IPasswordHasher passwordHasher,
    IUserRepository userRepository,
    IRefreshTokenRepository refreshTokenRepository,
    IUnitOfWork unitOfWork) : IAuthService
{
    public async Task<Result<AuthResponse>> RegisterAsync(
        RegisterRequest request,
        CancellationToken cancellationToken = default)
    {
        var isEmailUnique = await userRepository.IsEmailUniqueAsync(
            request.Email,
            cancellationToken);

        if (!isEmailUnique)
        {
            return Result<AuthResponse>.Failure(
                Error.Conflict(
                    "Auth.EmailAlreadyExists",
                    "The email is already registered."));
        }

        var user = new User
        {
            Email = request.Email,
            IsEmailVerified = false,
            Username = request.Username,
            PasswordHash = passwordHasher.Hash(request.Password)
        };

        var refreshTokenString = tokenService.GenerateRefreshToken();
        var accessTokenString = tokenService.GenerateAccessToken(user);

        var refreshToken = new RefreshToken
        {
            TokenHash = tokenService.HashRefreshToken(refreshTokenString),
            UserId = user.Id
        };

        await userRepository.AddAsync(user, cancellationToken);
        await refreshTokenRepository.AddAsync(refreshToken, cancellationToken);

        if (await unitOfWork.SaveChangesAsync(cancellationToken) == 0)
        {
            return Result<AuthResponse>.Failure(
                Error.InternalServerError);
        }

        return Result<AuthResponse>.Success(
            new AuthResponse(
                accessTokenString,
                refreshTokenString));
    }

    public async Task<Result<AuthResponse>> LoginAsync(LoginRequest request,
        CancellationToken cancellationToken = default)
    {
        var user = await userRepository.GetByIdentifierAsync(request.Identifier, cancellationToken);

        if (user is null)
        {
            return Result<AuthResponse>.Failure(Error.InvalidCredentials);
        }

        var passwordMatch = passwordHasher.Verify(request.Password, user.PasswordHash);

        if (!passwordMatch)
        {
            return Result<AuthResponse>.Failure(Error.InvalidCredentials);
        }

        var accessTokenString = tokenService.GenerateAccessToken(user);
        var refreshTokenString = tokenService.GenerateRefreshToken();

        var refreshToken = new RefreshToken()
        {
            TokenHash = tokenService.HashRefreshToken(refreshTokenString),
            UserId = user.Id
        };

        await refreshTokenRepository.AddAsync(refreshToken, cancellationToken);

        if (await unitOfWork.SaveChangesAsync(cancellationToken) == 0)
            return Result<AuthResponse>.Failure(Error.InternalServerError);

        return Result<AuthResponse>.Success(new AuthResponse(accessTokenString, refreshTokenString));
    }

    public async Task<Result<AuthResponse>> RefreshTokenAsync(
        string refreshToken,
        CancellationToken cancellationToken = default)
    {
        var existedRefreshToken =
            await refreshTokenRepository.GetByHashAsync(
                tokenService.HashRefreshToken(refreshToken),
                cancellationToken);

        if (existedRefreshToken is null ||
            existedRefreshToken.RevokedAt is not null ||
            existedRefreshToken.ExpiresAt <= DateTime.UtcNow)
        {
            return Result<AuthResponse>.Failure(
                Error.InvalidRefreshToken);
        }

        var user = await userRepository.GetByIdAsync(
            existedRefreshToken.UserId,
            cancellationToken);

        if (user is null)
        {
            return Result<AuthResponse>.Failure(new Error("Auth.UserNotFound",
                "The user associated with the refresh token was not found.",
                ErrorType.Unauthenticated));
        }

        await refreshTokenRepository.RevokeAsync(existedRefreshToken, cancellationToken);

        var refreshTokenString = tokenService.GenerateRefreshToken();

        var newRefreshToken = new RefreshToken
        {
            TokenHash = tokenService.HashRefreshToken(
                refreshTokenString),
            UserId = user.Id
        };

        await refreshTokenRepository.AddAsync(
            newRefreshToken,
            cancellationToken);

        var accessTokenString =
            tokenService.GenerateAccessToken(user);

        if (await unitOfWork.SaveChangesAsync(cancellationToken) == 0)
        {
            return Result<AuthResponse>.Failure(
                Error.InternalServerError);
        }

        return Result<AuthResponse>.Success(
            new AuthResponse(
                accessTokenString,
                refreshTokenString));
    }

    public async Task<Result> LogoutAsync(
        string refreshToken,
        CancellationToken cancellationToken = default)
    {
        var refreshTokenEntity =
            await refreshTokenRepository.GetByHashAsync(
                tokenService.HashRefreshToken(refreshToken),
                cancellationToken);

        if (refreshTokenEntity is null)
        {
            return Result.Failure(Error.InvalidRefreshToken);
        }

        if (refreshTokenEntity.RevokedAt is not null)
        {
            return Result.Success();
        }

        refreshTokenEntity.RevokedAt = DateTime.UtcNow;

        if (await unitOfWork.SaveChangesAsync(cancellationToken) == 0)
        {
            return Result.Failure(Error.InternalServerError);
        }

        return Result.Success();
    }
}