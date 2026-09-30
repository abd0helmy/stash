using Drive.Core.Enums;

namespace Drive.Core.Entities;

public class Subscription
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid UserId { get; set; }

    public Guid PlanId { get; set; }

    public SubscriptionStatus Status { get; set; } = SubscriptionStatus.Active;

    public DateTime StartedAt { get; set; } = DateTime.UtcNow;

    public DateTime CurrentPeriodStart { get; set; } = DateTime.UtcNow;

    public DateTime CurrentPeriodEnd { get; set; } = DateTime.UtcNow.AddMonths(1);

    public bool CancelAtPeriodEnd { get; set; }

    public DateTime? CanceledAt { get; set; }

    public string? StripeSubscriptionId { get; set; }

    public string? StripeCustomerId { get; set; }

    public User User { get; set; } = null!;

    public Plan Plan { get; set; } = null!;
}
