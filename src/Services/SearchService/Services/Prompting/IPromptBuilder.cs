using Dtos;

namespace Services.Prompting;

public interface IPromptBuilder
{
    LlmPrompt Build(string query, IReadOnlyList<SearchResultItem> rankedChunks);
}
