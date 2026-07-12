using Contracts;
using Microsoft.EntityFrameworkCore;
using Persistence;

namespace Repositories;

public sealed class DocumentMetadataStatusRepository : IDocumentMetadataStatusRepository
{
    private readonly EmbeddingReadDbContext _dbContext;

    public DocumentMetadataStatusRepository(EmbeddingReadDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Department> GetAuthorizedDepartmentsAsync(string documentId, CancellationToken cancellationToken = default)
    {
        var metadata = await _dbContext.DocumentMetadata
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.DocumentId == documentId, cancellationToken);

        return metadata?.AuthorizedDepartments ?? Department.None;
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
