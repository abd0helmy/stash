using Drive.Application.Authentication.Interfaces;
using Drive.Application.Billing.Common;
using Drive.Application.Billing.Plans.Interfaces;
using Drive.Application.Billing.Subscriptions.Interfaces;
using Drive.Application.Interfaces;
using Drive.Core.Common;
using Drive.Core.Common.Result;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using global::Stripe;
using global::Stripe.Checkout;
using StripeSubscription = global::Stripe.Subscription;
using StripeSubscriptionService = global::Stripe.SubscriptionService;
using StripeSubscriptionSearchOptions = global::Stripe.SubscriptionSearchOptions;
using StripeSubscriptionUpdateOptions = global::Stripe.SubscriptionUpdateOptions;

namespace Drive.Infrastructure.Billing.Stripe;

public class StripePaymentService : IPaymentService
{
    private readonly StripeOptions _options;
    private readonly ILogger<StripePaymentService> _logger;
    private readonly IPlanRepository _planRepository;
    private readonly ISubscriptionRepository _subscriptionRepository;
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly bool _isConfigured;

    public StripePaymentService(
        IOptions<StripeOptions> options,
        ILogger<StripePaymentService> logger,
        IPlanRepository planRepository,
        ISubscriptionRepository subscriptionRepository,
        IUserRepository userRepository,
        IUnitOfWork unitOfWork)
    {
        _options = options.Value;
        _logger = logger;
        _planRepository = planRepository;
        _subscriptionRepository = subscriptionRepository;
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _isConfigured = !string.IsNullOrWhiteSpace(_options.SecretKey);

        if (_isConfigured)
        {
            StripeConfiguration.ApiKey = _options.SecretKey;
        }
    }

    public async Task<Result<string>> CreateCheckoutSessionAsync(
        Guid userId,
        Guid planId,
        CancellationToken cancellationToken = default)
    {
        if (!_isConfigured)
        {
            return CreateSandboxCheckoutSession(userId, planId);
        }

        try
        {
            var plan = await _planRepository.GetByIdAsync(planId, cancellationToken);
            if (plan is null || !plan.IsActive)
            {
                return Result<string>.Failure(Error.PlanNotFound);
            }

            if (plan.Price <= 0)
            {
                return Result<string>.Failure(
                    Error.Validation("Billing.FreePlanNoCheckout", "Free plan does not require a checkout session."));
            }

            var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
            var existingSub = await _subscriptionRepository.GetActiveByUserIdAsync(userId, cancellationToken);

            // Look up or create the Stripe Price for this plan
            var priceId = await GetOrCreateStripePriceAsync(plan, cancellationToken);

            var sessionOptions = new SessionCreateOptions
            {
                Mode = "subscription",
                SuccessUrl = _options.SuccessUrl,
                CancelUrl = _options.CancelUrl,
                ClientReferenceId = userId.ToString(),
                PaymentMethodCollection = "always",
                LineItems =
                [
                    new SessionLineItemOptions
                    {
                        Price = priceId,
                        Quantity = 1
                    }
                ],
                Metadata = new Dictionary<string, string>
                {
                    ["drive_user_id"] = userId.ToString(),
                    ["drive_plan_id"] = planId.ToString()
                },
                SubscriptionData = new SessionSubscriptionDataOptions
                {
                    Metadata = new Dictionary<string, string>
                    {
                        ["drive_user_id"] = userId.ToString(),
                        ["drive_plan_id"] = planId.ToString()
                    }
                }
            };

            // Link existing Stripe Customer if available to avoid creating duplicate customers
            if (!string.IsNullOrWhiteSpace(existingSub?.StripeCustomerId))
            {
                sessionOptions.Customer = existingSub.StripeCustomerId;
            }
            else if (user is not null && !string.IsNullOrWhiteSpace(user.Email))
            {
                sessionOptions.CustomerEmail = user.Email;
            }

            // If the user already has a subscription, record previous plan id
            if (existingSub is not null)
            {
                sessionOptions.Metadata["drive_previous_plan_id"] = existingSub.PlanId.ToString();
            }

            var service = new SessionService();
            var session = await service.CreateAsync(sessionOptions, cancellationToken: cancellationToken);

            _logger.LogInformation(
                "Created Stripe checkout session {SessionId} for user {UserId}, plan {PlanId}",
                session.Id,
                userId,
                planId);

            return Result<string>.Success(session.Url);
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex,
                "Stripe error creating checkout session for user {UserId}, plan {PlanId}: {Message}",
                userId,
                planId,
                ex.Message);

            return Result<string>.Failure(
                new Error(
                    "Billing.PaymentProviderError",
                    $"Payment provider error: {ex.Message}",
                    Core.Common.ErrorType.Failure));
        }
    }

    public async Task<Result> CancelSubscriptionAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        if (!_isConfigured)
        {
            return CreateSandboxCancellation(userId);
        }

        try
        {
            var existingSub = await _subscriptionRepository.GetActiveByUserIdAsync(userId, cancellationToken);
            if (existingSub is null)
            {
                return Result.Failure(Error.SubscriptionNotFound);
            }

            var plan = existingSub.Plan ?? await _planRepository.GetByIdAsync(existingSub.PlanId, cancellationToken);
            
            // Free plan doesn't have a Stripe recurring subscription
            if (plan is not null && plan.Price <= 0 && string.IsNullOrWhiteSpace(existingSub.StripeSubscriptionId))
            {
                return Result.Success();
            }

            var subscriptionService = new StripeSubscriptionService();
            string? stripeSubscriptionId = existingSub.StripeSubscriptionId;

            // If StripeSubscriptionId was not saved locally, look it up via Stripe Search API
            if (string.IsNullOrWhiteSpace(stripeSubscriptionId))
            {
                var searchOptions = new StripeSubscriptionSearchOptions
                {
                    Query = $"metadata['drive_user_id']:'{userId}' AND status:'active'",
                    Limit = 1
                };

                var searchResult = await subscriptionService.SearchAsync(searchOptions, cancellationToken: cancellationToken);
                var foundSubscription = searchResult.Data.FirstOrDefault();

                if (foundSubscription is not null)
                {
                    stripeSubscriptionId = foundSubscription.Id;
                    // Persist for future operations
                    existingSub.StripeSubscriptionId = foundSubscription.Id;
                    if (string.IsNullOrWhiteSpace(existingSub.StripeCustomerId))
                    {
                        existingSub.StripeCustomerId = foundSubscription.CustomerId;
                    }
                    _subscriptionRepository.Update(existingSub);
                    await _unitOfWork.SaveChangesAsync(cancellationToken);
                }
            }

            if (string.IsNullOrWhiteSpace(stripeSubscriptionId))
            {
                _logger.LogWarning(
                    "No active Stripe subscription found for user {UserId} with paid plan {PlanId}",
                    userId,
                    existingSub.PlanId);

                return Result.Failure(
                    Error.NotFound(
                        "Billing.SubscriptionNotFoundOnGateway",
                        "No active subscription found on Stripe to cancel."));
            }

            // Cancel at period end so the user retains access for the remainder of their paid period
            var updateOptions = new StripeSubscriptionUpdateOptions
            {
                CancelAtPeriodEnd = true
            };

            await subscriptionService.UpdateAsync(
                stripeSubscriptionId,
                updateOptions,
                cancellationToken: cancellationToken);

            _logger.LogInformation(
                "Stripe subscription {SubscriptionId} for user {UserId} set to cancel at period end",
                stripeSubscriptionId,
                userId);

            return Result.Success();
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex,
                "Stripe error canceling subscription for user {UserId}: {Message}",
                userId,
                ex.Message);

            return Result.Failure(
                new Error(
                    "Billing.PaymentProviderError",
                    $"Payment provider error: {ex.Message}",
                    Core.Common.ErrorType.Failure));
        }
    }

    private async Task<string> GetOrCreateStripePriceAsync(
        Core.Entities.Plan plan,
        CancellationToken cancellationToken)
    {
        var productService = new ProductService();
        var priceService = new PriceService();

        long expectedUnitAmount = (long)Math.Round(plan.Price * 100m, MidpointRounding.AwayFromZero);
        string expectedCurrency = plan.Currency.ToLowerInvariant();
        string expectedInterval = plan.BillingInterval == "year" ? "year" : "month";

        // 1. If Plan already has StripePriceId cached, verify it exists and matches
        if (!string.IsNullOrWhiteSpace(plan.StripePriceId))
        {
            try
            {
                var cachedPrice = await priceService.GetAsync(plan.StripePriceId, cancellationToken: cancellationToken);
                if (cachedPrice is not null &&
                    cachedPrice.Active &&
                    cachedPrice.Currency == expectedCurrency &&
                    cachedPrice.UnitAmount == expectedUnitAmount &&
                    cachedPrice.Recurring?.Interval == expectedInterval)
                {
                    return cachedPrice.Id;
                }
            }
            catch (StripeException ex) when (ex.StripeError?.Code == "resource_missing")
            {
                // Price not found in Stripe, will re-create or search below
            }
        }

        // 2. Look up or create Product
        string productId = !string.IsNullOrWhiteSpace(plan.StripeProductId)
            ? plan.StripeProductId
            : $"drive_plan_{plan.Id:N}";

        Product? product = null;
        try
        {
            product = await productService.GetAsync(productId, cancellationToken: cancellationToken);
        }
        catch (StripeException ex) when (ex.StripeError?.Code == "resource_missing")
        {
            // Product doesn't exist yet
        }

        if (product is null)
        {
            var productCreateOptions = new ProductCreateOptions
            {
                Id = productId,
                Name = $"Drive {plan.Name} Plan",
                Description = $"{plan.Name} plan - {FormatBytes(plan.StorageLimitBytes)} storage, {plan.MonthlyRequestLimit:N0} API requests/month",
                Metadata = new Dictionary<string, string>
                {
                    ["drive_plan_id"] = plan.Id.ToString(),
                    ["drive_plan_name"] = plan.Name
                }
            };

            product = await productService.CreateAsync(productCreateOptions, cancellationToken: cancellationToken);

            _logger.LogInformation(
                "Created Stripe product {ProductId} for plan {PlanName}",
                product.Id,
                plan.Name);
        }

        // 3. Find existing matching price for this product
        var priceListOptions = new PriceListOptions
        {
            Product = product.Id,
            Active = true,
            Limit = 10
        };

        var existingPrices = await priceService.ListAsync(priceListOptions, cancellationToken: cancellationToken);
        var matchingPrice = existingPrices.Data.FirstOrDefault(p =>
            p.Active &&
            p.Currency == expectedCurrency &&
            p.UnitAmount == expectedUnitAmount &&
            p.Recurring?.Interval == expectedInterval);

        if (matchingPrice is not null)
        {
            await UpdatePlanStripeIdsIfChangedAsync(plan, product.Id, matchingPrice.Id, cancellationToken);
            return matchingPrice.Id;
        }

        // 4. Create new recurring price
        var priceCreateOptions = new PriceCreateOptions
        {
            Product = product.Id,
            Currency = expectedCurrency,
            UnitAmount = expectedUnitAmount,
            Recurring = new PriceRecurringOptions
            {
                Interval = expectedInterval
            },
            Metadata = new Dictionary<string, string>
            {
                ["drive_plan_id"] = plan.Id.ToString()
            }
        };

        var newPrice = await priceService.CreateAsync(priceCreateOptions, cancellationToken: cancellationToken);

        _logger.LogInformation(
            "Created Stripe price {PriceId} for plan {PlanName} at {Amount} {Currency}/{Interval}",
            newPrice.Id,
            plan.Name,
            plan.Price,
            plan.Currency,
            plan.BillingInterval);

        await UpdatePlanStripeIdsIfChangedAsync(plan, product.Id, newPrice.Id, cancellationToken);
        return newPrice.Id;
    }

    private async Task UpdatePlanStripeIdsIfChangedAsync(
        Core.Entities.Plan plan,
        string productId,
        string priceId,
        CancellationToken cancellationToken)
    {
        if (plan.StripeProductId != productId || plan.StripePriceId != priceId)
        {
            plan.StripeProductId = productId;
            plan.StripePriceId = priceId;
            plan.UpdatedAt = DateTime.UtcNow;
            _planRepository.Update(plan);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }

    #region Sandbox Fallbacks (when Stripe keys are not configured)

    private Result<string> CreateSandboxCheckoutSession(Guid userId, Guid planId)
    {
        var sessionId = $"cs_test_{Guid.NewGuid():N}";
        var checkoutUrl = _options.SuccessUrl.Contains("{CHECKOUT_SESSION_ID}")
            ? _options.SuccessUrl.Replace("{CHECKOUT_SESSION_ID}", sessionId)
            : $"{_options.SuccessUrl}{( _options.SuccessUrl.Contains('?') ? "&" : "?" )}session_id={sessionId}";

        _logger.LogWarning(
            "Stripe is not configured — returning sandbox direct success redirect for user {UserId}, plan {PlanId}",
            userId,
            planId);

        return Result<string>.Success(checkoutUrl);
    }

    private Result CreateSandboxCancellation(Guid userId)
    {
        _logger.LogWarning(
            "Stripe is not configured — sandbox subscription cancellation for user {UserId}",
            userId);

        return Result.Success();
    }

    #endregion

    private static string FormatBytes(long bytes)
    {
        string[] suffixes = ["B", "KB", "MB", "GB", "TB"];
        int counter = 0;
        decimal number = bytes;
        while (Math.Round(number / 1024) >= 1 && counter < suffixes.Length - 1)
        {
            number /= 1024;
            counter++;
        }
        return $"{number:0.##} {suffixes[counter]}";
    }
}
