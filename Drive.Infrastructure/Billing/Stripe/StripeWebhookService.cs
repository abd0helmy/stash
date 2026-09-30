using Drive.Application.Billing.Common;
using Drive.Application.Billing.Plans.Interfaces;
using Drive.Application.Billing.Subscriptions.Interfaces;
using Drive.Application.Billing.Usage.Interfaces;
using Drive.Application.Interfaces;
using Drive.Core.Common;
using Drive.Core.Common.Result;
using Drive.Core.Entities;
using Drive.Core.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using global::Stripe;
using global::Stripe.Checkout;
using LocalSubscription = Drive.Core.Entities.Subscription;
using StripeSubscription = global::Stripe.Subscription;
using StripeSubscriptionService = global::Stripe.SubscriptionService;

namespace Drive.Infrastructure.Billing.Stripe;

public class StripeWebhookService : IStripeWebhookService
{
    private const string CheckoutSessionCompleted = "checkout.session.completed";
    private const string CustomerSubscriptionUpdated = "customer.subscription.updated";
    private const string CustomerSubscriptionDeleted = "customer.subscription.deleted";
    private const string InvoicePaymentFailed = "invoice.payment_failed";

    private readonly StripeOptions _options;
    private readonly ILogger<StripeWebhookService> _logger;
    private readonly ISubscriptionRepository _subscriptionRepository;
    private readonly IPlanRepository _planRepository;
    private readonly IUsageRepository _usageRepository;
    private readonly IUnitOfWork _unitOfWork;

    public StripeWebhookService(
        IOptions<StripeOptions> options,
        ILogger<StripeWebhookService> logger,
        ISubscriptionRepository subscriptionRepository,
        IPlanRepository planRepository,
        IUsageRepository usageRepository,
        IUnitOfWork unitOfWork)
    {
        _options = options.Value;
        _logger = logger;
        _subscriptionRepository = subscriptionRepository;
        _planRepository = planRepository;
        _usageRepository = usageRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> HandleWebhookAsync(
        string jsonPayload,
        string? stripeSignature,
        CancellationToken cancellationToken = default)
    {
        Event stripeEvent;

        try
        {
            if (!string.IsNullOrWhiteSpace(_options.WebhookSecret) && !string.IsNullOrWhiteSpace(stripeSignature))
            {
                stripeEvent = EventUtility.ConstructEvent(
                    jsonPayload,
                    stripeSignature,
                    _options.WebhookSecret,
                    throwOnApiVersionMismatch: false);
            }
            else
            {
                _logger.LogWarning("Stripe WebhookSecret is not configured or signature missing — parsing payload without signature verification");
                stripeEvent = EventUtility.ParseEvent(jsonPayload);
            }
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Stripe webhook signature verification failed: {Message}", ex.Message);
            return Result.Failure(Error.Validation("Billing.InvalidWebhookSignature", "Invalid Stripe webhook signature."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse Stripe webhook: {Message}", ex.Message);
            return Result.Failure(Error.Validation("Billing.InvalidWebhookPayload", "Invalid Stripe webhook payload."));
        }

        _logger.LogInformation("Processing Stripe webhook event: {EventType} ({EventId})", stripeEvent.Type, stripeEvent.Id);

        return stripeEvent.Type switch
        {
            CheckoutSessionCompleted => await HandleCheckoutSessionCompletedAsync(stripeEvent, cancellationToken),
            CustomerSubscriptionUpdated => await HandleSubscriptionUpdatedAsync(stripeEvent, cancellationToken),
            CustomerSubscriptionDeleted => await HandleSubscriptionDeletedAsync(stripeEvent, cancellationToken),
            InvoicePaymentFailed => await HandleInvoicePaymentFailedAsync(stripeEvent, cancellationToken),
            _ => Result.Success()
        };
    }

    private async Task<Result> HandleCheckoutSessionCompletedAsync(Event stripeEvent, CancellationToken cancellationToken)
    {
        if (stripeEvent.Data.Object is not Session session)
        {
            return Result.Success();
        }

        if (session.Metadata is null ||
            !session.Metadata.TryGetValue("drive_user_id", out var userIdStr) ||
            !Guid.TryParse(userIdStr, out var userId) ||
            !session.Metadata.TryGetValue("drive_plan_id", out var planIdStr) ||
            !Guid.TryParse(planIdStr, out var planId))
        {
            _logger.LogWarning("Checkout session {SessionId} missing required metadata", session.Id);
            return Result.Success();
        }

        var plan = await _planRepository.GetByIdAsync(planId, cancellationToken);
        if (plan is null)
        {
            _logger.LogError("Plan {PlanId} from checkout session {SessionId} not found", planId, session.Id);
            return Result.Failure(Error.PlanNotFound);
        }

        DateTime periodStart = DateTime.UtcNow;
        DateTime periodEnd = plan.BillingInterval == "year" ? periodStart.AddYears(1) : periodStart.AddMonths(1);

        if (!string.IsNullOrWhiteSpace(session.SubscriptionId))
        {
            try
            {
                var subService = new StripeSubscriptionService();
                var stripeSub = await subService.GetAsync(session.SubscriptionId, cancellationToken: cancellationToken);
                if (stripeSub is not null)
                {
                    var firstItem = stripeSub.Items?.Data?.FirstOrDefault();
                    if (firstItem is not null)
                    {
                        periodStart = firstItem.CurrentPeriodStart;
                        periodEnd = firstItem.CurrentPeriodEnd;
                    }
                    else if (stripeSub.StartDate != default)
                    {
                        periodStart = stripeSub.StartDate;
                        periodEnd = plan.BillingInterval == "year" ? periodStart.AddYears(1) : periodStart.AddMonths(1);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not fetch subscription details for {SubscriptionId} from Stripe", session.SubscriptionId);
            }
        }

        var existingSub = await _subscriptionRepository.GetActiveByUserIdAsync(userId, cancellationToken);
        if (existingSub is not null)
        {
            existingSub.PlanId = plan.Id;
            existingSub.Plan = plan;
            existingSub.Status = SubscriptionStatus.Active;
            existingSub.StripeSubscriptionId = session.SubscriptionId;
            existingSub.StripeCustomerId = session.CustomerId;
            existingSub.CurrentPeriodStart = periodStart;
            existingSub.CurrentPeriodEnd = periodEnd;
            existingSub.CancelAtPeriodEnd = false;
            existingSub.CanceledAt = null;

            _subscriptionRepository.Update(existingSub);
        }
        else
        {
            var newSub = new LocalSubscription
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                PlanId = plan.Id,
                Plan = plan,
                Status = SubscriptionStatus.Active,
                StartedAt = DateTime.UtcNow,
                CurrentPeriodStart = periodStart,
                CurrentPeriodEnd = periodEnd,
                CancelAtPeriodEnd = false,
                CanceledAt = null,
                StripeSubscriptionId = session.SubscriptionId,
                StripeCustomerId = session.CustomerId
            };

            await _subscriptionRepository.AddAsync(newSub, cancellationToken);
        }

        // Ensure Usage record is initialized/active for this period
        var usage = new Usage
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            PeriodStart = periodStart,
            PeriodEnd = periodEnd,
            StorageUsedBytes = 0,
            ApiRequests = 0,
            BandwidthUsedBytes = 0
        };

        await _usageRepository.AddAsync(usage, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "User {UserId} upgraded to plan {PlanName} via checkout session {SessionId}",
            userId,
            plan.Name,
            session.Id);

        return Result.Success();
    }

    private async Task<Result> HandleSubscriptionUpdatedAsync(Event stripeEvent, CancellationToken cancellationToken)
    {
        if (stripeEvent.Data.Object is not StripeSubscription stripeSub)
        {
            return Result.Success();
        }

        var localSub = await _subscriptionRepository.GetByStripeSubscriptionIdAsync(stripeSub.Id, cancellationToken);
        if (localSub is null)
        {
            return Result.Success();
        }

        localSub.CancelAtPeriodEnd = stripeSub.CancelAtPeriodEnd;
        
        var firstItem = stripeSub.Items?.Data?.FirstOrDefault();
        if (firstItem is not null)
        {
            localSub.CurrentPeriodStart = firstItem.CurrentPeriodStart;
            localSub.CurrentPeriodEnd = firstItem.CurrentPeriodEnd;
        }

        localSub.Status = stripeSub.Status switch
        {
            "active" => SubscriptionStatus.Active,
            "trialing" => SubscriptionStatus.Trialing,
            "past_due" => SubscriptionStatus.PastDue,
            "canceled" => SubscriptionStatus.Canceled,
            _ => localSub.Status
        };

        if (stripeSub.CancelAtPeriodEnd && localSub.CanceledAt is null)
        {
            localSub.CanceledAt = DateTime.UtcNow;
        }

        _subscriptionRepository.Update(localSub);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Updated subscription {SubscriptionId} status to {Status}", stripeSub.Id, localSub.Status);

        return Result.Success();
    }

    private async Task<Result> HandleSubscriptionDeletedAsync(Event stripeEvent, CancellationToken cancellationToken)
    {
        if (stripeEvent.Data.Object is not StripeSubscription stripeSub)
        {
            return Result.Success();
        }

        var localSub = await _subscriptionRepository.GetByStripeSubscriptionIdAsync(stripeSub.Id, cancellationToken);
        if (localSub is null)
        {
            return Result.Success();
        }

        var freePlan = await _planRepository.GetByIdAsync(DefaultPlans.FreePlanId, cancellationToken)
                       ?? DefaultPlans.Free;

        // Downgrade to Free plan
        localSub.PlanId = freePlan.Id;
        localSub.Plan = freePlan;
        localSub.Status = SubscriptionStatus.Active;
        localSub.StripeSubscriptionId = null;
        localSub.CancelAtPeriodEnd = false;
        localSub.CanceledAt = null;
        localSub.CurrentPeriodStart = DateTime.UtcNow;
        localSub.CurrentPeriodEnd = DateTime.UtcNow.AddMonths(1);

        _subscriptionRepository.Update(localSub);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Subscription {SubscriptionId} deleted on Stripe — downgraded user {UserId} to Free plan",
            stripeSub.Id,
            localSub.UserId);

        return Result.Success();
    }

    private async Task<Result> HandleInvoicePaymentFailedAsync(Event stripeEvent, CancellationToken cancellationToken)
    {
        if (stripeEvent.Data.Object is not Invoice invoice)
        {
            return Result.Success();
        }

        var subscriptionId = invoice.Parent?.SubscriptionDetails?.SubscriptionId;
        if (string.IsNullOrWhiteSpace(subscriptionId))
        {
            return Result.Success();
        }

        var localSub = await _subscriptionRepository.GetByStripeSubscriptionIdAsync(subscriptionId, cancellationToken);
        if (localSub is null)
        {
            return Result.Success();
        }

        localSub.Status = SubscriptionStatus.PastDue;
        _subscriptionRepository.Update(localSub);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogWarning(
            "Invoice payment failed for subscription {SubscriptionId} (User {UserId}) — marked as PastDue",
            subscriptionId,
            localSub.UserId);

        return Result.Success();
    }
}
