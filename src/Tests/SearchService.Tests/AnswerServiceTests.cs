using Dtos;
using Services;
using Services.Llm;
using Services.Prompting;

namespace SearchService.Tests;

public sealed class AnswerServiceTests
{
    [Fact]
    public async Task AnswerAsync_WhenNoResults_ReturnsFixedAnswer_WithoutCallingPromptBuilderOrLlm()
    {
        var searchProcessingService = new FakeSearchProcessingService(new SearchResponse());
        var promptBuilder = new FakePromptBuilder(_ => throw new InvalidOperationException("Should not be called"));
        var llmClient = new FakeLlmClient(_ => throw new InvalidOperationException("Should not be called"));

        var sut = new AnswerService(searchProcessingService, promptBuilder, llmClient);

        var response = await sut.AnswerAsync(new SearchRequest { Query = "hello" });

        Assert.Equal("No relevant information was found for this query.", response.Answer);
        Assert.Empty(response.Sources);
    }

    [Fact]
    public async Task AnswerAsync_ReturnsLlmAnswer_AndOnlyIncludesSourcesThatMadeItIntoThePrompt()
    {
        var includedChunkId = Guid.NewGuid();
        var droppedChunkId = Guid.NewGuid();

        var includedResult = CreateResultItem(includedChunkId);
        var droppedResult = CreateResultItem(droppedChunkId);

        var searchProcessingService = new FakeSearchProcessingService(
            new SearchResponse { Results = new[] { includedResult, droppedResult } });

        var promptBuilder = new FakePromptBuilder(
            _ => new LlmPrompt("system", "user", new[] { includedChunkId }));

        var llmClient = new FakeLlmClient(_ => new LlmCompletion("generated answer", "stop"));

        var sut = new AnswerService(searchProcessingService, promptBuilder, llmClient);

        var response = await sut.AnswerAsync(new SearchRequest { Query = "hello" });

        Assert.Equal("generated answer", response.Answer);
        var source = Assert.Single(response.Sources);
        Assert.Equal(includedChunkId, source.ChunkId);
    }

    private static SearchResultItem CreateResultItem(Guid chunkId) => new()
    {
        ChunkId = chunkId,
        DocumentId = "doc-1",
        FileName = "file.pdf",
        ChunkIndex = 0,
        Content = "content",
        Departments = new[] { "Finance" },
        Score = 0.9f,
        CreatedAtUtc = DateTime.UtcNow
    };

    private sealed class FakeSearchProcessingService : ISearchProcessingService
    {
        private readonly SearchResponse _response;

        public FakeSearchProcessingService(SearchResponse response) => _response = response;

        public Task<SearchResponse> SearchAsync(SearchRequest request, CancellationToken cancellationToken = default)
            => Task.FromResult(_response);
    }

    private sealed class FakePromptBuilder : IPromptBuilder
    {
        private readonly Func<IReadOnlyList<SearchResultItem>, LlmPrompt> _build;

        public FakePromptBuilder(Func<IReadOnlyList<SearchResultItem>, LlmPrompt> build) => _build = build;

        public LlmPrompt Build(string query, IReadOnlyList<SearchResultItem> rankedChunks) => _build(rankedChunks);
    }

    private sealed class FakeLlmClient : ILlmClient
    {
        private readonly Func<LlmPrompt, LlmCompletion> _complete;

        public FakeLlmClient(Func<LlmPrompt, LlmCompletion> complete) => _complete = complete;

        public Task<LlmCompletion> CompleteAsync(LlmPrompt prompt, CancellationToken cancellationToken = default)
            => Task.FromResult(_complete(prompt));
    }
}
