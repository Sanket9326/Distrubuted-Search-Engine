namespace Dtos;

public sealed class AnswerResponse
{
    public string Answer { get; init; } = string.Empty;

    public IReadOnlyList<AnswerSource> Sources { get; init; } = Array.Empty<AnswerSource>();
}

public sealed class AnswerSource
{
    public Guid ChunkId { get; init; }

    public string DocumentId { get; init; } = string.Empty;

    public string FileName { get; init; } = string.Empty;

    public int ChunkIndex { get; init; }

    public float Score { get; init; }
}
