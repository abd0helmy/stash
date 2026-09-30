using Drive.Application.Billing.Subscriptions.Interfaces;
using Drive.Core.Entities;
using Drive.Core.Enums;
using Drive.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Drive.Infrastructure.Repositories;

public class SubscriptionRepository(DriveDbContext context) : ISubscriptionRepository
{
    public async Task<Subscription?> GetActiveByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await context.Subscriptions
            .Include(s => s.Plan)
            .FirstOrDefaultAsync(
                s => s.UserId == userId && (s.Status == SubscriptionStatus.Active || s.Status == SubscriptionStatus.Trialing),
                cancellationToken);
    }

    public async Task<IEnumerable<Subscription>> GetAllByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await context.Subscriptions
            .Include(s => s.Plan)
            .Where(s => s.UserId == userId)
            .OrderByDescending(s => s.StartedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<Subscription?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await context.Subscriptions
            .Include(s => s.Plan)
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
    }

    public async Task<Subscription?> GetByStripeSubscriptionIdAsync(string stripeSubscriptionId, CancellationToken cancellationToken = default)
    {
        return await context.Subscriptions
            .Include(s => s.Plan)
            .FirstOrDefaultAsync(s => s.StripeSubscriptionId == stripeSubscriptionId, cancellationToken);
    }

    public async Task AddAsync(Subscription subscription, CancellationToken cancellationToken = default)
    {
        await context.Subscriptions.AddAsync(subscription, cancellationToken);
    }

    public void Update(Subscription subscription)
    {
        context.Subscriptions.Update(subscription);
    }
}
