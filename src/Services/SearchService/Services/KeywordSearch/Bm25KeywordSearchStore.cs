using Contracts;
using Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Persistence;
using Services.Indexing;
using Services.VectorSearch;

namespace Services.KeywordSearch;

public sealed class Bm25KeywordSearchStore : IKeywordSearchStore
{
    private readonly KeywordIndexReadDbContext _dbContext;
    private readonly IKeywordAnalyzer _analyzer;
    private readonly SearchOptions _options;

    public Bm25KeywordSearchStore(
        KeywordIndexReadDbContext dbContext,
        IKeywordAnalyzer analyzer,
        IOptions<SearchOptions> options)
    {
        _dbContext = dbContext;
        _analyzer = analyzer;
        _options = options.Value;
    }

    public async Task<IReadOnlyList<ScoredChunk>> SearchAsync(
        string query,
        Department departments,
        int limit,
        CancellationToken cancellationToken = default)
    {
        var queryTerms = _analyzer.Analyze(query).Select(t => t.Term).Distinct().ToList();
        if (queryTerms.Count == 0)
        {
            return [];
        }

        var terms = await _dbContext.IndexTerms
            .Where(t => queryTerms.Contains(t.Term))
            .ToDictionaryAsync(t => t.TermId, cancellationToken);
        if (terms.Count == 0)
        {
            return [];
        }

        var termIds = terms.Keys.ToList();
        var postings = await _dbContext.IndexPostings
            .Where(p => termIds.Contains(p.TermId))
            .ToListAsync(cancellationToken);
        if (postings.Count == 0)
        {
            return [];
        }

        var documentIds = postings.Select(p => p.DocumentId).Distinct().ToList();
        var metadataByDocument = await _dbContext.IndexDocumentMetadata
            .Where(m => documentIds.Contains(m.DocumentId))
            .ToDictionaryAsync(m => m.DocumentId, cancellationToken);

        var chunkIds = postings.Select(p => p.ChunkId).Distinct().ToList();
        var chunkStatsByChunk = await _dbContext.IndexChunkStats
            .Where(c => chunkIds.Contains(c.ChunkId))
            .ToDictionaryAsync(c => c.ChunkId, cancellationToken);

        var stats = await _dbContext.IndexStats
            .FirstOrDefaultAsync(s => s.Id == IndexStats.SingletonId, cancellationToken);
        var totalChunks = stats?.TotalChunks ?? 0;
        var averageChunkLength = totalChunks > 0 ? (double)stats!.TotalTokenLength / totalChunks : 0;

        var scoresByChunk = new Dictionary<Guid, double>();
        var documentIdByChunk = new Dictionary<Guid, string>();

        foreach (var posting in postings)
        {
            if (!metadataByDocument.TryGetValue(posting.DocumentId, out var metadata) ||
                ((Department)metadata.AuthorizedDepartments & departments) == Department.None)
            {
                continue;
            }

            var term = terms[posting.TermId];
            var chunkLength = chunkStatsByChunk.TryGetValue(posting.ChunkId, out var chunkStats) ? chunkStats.TokenCount : 0;

            var score = Bm25Scorer.ScoreTerm(
                posting.TermFrequency, term.DocumentFrequency, totalChunks, chunkLength, averageChunkLength,
                _options.Bm25K1, _options.Bm25B);

            scoresByChunk[posting.ChunkId] = scoresByChunk.GetValueOrDefault(posting.ChunkId) + score;
            documentIdByChunk[posting.ChunkId] = posting.DocumentId;
        }

        if (scoresByChunk.Count == 0)
        {
            return [];
        }

        var topChunkIds = scoresByChunk
            .OrderByDescending(kvp => kvp.Value)
            .Take(limit)
            .Select(kvp => kvp.Key)
            .ToList();

        var chunksById = await _dbContext.DocumentChunks
            .Where(c => topChunkIds.Contains(c.Id))
            .ToDictionaryAsync(c => c.Id, cancellationToken);

        var results = new List<ScoredChunk>(topChunkIds.Count);
        foreach (var chunkId in topChunkIds)
        {
            if (!chunksById.TryGetValue(chunkId, out var chunk))
            {
                continue;
            }

            var documentId = documentIdByChunk[chunkId];
            var metadata = metadataByDocument[documentId];

            results.Add(new ScoredChunk(
                ChunkId: chunkId,
                DocumentId: documentId,
                FileName: metadata.FileName,
                ChunkIndex: chunk.ChunkIndex,
                Content: chunk.Content,
                Departments: ToDepartmentNames((Department)metadata.AuthorizedDepartments),
                CreatedAtUtc: chunk.CreatedAtUtc,
                Score: (float)scoresByChunk[chunkId]));
        }

        return results;
    }

    // Mirrors QdrantVectorSearchStore's ToDepartmentNames - kept as a small, per-service duplicate
    // rather than shared code, per this repo's "no shared code between services" convention.
    private static string[] ToDepartmentNames(Department departments)
    {
        if (departments == Department.None)
        {
            return [];
        }

        return Enum.GetValues<Department>()
            .Where(d => d != Department.None && d != Department.All && departments.HasFlag(d))
            .Select(d => d.ToString())
            .ToArray();
    }
}
