namespace Drive.Core.Entities;

public class Plan
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Name { get; set; } = null!;

    public decimal Price { get; set; }

    public string Currency { get; set; } = "USD";

    public string BillingInterval { get; set; } = "month";

    public long StorageLimitBytes { get; set; }

    public int MonthlyRequestLimit { get; set; }

    public long MaxFileSizeBytes { get; set; }

    public bool IsActive { get; set; } = true;

    public string? StripeProductId { get; set; }

    public string? StripePriceId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }
}

public static class DefaultPlans
{
    public static readonly Guid FreePlanId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    public static readonly Guid ProPlanId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    public static readonly Guid BusinessPlanId = Guid.Parse("33333333-3333-3333-3333-333333333333");

    public static Plan Free => new()
    {
        Id = FreePlanId,
        Name = "Free",
        Price = 0.00m,
        Currency = "USD",
        BillingInterval = "month",
        StorageLimitBytes = 5L * 1024 * 1024 * 1024, // 5 GB
        MonthlyRequestLimit = 10_000,
        MaxFileSizeBytes = 100L * 1024 * 1024, // 100 MB
        IsActive = true,
        CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
    };

    public static Plan Pro => new()
    {
        Id = ProPlanId,
        Name = "Pro",
        Price = 4.99m,
        Currency = "USD",
        BillingInterval = "month",
        StorageLimitBytes = 100L * 1024 * 1024 * 1024, // 100 GB
        MonthlyRequestLimit = 100_000,
        MaxFileSizeBytes = 2L * 1024 * 1024 * 1024, // 2 GB
        IsActive = true,
        CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
    };

    public static Plan Business => new()
    {
        Id = BusinessPlanId,
        Name = "Business",
        Price = 14.99m,
        Currency = "USD",
        BillingInterval = "month",
        StorageLimitBytes = 1024L * 1024 * 1024 * 1024, // 1 TB
        MonthlyRequestLimit = 1_000_000,
        MaxFileSizeBytes = 10L * 1024 * 1024 * 1024, // 10 GB
        IsActive = true,
        CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
    };

    public static IReadOnlyList<Plan> All => [Free, Pro, Business];
}
