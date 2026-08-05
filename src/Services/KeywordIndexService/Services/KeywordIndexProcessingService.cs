using System.Text.Json;
using Contracts.Events;
using Contracts.Reliability;
using Entities;
using Infrastructure;
using Prometheus;
using Repositories;
using Services.Indexing;
using SharedKernel;

namespace Services;

public interface IKeywordIndexProcessingService
{
    Task ProcessAsync(ChunksCreatedEvent message, RetryContext retryContext, CancellationToken cancellationToken = default);
}

public sealed class KeywordIndexProcessingService : IKeywordIndexProcessingService
{
    private static readonly Counter ChunksIndexedTotal = Metrics.CreateCounter(
        "chunks_keyword_indexed_total", "Number of chunks successfully tokenized and upserted into the inverted index");

    private readonly IDocumentChunkReadRepository _chunkRepository;
    private readonly IKeywordIndexStatusRepository _statusRepository;
    private readonly IIndexRepository _indexRepository;
    private readonly IKeywordAnalyzer _analyzer;
    private readonly IRetryQueue _retryQueue;
    private readonly ILogger<KeywordIndexProcessingService> _logger;

    public KeywordIndexProcessingService(
        IDocumentChunkReadRepository chunkRepository,
        IKeywordIndexStatusRepository statusRepository,
        IIndexRepository indexRepository,
        IKeywordAnalyzer analyzer,
        IRetryQueue retryQueue,
        ILogger<KeywordIndexProcessingService> logger)
    {
        _chunkRepository = chunkRepository;
        _statusRepository = statusRepository;
        _indexRepository = indexRepository;
        _analyzer = analyzer;
        _retryQueue = retryQueue;
        _logger = logger;
    }

    public async Task ProcessAsync(ChunksCreatedEvent message, RetryContext retryContext, CancellationToken cancellationToken = default)
    {
        try
        {
            await _statusRepository.UpdateStatusAsync(message.DocumentId, KeywordIndexStatus.Indexing, null, cancellationToken);

            var chunks = await _chunkRepository.GetByDocumentIdAsync(message.DocumentId, cancellationToken);
            if (chunks.Count == 0)
            {
                _logger.LogWarning("No chunks found for document '{DocumentId}'; skipping keyword indexing.", message.DocumentId);
                await _statusRepository.UpdateStatusAsync(message.DocumentId, KeywordIndexStatus.Indexed, null, cancellationToken);
                return;
            }

            var chunkTermStats = chunks.ToDictionary(
                chunk => chunk.Id,
                chunk => (IReadOnlyList<ChunkTermStats>)_analyzer.Analyze(chunk.Content));

            await _indexRepository.ReindexDocumentAsync(message.DocumentId, chunkTermStats, cancellationToken);

            await _statusRepository.UpdateStatusAsync(message.DocumentId, KeywordIndexStatus.Indexed, null, cancellationToken);

            ChunksIndexedTotal.Inc(chunks.Count);

            _logger.LogInformation("Document '{DocumentId}' keyword-indexed across {ChunkCount} chunks.", message.DocumentId, chunks.Count);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            var willRetry = await _retryQueue.ScheduleAsync(
                Constants.KafkaTopics.KeywordIndexing,
                message.DocumentId,
                JsonSerializer.Serialize(message),
                retryContext.RetryCount,
                retryContext.FirstFailedAtUtc,
                ex.Message,
                cancellationToken);

            await _statusRepository.UpdateStatusAsync(
                message.DocumentId,
                willRetry ? KeywordIndexStatus.PendingRetry : KeywordIndexStatus.IndexingFailed,
                ex.Message,
                cancellationToken);

            _logger.LogError(ex, "Failed to keyword-index document '{DocumentId}' (attempt {RetryCount}).", message.DocumentId, retryContext.RetryCount + 1);
        }
    }
}
