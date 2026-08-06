namespace Services.Indexing;

/// <summary>
/// Tokenize -> lowercase -> stop-word filter -> stem -> group into per-term (frequency, positions).
/// Mirrors KeywordIndexService's KeywordAnalyzer exactly - query terms must go through the identical
/// pipeline used at index time, or stemmed terms won't match postings. Kept as a per-service
/// duplicate rather than shared code, per this repo's "no shared code between services" convention.
/// </summary>
public sealed class KeywordAnalyzer : IKeywordAnalyzer
{
    private readonly ITokenizer _tokenizer;
    private readonly IStopWordFilter _stopWordFilter;
    private readonly IStemmer _stemmer;

    public KeywordAnalyzer(ITokenizer tokenizer, IStopWordFilter stopWordFilter, IStemmer stemmer)
    {
        _tokenizer = tokenizer;
        _stopWordFilter = stopWordFilter;
        _stemmer = stemmer;
    }

    public IReadOnlyList<ChunkTermStats> Analyze(string text)
    {
        var termPositions = new Dictionary<string, List<int>>();

        foreach (var token in _tokenizer.Tokenize(text))
        {
            var lowercased = token.Value.ToLowerInvariant();
            if (_stopWordFilter.IsStopWord(lowercased))
            {
                continue;
            }

            var stemmed = _stemmer.Stem(lowercased);
            if (!termPositions.TryGetValue(stemmed, out var positions))
            {
                positions = [];
                termPositions[stemmed] = positions;
            }

            positions.Add(token.Position);
        }

        return termPositions
            .Select(kvp => new ChunkTermStats(kvp.Key, kvp.Value.Count, kvp.Value.ToArray()))
            .ToList();
    }
}
