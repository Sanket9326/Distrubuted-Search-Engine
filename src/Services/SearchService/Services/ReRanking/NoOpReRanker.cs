using Services.VectorSearch;

namespace Services.ReRanking;

/// <summary>
/// Pass-through re-ranker: Qdrant already returns candidates sorted by score, so this just truncates
/// to topK. A seam for a future cross-encoder/LLM-based re-ranker ahead of a RAG generation step —
/// swapping the DI registration is the only change needed when one is introduced.
/// </summary>
public sealed class NoOpReRanker : IReRanker
{
    public Task<IReadOnlyList<ScoredChunk>> RerankAsync(
        string query,
        IReadOnlyList<ScoredChunk> candidates,
        int topK,
        CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<ScoredChunk>>(candidates.Take(topK).ToList());
}
