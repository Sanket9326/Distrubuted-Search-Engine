namespace Services.Indexing;

/// <summary>A stemmed term's stats within a single chunk/query: how many times it occurs and where.</summary>
public sealed record ChunkTermStats(string Term, int TermFrequency, int[] Positions);

public interface IKeywordAnalyzer
{
    IReadOnlyList<ChunkTermStats> Analyze(string text);
}
