using SharedKernel.Exceptions;

namespace Exceptions;

public sealed class FileTooLargeException : BaseException
{
    public FileTooLargeException(string message)
        : base(message)
    {
    }
}
