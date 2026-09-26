using System.ComponentModel.DataAnnotations;

namespace Drive.Infrastructure.Caching.Options;

public class RedisOptions
{
    public const string SectionName = "Redis";

    [Required]
    public string ConnectionString { get; set; } = "localhost:6379";
}
