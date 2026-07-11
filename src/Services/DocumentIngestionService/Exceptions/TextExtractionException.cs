using SharedKernel.Exceptions;

namespace Exceptions;

public sealed class TextExtractionException : BaseException
{
    public TextExtractionException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
