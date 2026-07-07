using Contracts.Events;
using Infrastructure;
using SharedKernel;

public class FileHandlerService : IFileHandlerService
{
    private readonly ILogger<FileHandlerService> _logger;
    private readonly IKafkaProducer _kafkaProducer;
    private readonly IMinioStorage _minioStorage;

    public FileHandlerService(ILogger<FileHandlerService> logger, IKafkaProducer kafkaProducer, IMinioStorage minioStorage)
    {
        _logger = logger;
        _kafkaProducer = kafkaProducer;
        _minioStorage = minioStorage;
    }

    public async Task<(bool IsSuccess, DocumentUploadedEvent Event)> HandleFileUploadAsync(IFormFile file)
    {
        try
        {
            var objectName = $"{Guid.NewGuid()}-{file.FileName}";

            using var stream = file.OpenReadStream();
            var storedObjectName = await _minioStorage.UploadFileAsync(objectName, stream, file.Length, file.ContentType);

            var documentEvent = new DocumentUploadedEvent
            {
                DocumentId = objectName,
                FileName = file.FileName,
                ContentType = file.ContentType,
                UploadedAtUtc = DateTime.UtcNow
            };

            await _kafkaProducer.PublishAsync(Constants.KafkaTopics.DocumentIngestion, documentEvent);

            return (true, documentEvent);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to handle file upload for {FileName}", file.FileName);
            return (false, new DocumentUploadedEvent());
        }
    }
}