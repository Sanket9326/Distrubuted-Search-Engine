using Services.Indexing;

namespace Repositories;

public interface IIndexRepository
{
    /// <summary>
    /// Idempotently (re)builds the posting lists for every chunk of a document: deletes any
    /// postings already recorded for this document (adjusting term DocumentFrequency down),
    /// then inserts the freshly analyzed postings (adjusting DocumentFrequency back up). Safe
    /// to call repeatedly for the same document, e.g. on a retried ChunksCreatedEvent.
    /// </summary>
    Task ReindexDocumentAsync(
        string documentId,
        IReadOnlyDictionary<Guid, IReadOnlyList<ChunkTermStats>> chunkTermStats,
        CancellationToken cancellationToken = default);
}
