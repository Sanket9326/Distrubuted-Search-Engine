using Entities;

namespace Repositories;

public interface IDocumentChunkRepository
{
    Task AddRangeAsync(IEnumerable<DocumentChunk> chunks, CancellationToken cancellationToken = default);
}
