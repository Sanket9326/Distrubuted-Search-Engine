namespace Entities;

/// <summary>Read-only mapping onto index_postings, owned and migrated by KeywordIndexService.</summary>
public sealed class IndexPosting
{
    public int TermId { get; init; }

    public Guid ChunkId { get; init; }

    public string DocumentId { get; init; } = string.Empty;

    public int TermFrequency { get; init; }

    public int[] Positions { get; init; } = [];
}
