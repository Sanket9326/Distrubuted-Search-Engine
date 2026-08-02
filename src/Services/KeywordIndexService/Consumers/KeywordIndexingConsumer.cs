using Contracts.Events;
using Contracts.Reliability;
using Services;

namespace Consumers;

public sealed class KeywordIndexingConsumer : IKeywordIndexingConsumer
{
    private readonly IKeywordIndexProcessingService _processingService;
    private readonly ILogger<KeywordIndexingConsumer> _logger;

    public KeywordIndexingConsumer(IKeywordIndexProcessingService processingService, ILogger<KeywordIndexingConsumer> logger)
    {
        _processingService = processingService;
        _logger = logger;
    }

    public async Task ConsumeAsync(ChunksCreatedEvent message, RetryContext retryContext, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Processing ChunksCreatedEvent for keyword indexing of document '{DocumentId}' ({ChunkCount} chunks) created at {CreatedAtUtc}, retry attempt {RetryCount}",
            message.DocumentId,
            message.ChunkCount,
            message.CreatedAtUtc,
            retryContext.RetryCount);

        await _processingService.ProcessAsync(message, retryContext, cancellationToken);
    }
}
