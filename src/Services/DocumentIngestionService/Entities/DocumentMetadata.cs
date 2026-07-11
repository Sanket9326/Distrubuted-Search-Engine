namespace Entities;

public sealed class DocumentMetadata
{
    public string DocumentId { get; init; } = string.Empty;

    public string FileName { get; init; } = string.Empty;

    public string ContentType { get; init; } = string.Empty;

    public Department AuthorizedDepartments { get; init; } = Department.None;

    public DateTime UploadedAtUtc { get; init; }

    public DateTime IngestedAtUtc { get; init; }

    public DocumentProcessingStatus Status { get; set; } = DocumentProcessingStatus.Pending;

    public string? ErrorMessage { get; set; }
}