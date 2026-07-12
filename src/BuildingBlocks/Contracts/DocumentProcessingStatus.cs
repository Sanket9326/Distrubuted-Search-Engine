namespace Contracts;

public enum DocumentProcessingStatus
{
    Pending,
    Processing,
    Chunked,
    Failed,
    Unsupported,
    Embedding,
    Embedded,
    EmbeddingFailed
}
