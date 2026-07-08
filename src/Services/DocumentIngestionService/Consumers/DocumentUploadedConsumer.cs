using Contracts.Events;
using Entities;
using Repositories;

namespace Consumers;

public sealed class DocumentUploadedConsumer : IDocumentUploadedConsumer
{
    private readonly IDocumentMetadataRepository _repository;
    private readonly ILogger<DocumentUploadedConsumer> _logger;

    public DocumentUploadedConsumer(IDocumentMetadataRepository repository, ILogger<DocumentUploadedConsumer> logger)
    {
        _repository = repository;
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

        var metadata = new DocumentMetadata
        {
            DocumentId = message.DocumentId,
            FileName = message.FileName,
            ContentType = message.ContentType,
            AuthorizedDepartments = Department.None,
            UploadedAtUtc = message.UploadedAtUtc,
            IngestedAtUtc = DateTime.UtcNow
        };

        await _repository.AddAsync(metadata, cancellationToken);
    }
}
