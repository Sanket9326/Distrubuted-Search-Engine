public sealed class SearchOptions
{
    public const string SectionName = "Search";

    public int DefaultTopK { get; init; } = 5;

    public int MaxTopK { get; init; } = 20;

    public int MaxQueryLength { get; init; } = 2000;

    public float MinimumScore { get; init; } = 0.5f;

    public int RetrievalMultiplier { get; init; } = 3;
}
