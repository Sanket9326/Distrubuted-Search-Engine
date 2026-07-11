public sealed class ChunkingOptions
{
    public const string SectionName = "Chunking";

    public int MaxChunkSize { get; init; } = 1200;

    public int OverlapSize { get; init; } = 150;
}
