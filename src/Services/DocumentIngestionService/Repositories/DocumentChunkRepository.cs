using Entities;
using Persistence;

namespace Repositories;

public sealed class DocumentChunkRepository : IDocumentChunkRepository
{
    private readonly DocumentIngestionDbContext _dbContext;

    public DocumentChunkRepository(DocumentIngestionDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddRangeAsync(IEnumerable<DocumentChunk> chunks, CancellationToken cancellationToken = default)
    {
        await _dbContext.DocumentChunks.AddRangeAsync(chunks, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
