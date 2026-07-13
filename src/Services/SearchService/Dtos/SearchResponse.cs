namespace Dtos;

public sealed class SearchResponse
{
    public IReadOnlyList<SearchResultItem> Results { get; init; } = Array.Empty<SearchResultItem>();
}

public sealed class SearchResultItem
{
    public Guid ChunkId { get; init; }

    public string DocumentId { get; init; } = string.Empty;

    public string FileName { get; init; } = string.Empty;

    public int ChunkIndex { get; init; }

    public string Content { get; init; } = string.Empty;

    public string[] Departments { get; init; } = Array.Empty<string>();

    public float Score { get; init; }

    public DateTime CreatedAtUtc { get; init; }
}
