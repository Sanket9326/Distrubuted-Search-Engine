using System.Net.Http.Json;
using Exceptions;
using Infrastructure;
using Microsoft.Extensions.Options;

namespace Services.Embedding;

public sealed class OllamaEmbeddingGenerator : IEmbeddingGenerator
{
    private readonly HttpClient _httpClient;
    private readonly OllamaOptions _options;
    private readonly ILogger<OllamaEmbeddingGenerator> _logger;

    public OllamaEmbeddingGenerator(HttpClient httpClient, IOptions<OllamaOptions> options, ILogger<OllamaEmbeddingGenerator> logger)
    {
        _options = options.Value;
        _httpClient = httpClient;
        _httpClient.BaseAddress = new Uri(_options.Endpoint);
        // The per-batch timeout below is the sole authority on timing - let it govern instead
        // of racing against HttpClient's own opaque 100s default.
        _httpClient.Timeout = Timeout.InfiniteTimeSpan;
        _logger = logger;
    }

    public async Task<IReadOnlyList<float[]>> GenerateAsync(IReadOnlyList<string> texts, CancellationToken cancellationToken = default)
    {
        if (texts.Count == 0)
        {
            return Array.Empty<float[]>();
        }

        var batchSize = Math.Max(1, _options.EmbeddingBatchSize);
        var batchCount = (texts.Count + batchSize - 1) / batchSize;
        var embeddings = new List<float[]>(texts.Count);

        for (var offset = 0; offset < texts.Count; offset += batchSize)
        {
            var batch = texts.Skip(offset).Take(batchSize).ToList();
            var batchNumber = offset / batchSize + 1;

            _logger.LogInformation(
                "Embedding batch {BatchNumber}/{BatchCount} ({BatchChunkCount} chunks) via Ollama.",
                batchNumber, batchCount, batch.Count);

            embeddings.AddRange(await GenerateBatchAsync(batch, cancellationToken));
        }

        return embeddings;
    }

    private async Task<IReadOnlyList<float[]>> GenerateBatchAsync(IReadOnlyList<string> batch, CancellationToken cancellationToken)
    {
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(TimeSpan.FromSeconds(_options.EmbeddingRequestTimeoutSeconds));

        HttpResponseMessage response;
        try
        {
            response = await _httpClient.PostAsJsonAsync(
                "api/embed",
                new OllamaEmbedRequest(_options.Model, batch),
                timeoutCts.Token);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            // The linked token fired from our own CancelAfter (not the caller's token), so this
            // is a request timeout, not a shutdown/cancellation. Surface it as a plain exception
            // so it's caught by the processing service's retry-scheduling catch block instead of
            // being mistaken for shutdown by the Kafka consumer loop further up the call chain.
            throw new EmbeddingGenerationTimeoutException(
                $"Ollama did not respond to an embedding request for {batch.Count} chunk(s) within {_options.EmbeddingRequestTimeoutSeconds}s.");
        }

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
