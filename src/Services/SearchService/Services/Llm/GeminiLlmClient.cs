using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using Services.Prompting;

namespace Services.Llm;

/// <summary>
/// Calls Google's Gemini generateContent API to generate a grounded answer from the
/// prompt built out of the re-ranked context chunks.
/// </summary>
public sealed class GeminiLlmClient : ILlmClient
{
    private readonly HttpClient _httpClient;
    private readonly GeminiOptions _options;

    public GeminiLlmClient(HttpClient httpClient, IOptions<GeminiOptions> options)
    {
        _options = options.Value;
        _httpClient = httpClient;
        _httpClient.BaseAddress = new Uri(_options.Endpoint);
        _httpClient.DefaultRequestHeaders.Add("x-goog-api-key", _options.ApiKey);
    }

    public async Task<LlmCompletion> CompleteAsync(LlmPrompt prompt, CancellationToken cancellationToken = default)
    {
        var requestBody = new GeminiRequest(
            new GeminiSystemInstruction(new[] { new GeminiPart(prompt.SystemMessage) }),
            new[] { new GeminiContent("user", new[] { new GeminiPart(prompt.UserMessage) }) },
            new GeminiGenerationConfig(_options.Temperature, _options.MaxCompletionTokens));

        var response = await _httpClient.PostAsJsonAsync(
            $"models/{_options.Model}:generateContent",
            requestBody,
            cancellationToken);

        response.EnsureSuccessStatusCode();

        var completion = await response.Content.ReadFromJsonAsync<GeminiResponse>(cancellationToken: cancellationToken);
        var candidate = completion?.Candidates.FirstOrDefault();
        var text = candidate?.Content?.Parts.FirstOrDefault()?.Text;

        if (string.IsNullOrEmpty(text))
        {
            throw new InvalidOperationException("Gemini returned no completion candidates.");
        }

        return new LlmCompletion(text, candidate!.FinishReason ?? string.Empty);
    }

    private sealed record GeminiRequest(
        [property: JsonPropertyName("systemInstruction")] GeminiSystemInstruction SystemInstruction,
        [property: JsonPropertyName("contents")] IReadOnlyList<GeminiContent> Contents,
        [property: JsonPropertyName("generationConfig")] GeminiGenerationConfig GenerationConfig);

    private sealed record GeminiSystemInstruction(
        [property: JsonPropertyName("parts")] IReadOnlyList<GeminiPart> Parts);

    private sealed record GeminiContent(
        [property: JsonPropertyName("role")] string Role,
        [property: JsonPropertyName("parts")] IReadOnlyList<GeminiPart> Parts);

    private sealed record GeminiPart([property: JsonPropertyName("text")] string Text);

    private sealed record GeminiGenerationConfig(
        [property: JsonPropertyName("temperature")] double Temperature,
        [property: JsonPropertyName("maxOutputTokens")] int MaxOutputTokens);

    private sealed record GeminiResponse(
        [property: JsonPropertyName("candidates")] IReadOnlyList<GeminiCandidate> Candidates);

    private sealed record GeminiCandidate(
        [property: JsonPropertyName("content")] GeminiContent? Content,
        [property: JsonPropertyName("finishReason")] string? FinishReason);
}
