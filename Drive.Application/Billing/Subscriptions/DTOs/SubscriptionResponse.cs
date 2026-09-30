namespace Drive.Application.Billing.Subscriptions.DTOs;

public record SubscriptionResponse(
    Guid Id,
    Guid UserId,
    Guid PlanId,
    string PlanName,
    string Status,
    decimal Price,
    string Currency,
    string BillingInterval,
    DateTime StartedAt,
    DateTime CurrentPeriodStart,
    DateTime CurrentPeriodEnd,
    bool CancelAtPeriodEnd,
    DateTime? CanceledAt
);
