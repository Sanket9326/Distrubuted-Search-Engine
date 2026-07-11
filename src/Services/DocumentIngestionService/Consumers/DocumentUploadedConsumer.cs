using Contracts.Events;
using Services;

namespace Consumers;

public sealed class DocumentUploadedConsumer : IDocumentUploadedConsumer
{
    private readonly IDocumentProcessingService _processingService;
    private readonly ILogger<DocumentUploadedConsumer> _logger;

    public DocumentUploadedConsumer(IDocumentProcessingService processingService, ILogger<DocumentUploadedConsumer> logger)
    {
        _processingService = processingService;
        _logger = logger;
    }

    public async Task ConsumeAsync(DocumentUploadedEvent message, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Processing DocumentUploadedEvent for document '{DocumentId}' ({FileName}, {ContentType}) uploaded at {UploadedAtUtc}",
            message.DocumentId,
            message.FileName,
            message.ContentType,
            message.UploadedAtUtc);

        await _processingService.ProcessAsync(message, cancellationToken);
    }
}
