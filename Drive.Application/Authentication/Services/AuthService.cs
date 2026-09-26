using System.ComponentModel.DataAnnotations;
using Drive.Application.Authentication.DTOs;
using Drive.Application.Authentication.Interfaces;
using Drive.Core.Common;
using Drive.Core.Common.Result;
using Drive.Core.Entities;
using Drive.Core.Enums;
using Drive.Application.Interfaces;

namespace Drive.Application.Authentication.Services;

public class AuthService(
    ITokenService tokenService,
    IPasswordHasher passwordHasher,
    IUserRepository userRepository,
    IRefreshTokenRepository refreshTokenRepository,
    IUnitOfWork unitOfWork,
    IEmailService emailService) : IAuthService
{
    public async Task<Result<RegisterResponse>> RegisterAsync(
        RegisterRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Email))
        {
            return Result<RegisterResponse>.Failure(
                Error.Validation("Auth.InvalidEmail", "Email is required."));
        }

        var normalizedEmail = request.Email.Trim().ToLowerInvariant();

        if (!new EmailAddressAttribute().IsValid(normalizedEmail))
        {
            return Result<RegisterResponse>.Failure(
                Error.Validation("Auth.InvalidEmail", "Email format is invalid."));
        }

        if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 6)
        {
            return Result<RegisterResponse>.Failure(
                Error.Validation("Auth.InvalidPassword", "Password must be at least 6 characters long."));
        }

        var existingUser = await userRepository.GetByEmailAsync(normalizedEmail, cancellationToken);

        if (existingUser is not null)
        {
            if (existingUser.IsEmailVerified)
            {
                return Result<RegisterResponse>.Failure(
                    Error.Conflict(
                        "Auth.EmailAlreadyExists",
                        "The email is already registered."));
            }

            // The email exists but was NEVER verified (e.g. unverified or malicious attempt).
            // Overwrite the unverified user with the new password
            existingUser.PasswordHash = passwordHasher.Hash(request.Password);
            existingUser.UpdatedAt = DateTime.UtcNow;

            userRepository.Update(existingUser);

            if (await unitOfWork.SaveChangesAsync(cancellationToken) == 0)
            {
                return Result<RegisterResponse>.Failure(Error.InternalServerError);
            }

            var existingUserToken = await tokenService.GenerateEmailVerificationTokenAsync(existingUser, cancellationToken);
            await emailService.SendVerificationEmailAsync(existingUser.Email, existingUserToken, cancellationToken);

            return Result<RegisterResponse>.Success(
                new RegisterResponse(existingUser.Id, existingUser.Email));
        }

        var user = new User
        {
            Email = normalizedEmail,
            PasswordHash = passwordHasher.Hash(request.Password),
            IsEmailVerified = false,
            Role = UserRole.User,
            CreatedAt = DateTime.UtcNow
        };

        await userRepository.AddAsync(user, cancellationToken);

        if (await unitOfWork.SaveChangesAsync(cancellationToken) == 0)
        {
            return Result<RegisterResponse>.Failure(
                Error.InternalServerError);
        }

        var newUserToken = await tokenService.GenerateEmailVerificationTokenAsync(user, cancellationToken);
        await emailService.SendVerificationEmailAsync(user.Email, newUserToken, cancellationToken);

        return Result<RegisterResponse>.Success(
            new RegisterResponse(user.Id, user.Email));
    }

    public async Task<Result<AuthResponse>> LoginAsync(LoginRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            return Result<AuthResponse>.Failure(Error.InvalidCredentials);
        }

        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var user = await userRepository.GetByEmailAsync(normalizedEmail, cancellationToken);

        if (user is null)
        {
            return Result<AuthResponse>.Failure(Error.InvalidCredentials);
        }

        var passwordMatch = passwordHasher.Verify(request.Password, user.PasswordHash);

        if (!passwordMatch)
        {
            return Result<AuthResponse>.Failure(Error.InvalidCredentials);
        }

        if (!user.IsEmailVerified)
        {
            return Result<AuthResponse>.Failure(Error.EmailNotVerified);
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

    public async Task<Result> VerifyEmailAsync(
        VerifyEmailRequest request,
        CancellationToken cancellationToken = default)
    {
        var tokenResult = await tokenService.ValidateEmailVerificationTokenAsync(request.Token, cancellationToken);

        if (tokenResult.IsFailure)
            return Result.Failure(tokenResult.Error!);

        var (userId, _) = tokenResult.Value!;

        var user = await userRepository.GetByIdAsync(userId, cancellationToken);

        if (user is null)
            return Result.Failure(Error.Validation("Auth.UserNotFound", "User not found."));

        if (user.IsEmailVerified)
            return Result.Success();

        user.IsEmailVerified = true;
        user.UpdatedAt = DateTime.UtcNow;
        userRepository.Update(user);

        if (await unitOfWork.SaveChangesAsync(cancellationToken) == 0)
            return Result.Failure(Error.InternalServerError);

        return Result.Success();
    }

    public async Task<Result> ResendVerificationEmailAsync(
        ResendVerificationEmailRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Email))
            return Result.Failure(Error.Validation("Auth.InvalidEmail", "Email is required."));

        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var user = await userRepository.GetByEmailAsync(normalizedEmail, cancellationToken);

        // Always return success to prevent email enumeration
        if (user is null || user.IsEmailVerified)
            return Result.Success();

        var verificationToken = await tokenService.GenerateEmailVerificationTokenAsync(user, cancellationToken);
        await emailService.SendVerificationEmailAsync(user.Email, verificationToken, cancellationToken);

        return Result.Success();
    }

    public async Task<Result> ForgotPasswordAsync(
        ForgotPasswordRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Email))
            return Result.Failure(Error.Validation("Auth.InvalidEmail", "Email is required."));

        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var user = await userRepository.GetByEmailAsync(normalizedEmail, cancellationToken);

        // Always return success to prevent email enumeration
        if (user is null || !user.IsEmailVerified)
            return Result.Success();

        var resetToken = await tokenService.GeneratePasswordResetTokenAsync(user, cancellationToken);
        await emailService.SendPasswordResetEmailAsync(user.Email, resetToken, cancellationToken);

        return Result.Success();
    }

    public async Task<Result> ResetPasswordAsync(
        ResetPasswordRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.NewPassword) || request.NewPassword.Length < 6)
            return Result.Failure(Error.Validation("Auth.InvalidPassword", "Password must be at least 6 characters long."));

        var tokenResult = await tokenService.ValidatePasswordResetTokenAsync(request.Token, cancellationToken);

        if (tokenResult.IsFailure)
            return Result.Failure(tokenResult.Error!);

        var (userId, _) = tokenResult.Value!;

        var user = await userRepository.GetByIdAsync(userId, cancellationToken);

        if (user is null)
            return Result.Failure(Error.Validation("Auth.UserNotFound", "User not found."));

        user.PasswordHash = passwordHasher.Hash(request.NewPassword);
        user.UpdatedAt = DateTime.UtcNow;
        userRepository.Update(user);

        // Revoke all existing refresh tokens for security
        await refreshTokenRepository.RevokeAllForUserAsync(user.Id, cancellationToken);

        if (await unitOfWork.SaveChangesAsync(cancellationToken) == 0)
            return Result.Failure(Error.InternalServerError);

        return Result.Success();
    }
}