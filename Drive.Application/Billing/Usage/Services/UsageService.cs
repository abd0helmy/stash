using Drive.Application.Billing.Plans.Interfaces;
using Drive.Application.Billing.Subscriptions.Interfaces;
using Drive.Application.Billing.Usage.DTOs;
using Drive.Application.Billing.Usage.Interfaces;
using Drive.Core.Common;
using Drive.Core.Common.Result;

namespace Drive.Application.Billing.Usage.Services;

public class UsageService(
    ISubscriptionRepository subscriptionRepository,
    ISubscriptionService subscriptionService,
    IUsageRepository usageRepository,
    IPlanRepository planRepository) : IUsageService
{
    public async Task<Result<UsageResponse>> GetCurrentUsageAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var subscription = await subscriptionRepository.GetActiveByUserIdAsync(userId, cancellationToken);
        if (subscription is null)
        {
            var initResult = await subscriptionService.CreateDefaultFreeSubscriptionAsync(userId, cancellationToken);
            if (initResult.IsFailure)
            {
                return Result<UsageResponse>.Failure(initResult.Error!);
            }
            subscription = initResult.Value!;
        }

        var plan = subscription.Plan ?? await planRepository.GetByIdAsync(subscription.PlanId, cancellationToken);
        if (plan is null)
        {
            return Result<UsageResponse>.Failure(Error.PlanNotFound);
        }

        var usage = await usageRepository.GetOrCreateCurrentUsageAsync(
            userId,
            subscription.CurrentPeriodStart,
            subscription.CurrentPeriodEnd,
            cancellationToken);

        var storagePercentage = plan.StorageLimitBytes > 0
            ? Math.Round((double)usage.StorageUsedBytes / plan.StorageLimitBytes * 100.0, 2)
            : 0.0;

        var requestsPercentage = plan.MonthlyRequestLimit > 0
            ? Math.Round((double)usage.ApiRequests / plan.MonthlyRequestLimit * 100.0, 2)
            : 0.0;

        var response = new UsageResponse(
            new UsagePlanDto(plan.Name, plan.Price, plan.Currency, plan.BillingInterval),
            new UsageStorageDto(usage.StorageUsedBytes, plan.StorageLimitBytes, storagePercentage),
            new UsageApiRequestsDto(usage.ApiRequests, plan.MonthlyRequestLimit, requestsPercentage),
            new UsageBandwidthDto(usage.BandwidthUsedBytes, null)
        );

        return Result<UsageResponse>.Success(response);
    }

    public async Task<Result> IncrementApiRequestsAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var subscription = await subscriptionRepository.GetActiveByUserIdAsync(userId, cancellationToken);
        if (subscription is null)
        {
            var initResult = await subscriptionService.CreateDefaultFreeSubscriptionAsync(userId, cancellationToken);
            if (initResult.IsFailure)
            {
                return Result.Failure(initResult.Error!);
            }
            subscription = initResult.Value!;
        }

        var usage = await usageRepository.GetOrCreateCurrentUsageAsync(
            userId,
            subscription.CurrentPeriodStart,
            subscription.CurrentPeriodEnd,
            cancellationToken);

        await usageRepository.IncrementApiRequestsAsync(usage.Id, cancellationToken);
        return Result.Success();
    }
}
