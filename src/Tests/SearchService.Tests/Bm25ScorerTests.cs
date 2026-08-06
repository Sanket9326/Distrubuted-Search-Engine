using Services.KeywordSearch;

namespace SearchService.Tests;

public sealed class Bm25ScorerTests
{
    [Fact]
    public void ScoreTerm_WhenTotalChunksIsZero_ReturnsZero()
    {
        var score = Bm25Scorer.ScoreTerm(termFrequency: 3, documentFrequency: 1, totalChunks: 0, chunkLength: 10, averageChunkLength: 10, k1: 1.2, b: 0.75);

        Assert.Equal(0, score);
    }

    [Fact]
    public void ScoreTerm_WhenTermFrequencyIsZero_ReturnsZero()
    {
        var score = Bm25Scorer.ScoreTerm(termFrequency: 0, documentFrequency: 1, totalChunks: 100, chunkLength: 10, averageChunkLength: 10, k1: 1.2, b: 0.75);

        Assert.Equal(0, score);
    }

    [Fact]
    public void ScoreTerm_RarerTerm_ScoresHigherThanCommonTerm_AllElseEqual()
    {
        var rareTermScore = Bm25Scorer.ScoreTerm(termFrequency: 2, documentFrequency: 1, totalChunks: 100, chunkLength: 20, averageChunkLength: 20, k1: 1.2, b: 0.75);
        var commonTermScore = Bm25Scorer.ScoreTerm(termFrequency: 2, documentFrequency: 50, totalChunks: 100, chunkLength: 20, averageChunkLength: 20, k1: 1.2, b: 0.75);

        Assert.True(rareTermScore > commonTermScore);
    }

    [Fact]
    public void ScoreTerm_HigherTermFrequency_ScoresHigher_AllElseEqual()
    {
        var lowFrequencyScore = Bm25Scorer.ScoreTerm(termFrequency: 1, documentFrequency: 10, totalChunks: 100, chunkLength: 20, averageChunkLength: 20, k1: 1.2, b: 0.75);
        var highFrequencyScore = Bm25Scorer.ScoreTerm(termFrequency: 5, documentFrequency: 10, totalChunks: 100, chunkLength: 20, averageChunkLength: 20, k1: 1.2, b: 0.75);

        Assert.True(highFrequencyScore > lowFrequencyScore);
    }

    [Fact]
    public void ScoreTerm_LongerThanAverageChunk_ScoresLower_AllElseEqual()
    {
        var shortChunkScore = Bm25Scorer.ScoreTerm(termFrequency: 3, documentFrequency: 10, totalChunks: 100, chunkLength: 10, averageChunkLength: 20, k1: 1.2, b: 0.75);
        var longChunkScore = Bm25Scorer.ScoreTerm(termFrequency: 3, documentFrequency: 10, totalChunks: 100, chunkLength: 40, averageChunkLength: 20, k1: 1.2, b: 0.75);

        Assert.True(shortChunkScore > longChunkScore);
    }
}
