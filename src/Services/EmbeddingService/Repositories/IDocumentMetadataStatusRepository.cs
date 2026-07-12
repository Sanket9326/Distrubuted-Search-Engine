using Contracts;

namespace Repositories;

public interface IDocumentMetadataStatusRepository
{
    Task<Department> GetAuthorizedDepartmentsAsync(string documentId, CancellationToken cancellationToken = default);

    Task UpdateStatusAsync(string documentId, DocumentProcessingStatus status, string? errorMessage, CancellationToken cancellationToken = default);
}
