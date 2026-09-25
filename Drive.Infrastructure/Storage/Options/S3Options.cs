namespace Drive.Infrastructure.Storage.Options;

public class S3Options
{
    public const string SectionName = "S3";

    public string ServiceUrl { get; set; } = null!;

    public string AccessKey { get; set; } = null!;

    public string SecretKey { get; set; } = null!;

    public string BucketName { get; set; } = null!;
}