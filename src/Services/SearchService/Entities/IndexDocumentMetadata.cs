namespace Entities;

/// <summary>Read-only mapping onto index_document_metadata, owned and migrated by KeywordIndexService.</summary>
public sealed class IndexDocumentMetadata
{
    public string DocumentId { get; init; } = string.Empty;

    public int AuthorizedDepartments { get; init; }

    public string FileName { get; init; } = string.Empty;

    public DateTime UpdatedAtUtc { get; init; }
}
