using Entities;
using Microsoft.EntityFrameworkCore;
using Persistence;

namespace Repositories;

public sealed class DocumentMetadataRepository : IDocumentMetadataRepository
{
    private readonly DocumentIngestionDbContext _dbContext;

    public DocumentMetadataRepository(DocumentIngestionDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(DocumentMetadata metadata, CancellationToken cancellationToken = default)
    {
        await _dbContext.DocumentMetadata.AddAsync(metadata, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task<DocumentMetadata?> GetByDocumentIdAsync(string documentId, CancellationToken cancellationToken = default)
    {
        return _dbContext.DocumentMetadata
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.DocumentId == documentId, cancellationToken);
    }

    public async Task UpdateStatusAsync(string documentId, DocumentProcessingStatus status, string? errorMessage, CancellationToken cancellationToken = default)
    {
        var metadata = await _dbContext.DocumentMetadata
            .FirstOrDefaultAsync(m => m.DocumentId == documentId, cancellationToken);

        if (metadata is null)
        {
            return;
        }

        metadata.Status = status;
        metadata.ErrorMessage = errorMessage;

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
