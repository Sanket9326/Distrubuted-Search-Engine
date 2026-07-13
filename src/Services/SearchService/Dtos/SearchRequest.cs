namespace Dtos;

public sealed class SearchRequest
{
    public string Query { get; init; } = string.Empty;

    public string[]? Departments { get; init; }

    public int? TopK { get; init; }
}
