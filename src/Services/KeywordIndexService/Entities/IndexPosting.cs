namespace Entities;

public sealed class IndexPosting
{
    public int TermId { get; init; }

    public Guid ChunkId { get; init; }

    /// <summary>Denormalized from the chunk so search-time filtering doesn't need a join back into document_chunks.</summary>
    public string DocumentId { get; init; } = string.Empty;

    /// <summary>Raw occurrence count of the term within this chunk.</summary>
    public int TermFrequency { get; init; }

    /// <summary>0-based word offsets within the chunk, assigned before stop-word removal so gaps reflect true text positions.</summary>
    public int[] Positions { get; init; } = [];
}
