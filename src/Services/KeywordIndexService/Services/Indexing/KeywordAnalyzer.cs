namespace Services.Indexing;

/// <summary>
/// Tokenize -> lowercase -> stop-word filter -> stem -> group into per-term (frequency, positions).
/// Positions are assigned during tokenization, before stop-word removal, so they reflect true word
/// offsets in the original text (gaps appear where stop words were dropped) - needed for phrase/
/// proximity queries later, not just single-term lookup.
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

    public IReadOnlyList<ChunkTermStats> Analyze(string chunkContent)
    {
        var termPositions = new Dictionary<string, List<int>>();

        foreach (var token in _tokenizer.Tokenize(chunkContent))
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
