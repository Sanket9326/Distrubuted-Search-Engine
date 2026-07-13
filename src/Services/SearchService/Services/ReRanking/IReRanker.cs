using Services.VectorSearch;

namespace Services.ReRanking;

public interface IReRanker
{
    Task<IReadOnlyList<ScoredChunk>> RerankAsync(
        string query,
        IReadOnlyList<ScoredChunk> candidates,
        int topK,
        CancellationToken cancellationToken = default);
}
