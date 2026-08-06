using Services.Indexing;

namespace Repositories;

public interface IIndexRepository
{
    /// <summary>
    /// Idempotently (re)builds the posting lists for every chunk of a document: deletes any
    /// postings already recorded for this document (adjusting term DocumentFrequency down),
    /// then inserts the freshly analyzed postings (adjusting DocumentFrequency back up). Also
    /// maintains index_chunk_stats/index_stats (BM25 length-normalization inputs) and the
    /// index_document_metadata row (authorization/display data for hybrid queries) in the same
    /// transaction. Safe to call repeatedly for the same document, e.g. on a retried
    /// ChunksCreatedEvent.
    /// </summary>
    Task ReindexDocumentAsync(
        string documentId,
        string fileName,
        int authorizedDepartments,
        IReadOnlyDictionary<Guid, IReadOnlyList<ChunkTermStats>> chunkTermStats,
        CancellationToken cancellationToken = default);
}
