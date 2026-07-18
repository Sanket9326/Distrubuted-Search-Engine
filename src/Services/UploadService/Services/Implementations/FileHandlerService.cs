using Common.Utilities;
using Contracts.Events;
using Infrastructure;
using Prometheus;
using SharedKernel;

public class FileHandlerService : IFileHandlerService
{
    private static readonly Counter DocumentsUploadedTotal = Metrics.CreateCounter(
        "documents_uploaded_total", "Number of documents successfully uploaded");

    private readonly ILogger<FileHandlerService> _logger;
    private readonly IKafkaProducer _kafkaProducer;
    private readonly IMinioStorage _minioStorage;
    private readonly IGuidGenerator _guidGenerator;

    public FileHandlerService(
        ILogger<FileHandlerService> logger,
        IKafkaProducer kafkaProducer,
        IMinioStorage minioStorage,
        IGuidGenerator guidGenerator)
    {
        _logger = logger;
        _kafkaProducer = kafkaProducer;
        _minioStorage = minioStorage;
        _guidGenerator = guidGenerator;
    }

    public async Task<(bool IsSuccess, DocumentUploadedEvent Event)> HandleFileUploadAsync(IFormFile file, string? departments)
    {
        try
        {
            var objectName = $"{_guidGenerator.NewGuid()}-{file.FileName}";

            using var stream = file.OpenReadStream();
            var storedObjectName = await _minioStorage.UploadFileAsync(objectName, stream, file.Length, file.ContentType);

            var documentEvent = new DocumentUploadedEvent
            {
                DocumentId = objectName,
                FileName = file.FileName,
                ContentType = file.ContentType,
                AuthorizedDepartments = DepartmentParser.Parse(departments),
                UploadedAtUtc = DateTime.UtcNow
            };

            await _kafkaProducer.PublishAsync(Constants.KafkaTopics.DocumentIngestion, documentEvent);

            DocumentsUploadedTotal.Inc();

            return (true, documentEvent);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to handle file upload for {FileName}", file.FileName);
            return (false, new DocumentUploadedEvent());
        }
    }
}