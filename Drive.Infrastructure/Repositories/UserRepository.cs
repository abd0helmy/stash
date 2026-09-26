using Drive.Application.Authentication.Interfaces;
using Drive.Core.Entities;
using Drive.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Drive.Infrastructure.Repositories;

public class UserRepository(DriveDbContext context) : IUserRepository
{
    public async Task<User?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await context.Users.FindAsync(
            [id],
            cancellationToken);
    }

    public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        var normalized = email.Trim().ToLower();
        return await context.Users.FirstOrDefaultAsync(user => user.Email.ToLower() == normalized, cancellationToken);
    }

    public async Task<bool> IsEmailAvailableAsync(string email, CancellationToken cancellationToken = default)
    {
        var normalized = email.Trim().ToLower();
        return !await context.Users.AnyAsync(u => u.Email.ToLower() == normalized, cancellationToken);
    }



    public async Task<bool> IsEmailUniqueAsync(string email, CancellationToken cancellationToken = default)
    {
        return await IsEmailAvailableAsync(email, cancellationToken);
    }

    public async Task AddAsync(User user, CancellationToken cancellationToken = default)
    {
        await context.Users.AddAsync(user, cancellationToken);
    }

    public void Update(User user)
    {
        context.Users.Update(user);
    }

    public void Delete(User user)
    {
        context.Users.Remove(user);
    }
}