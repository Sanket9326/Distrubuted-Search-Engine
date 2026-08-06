using Contracts;
using Services.VectorSearch;

namespace Services.KeywordSearch;

public interface IKeywordSearchStore
{
    Task<IReadOnlyList<ScoredChunk>> SearchAsync(
        string query,
        Department departments,
        int limit,
        CancellationToken cancellationToken = default);
}
