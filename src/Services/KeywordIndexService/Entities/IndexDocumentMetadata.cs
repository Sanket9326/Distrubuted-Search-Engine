namespace Entities;

/// <summary>
/// Keyword-index-side equivalent of Qdrant's per-point "authorizedDepartments" payload field -
/// denormalized here so hybrid queries can filter/display without reaching back into
/// DocumentIngestionService's document_metadata table.
/// </summary>
public sealed class IndexDocumentMetadata
{
    public string DocumentId { get; init; } = string.Empty;

    public int AuthorizedDepartments { get; set; }

    public string FileName { get; set; } = string.Empty;

    public DateTime UpdatedAtUtc { get; set; }
}
