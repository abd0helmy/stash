using Drive.Core.Common.Result;

namespace Drive.Application.Billing.Common;

public interface IQuotaService
{
    Task<Result> CanUploadAsync(
        Guid userId,
        long fileSize,
        CancellationToken cancellationToken = default);

    Task<Result> CanMakeRequestAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<Result> CanUseFeatureAsync(
        Guid userId,
        string feature,
        CancellationToken cancellationToken = default);

    Task<Result> ReserveStorageAsync(
        Guid userId,
        long fileSize,
        CancellationToken cancellationToken = default);

    Task ReleaseStorageAsync(
        Guid userId,
        long fileSize,
        CancellationToken cancellationToken = default);
}
