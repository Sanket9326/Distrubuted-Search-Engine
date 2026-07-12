public sealed class QdrantOptions
{
    public const string SectionName = "Qdrant";

    public string Endpoint { get; init; } = string.Empty;

    public int Port { get; init; } = 6334;

    public string CollectionName { get; init; } = "document_chunks";

    public int VectorSize { get; init; } = 768;
}
