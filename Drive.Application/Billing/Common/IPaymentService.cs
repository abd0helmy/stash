using Drive.Core.Common.Result;

namespace Drive.Application.Billing.Common;

public interface IPaymentService
{
    Task<Result<string>> CreateCheckoutSessionAsync(
        Guid userId,
        Guid planId,
        CancellationToken cancellationToken = default);

    Task<Result> CancelSubscriptionAsync(
        Guid userId,
        CancellationToken cancellationToken = default);
}
