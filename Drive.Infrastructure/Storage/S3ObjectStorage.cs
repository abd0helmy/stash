using Amazon.S3;
using Amazon.S3.Model;
using Drive.Application.Files.Interfaces;
using Drive.Infrastructure.Storage.Options;
using Microsoft.Extensions.Options;

namespace Drive.Infrastructure.Storage;

public class S3ObjectStorage(
    IAmazonS3 s3Client,
    IOptions<S3Options> options) : IObjectStorage
{
    private readonly S3Options _options = options.Value;

    public async Task UploadAsync(
        Stream stream,
        string objectKey,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        var request = new PutObjectRequest
        {
            BucketName = _options.BucketName,
            Key = objectKey,
            InputStream = stream,
            ContentType = contentType
        };

        await s3Client.PutObjectAsync(request, cancellationToken);
    }

    public async Task<Stream?> DownloadAsync(
        string objectKey,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await s3Client.GetObjectAsync(
                new GetObjectRequest
                {
                    BucketName = _options.BucketName,
                    Key = objectKey
                },
                cancellationToken);

            var memoryStream = new MemoryStream();

            await response.ResponseStream.CopyToAsync(
                memoryStream,
                cancellationToken);

            memoryStream.Position = 0;

            return memoryStream;
        }
        catch (AmazonS3Exception ex)
            when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task DeleteAsync(
        string objectKey,
        CancellationToken cancellationToken = default)
    {
        await s3Client.DeleteObjectAsync(
            new DeleteObjectRequest
            {
                BucketName = _options.BucketName,
                Key = objectKey
            },
            cancellationToken);
    }

    public async Task<bool> ExistsAsync(
        string objectKey,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await s3Client.GetObjectMetadataAsync(
                new GetObjectMetadataRequest
                {
                    BucketName = _options.BucketName,
                    Key = objectKey
                },
                cancellationToken);

            return true;
        }
        catch (AmazonS3Exception ex)
            when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return false;
        }
    }
}