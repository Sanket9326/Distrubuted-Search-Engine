namespace Entities;

/// <summary>Read-only mapping onto index_terms, owned and migrated by KeywordIndexService.</summary>
public sealed class IndexTerm
{
    public int TermId { get; init; }

    public string Term { get; init; } = string.Empty;

    public int DocumentFrequency { get; init; }
}
