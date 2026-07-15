namespace Services.Prompting;

public sealed record LlmPrompt(string SystemMessage, string UserMessage, IReadOnlyList<Guid> IncludedChunkIds);
