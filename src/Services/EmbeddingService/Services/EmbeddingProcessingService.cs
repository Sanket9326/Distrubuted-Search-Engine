using System.Text.Json;
using Contracts;
using Contracts.Events;
using Contracts.Reliability;
using Infrastructure;
using Prometheus;
using Repositories;
using Services.VectorStorage;
using SharedKernel;

namespace Services;

public interface IEmbeddingProcessingService
{
    Task ProcessAsync(ChunksCreatedEvent message, RetryContext retryContext, CancellationToken cancellationToken = default);
}

public sealed class EmbeddingProcessingService : IEmbeddingProcessingService
{
    private static readonly Counter ChunksEmbeddedTotal = Metrics.CreateCounter(
        "chunks_embedded_total", "Number of chunks successfully embedded and upserted into Qdrant");

    private readonly IDocumentChunkReadRepository _chunkRepository;
    private readonly IDocumentMetadataStatusRepository _metadataStatusRepository;
    private readonly IEmbeddingGenerator _embeddingGenerator;
    private readonly IVectorStore _vectorStore;
    private readonly IRetryQueue _retryQueue;
    private readonly ILogger<EmbeddingProcessingService> _logger;

    public EmbeddingProcessingService(
        IDocumentChunkReadRepository chunkRepository,
        IDocumentMetadataStatusRepository metadataStatusRepository,
        IEmbeddingGenerator embeddingGenerator,
        IVectorStore vectorStore,
        IRetryQueue retryQueue,
        ILogger<EmbeddingProcessingService> logger)
    {
        _chunkRepository = chunkRepository;
        _metadataStatusRepository = metadataStatusRepository;
        _embeddingGenerator = embeddingGenerator;
        _vectorStore = vectorStore;
        _retryQueue = retryQueue;
        _logger = logger;
    }

    public async Task ProcessAsync(ChunksCreatedEvent message, RetryContext retryContext, CancellationToken cancellationToken = default)
    {
        try
        {
            await _metadataStatusRepository.UpdateStatusAsync(message.DocumentId, DocumentProcessingStatus.Embedding, null, cancellationToken);

            var chunks = await _chunkRepository.GetByDocumentIdAsync(message.DocumentId, cancellationToken);
            if (chunks.Count == 0)
            {
                _logger.LogWarning("No chunks found for document '{DocumentId}'; skipping embedding.", message.DocumentId);
                await _metadataStatusRepository.UpdateStatusAsync(message.DocumentId, DocumentProcessingStatus.Embedded, null, cancellationToken);
                return;
            }

            var embeddings = await _embeddingGenerator.GenerateAsync(chunks.Select(c => c.Content).ToList(), cancellationToken);
            if (embeddings.Count != chunks.Count)
            {
                throw new InvalidOperationException(
                    $"Embedding count ({embeddings.Count}) does not match chunk count ({chunks.Count}) for document '{message.DocumentId}'.");
            }

            var (authorizedDepartments, fileName) = await _metadataStatusRepository.GetDocumentInfoAsync(message.DocumentId, cancellationToken);

            var embeddedChunks = chunks.Zip(embeddings, (chunk, embedding) =>
                new EmbeddedChunk(chunk.Id, chunk.DocumentId, fileName, chunk.ChunkIndex, chunk.Content, chunk.CreatedAtUtc, authorizedDepartments, embedding));

            await _vectorStore.UpsertAsync(embeddedChunks, cancellationToken);

            await _metadataStatusRepository.UpdateStatusAsync(message.DocumentId, DocumentProcessingStatus.Embedded, null, cancellationToken);

            ChunksEmbeddedTotal.Inc(chunks.Count);

            _logger.LogInformation("Document '{DocumentId}' embedded into {ChunkCount} vectors.", message.DocumentId, chunks.Count);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            var willRetry = await _retryQueue.ScheduleAsync(
                Constants.KafkaTopics.ChunksCreated,
                message.DocumentId,
                JsonSerializer.Serialize(message),
                retryContext.RetryCount,
                retryContext.FirstFailedAtUtc,
                ex.Message,
                cancellationToken);

            await _metadataStatusRepository.UpdateStatusAsync(
                message.DocumentId,
                willRetry ? DocumentProcessingStatus.PendingRetry : DocumentProcessingStatus.EmbeddingFailed,
                ex.Message,
                cancellationToken);

            _logger.LogError(ex, "Failed to generate embeddings for document '{DocumentId}' (attempt {RetryCount}).", message.DocumentId, retryContext.RetryCount + 1);
        }
    }
}
