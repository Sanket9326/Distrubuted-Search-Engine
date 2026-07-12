namespace Contracts.Events;

public sealed class ChunksCreatedEvent
{
    public string DocumentId { get; init; } = string.Empty;

    public int ChunkCount { get; init; }

    public DateTime CreatedAtUtc { get; init; } = DateTime.UtcNow;
}
