namespace Entities;

/// <summary>Per-chunk token count (post stop-word-filter), needed for BM25 length normalization.</summary>
public sealed class IndexChunkStats
{
    public Guid ChunkId { get; init; }

    /// <summary>Denormalized so a document's chunk-stats rows can be found/removed without a term join.</summary>
    public string DocumentId { get; init; } = string.Empty;

    public int TokenCount { get; init; }
}
