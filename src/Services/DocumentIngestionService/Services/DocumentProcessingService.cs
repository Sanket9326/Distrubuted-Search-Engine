using Common.FileValidation;
using Common.TextProcessing;
using Contracts;
using Contracts.Events;
using Entities;
using Exceptions;
using Infrastructure;
using Microsoft.Extensions.Options;
using Repositories;
using Services.Chunking;
using Services.TextExtraction;
using SharedKernel;

namespace Services;

public interface IDocumentProcessingService
{
    Task ProcessAsync(DocumentUploadedEvent message, CancellationToken cancellationToken = default);
}

public sealed class DocumentProcessingService : IDocumentProcessingService
{
    private readonly IDocumentMetadataRepository _metadataRepository;
    private readonly IDocumentChunkRepository _chunkRepository;
    private readonly IFileStorage _fileStorage;
    private readonly IFileSignatureValidator _fileSignatureValidator;
    private readonly ITextExtractorResolver _textExtractorResolver;
    private readonly IChunkingService _chunkingService;
    private readonly IKafkaProducer _kafkaProducer;
    private readonly ChunkingOptions _chunkingOptions;
    private readonly long _maxFileSizeBytes;
    private readonly ILogger<DocumentProcessingService> _logger;

    public DocumentProcessingService(
        IDocumentMetadataRepository metadataRepository,
        IDocumentChunkRepository chunkRepository,
        IFileStorage fileStorage,
        IFileSignatureValidator fileSignatureValidator,
        ITextExtractorResolver textExtractorResolver,
        IChunkingService chunkingService,
        IKafkaProducer kafkaProducer,
        IOptions<ChunkingOptions> chunkingOptions,
        IOptions<FileProcessingOptions> fileProcessingOptions,
        ILogger<DocumentProcessingService> logger)
    {
        _metadataRepository = metadataRepository;
        _chunkRepository = chunkRepository;
        _fileStorage = fileStorage;
        _fileSignatureValidator = fileSignatureValidator;
        _textExtractorResolver = textExtractorResolver;
        _chunkingService = chunkingService;
        _kafkaProducer = kafkaProducer;
        _chunkingOptions = chunkingOptions.Value;
        _maxFileSizeBytes = fileProcessingOptions.Value.MaxFileSizeBytes;
        _logger = logger;
    }

    public async Task ProcessAsync(DocumentUploadedEvent message, CancellationToken cancellationToken = default)
    {
        var metadata = new DocumentMetadata
        {
            DocumentId = message.DocumentId,
            FileName = message.FileName,
            ContentType = message.ContentType,
            AuthorizedDepartments = message.AuthorizedDepartments,
            UploadedAtUtc = message.UploadedAtUtc,
            IngestedAtUtc = DateTime.UtcNow,
            Status = DocumentProcessingStatus.Pending
        };

        await _metadataRepository.AddAsync(metadata, cancellationToken);

        try
        {
            await _metadataRepository.UpdateStatusAsync(message.DocumentId, DocumentProcessingStatus.Processing, null, cancellationToken);

            var size = await _fileStorage.GetSizeAsync(message.DocumentId, cancellationToken);
            if (size > _maxFileSizeBytes)
            {
                throw new FileTooLargeException(
                    $"File '{message.FileName}' is {size} bytes, exceeding the max allowed size of {_maxFileSizeBytes} bytes.");
            }

            await using var content = await _fileStorage.DownloadAsync(message.DocumentId, cancellationToken);

            var validationResult = await _fileSignatureValidator.ValidateAsync(message.FileName, content, cancellationToken);
            if (!validationResult.IsValid || validationResult.FileType is null)
            {
                await _metadataRepository.UpdateStatusAsync(
                    message.DocumentId, DocumentProcessingStatus.Unsupported, validationResult.ErrorMessage, cancellationToken);
                _logger.LogWarning("Document '{DocumentId}' rejected: {Reason}", message.DocumentId, validationResult.ErrorMessage);
                return;
            }

            var extractor = _textExtractorResolver.Resolve(validationResult.FileType.Value);
            var rawText = await extractor.ExtractAsync(content, cancellationToken);
            var text = TextSanitizer.Sanitize(rawText);

            var chunks = _chunkingService.Chunk(message.DocumentId, text, _chunkingOptions);
            if (chunks.Count > 0)
            {
                await _chunkRepository.AddRangeAsync(chunks, cancellationToken);

                await _kafkaProducer.PublishAsync(
                    Constants.KafkaTopics.ChunksCreated,
                    new ChunksCreatedEvent
                    {
                        DocumentId = message.DocumentId,
                        ChunkCount = chunks.Count,
                        CreatedAtUtc = DateTime.UtcNow
                    },
                    cancellationToken);
            }

            var noExtractableText = string.IsNullOrWhiteSpace(text);
            await _metadataRepository.UpdateStatusAsync(
                message.DocumentId,
                DocumentProcessingStatus.Chunked,
                noExtractableText ? "No extractable text found (document may be image-only or scanned)." : null,
                cancellationToken);

            if (noExtractableText)
            {
                _logger.LogWarning("Document '{DocumentId}' produced no extractable text (image-only or scanned content?).", message.DocumentId);
            }
            else
            {
                _logger.LogInformation("Document '{DocumentId}' chunked into {ChunkCount} chunks.", message.DocumentId, chunks.Count);
            }
        }
        catch (UnsupportedFileTypeException ex)
        {
            await _metadataRepository.UpdateStatusAsync(message.DocumentId, DocumentProcessingStatus.Unsupported, ex.Message, cancellationToken);
            _logger.LogWarning(ex, "Document '{DocumentId}' is an unsupported file type.", message.DocumentId);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            await _metadataRepository.UpdateStatusAsync(message.DocumentId, DocumentProcessingStatus.Failed, ex.Message, cancellationToken);
            _logger.LogError(ex, "Failed to process document '{DocumentId}'.", message.DocumentId);
        }
    }
}
