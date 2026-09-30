namespace Drive.Core.Entities;

public class Usage
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid UserId { get; set; }

    public DateTime PeriodStart { get; set; }

    public DateTime PeriodEnd { get; set; }

    public long StorageUsedBytes { get; set; }

    public int ApiRequests { get; set; }

    public long BandwidthUsedBytes { get; set; }

    public User User { get; set; } = null!;
}
