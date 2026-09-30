using Drive.Application.Billing.Subscriptions.DTOs;
using Drive.Core.Common.Result;
using Drive.Core.Entities;

namespace Drive.Application.Billing.Subscriptions.Interfaces;

public interface ISubscriptionService
{
    Task<Result<SubscriptionResponse>> GetActiveSubscriptionAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<Result<Subscription>> CreateDefaultFreeSubscriptionAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<Result<string>> CreateCheckoutSessionAsync(Guid userId, Guid planId, CancellationToken cancellationToken = default);

    Task<Result> CancelSubscriptionAsync(Guid userId, CancellationToken cancellationToken = default);
}
