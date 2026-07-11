using Exceptions;
using Infrastructure;
using Microsoft.Extensions.Options;
using Minio;
using Minio.DataModel.Args;

namespace Storage;

public sealed class MinioFileStorageService : IFileStorage
{
    private readonly IMinioClient _minioClient;
    private readonly ILogger<MinioFileStorageService> _logger;
    private readonly string _bucketName;

    public MinioFileStorageService(IOptions<MinioSettings> settings, ILogger<MinioFileStorageService> logger)
    {
        _logger = logger;
        _bucketName = settings.Value.BucketName;

        _minioClient = new MinioClient()
            .WithEndpoint(settings.Value.Endpoint)
            .WithCredentials(settings.Value.AccessKey, settings.Value.SecretKey)
            .WithSSL(settings.Value.UseSSL)
            .Build();
    }

    public async Task<Stream> DownloadAsync(string objectName, CancellationToken cancellationToken = default)
    {
        try
        {
            var buffer = new MemoryStream();

            var getObjectArgs = new GetObjectArgs()
                .WithBucket(_bucketName)
                .WithObject(objectName)
                .WithCallbackStream(async (stream, ct) => await stream.CopyToAsync(buffer, ct));

            await _minioClient.GetObjectAsync(getObjectArgs, cancellationToken);

            buffer.Position = 0;
            return buffer;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to download object '{ObjectName}' from bucket '{BucketName}'", objectName, _bucketName);
            throw new FileDownloadException($"Failed to download object '{objectName}' from MinIO.", ex);
        }
    }

    public async Task<long> GetSizeAsync(string objectName, CancellationToken cancellationToken = default)
    {
        try
        {
            var statObjectArgs = new StatObjectArgs()
                .WithBucket(_bucketName)
                .WithObject(objectName);

            var stat = await _minioClient.StatObjectAsync(statObjectArgs, cancellationToken);
            return stat.Size;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to stat object '{ObjectName}' in bucket '{BucketName}'", objectName, _bucketName);
            throw new FileDownloadException($"Failed to read metadata for object '{objectName}' from MinIO.", ex);
        }
    }
}
