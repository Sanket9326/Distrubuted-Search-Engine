using Dtos;
using Microsoft.Extensions.Options;
using Services.Prompting;

namespace SearchService.Tests;

public sealed class PromptBuilderTests
{
    [Fact]
    public void Build_IncludesAllChunks_WhenTheyFitWithinBudget()
    {
        var chunks = new[]
        {
            CreateChunk("short content one"),
            CreateChunk("short content two")
        };

        var sut = CreateSut(new GeminiOptions { MaxPromptTokens = 4000, MaxCompletionTokens = 512 });

        var prompt = sut.Build("what happened?", chunks);

        Assert.Equal(2, prompt.IncludedChunkIds.Count);
        Assert.Contains(chunks[0].ChunkId, prompt.IncludedChunkIds);
        Assert.Contains(chunks[1].ChunkId, prompt.IncludedChunkIds);
        Assert.Contains("what happened?", prompt.UserMessage);
        Assert.Contains("[1]", prompt.UserMessage);
        Assert.Contains("[2]", prompt.UserMessage);
    }

    [Fact]
    public void Build_DropsLowerRankedChunks_WhenBudgetIsTooSmall()
    {
        var chunks = new[]
        {
            CreateChunk(new string('a', 400)),
            CreateChunk(new string('b', 400)),
            CreateChunk(new string('c', 400))
        };

        // Budget only large enough for the system message, query, completion reserve,
        // and roughly one ~400-char chunk.
        var sut = CreateSut(new GeminiOptions { MaxPromptTokens = 250, MaxCompletionTokens = 20 });

        var prompt = sut.Build("q", chunks);

        Assert.True(prompt.IncludedChunkIds.Count < chunks.Length);
    }

    [Fact]
    public void Build_ReturnsEmptyIncludedChunkIds_WhenNoChunksProvided()
    {
        var sut = CreateSut(new GeminiOptions());

        var prompt = sut.Build("q", Array.Empty<SearchResultItem>());

        Assert.Empty(prompt.IncludedChunkIds);
        Assert.Contains("q", prompt.UserMessage);
    }

    private static PromptBuilder CreateSut(GeminiOptions options) => new(Options.Create(options));

    private static SearchResultItem CreateChunk(string content) => new()
    {
        ChunkId = Guid.NewGuid(),
        DocumentId = "doc-1",
        FileName = "file.pdf",
        ChunkIndex = 0,
        Content = content,
        Departments = new[] { "Finance" },
        Score = 0.9f,
        CreatedAtUtc = DateTime.UtcNow
    };
}
