namespace Drive.Application.Interfaces;

public interface ICacheService
{
    Task SetAsync<T>(
        string key,
        T value,
        TimeSpan? expiry = null,
        CancellationToken cancellationToken = default);

    Task<T?> GetAsync<T>(
        string key,
        CancellationToken cancellationToken = default);

    Task<T?> GetAndDeleteAsync<T>(
        string key,
        CancellationToken cancellationToken = default);

    Task RemoveAsync(
        string key,
        CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(
        string key,
        CancellationToken cancellationToken = default);
}
