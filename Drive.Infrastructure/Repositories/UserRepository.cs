using Drive.Application.Interfaces;
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
        return await context.Users.FirstOrDefaultAsync(user => user.Email == email, cancellationToken);
    }
    public async Task<User?> GetByIdentifierAsync(
        string identifier,
        CancellationToken cancellationToken = default)
    {
        return await context.Users
            .FirstOrDefaultAsync(
                u => u.Email == identifier || u.Username == identifier,
                cancellationToken);
    }

    public async Task<bool> IsEmailUniqueAsync(string email, CancellationToken cancellationToken = default)
    {
        return await context.Users.AnyAsync(user => user.Email == email, cancellationToken);
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