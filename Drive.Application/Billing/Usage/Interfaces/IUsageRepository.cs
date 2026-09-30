using Drive.Core.Entities;

namespace Drive.Application.Billing.Usage.Interfaces;

public interface IUsageRepository
{
    Task<Core.Entities.Usage?> GetCurrentUsageAsync(
        Guid userId,
        DateTime periodStart,
        DateTime periodEnd,
        CancellationToken cancellationToken = default);

    Task<Core.Entities.Usage> GetOrCreateCurrentUsageAsync(
        Guid userId,
        DateTime periodStart,
        DateTime periodEnd,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        Core.Entities.Usage usage,
        CancellationToken cancellationToken = default);

    void Update(Core.Entities.Usage usage);

    Task<bool> TryReserveStorageAsync(
        Guid usageId,
        long bytesToAdd,
        long storageLimitBytes,
        CancellationToken cancellationToken = default);

    Task ReleaseStorageAsync(
        Guid usageId,
        long bytesToRelease,
        CancellationToken cancellationToken = default);

    Task IncrementApiRequestsAsync(
        Guid usageId,
        CancellationToken cancellationToken = default);
}
