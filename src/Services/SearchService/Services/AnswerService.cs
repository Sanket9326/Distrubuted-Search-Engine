using Common.TextProcessing;
using Dtos;
using Prometheus;
using Services.Llm;
using Services.Prompting;

namespace Services;

public interface IAnswerService
{
    Task<AnswerResponse> AnswerAsync(SearchRequest request, CancellationToken cancellationToken = default);
}

public sealed class AnswerService : IAnswerService
{
    private const string NoResultsAnswer = "No relevant information was found for this query.";

    private static readonly Counter RagAnswersGeneratedTotal = Metrics.CreateCounter(
        "rag_answers_generated_total", "Number of RAG answers generated via Gemini");

    private readonly ISearchProcessingService _searchProcessingService;
    private readonly IPromptBuilder _promptBuilder;
    private readonly ILlmClient _llmClient;

    public AnswerService(
        ISearchProcessingService searchProcessingService,
        IPromptBuilder promptBuilder,
        ILlmClient llmClient)
    {
        _searchProcessingService = searchProcessingService;
        _promptBuilder = promptBuilder;
        _llmClient = llmClient;
    }

    public async Task<AnswerResponse> AnswerAsync(SearchRequest request, CancellationToken cancellationToken = default)
    {
        var searchResponse = await _searchProcessingService.SearchAsync(request, cancellationToken);
        if (searchResponse.Results.Count == 0)
        {
            return new AnswerResponse { Answer = NoResultsAnswer };
        }

        var sanitizedQuery = TextSanitizer.Sanitize(request.Query).Trim();
        var prompt = _promptBuilder.Build(sanitizedQuery, searchResponse.Results);
        var completion = await _llmClient.CompleteAsync(prompt, cancellationToken);

        var sources = searchResponse.Results
            .Where(r => prompt.IncludedChunkIds.Contains(r.ChunkId))
            .Select(ToSource)
            .ToList();

        RagAnswersGeneratedTotal.Inc();

        return new AnswerResponse { Answer = completion.Text, Sources = sources };
    }

    private static AnswerSource ToSource(SearchResultItem item) => new()
    {
        ChunkId = item.ChunkId,
        DocumentId = item.DocumentId,
        FileName = item.FileName,
        ChunkIndex = item.ChunkIndex,
        Score = item.Score
    };
}
