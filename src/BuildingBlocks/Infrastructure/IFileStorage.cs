namespace Infrastructure;

public interface IFileStorage
{
    /// <summary>
    /// Downloads an object's full content into a seekable, in-memory stream positioned at the start.
    /// </summary>
    /// <param name="objectName">The object key to download.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    Task<Stream> DownloadAsync(string objectName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the size, in bytes, of a stored object without downloading its content.
    /// </summary>
    /// <param name="objectName">The object key to inspect.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    Task<long> GetSizeAsync(string objectName, CancellationToken cancellationToken = default);
}
