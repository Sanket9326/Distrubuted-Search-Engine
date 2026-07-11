using Entities;

namespace Services.Chunking;

public interface IChunkingService
{
    IReadOnlyList<DocumentChunk> Chunk(string documentId, string text, ChunkingOptions options);
}
