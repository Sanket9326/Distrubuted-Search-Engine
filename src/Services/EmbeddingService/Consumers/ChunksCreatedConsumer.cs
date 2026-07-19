using Contracts.Events;
using Contracts.Reliability;
using Services;

namespace Consumers;

public sealed class ChunksCreatedConsumer : IChunksCreatedConsumer
{
    private readonly IEmbeddingProcessingService _processingService;
    private readonly ILogger<ChunksCreatedConsumer> _logger;

    public ChunksCreatedConsumer(IEmbeddingProcessingService processingService, ILogger<ChunksCreatedConsumer> logger)
    {
        _processingService = processingService;
        _logger = logger;
    }

    public async Task ConsumeAsync(ChunksCreatedEvent message, RetryContext retryContext, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Processing ChunksCreatedEvent for document '{DocumentId}' ({ChunkCount} chunks) created at {CreatedAtUtc}, retry attempt {RetryCount}",
            message.DocumentId,
            message.ChunkCount,
            message.CreatedAtUtc,
            retryContext.RetryCount);

        await _processingService.ProcessAsync(message, retryContext, cancellationToken);
    }
}
