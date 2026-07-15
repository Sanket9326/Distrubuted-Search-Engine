using System.Text;
using Dtos;
using Microsoft.Extensions.Options;

namespace Services.Prompting;

public sealed class PromptBuilder : IPromptBuilder
{
    private const string SystemMessage =
        "You are a helpful assistant that answers questions using only the numbered context " +
        "passages provided by the user. Cite the passages you rely on using their numbers, " +
        "e.g. [1], [2]. If the context does not contain enough information to answer, say so " +
        "clearly instead of guessing.";

    private const int SafetyMarginTokens = 50;

    private readonly GeminiOptions _options;

    public PromptBuilder(IOptions<GeminiOptions> options)
    {
        _options = options.Value;
    }

    public LlmPrompt Build(string query, IReadOnlyList<SearchResultItem> rankedChunks)
    {
        var reservedTokens = EstimateTokens(SystemMessage)
            + EstimateTokens(query)
            + _options.MaxCompletionTokens
            + SafetyMarginTokens;

        var remainingBudget = Math.Max(0, _options.MaxPromptTokens - reservedTokens);

        var contextBuilder = new StringBuilder();
        var includedChunkIds = new List<Guid>();
        var usedTokens = 0;

        for (var i = 0; i < rankedChunks.Count; i++)
        {
            var chunk = rankedChunks[i];
            var block = $"[{includedChunkIds.Count + 1}] {chunk.FileName}: {chunk.Content}";
            var blockTokens = EstimateTokens(block);

            if (usedTokens + blockTokens > remainingBudget)
            {
                continue;
            }

            contextBuilder.AppendLine(block);
            includedChunkIds.Add(chunk.ChunkId);
            usedTokens += blockTokens;
        }

        var userMessage = $"Question: {query}\n\nContext:\n{contextBuilder}";

        return new LlmPrompt(SystemMessage, userMessage, includedChunkIds);
    }

    private static int EstimateTokens(string text) => (int)Math.Ceiling(text.Length / 4.0);
}
