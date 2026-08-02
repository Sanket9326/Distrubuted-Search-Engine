using Entities;
using Microsoft.EntityFrameworkCore;
using Persistence;
using Services.Indexing;

namespace Repositories;

public sealed class IndexRepository : IIndexRepository
{
    private readonly KeywordIndexDbContext _dbContext;

    public IndexRepository(KeywordIndexDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task ReindexDocumentAsync(
        string documentId,
        IReadOnlyDictionary<Guid, IReadOnlyList<ChunkTermStats>> chunkTermStats,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        await RemoveExistingPostingsAsync(documentId, cancellationToken);

        var newTermChunkCounts = chunkTermStats
            .SelectMany(kvp => kvp.Value.Select(stat => (ChunkId: kvp.Key, stat.Term)))
            .GroupBy(x => x.Term)
            .ToDictionary(g => g.Key, g => g.Select(x => x.ChunkId).Distinct().Count());

        var termIds = await GetOrCreateTermIdsAsync(newTermChunkCounts.Keys, cancellationToken);

        foreach (var (chunkId, stats) in chunkTermStats)
        {
            foreach (var stat in stats)
            {
                _dbContext.IndexPostings.Add(new IndexPosting
                {
                    TermId = termIds[stat.Term],
                    ChunkId = chunkId,
                    DocumentId = documentId,
                    TermFrequency = stat.TermFrequency,
                    Positions = stat.Positions
                });
            }
        }

        foreach (var (term, chunkCount) in newTermChunkCounts)
        {
            var termId = termIds[term];
            var termEntity = await _dbContext.IndexTerms.FirstAsync(t => t.TermId == termId, cancellationToken);
            termEntity.DocumentFrequency += chunkCount;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    private async Task RemoveExistingPostingsAsync(string documentId, CancellationToken cancellationToken)
    {
        var existingPostings = await _dbContext.IndexPostings
            .Where(p => p.DocumentId == documentId)
            .ToListAsync(cancellationToken);

        if (existingPostings.Count == 0)
        {
            return;
        }

        var affectedTermChunkCounts = existingPostings
            .GroupBy(p => p.TermId)
            .ToDictionary(g => g.Key, g => g.Select(p => p.ChunkId).Distinct().Count());

        foreach (var (termId, chunkCount) in affectedTermChunkCounts)
        {
            var termEntity = await _dbContext.IndexTerms.FirstOrDefaultAsync(t => t.TermId == termId, cancellationToken);
            if (termEntity is not null)
            {
                termEntity.DocumentFrequency = Math.Max(0, termEntity.DocumentFrequency - chunkCount);
            }
        }

        _dbContext.IndexPostings.RemoveRange(existingPostings);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<Dictionary<string, int>> GetOrCreateTermIdsAsync(IEnumerable<string> terms, CancellationToken cancellationToken)
    {
        var termList = terms.ToList();
        var termIds = await _dbContext.IndexTerms
            .Where(t => termList.Contains(t.Term))
            .ToDictionaryAsync(t => t.Term, t => t.TermId, cancellationToken);

        var missingTerms = termList.Where(t => !termIds.ContainsKey(t)).Distinct().ToList();
        if (missingTerms.Count == 0)
        {
            return termIds;
        }

        var newTerms = missingTerms.Select(term => new IndexTerm { Term = term, DocumentFrequency = 0 }).ToList();
        _dbContext.IndexTerms.AddRange(newTerms);
        await _dbContext.SaveChangesAsync(cancellationToken);

        foreach (var newTerm in newTerms)
        {
            termIds[newTerm.Term] = newTerm.TermId;
        }

        return termIds;
    }
}
