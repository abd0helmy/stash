using Drive.Application.Authentication.Interfaces;
using Drive.Core.Entities;
using Microsoft.AspNetCore.Identity;

namespace Drive.Infrastructure.Identity;

public class PasswordHasher(IPasswordHasher<User> passwordHasher) : IPasswordHasher
{
    public string Hash(string password)
    {
        var user = new User();

        return passwordHasher.HashPassword(user, password);
    }

    public bool Verify(string password, string hash)
    {
        var user = new User();

        var result = passwordHasher.VerifyHashedPassword(user, hash, password);

        return result is PasswordVerificationResult.Success or PasswordVerificationResult.SuccessRehashNeeded;
    }
}