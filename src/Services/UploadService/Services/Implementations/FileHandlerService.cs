using Contracts.Events;
using Infrastructure;
using SharedKernel;

public class FileHandlerService : IFileHandlerService
{
    private readonly ILogger<FileHandlerService> _logger;
    private readonly IKafkaProducer _kafkaProducer;

    public FileHandlerService(ILogger<FileHandlerService> logger, IKafkaProducer kafkaProducer)
    {
        _logger = logger;
        _kafkaProducer = kafkaProducer;
    }

    public async Task<(bool IsSuccess, DocumentUploadedEvent Event)> HandleFileUploadAsync(IFormFile file)
    {
        try
        {
            var storagePath = Path.Combine(
                    Directory.GetCurrentDirectory(),
                    "Storage");

            Directory.CreateDirectory(storagePath);

            var filePath = Path.Combine(storagePath, file.FileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var documentEvent = new DocumentUploadedEvent
            {
                FileName = file.FileName,
                FilePath = filePath,
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