using SharedKernel.Exceptions;

namespace Exceptions;

public sealed class UnsupportedFileTypeException : BaseException
{
    public UnsupportedFileTypeException(string message)
        : base(message)
    {
    }
}
