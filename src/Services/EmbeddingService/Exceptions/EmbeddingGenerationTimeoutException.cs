using SharedKernel.Exceptions;

namespace Exceptions;

/// <summary>
/// Thrown when Ollama doesn't respond to an embedding batch within the configured timeout.
/// Deliberately NOT an <see cref="OperationCanceledException"/> - the consumer hosted service
/// treats any OperationCanceledException as "shutting down" and silently stops consuming, so a
/// timeout needs to surface as a plain exception to flow into the scheduled-retry path instead.
/// </summary>
public sealed class EmbeddingGenerationTimeoutException : BaseException
{
    public EmbeddingGenerationTimeoutException(string message)
        : base(message)
    {
    }
}
