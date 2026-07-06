namespace Contracts.Events;

public sealed class DocumentUploadedEvent
{
    public Guid DocumentId { get; init; } = Guid.NewGuid();

    public string FileName { get; init; } = string.Empty;

    public string FilePath { get; init; } = string.Empty;

    public string ContentType { get; init; } = string.Empty;

    public DateTime UploadedAtUtc { get; init; } = DateTime.UtcNow;
}
