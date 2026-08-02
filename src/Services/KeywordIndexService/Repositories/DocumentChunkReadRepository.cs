using Entities;
using Microsoft.EntityFrameworkCore;
using Persistence;

namespace Repositories;

public sealed class DocumentChunkReadRepository : IDocumentChunkReadRepository
{
    private readonly KeywordIndexReadDbContext _dbContext;

    public DocumentChunkReadRepository(KeywordIndexReadDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<DocumentChunk>> GetByDocumentIdAsync(string documentId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.DocumentChunks
            .AsNoTracking()
            .Where(c => c.DocumentId == documentId)
            .OrderBy(c => c.ChunkIndex)
            .ToListAsync(cancellationToken);
    }
}
