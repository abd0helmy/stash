using Drive.Api.Contracts.Users;
using Drive.Application.Authentication.Interfaces;
using Drive.Application.Interfaces;
using Drive.Core.Common;
using Drive.Core.Common.Result;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Drive.Api.Controllers;

[Authorize]
public class UsersController(
    IUserRepository userRepository,
    IPasswordHasher passwordHasher,
    IUnitOfWork unitOfWork) : BaseApiController
{
    [HttpGet("me")]
    public async Task<IActionResult> GetProfile(CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(CurrentUserId, cancellationToken);
        if (user is null)
        {
            return HandleResult(Result<UserProfileResponse>.Failure(
                Error.NotFound("User.NotFound", "User profile was not found.")));
        }

        var response = new UserProfileResponse(
            user.Id,
            user.Email,
            user.Role.ToString(),
            user.IsEmailVerified,
            user.CreatedAt);

        return HandleResult(Result<UserProfileResponse>.Success(response));
    }

    [HttpPatch("password")]
    public async Task<IActionResult> ChangePassword(
        [FromBody] ChangePasswordRequest request,
        CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(CurrentUserId, cancellationToken);
        if (user is null)
        {
            return HandleResult(Result.Failure(
                Error.NotFound("User.NotFound", "User profile was not found.")));
        }

        if (!passwordHasher.Verify(user.PasswordHash, request.CurrentPassword))
        {
            return HandleResult(Result.Failure(
                Error.Validation("Auth.InvalidCurrentPassword", "Current password does not match.")));
        }

        user.PasswordHash = passwordHasher.Hash(request.NewPassword);
        user.UpdatedAt = DateTime.UtcNow;
        userRepository.Update(user);

        if (await unitOfWork.SaveChangesAsync(cancellationToken) == 0)
        {
            return HandleResult(Result.Failure(Error.InternalServerError));
        }

        return HandleResult(Result.Success());
    }
}
