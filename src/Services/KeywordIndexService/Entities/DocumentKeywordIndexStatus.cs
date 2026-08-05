namespace Entities;

public sealed class DocumentKeywordIndexStatus
{
    public string DocumentId { get; init; } = string.Empty;

    public KeywordIndexStatus Status { get; set; } = KeywordIndexStatus.Pending;

    public string? ErrorMessage { get; set; }

    public DateTime? IndexedAtUtc { get; set; }

    public DateTime UpdatedAtUtc { get; set; }
}
