using Drive.Core.Entities;

namespace Drive.Application.Billing.Subscriptions.Interfaces;

public interface ISubscriptionRepository
{
    Task<Subscription?> GetActiveByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<IEnumerable<Subscription>> GetAllByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<Subscription?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Subscription?> GetByStripeSubscriptionIdAsync(string stripeSubscriptionId, CancellationToken cancellationToken = default);

    Task AddAsync(Subscription subscription, CancellationToken cancellationToken = default);

    void Update(Subscription subscription);
}
