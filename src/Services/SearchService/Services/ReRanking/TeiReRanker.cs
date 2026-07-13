using System.Net.Http.Json;
using Microsoft.Extensions.Options;
using Services.VectorSearch;

namespace Services.ReRanking;

/// <summary>
/// Calls a Text Embeddings Inference (TEI) server running a real cross-encoder reranker
/// (BAAI/bge-reranker-v2-m3) to score each candidate against the query, and replaces the
/// Qdrant cosine score with the reranker's score — a true relevance judgment, not an ANN distance.
/// </summary>
public sealed class TeiReRanker : IReRanker
{
    private readonly HttpClient _httpClient;

    public TeiReRanker(HttpClient httpClient, IOptions<TeiOptions> options)
    {
        _httpClient = httpClient;
        _httpClient.BaseAddress = new Uri(options.Value.Endpoint);
    }

    public async Task<IReadOnlyList<ScoredChunk>> RerankAsync(
        string query,
        IReadOnlyList<ScoredChunk> candidates,
        int topK,
        CancellationToken cancellationToken = default)
    {
        if (candidates.Count == 0)
        {
            return Array.Empty<ScoredChunk>();
        }

        var response = await _httpClient.PostAsJsonAsync(
            "rerank",
            new TeiRerankRequest(query, candidates.Select(c => c.Content).ToList()),
            cancellationToken);

        response.EnsureSuccessStatusCode();

        var ranks = await response.Content.ReadFromJsonAsync<IReadOnlyList<TeiRank>>(cancellationToken: cancellationToken);
        if (ranks is null)
        {
            throw new InvalidOperationException("Reranker returned no ranks.");
        }

        return ranks
            .OrderByDescending(r => r.Score)
            .Take(topK)
            .Select(r => candidates[r.Index] with { Score = r.Score })
            .ToList();
    }

    private sealed record TeiRerankRequest(string Query, IReadOnlyList<string> Texts);

    private sealed record TeiRank(int Index, float Score);
}
