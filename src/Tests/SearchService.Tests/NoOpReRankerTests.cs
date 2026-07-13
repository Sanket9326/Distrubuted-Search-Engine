using Services.ReRanking;
using Services.VectorSearch;

namespace SearchService.Tests;

public sealed class NoOpReRankerTests
{
    [Fact]
    public async Task RerankAsync_TruncatesToTopK_PreservingOrder()
    {
        var chunks = Enumerable.Range(0, 5)
            .Select(i => new ScoredChunk(Guid.NewGuid(), $"doc-{i}", "file.pdf", i, "content", Array.Empty<string>(), DateTime.UtcNow, 1f - (i * 0.1f)))
            .ToList();

        var sut = new NoOpReRanker();

        var result = await sut.RerankAsync("query", chunks, topK: 3);

        Assert.Equal(3, result.Count);
        Assert.Equal(chunks.Take(3), result);
    }

    [Fact]
    public async Task RerankAsync_WhenFewerCandidatesThanTopK_ReturnsAllCandidates()
    {
        var chunks = new List<ScoredChunk>
        {
            new(Guid.NewGuid(), "doc-1", "file.pdf", 0, "content", Array.Empty<string>(), DateTime.UtcNow, 0.9f)
        };

        var sut = new NoOpReRanker();

        var result = await sut.RerankAsync("query", chunks, topK: 5);

        Assert.Single(result);
    }
}
