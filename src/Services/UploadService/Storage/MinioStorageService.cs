using Infrastructure;
using Microsoft.Extensions.Options;
using Minio;
using Minio.DataModel.Args;

public sealed class MinioStorageService : IMinioStorage
{
    private readonly IMinioClient _minioClient;
    private readonly ILogger<MinioStorageService> _logger;
    private readonly string _bucketName;

    public MinioStorageService(IOptions<MinioSettings> settings, ILogger<MinioStorageService> logger)
    {
        _logger = logger;
        _bucketName = settings.Value.BucketName;

        _minioClient = new MinioClient()
            .WithEndpoint(settings.Value.Endpoint)
            .WithCredentials(settings.Value.AccessKey, settings.Value.SecretKey)
            .WithSSL(settings.Value.UseSSL)
            .Build();
    }

    public async Task<string> UploadFileAsync(
        string objectName,
        Stream fileStream,
        long fileSize,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var bucketExistsArgs = new BucketExistsArgs().WithBucket(_bucketName);
            var bucketExists = await _minioClient.BucketExistsAsync(bucketExistsArgs, cancellationToken);

            if (!bucketExists)
            {
                var makeBucketArgs = new MakeBucketArgs().WithBucket(_bucketName);
                await _minioClient.MakeBucketAsync(makeBucketArgs, cancellationToken);
            }

            var putObjectArgs = new PutObjectArgs()
                .WithBucket(_bucketName)
                .WithObject(objectName)
                .WithStreamData(fileStream)
                .WithObjectSize(fileSize)
                .WithContentType(contentType);

            await _minioClient.PutObjectAsync(putObjectArgs, cancellationToken);

            _logger.LogInformation(
                "Uploaded object '{ObjectName}' to bucket '{BucketName}'",
                objectName,
                _bucketName);

            return objectName;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to upload object '{ObjectName}' to bucket '{BucketName}'",
                objectName,
                _bucketName);
            throw;
        }
    }
}
