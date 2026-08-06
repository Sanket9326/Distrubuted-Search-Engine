namespace Entities;

/// <summary>
/// Singleton row (Id = SingletonId) holding corpus-wide totals needed for BM25's IDF and length
/// normalization (N and avgdl), maintained incrementally alongside postings/chunk-stats so query
/// time never needs a full-table aggregate.
/// </summary>
public sealed class IndexStats
{
    public const int SingletonId = 1;

    public int Id { get; init; } = SingletonId;

    public long TotalChunks { get; set; }

    public long TotalTokenLength { get; set; }
}
