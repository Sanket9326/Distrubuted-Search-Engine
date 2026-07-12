using Entities;

namespace Repositories;

public interface IDocumentChunkReadRepository
{
    Task<IReadOnlyList<DocumentChunk>> GetByDocumentIdAsync(string documentId, CancellationToken cancellationToken = default);
}
