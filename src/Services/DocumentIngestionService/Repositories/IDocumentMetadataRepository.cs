using Contracts;
using Entities;

namespace Repositories;

public interface IDocumentMetadataRepository
{
    Task AddAsync(DocumentMetadata metadata, CancellationToken cancellationToken = default);

    Task<DocumentMetadata?> GetByDocumentIdAsync(string documentId, CancellationToken cancellationToken = default);

    Task UpdateStatusAsync(string documentId, DocumentProcessingStatus status, string? errorMessage, CancellationToken cancellationToken = default);
}