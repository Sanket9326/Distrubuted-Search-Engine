public sealed class OllamaOptions
{
    public const string SectionName = "Ollama";

    public string Endpoint { get; init; } = string.Empty;

    public string Model { get; init; } = "nomic-embed-text";

    /// <summary>
    /// Chunks sent to Ollama per /api/embed call. Kept small so one large document can't force
    /// a single huge (and hence slow/fragile) request onto Ollama.
    /// </summary>
    public int EmbeddingBatchSize { get; init; } = 16;

    /// <summary>
    /// Timeout for a single batch request. Generous by design - Ollama embedding inference
    /// (especially on CPU) can genuinely take a while - but bounded, so a hung request fails
    /// over to the retry queue instead of blocking the consumer forever.
    /// </summary>
    public int EmbeddingRequestTimeoutSeconds { get; init; } = 120;
}
