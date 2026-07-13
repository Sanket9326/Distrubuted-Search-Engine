using Dtos;
using Infrastructure;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Services;
using Services.ReRanking;
using Services.VectorSearch;

namespace SearchService.Tests;

public sealed class SearchProcessingServiceTests
{
    [Fact]
    public async Task SearchAsync_WhenNoRecognizedDepartments_ReturnsEmptyResults_AndSkipsEmbeddingGenerator()
    {
        var embeddingGenerator = new FakeEmbeddingGenerator(_ => throw new InvalidOperationException("Should not be called"));
        var vectorSearchStore = new FakeVectorSearchStore(Array.Empty<ScoredChunk>());
        var reRanker = new FakeReRanker();

        var sut = CreateSut(embeddingGenerator, vectorSearchStore, reRanker);

        var response = await sut.SearchAsync(new SearchRequest { Query = "hello", Departments = null });

        Assert.Empty(response.Results);
        Assert.False(vectorSearchStore.WasCalled);
    }

    [Fact]
    public async Task SearchAsync_ClampsTopK_AndRequestsRetrievalMultiplierCandidates()
    {
        var embeddingGenerator = new FakeEmbeddingGenerator(_ => new[] { new float[] { 0.1f, 0.2f } });
        var vectorSearchStore = new FakeVectorSearchStore(Array.Empty<ScoredChunk>());
        var reRanker = new FakeReRanker();

        var sut = CreateSut(embeddingGenerator, vectorSearchStore, reRanker, new SearchOptions
        {
            DefaultTopK = 5,
            MaxTopK = 10,
            MinimumScore = 0.5f,
            RetrievalMultiplier = 3
        });

        await sut.SearchAsync(new SearchRequest { Query = "hello", Departments = new[] { "Finance" }, TopK = 100 });

        Assert.Equal(10 * 3, vectorSearchStore.LastLimit);
        Assert.Equal(0.5f, vectorSearchStore.LastMinimumScore);
    }

    [Fact]
    public async Task SearchAsync_MapsRerankedChunks_IntoResponse()
    {
        var chunkId = Guid.NewGuid();
        var createdAt = DateTime.UtcNow;
        var candidate = new ScoredChunk(chunkId, "doc-1", "file.pdf", 2, "content", new[] { "Finance" }, createdAt, 0.9f);

        var embeddingGenerator = new FakeEmbeddingGenerator(_ => new[] { new float[] { 0.1f } });
        var vectorSearchStore = new FakeVectorSearchStore(new[] { candidate });
        var reRanker = new FakeReRanker();

        var sut = CreateSut(embeddingGenerator, vectorSearchStore, reRanker);

        var response = await sut.SearchAsync(new SearchRequest { Query = "hello", Departments = new[] { "Finance" } });

        var result = Assert.Single(response.Results);
        Assert.Equal(chunkId, result.ChunkId);
        Assert.Equal("doc-1", result.DocumentId);
        Assert.Equal("file.pdf", result.FileName);
        Assert.Equal(2, result.ChunkIndex);
        Assert.Equal("content", result.Content);
        Assert.Equal(new[] { "Finance" }, result.Departments);
        Assert.Equal(0.9f, result.Score);
        Assert.Equal(createdAt, result.CreatedAtUtc);
        Assert.True(reRanker.WasCalled);
    }

    private static SearchProcessingService CreateSut(
        FakeEmbeddingGenerator embeddingGenerator,
        FakeVectorSearchStore vectorSearchStore,
        FakeReRanker reRanker,
        SearchOptions? options = null)
        => new(
            embeddingGenerator,
            vectorSearchStore,
            reRanker,
            Microsoft.Extensions.Options.Options.Create(options ?? new SearchOptions()),
            NullLogger<SearchProcessingService>.Instance);

    private sealed class FakeEmbeddingGenerator : IEmbeddingGenerator
    {
        private readonly Func<IReadOnlyList<string>, IReadOnlyList<float[]>> _generate;

        public FakeEmbeddingGenerator(Func<IReadOnlyList<string>, IReadOnlyList<float[]>> generate) => _generate = generate;

        public Task<IReadOnlyList<float[]>> GenerateAsync(IReadOnlyList<string> texts, CancellationToken cancellationToken = default)
            => Task.FromResult(_generate(texts));
    }

    private sealed class FakeVectorSearchStore : IVectorSearchStore
    {
        private readonly IReadOnlyList<ScoredChunk> _results;

        public FakeVectorSearchStore(IReadOnlyList<ScoredChunk> results) => _results = results;

        public bool WasCalled { get; private set; }

        public int LastLimit { get; private set; }

        public float LastMinimumScore { get; private set; }

        public Task<IReadOnlyList<ScoredChunk>> SearchAsync(
            float[] queryVector, Contracts.Department departments, int limit, float minimumScore, CancellationToken cancellationToken = default)
        {
            WasCalled = true;
            LastLimit = limit;
            LastMinimumScore = minimumScore;
            return Task.FromResult(_results);
        }
    }

    private sealed class FakeReRanker : IReRanker
    {
        public bool WasCalled { get; private set; }

        public Task<IReadOnlyList<ScoredChunk>> RerankAsync(
            string query, IReadOnlyList<ScoredChunk> candidates, int topK, CancellationToken cancellationToken = default)
        {
            WasCalled = true;
            return Task.FromResult<IReadOnlyList<ScoredChunk>>(candidates.Take(topK).ToList());
        }
    }
}
