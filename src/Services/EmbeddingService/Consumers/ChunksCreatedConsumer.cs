using Contracts.Events;
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

    public async Task ConsumeAsync(ChunksCreatedEvent message, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Processing ChunksCreatedEvent for document '{DocumentId}' ({ChunkCount} chunks) created at {CreatedAtUtc}",
            message.DocumentId,
            message.ChunkCount,
            message.CreatedAtUtc);

        await _processingService.ProcessAsync(message, cancellationToken);
    }
}
