using Contracts;

namespace Entities;

public sealed class DocumentMetadataStatus
{
    public string DocumentId { get; init; } = string.Empty;

    public string FileName { get; init; } = string.Empty;

    public Department AuthorizedDepartments { get; init; } = Department.None;

    public DocumentProcessingStatus Status { get; set; } = DocumentProcessingStatus.Pending;

    public string? ErrorMessage { get; set; }
}
