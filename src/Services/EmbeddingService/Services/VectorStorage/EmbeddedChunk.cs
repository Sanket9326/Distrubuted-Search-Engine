using Contracts;

namespace Services.VectorStorage;

public sealed record EmbeddedChunk(
    Guid Id,
    string DocumentId,
    int ChunkIndex,
    string Content,
    DateTime CreatedAtUtc,
    Department AuthorizedDepartments,
    float[] Embedding);
