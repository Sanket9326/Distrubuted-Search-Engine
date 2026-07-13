using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.Options;
using Services.ReRanking;
using Services.VectorSearch;

namespace SearchService.Tests;

public sealed class TeiReRankerTests
{
    [Fact]
    public async Task RerankAsync_MapsIndicesBackToCandidates_SortsByScore_AndTruncatesToTopK()
    {
        var candidates = new List<ScoredChunk>
        {
            new(Guid.NewGuid(), "doc-a", "a.pdf", 0, "content-a", Array.Empty<string>(), DateTime.UtcNow, 0.7f),
            new(Guid.NewGuid(), "doc-b", "b.pdf", 0, "content-b", Array.Empty<string>(), DateTime.UtcNow, 0.6f),
            new(Guid.NewGuid(), "doc-c", "c.pdf", 0, "content-c", Array.Empty<string>(), DateTime.UtcNow, 0.5f)
        };

        // TEI returns ranks out of order and with different scores than the original Qdrant scores,
        // e.g. candidate index 2 (lowest cosine score) is actually the most relevant per the cross-encoder.
        var teiResponse = new[]
        {
            new { index = 0, score = 0.2f },
            new { index = 2, score = 0.95f },
            new { index = 1, score = 0.5f }
        };

        var handler = new FakeHttpMessageHandler(teiResponse);
        var httpClient = new HttpClient(handler);
        var sut = new TeiReRanker(httpClient, Options.Create(new TeiOptions { Endpoint = "http://reranker.local" }));

        var result = await sut.RerankAsync("query", candidates, topK: 2);

        Assert.Equal(2, result.Count);
        Assert.Equal("doc-c", result[0].DocumentId);
        Assert.Equal(0.95f, result[0].Score);
        Assert.Equal("doc-b", result[1].DocumentId);
        Assert.Equal(0.5f, result[1].Score);
    }

    [Fact]
    public async Task RerankAsync_WhenNoCandidates_ReturnsEmpty_WithoutCallingReranker()
    {
        var handler = new FakeHttpMessageHandler(Array.Empty<object>());
        var httpClient = new HttpClient(handler);
        var sut = new TeiReRanker(httpClient, Options.Create(new TeiOptions { Endpoint = "http://reranker.local" }));

        var result = await sut.RerankAsync("query", Array.Empty<ScoredChunk>(), topK: 5);

        Assert.Empty(result);
        Assert.False(handler.WasCalled);
    }

    private sealed class FakeHttpMessageHandler : HttpMessageHandler
    {
        private readonly object _responseBody;

        public FakeHttpMessageHandler(object responseBody) => _responseBody = responseBody;

        public bool WasCalled { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            WasCalled = true;
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(_responseBody)
            };
            return Task.FromResult(response);
        }
    }
}
