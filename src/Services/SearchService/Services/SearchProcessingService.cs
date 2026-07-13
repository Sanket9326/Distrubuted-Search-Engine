using Common.TextProcessing;
using Common.Utilities;
using Contracts;
using Dtos;
using Infrastructure;
using Microsoft.Extensions.Options;
using Services.ReRanking;
using Services.VectorSearch;

namespace Services;

public interface ISearchProcessingService
{
    Task<SearchResponse> SearchAsync(SearchRequest request, CancellationToken cancellationToken = default);
}

public sealed class SearchProcessingService : ISearchProcessingService
{
    private readonly IEmbeddingGenerator _embeddingGenerator;
    private readonly IVectorSearchStore _vectorSearchStore;
    private readonly IReRanker _reRanker;
    private readonly SearchOptions _options;
    private readonly ILogger<SearchProcessingService> _logger;

    public SearchProcessingService(
        IEmbeddingGenerator embeddingGenerator,
        IVectorSearchStore vectorSearchStore,
        IReRanker reRanker,
        IOptions<SearchOptions> options,
        ILogger<SearchProcessingService> logger)
    {
        _embeddingGenerator = embeddingGenerator;
        _vectorSearchStore = vectorSearchStore;
        _reRanker = reRanker;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<SearchResponse> SearchAsync(SearchRequest request, CancellationToken cancellationToken = default)
    {
        var departments = DepartmentParser.Parse(request.Departments);
        if (departments == Department.None)
        {
            _logger.LogInformation("Search request had no recognized authorized departments; returning no results.");
            return new SearchResponse();
        }

        var sanitizedQuery = TextSanitizer.Sanitize(request.Query).Trim();
        var topK = Math.Clamp(request.TopK ?? _options.DefaultTopK, 1, _options.MaxTopK);
        var retrievalLimit = topK * _options.RetrievalMultiplier;

        var embeddings = await _embeddingGenerator.GenerateAsync(new[] { sanitizedQuery }, cancellationToken);
        var queryVector = embeddings[0];

        var candidates = await _vectorSearchStore.SearchAsync(
            queryVector, departments, retrievalLimit, _options.MinimumScore, cancellationToken);

        var reranked = await _reRanker.RerankAsync(sanitizedQuery, candidates, topK, cancellationToken);

        return new SearchResponse
        {
            Results = reranked.Select(ToResultItem).ToList()
        };
    }

    private static SearchResultItem ToResultItem(ScoredChunk chunk) => new()
    {
        ChunkId = chunk.ChunkId,
        DocumentId = chunk.DocumentId,
        FileName = chunk.FileName,
        ChunkIndex = chunk.ChunkIndex,
        Content = chunk.Content,
        Departments = chunk.Departments,
        Score = chunk.Score,
        CreatedAtUtc = chunk.CreatedAtUtc
    };
}
