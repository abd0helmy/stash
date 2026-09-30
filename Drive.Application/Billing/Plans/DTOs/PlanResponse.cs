namespace Drive.Application.Billing.Plans.DTOs;

public record PlanResponse(
    Guid Id,
    string Name,
    decimal Price,
    string Currency,
    string BillingInterval,
    long StorageLimitBytes,
    int MonthlyRequestLimit,
    long MaxFileSizeBytes,
    bool IsActive,
    IReadOnlyList<string> Features
);
