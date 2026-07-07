namespace Infrastructure;

public interface IMinioStorage
{
    /// <summary>
    /// Uploads a file stream to MinIO object storage.
    /// </summary>
    /// <param name="objectName">The key under which the object is stored in the bucket.</param>
    /// <param name="fileStream">The file content stream.</param>
    /// <param name="fileSize">The size of the file in bytes.</param>
    /// <param name="contentType">The MIME content type of the file.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The object name the file was stored under.</returns>
    Task<string> UploadFileAsync(
        string objectName,
        Stream fileStream,
        long fileSize,
        string contentType,
        CancellationToken cancellationToken = default);
}
