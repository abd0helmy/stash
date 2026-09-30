using Drive.Application.Billing.Common;
using Drive.Application.Billing.Plans.Interfaces;
using Drive.Application.Billing.Subscriptions.DTOs;
using Drive.Application.Billing.Subscriptions.Interfaces;
using Drive.Application.Billing.Usage.Interfaces;
using Drive.Application.Interfaces;
using Drive.Core.Common;
using Drive.Core.Common.Result;
using Drive.Core.Entities;
using Drive.Core.Enums;

namespace Drive.Application.Billing.Subscriptions.Services;

public class SubscriptionService(
    ISubscriptionRepository subscriptionRepository,
    IPlanRepository planRepository,
    IUsageRepository usageRepository,
    IUnitOfWork unitOfWork,
    IPaymentService paymentService) : ISubscriptionService
{
    public async Task<Result<SubscriptionResponse>> GetActiveSubscriptionAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var subscription = await subscriptionRepository.GetActiveByUserIdAsync(userId, cancellationToken);
        if (subscription is null)
        {
            var createResult = await CreateDefaultFreeSubscriptionAsync(userId, cancellationToken);
            if (createResult.IsFailure)
            {
                return Result<SubscriptionResponse>.Failure(createResult.Error!);
            }
            subscription = createResult.Value!;
        }

        var plan = subscription.Plan ?? await planRepository.GetByIdAsync(subscription.PlanId, cancellationToken);
        if (plan is null)
        {
            return Result<SubscriptionResponse>.Failure(Error.PlanNotFound);
        }

        return Result<SubscriptionResponse>.Success(MapToResponse(subscription, plan));
    }

    public async Task<Result<Subscription>> CreateDefaultFreeSubscriptionAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var existing = await subscriptionRepository.GetActiveByUserIdAsync(userId, cancellationToken);
        if (existing is not null)
        {
            return Result<Subscription>.Success(existing);
        }

        var freePlan = await planRepository.GetByIdAsync(DefaultPlans.FreePlanId, cancellationToken)
                       ?? DefaultPlans.Free;

        var now = DateTime.UtcNow;
        var subscription = new Subscription
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            PlanId = freePlan.Id,
            Plan = freePlan,
            Status = SubscriptionStatus.Active,
            StartedAt = now,
            CurrentPeriodStart = now,
            CurrentPeriodEnd = now.AddMonths(1),
            CancelAtPeriodEnd = false,
            CanceledAt = null
        };

        await subscriptionRepository.AddAsync(subscription, cancellationToken);

        var usage = new Core.Entities.Usage
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            PeriodStart = subscription.CurrentPeriodStart,
            PeriodEnd = subscription.CurrentPeriodEnd,
            StorageUsedBytes = 0,
            ApiRequests = 0,
            BandwidthUsedBytes = 0
        };

        await usageRepository.AddAsync(usage, cancellationToken);

        if (await unitOfWork.SaveChangesAsync(cancellationToken) == 0)
        {
            return Result<Subscription>.Failure(Error.InternalServerError);
        }

        return Result<Subscription>.Success(subscription);
    }

    public async Task<Result<string>> CreateCheckoutSessionAsync(
        Guid userId,
        Guid planId,
        CancellationToken cancellationToken = default)
    {
        var targetPlan = await planRepository.GetByIdAsync(planId, cancellationToken);
        if (targetPlan is null || !targetPlan.IsActive)
        {
            return Result<string>.Failure(Error.PlanNotFound);
        }

        var currentSubscription = await subscriptionRepository.GetActiveByUserIdAsync(userId, cancellationToken);
        if (currentSubscription is not null && currentSubscription.PlanId == planId && currentSubscription.Status == SubscriptionStatus.Active)
        {
            return Result<string>.Failure(
                Error.Conflict("Billing.AlreadySubscribed", "You are already subscribed to this plan."));
        }

        return await paymentService.CreateCheckoutSessionAsync(userId, planId, cancellationToken);
    }

    public async Task<Result> CancelSubscriptionAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var subscription = await subscriptionRepository.GetActiveByUserIdAsync(userId, cancellationToken);
        if (subscription is null)
        {
            return Result.Failure(Error.SubscriptionNotFound);
        }

        if (subscription.CancelAtPeriodEnd)
        {
            return Result.Success();
        }

        var paymentCancelResult = await paymentService.CancelSubscriptionAsync(userId, cancellationToken);
        if (paymentCancelResult.IsFailure)
        {
            return paymentCancelResult;
        }

        subscription.CancelAtPeriodEnd = true;
        subscription.CanceledAt = DateTime.UtcNow;
        subscriptionRepository.Update(subscription);

        if (await unitOfWork.SaveChangesAsync(cancellationToken) == 0)
        {
            return Result.Failure(Error.InternalServerError);
        }

        return Result.Success();
    }

    private static SubscriptionResponse MapToResponse(Subscription subscription, Plan plan) =>
        new(
            subscription.Id,
            subscription.UserId,
            plan.Id,
            plan.Name,
            subscription.Status.ToString(),
            plan.Price,
            plan.Currency,
            plan.BillingInterval,
            subscription.StartedAt,
            subscription.CurrentPeriodStart,
            subscription.CurrentPeriodEnd,
            subscription.CancelAtPeriodEnd,
            subscription.CanceledAt
        );
}
