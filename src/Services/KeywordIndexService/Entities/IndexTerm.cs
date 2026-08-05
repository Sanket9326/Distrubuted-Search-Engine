namespace Entities;

public sealed class IndexTerm
{
    public int TermId { get; set; }

    public string Term { get; init; } = string.Empty;

    /// <summary>Number of distinct chunks currently containing this term.</summary>
    public int DocumentFrequency { get; set; }
}
