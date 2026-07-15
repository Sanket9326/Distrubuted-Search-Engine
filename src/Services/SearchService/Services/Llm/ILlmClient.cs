using Services.Prompting;

namespace Services.Llm;

public interface ILlmClient
{
    Task<LlmCompletion> CompleteAsync(LlmPrompt prompt, CancellationToken cancellationToken = default);
}
