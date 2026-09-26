using Drive.Core.Entities;

namespace Drive.Application.Authentication.Interfaces;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<User?> GetByEmailAsync(
        string email,
        CancellationToken cancellationToken = default);

    Task<bool> IsEmailAvailableAsync(
        string email,
        CancellationToken cancellationToken = default);



    Task<bool> IsEmailUniqueAsync(
        string email,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        User user,
        CancellationToken cancellationToken = default);

    void Update(User user);

    void Delete(User user);
}