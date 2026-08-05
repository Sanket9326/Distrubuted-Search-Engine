namespace Entities;

public sealed class DocumentChunk
{
    public Guid Id { get; init; }

    public string DocumentId { get; init; } = string.Empty;

    public int ChunkIndex { get; init; }

    public string Content { get; init; } = string.Empty;

    public int CharCount { get; init; }

    public DateTime CreatedAtUtc { get; init; }
}
