using Dtos;
using Infrastructure;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Services;
using Services.KeywordSearch;
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
        var keywordSearchStore = new FakeKeywordSearchStore(Array.Empty<ScoredChunk>());
        var reRanker = new FakeReRanker();

        var sut = CreateSut(embeddingGenerator, vectorSearchStore, keywordSearchStore, reRanker);

        var response = await sut.SearchAsync(new SearchRequest { Query = "hello", Departments = null });

        Assert.Empty(response.Results);
        Assert.False(vectorSearchStore.WasCalled);
        Assert.False(keywordSearchStore.WasCalled);
    }

    [Fact]
    public async Task SearchAsync_ClampsTopK_AndRequestsRetrievalMultiplierCandidates_FromBothStores()
    {
        var embeddingGenerator = new FakeEmbeddingGenerator(_ => new[] { new float[] { 0.1f, 0.2f } });
        var vectorSearchStore = new FakeVectorSearchStore(Array.Empty<ScoredChunk>());
        var keywordSearchStore = new FakeKeywordSearchStore(Array.Empty<ScoredChunk>());
        var reRanker = new FakeReRanker();

        var sut = CreateSut(embeddingGenerator, vectorSearchStore, keywordSearchStore, reRanker, new SearchOptions
        {
            DefaultTopK = 5,
            MaxTopK = 10,
            MinimumScore = 0.5f,
            RetrievalMultiplier = 3
        });

        await sut.SearchAsync(new SearchRequest { Query = "hello", Departments = new[] { "Finance" }, TopK = 100 });

        Assert.Equal(10 * 3, vectorSearchStore.LastLimit);
        Assert.Equal(0.5f, vectorSearchStore.LastMinimumScore);
        Assert.Equal(10 * 3, keywordSearchStore.LastLimit);
    }

    [Fact]
    public async Task SearchAsync_MapsRerankedChunks_IntoResponse()
    {
        var chunkId = Guid.NewGuid();
        var createdAt = DateTime.UtcNow;
        var candidate = new ScoredChunk(chunkId, "doc-1", "file.pdf", 2, "content", new[] { "Finance" }, createdAt, 0.9f);

        var embeddingGenerator = new FakeEmbeddingGenerator(_ => new[] { new float[] { 0.1f } });
        var vectorSearchStore = new FakeVectorSearchStore(new[] { candidate });
        var keywordSearchStore = new FakeKeywordSearchStore(Array.Empty<ScoredChunk>());
        var reRanker = new FakeReRanker();

        var sut = CreateSut(embeddingGenerator, vectorSearchStore, keywordSearchStore, reRanker);

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

    [Fact]
    public async Task SearchAsync_MergesVectorAndKeywordCandidates_DedupingByChunkId()
    {
        var sharedChunkId = Guid.NewGuid();
        var vectorOnlyId = Guid.NewGuid();
        var keywordOnlyId = Guid.NewGuid();
        var createdAt = DateTime.UtcNow;

        var vectorCandidates = new[]
        {
            new ScoredChunk(sharedChunkId, "doc-shared", "shared.pdf", 0, "shared content", new[] { "Finance" }, createdAt, 0.8f),
            new ScoredChunk(vectorOnlyId, "doc-vector", "vector.pdf", 0, "vector content", new[] { "Finance" }, createdAt, 0.7f)
        };
        var keywordCandidates = new[]
        {
            new ScoredChunk(sharedChunkId, "doc-shared", "shared.pdf", 0, "shared content", new[] { "Finance" }, createdAt, 5.0f),
            new ScoredChunk(keywordOnlyId, "doc-keyword", "keyword.pdf", 0, "keyword content", new[] { "Finance" }, createdAt, 3.0f)
        };

        var embeddingGenerator = new FakeEmbeddingGenerator(_ => new[] { new float[] { 0.1f } });
        var vectorSearchStore = new FakeVectorSearchStore(vectorCandidates);
        var keywordSearchStore = new FakeKeywordSearchStore(keywordCandidates);
        var reRanker = new FakeReRanker();

        var sut = CreateSut(embeddingGenerator, vectorSearchStore, keywordSearchStore, reRanker, new SearchOptions { DefaultTopK = 10 });

        await sut.SearchAsync(new SearchRequest { Query = "hello", Departments = new[] { "Finance" } });

        Assert.NotNull(reRanker.LastCandidates);
        Assert.Equal(3, reRanker.LastCandidates!.Count);
        Assert.Contains(reRanker.LastCandidates!, c => c.ChunkId == sharedChunkId);
        Assert.Contains(reRanker.LastCandidates!, c => c.ChunkId == vectorOnlyId);
        Assert.Contains(reRanker.LastCandidates!, c => c.ChunkId == keywordOnlyId);
    }

    private static SearchProcessingService CreateSut(
        FakeEmbeddingGenerator embeddingGenerator,
        FakeVectorSearchStore vectorSearchStore,
        FakeKeywordSearchStore keywordSearchStore,
        FakeReRanker reRanker,
        SearchOptions? options = null)
        => new(
            embeddingGenerator,
            vectorSearchStore,
            keywordSearchStore,
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

    private sealed class FakeKeywordSearchStore : IKeywordSearchStore
    {
        private readonly IReadOnlyList<ScoredChunk> _results;

        public FakeKeywordSearchStore(IReadOnlyList<ScoredChunk> results) => _results = results;

        public bool WasCalled { get; private set; }

        public int LastLimit { get; private set; }

        public Task<IReadOnlyList<ScoredChunk>> SearchAsync(
            string query, Contracts.Department departments, int limit, CancellationToken cancellationToken = default)
        {
            WasCalled = true;
            LastLimit = limit;
            return Task.FromResult(_results);
        }
    }

    private sealed class FakeReRanker : IReRanker
    {
        public bool WasCalled { get; private set; }

        public IReadOnlyList<ScoredChunk>? LastCandidates { get; private set; }

        public Task<IReadOnlyList<ScoredChunk>> RerankAsync(
            string query, IReadOnlyList<ScoredChunk> candidates, int topK, CancellationToken cancellationToken = default)
        {
            WasCalled = true;
            LastCandidates = candidates;
            return Task.FromResult<IReadOnlyList<ScoredChunk>>(candidates.Take(topK).ToList());
        }
    }
}
