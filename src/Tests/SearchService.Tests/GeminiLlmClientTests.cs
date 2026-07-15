using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.Options;
using Services.Llm;
using Services.Prompting;

namespace SearchService.Tests;

public sealed class GeminiLlmClientTests
{
    [Fact]
    public async Task CompleteAsync_ReturnsFirstCandidate_AndSendsApiKeyHeader()
    {
        var geminiResponse = new
        {
            candidates = new[]
            {
                new
                {
                    content = new { role = "model", parts = new[] { new { text = "the answer" } } },
                    finishReason = "STOP"
                }
            }
        };

        var handler = new FakeHttpMessageHandler(geminiResponse);
        var httpClient = new HttpClient(handler);
        var sut = new GeminiLlmClient(httpClient, Options.Create(new GeminiOptions
        {
            Endpoint = "http://gemini.local/",
            ApiKey = "test-key",
            Model = "gemini-2.5-flash"
        }));

        var result = await sut.CompleteAsync(new LlmPrompt("system", "user", Array.Empty<Guid>()));

        Assert.Equal("the answer", result.Text);
        Assert.Equal("STOP", result.FinishReason);
        Assert.Equal("test-key", handler.LastRequest!.Headers.GetValues("x-goog-api-key").Single());
        Assert.Equal("/models/gemini-2.5-flash:generateContent", handler.LastRequest!.RequestUri!.AbsolutePath);
    }

    [Fact]
    public async Task CompleteAsync_WhenNoCandidatesReturned_Throws()
    {
        var handler = new FakeHttpMessageHandler(new { candidates = Array.Empty<object>() });
        var httpClient = new HttpClient(handler);
        var sut = new GeminiLlmClient(httpClient, Options.Create(new GeminiOptions
        {
            Endpoint = "http://gemini.local/",
            ApiKey = "test-key"
        }));

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => sut.CompleteAsync(new LlmPrompt("system", "user", Array.Empty<Guid>())));
    }

    private sealed class FakeHttpMessageHandler : HttpMessageHandler
    {
        private readonly object _responseBody;

        public FakeHttpMessageHandler(object responseBody) => _responseBody = responseBody;

        public HttpRequestMessage? LastRequest { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequest = request;
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(_responseBody)
            };
            return Task.FromResult(response);
        }
    }
}
