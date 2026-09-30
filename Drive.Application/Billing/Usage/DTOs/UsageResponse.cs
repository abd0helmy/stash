namespace Drive.Application.Billing.Usage.DTOs;

public record UsageResponse(
    UsagePlanDto Plan,
    UsageStorageDto Storage,
    UsageApiRequestsDto ApiRequests,
    UsageBandwidthDto Bandwidth
);

public record UsagePlanDto(
    string Name,
    decimal Price,
    string Currency,
    string BillingInterval
);

public record UsageStorageDto(
    long UsedBytes,
    long LimitBytes,
    double Percentage
);

public record UsageApiRequestsDto(
    int Used,
    int Limit,
    double Percentage
);

public record UsageBandwidthDto(
    long UsedBytes,
    long? LimitBytes
);
