namespace Services.KeywordSearch;

/// <summary>Standard Okapi BM25 term score, isolated as a pure function so it's testable without a database.</summary>
public static class Bm25Scorer
{
    public static double ScoreTerm(
        int termFrequency,
        int documentFrequency,
        long totalChunks,
        int chunkLength,
        double averageChunkLength,
        double k1,
        double b)
    {
        if (totalChunks <= 0 || documentFrequency <= 0 || termFrequency <= 0)
        {
            return 0;
        }

        var idf = Math.Log(1 + ((totalChunks - documentFrequency + 0.5) / (documentFrequency + 0.5)));
        var lengthNorm = averageChunkLength > 0 ? chunkLength / averageChunkLength : 1.0;
        var denominator = termFrequency + (k1 * (1 - b + (b * lengthNorm)));

        return idf * (termFrequency * (k1 + 1)) / denominator;
    }
}
