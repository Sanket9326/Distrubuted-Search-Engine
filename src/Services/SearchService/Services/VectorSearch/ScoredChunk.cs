namespace Services.VectorSearch;

public sealed record ScoredChunk(
    Guid ChunkId,
    string DocumentId,
    string FileName,
    int ChunkIndex,
    string Content,
    string[] Departments,
    DateTime CreatedAtUtc,
    float Score);
