namespace Drive.Application.Files.Interfaces;

public interface IObjectStorage
{
    Task UploadAsync(
        Stream stream,
        string objectKey,
        string contentType,
        CancellationToken cancellationToken = default);

    Task<Stream?> DownloadAsync(
        string objectKey,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        string objectKey,
        CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(
        string objectKey,
        CancellationToken cancellationToken = default);
}