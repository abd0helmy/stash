using Drive.Application.Billing.Usage.DTOs;
using Drive.Core.Common.Result;

namespace Drive.Application.Billing.Usage.Interfaces;

public interface IUsageService
{
    Task<Result<UsageResponse>> GetCurrentUsageAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<Result> IncrementApiRequestsAsync(
        Guid userId,
        CancellationToken cancellationToken = default);
}
