using Contracts;

namespace Services.VectorSearch;

public interface IVectorSearchStore
{
    Task<IReadOnlyList<ScoredChunk>> SearchAsync(
        float[] queryVector,
        Department departments,
        int limit,
        float minimumScore,
        CancellationToken cancellationToken = default);
}
