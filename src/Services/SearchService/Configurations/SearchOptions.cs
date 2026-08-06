public sealed class SearchOptions
{
    public const string SectionName = "Search";

    public int DefaultTopK { get; init; } = 5;

    public int MaxTopK { get; init; } = 20;

    public int MaxQueryLength { get; init; } = 2000;

    public float MinimumScore { get; init; } = 0.5f;

    public int RetrievalMultiplier { get; init; } = 3;

    /// <summary>BM25 term-frequency saturation parameter.</summary>
    public double Bm25K1 { get; init; } = 1.2;

    /// <summary>BM25 chunk-length normalization parameter (0 = no normalization, 1 = full).</summary>
    public double Bm25B { get; init; } = 0.75;
}
