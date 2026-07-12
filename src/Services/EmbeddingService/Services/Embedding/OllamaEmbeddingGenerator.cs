using System.Net.Http.Json;
using Microsoft.Extensions.Options;

namespace Services.Embedding;

public sealed class OllamaEmbeddingGenerator : IEmbeddingGenerator
{
    private readonly HttpClient _httpClient;
    private readonly OllamaOptions _options;

    public OllamaEmbeddingGenerator(HttpClient httpClient, IOptions<OllamaOptions> options)
    {
        _options = options.Value;
        _httpClient = httpClient;
        _httpClient.BaseAddress = new Uri(_options.Endpoint);
    }

    public async Task<IReadOnlyList<float[]>> GenerateAsync(IReadOnlyList<string> texts, CancellationToken cancellationToken = default)
    {
        if (texts.Count == 0)
        {
            return Array.Empty<float[]>();
        }

        var response = await _httpClient.PostAsJsonAsync(
            "api/embed",
            new OllamaEmbedRequest(_options.Model, texts),
            cancellationToken);

        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<OllamaEmbedResponse>(cancellationToken: cancellationToken);
        if (result?.Embeddings is null)
        {
            throw new InvalidOperationException("Ollama returned no embeddings.");
        }

        return result.Embeddings;
    }

    private sealed record OllamaEmbedRequest(string Model, IReadOnlyList<string> Input);

    private sealed record OllamaEmbedResponse(string? Model, IReadOnlyList<float[]>? Embeddings);
}
