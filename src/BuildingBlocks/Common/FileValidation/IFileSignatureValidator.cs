namespace Common.FileValidation;

public interface IFileSignatureValidator
{
    /// <summary>
    /// Validates that a file's name/extension is on the supported allow-list and that its actual
    /// content (magic bytes) matches the declared type, rather than trusting the extension or
    /// content-type header alone.
    /// </summary>
    /// <param name="fileName">The original file name, used to derive the extension.</param>
    /// <param name="content">A seekable stream positioned at the start of the file content. The stream's
    /// position is restored to the start before this method returns, regardless of the outcome.</param>
    Task<FileValidationResult> ValidateAsync(string fileName, Stream content, CancellationToken cancellationToken = default);
}
