using Drive.Application.Billing.Common;
using Drive.Application.Billing.Plans.Interfaces;
using Drive.Application.Billing.Subscriptions.Interfaces;
using Drive.Application.Billing.Usage.Interfaces;
using Drive.Core.Common;
using Drive.Core.Common.Result;
using Drive.Core.Entities;
using Drive.Core.Enums;

namespace Drive.Application.Billing.Services;

public class QuotaService(
    ISubscriptionRepository subscriptionRepository,
    ISubscriptionService subscriptionService,
    IPlanRepository planRepository,
    IUsageRepository usageRepository) : IQuotaService
{
    public async Task<Result> CanUploadAsync(
        Guid userId,
        long fileSize,
        CancellationToken cancellationToken = default)
    {
        if (fileSize <= 0)
        {
            return Result.Failure(
                Error.Validation("Billing.InvalidFileSize", "File size must be greater than zero."));
        }

        var (subscription, plan, usage, error) = await GetBillingContextAsync(userId, cancellationToken);
        if (error is not null)
        {
            return Result.Failure(error);
        }

        if (subscription!.Status != SubscriptionStatus.Active && subscription.Status != SubscriptionStatus.Trialing)
        {
            return Result.Failure(Error.SubscriptionInactive);
        }

        if (fileSize > plan!.MaxFileSizeBytes)
        {
            return Result.Failure(Error.FileSizeLimitExceeded(fileSize, plan.MaxFileSizeBytes));
        }

        if (usage!.StorageUsedBytes + fileSize > plan.StorageLimitBytes)
        {
            return Result.Failure(Error.StorageQuotaExceeded(usage.StorageUsedBytes, plan.StorageLimitBytes, fileSize));
        }

        return Result.Success();
    }

    public async Task<Result> CanMakeRequestAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var (subscription, plan, usage, error) = await GetBillingContextAsync(userId, cancellationToken);
        if (error is not null)
        {
            return Result.Failure(error);
        }

        if (subscription!.Status != SubscriptionStatus.Active && subscription.Status != SubscriptionStatus.Trialing)
        {
            return Result.Failure(Error.SubscriptionInactive);
        }

        if (usage!.ApiRequests >= plan!.MonthlyRequestLimit)
        {
            return Result.Failure(Error.ApiRequestQuotaExceeded(usage.ApiRequests, plan.MonthlyRequestLimit));
        }

        return Result.Success();
    }

    public async Task<Result> CanUseFeatureAsync(
        Guid userId,
        string feature,
        CancellationToken cancellationToken = default)
    {
        var (subscription, plan, _, error) = await GetBillingContextAsync(userId, cancellationToken);
        if (error is not null)
        {
            return Result.Failure(error);
        }

        if (subscription!.Status != SubscriptionStatus.Active && subscription.Status != SubscriptionStatus.Trialing)
        {
            return Result.Failure(Error.SubscriptionInactive);
        }

        var normalizedFeature = feature.Trim().ToLowerInvariant();

        // Feature tiers based on plan properties
        var isAllowed = normalizedFeature switch
        {
            "basic_storage" => true,
            "file_sharing" => true,
            "advanced_sharing" => plan!.Price >= 4.99m,
            "priority_support" => plan!.Price >= 4.99m,
            "audit_logs" => plan!.Price >= 14.99m,
            "custom_domain" => plan!.Price >= 14.99m,
            _ => plan!.Price > 0m
        };

        if (!isAllowed)
        {
            return Result.Failure(Error.FeatureNotAvailable(feature));
        }

        return Result.Success();
    }

    public async Task<Result> ReserveStorageAsync(
        Guid userId,
        long fileSize,
        CancellationToken cancellationToken = default)
    {
        if (fileSize <= 0)
        {
            return Result.Failure(
                Error.Validation("Billing.InvalidFileSize", "File size must be greater than zero."));
        }

        var (subscription, plan, usage, error) = await GetBillingContextAsync(userId, cancellationToken);
        if (error is not null)
        {
            return Result.Failure(error);
        }

        if (subscription!.Status != SubscriptionStatus.Active && subscription.Status != SubscriptionStatus.Trialing)
        {
            return Result.Failure(Error.SubscriptionInactive);
        }

        if (fileSize > plan!.MaxFileSizeBytes)
        {
            return Result.Failure(Error.FileSizeLimitExceeded(fileSize, plan.MaxFileSizeBytes));
        }

        var reserved = await usageRepository.TryReserveStorageAsync(
            usage!.Id,
            fileSize,
            plan.StorageLimitBytes,
            cancellationToken);

        if (!reserved)
        {
            var currentUsage = await usageRepository.GetCurrentUsageAsync(
                userId,
                subscription.CurrentPeriodStart,
                subscription.CurrentPeriodEnd,
                cancellationToken);

            var used = currentUsage?.StorageUsedBytes ?? usage.StorageUsedBytes;
            return Result.Failure(Error.StorageQuotaExceeded(used, plan.StorageLimitBytes, fileSize));
        }

        return Result.Success();
    }

    public async Task ReleaseStorageAsync(
        Guid userId,
        long fileSize,
        CancellationToken cancellationToken = default)
    {
        if (fileSize <= 0)
        {
            return;
        }

        var subscription = await subscriptionRepository.GetActiveByUserIdAsync(userId, cancellationToken);
        if (subscription is null)
        {
            return;
        }

        var usage = await usageRepository.GetCurrentUsageAsync(
            userId,
            subscription.CurrentPeriodStart,
            subscription.CurrentPeriodEnd,
            cancellationToken);

        if (usage is not null)
        {
            await usageRepository.ReleaseStorageAsync(usage.Id, fileSize, cancellationToken);
        }
    }

    private async Task<(Subscription? Subscription, Plan? Plan, Core.Entities.Usage? Usage, Error? Error)> GetBillingContextAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        var subscription = await subscriptionRepository.GetActiveByUserIdAsync(userId, cancellationToken);
        if (subscription is null)
        {
            var initResult = await subscriptionService.CreateDefaultFreeSubscriptionAsync(userId, cancellationToken);
            if (initResult.IsFailure)
            {
                return (null, null, null, initResult.Error);
            }
            subscription = initResult.Value!;
        }

        var plan = subscription.Plan ?? await planRepository.GetByIdAsync(subscription.PlanId, cancellationToken);
        if (plan is null)
        {
            return (null, null, null, Error.PlanNotFound);
        }

        var usage = await usageRepository.GetOrCreateCurrentUsageAsync(
            userId,
            subscription.CurrentPeriodStart,
            subscription.CurrentPeriodEnd,
            cancellationToken);

        return (subscription, plan, usage, null);
    }
}
