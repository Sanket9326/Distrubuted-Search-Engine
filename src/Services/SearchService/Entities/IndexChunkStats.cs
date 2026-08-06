namespace Entities;

/// <summary>Read-only mapping onto index_chunk_stats, owned and migrated by KeywordIndexService.</summary>
public sealed class IndexChunkStats
{
    public Guid ChunkId { get; init; }

    public string DocumentId { get; init; } = string.Empty;

    public int TokenCount { get; init; }
}
