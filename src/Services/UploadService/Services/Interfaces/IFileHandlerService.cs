using Contracts.Events;

/// <summary>
/// Interface for handling file upload operations.
/// </summary>
public interface IFileHandlerService
{
    /// <summary>
    /// Handles the file upload process asynchronously.
    /// </summary>
    /// <param name="file">The file to upload.</param>
    /// <param name="departments">Comma-separated department names authorized to access this document (e.g. "Finance,Engineering").</param>
    /// <returns>A tuple indicating the success of the operation and the uploaded document event.</returns>
    Task<(bool IsSuccess, DocumentUploadedEvent Event)> HandleFileUploadAsync(IFormFile file, string? departments);
}