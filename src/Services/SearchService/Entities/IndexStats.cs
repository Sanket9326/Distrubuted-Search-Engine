namespace Entities;

/// <summary>Read-only mapping onto index_stats' singleton row, owned and migrated by KeywordIndexService.</summary>
public sealed class IndexStats
{
    public const int SingletonId = 1;

    public int Id { get; init; }

    public long TotalChunks { get; init; }

    public long TotalTokenLength { get; init; }
}
