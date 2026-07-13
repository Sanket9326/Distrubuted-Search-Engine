using Contracts;
using Contracts.Events;
using Entities;
using Infrastructure;
using Microsoft.Extensions.Logging.Abstractions;
using Repositories;
using Services;
using Services.VectorStorage;

namespace EmbeddingService.Tests;

public sealed class EmbeddingProcessingServiceTests
{
    [Fact]
    public async Task ProcessAsync_EmbedsChunks_AndMarksDocumentEmbedded()
    {
        var chunkRepository = new FakeChunkRepository(new[]
        {
            new DocumentChunk { Id = Guid.NewGuid(), DocumentId = "doc-1", ChunkIndex = 0, Content = "hello", CharCount = 5, CreatedAtUtc = DateTime.UtcNow },
            new DocumentChunk { Id = Guid.NewGuid(), DocumentId = "doc-1", ChunkIndex = 1, Content = "world", CharCount = 5, CreatedAtUtc = DateTime.UtcNow }
        });
        var statusRepository = new FakeStatusRepository();
        var embeddingGenerator = new FakeEmbeddingGenerator(texts => texts.Select(_ => new float[] { 0.1f, 0.2f }).ToList());
        var vectorStore = new FakeVectorStore();

        var sut = new EmbeddingProcessingService(
            chunkRepository, statusRepository, embeddingGenerator, vectorStore, NullLogger<EmbeddingProcessingService>.Instance);

        await sut.ProcessAsync(new ChunksCreatedEvent { DocumentId = "doc-1", ChunkCount = 2, CreatedAtUtc = DateTime.UtcNow });

        Assert.Equal(2, vectorStore.UpsertedChunks.Count);
        Assert.Equal(new[] { DocumentProcessingStatus.Embedding, DocumentProcessingStatus.Embedded }, statusRepository.RecordedStatuses);
    }

    [Fact]
    public async Task ProcessAsync_CarriesAuthorizedDepartments_IntoUpsertedChunks()
    {
        var chunkRepository = new FakeChunkRepository(new[]
        {
            new DocumentChunk { Id = Guid.NewGuid(), DocumentId = "doc-4", ChunkIndex = 0, Content = "hello", CharCount = 5, CreatedAtUtc = DateTime.UtcNow }
        });
        var statusRepository = new FakeStatusRepository { AuthorizedDepartments = Department.Finance | Department.Engineering };
        var embeddingGenerator = new FakeEmbeddingGenerator(texts => texts.Select(_ => new float[] { 0.1f }).ToList());
        var vectorStore = new FakeVectorStore();

        var sut = new EmbeddingProcessingService(
            chunkRepository, statusRepository, embeddingGenerator, vectorStore, NullLogger<EmbeddingProcessingService>.Instance);

        await sut.ProcessAsync(new ChunksCreatedEvent { DocumentId = "doc-4", ChunkCount = 1, CreatedAtUtc = DateTime.UtcNow });

        var upserted = Assert.Single(vectorStore.UpsertedChunks);
        Assert.Equal(Department.Finance | Department.Engineering, upserted.AuthorizedDepartments);
    }

    [Fact]
    public async Task ProcessAsync_WhenNoChunksExist_MarksEmbeddedWithoutCallingGeneratorOrStore()
    {
        var chunkRepository = new FakeChunkRepository(Array.Empty<DocumentChunk>());
        var statusRepository = new FakeStatusRepository();
        var embeddingGenerator = new FakeEmbeddingGenerator(texts => throw new InvalidOperationException("Should not be called"));
        var vectorStore = new FakeVectorStore();

        var sut = new EmbeddingProcessingService(
            chunkRepository, statusRepository, embeddingGenerator, vectorStore, NullLogger<EmbeddingProcessingService>.Instance);

        await sut.ProcessAsync(new ChunksCreatedEvent { DocumentId = "doc-2", ChunkCount = 0, CreatedAtUtc = DateTime.UtcNow });

        Assert.Empty(vectorStore.UpsertedChunks);
        Assert.Equal(new[] { DocumentProcessingStatus.Embedding, DocumentProcessingStatus.Embedded }, statusRepository.RecordedStatuses);
    }

    [Fact]
    public async Task ProcessAsync_WhenEmbeddingCountMismatches_MarksEmbeddingFailed()
    {
        var chunkRepository = new FakeChunkRepository(new[]
        {
            new DocumentChunk { Id = Guid.NewGuid(), DocumentId = "doc-3", ChunkIndex = 0, Content = "hello", CharCount = 5, CreatedAtUtc = DateTime.UtcNow }
        });
        var statusRepository = new FakeStatusRepository();
        var embeddingGenerator = new FakeEmbeddingGenerator(_ => Array.Empty<float[]>());
        var vectorStore = new FakeVectorStore();

        var sut = new EmbeddingProcessingService(
            chunkRepository, statusRepository, embeddingGenerator, vectorStore, NullLogger<EmbeddingProcessingService>.Instance);

        await sut.ProcessAsync(new ChunksCreatedEvent { DocumentId = "doc-3", ChunkCount = 1, CreatedAtUtc = DateTime.UtcNow });

        Assert.Empty(vectorStore.UpsertedChunks);
        Assert.Equal(
            new[] { DocumentProcessingStatus.Embedding, DocumentProcessingStatus.EmbeddingFailed },
            statusRepository.RecordedStatuses);
    }

    private sealed class FakeChunkRepository : IDocumentChunkReadRepository
    {
        private readonly IReadOnlyList<DocumentChunk> _chunks;

        public FakeChunkRepository(IReadOnlyList<DocumentChunk> chunks) => _chunks = chunks;

        public Task<IReadOnlyList<DocumentChunk>> GetByDocumentIdAsync(string documentId, CancellationToken cancellationToken = default)
            => Task.FromResult(_chunks);
    }

    private sealed class FakeStatusRepository : IDocumentMetadataStatusRepository
    {
        public List<DocumentProcessingStatus> RecordedStatuses { get; } = new();

        public Department AuthorizedDepartments { get; set; } = Department.None;

        public string FileName { get; set; } = string.Empty;

        public Task<(Department AuthorizedDepartments, string FileName)> GetDocumentInfoAsync(string documentId, CancellationToken cancellationToken = default)
            => Task.FromResult((AuthorizedDepartments, FileName));

        public Task UpdateStatusAsync(string documentId, DocumentProcessingStatus status, string? errorMessage, CancellationToken cancellationToken = default)
        {
            RecordedStatuses.Add(status);
            return Task.CompletedTask;
        }
    }

    private sealed class FakeEmbeddingGenerator : IEmbeddingGenerator
    {
        private readonly Func<IReadOnlyList<string>, IReadOnlyList<float[]>> _generate;

        public FakeEmbeddingGenerator(Func<IReadOnlyList<string>, IReadOnlyList<float[]>> generate) => _generate = generate;

        public Task<IReadOnlyList<float[]>> GenerateAsync(IReadOnlyList<string> texts, CancellationToken cancellationToken = default)
            => Task.FromResult(_generate(texts));
    }

    private sealed class FakeVectorStore : IVectorStore
    {
        public List<EmbeddedChunk> UpsertedChunks { get; } = new();

        public Task EnsureCollectionAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task UpsertAsync(IEnumerable<EmbeddedChunk> chunks, CancellationToken cancellationToken = default)
        {
            UpsertedChunks.AddRange(chunks);
            return Task.CompletedTask;
        }
    }
}
