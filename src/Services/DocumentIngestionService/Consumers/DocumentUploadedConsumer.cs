using Contracts.Events;
using Contracts.Reliability;
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

    public async Task ConsumeAsync(DocumentUploadedEvent message, RetryContext retryContext, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Processing DocumentUploadedEvent for document '{DocumentId}' ({FileName}, {ContentType}) uploaded at {UploadedAtUtc}, retry attempt {RetryCount}",
            message.DocumentId,
            message.FileName,
            message.ContentType,
            message.UploadedAtUtc,
            retryContext.RetryCount);

        await _processingService.ProcessAsync(message, retryContext, cancellationToken);
    }
}
