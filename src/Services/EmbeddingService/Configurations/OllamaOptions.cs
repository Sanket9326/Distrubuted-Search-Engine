public sealed class OllamaOptions
{
    public const string SectionName = "Ollama";

    public string Endpoint { get; init; } = string.Empty;

    public string Model { get; init; } = "nomic-embed-text";
}
