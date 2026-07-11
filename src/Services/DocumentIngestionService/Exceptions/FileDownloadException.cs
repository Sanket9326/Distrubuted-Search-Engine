using SharedKernel.Exceptions;

namespace Exceptions;

public sealed class FileDownloadException : BaseException
{
    public FileDownloadException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
