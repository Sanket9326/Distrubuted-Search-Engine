namespace Contracts.Events;

public sealed class DocumentUploadedEvent
{
    public string DocumentId { get; init; } = string.Empty;

    public string FileName { get; init; } = string.Empty;

    public string ContentType { get; init; } = string.Empty;

    public DateTime UploadedAtUtc { get; init; } = DateTime.UtcNow;
}
