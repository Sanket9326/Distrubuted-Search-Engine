public sealed class GeminiOptions
{
    public const string SectionName = "Gemini";

    public string Endpoint { get; init; } = "https://generativelanguage.googleapis.com/v1beta/";

    public string ApiKey { get; init; } = string.Empty;

    public string Model { get; init; } = "gemini-flash-lite-latest";

    public int MaxPromptTokens { get; init; } = 4000;

    public int MaxCompletionTokens { get; init; } = 512;

    public double Temperature { get; init; } = 0.2;
}
