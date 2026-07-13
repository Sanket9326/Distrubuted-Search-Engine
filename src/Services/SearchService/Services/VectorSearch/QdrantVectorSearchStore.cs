using Contracts;
using Microsoft.Extensions.Options;
using Qdrant.Client;
using Qdrant.Client.Grpc;

namespace Services.VectorSearch;

public sealed class QdrantVectorSearchStore : IVectorSearchStore
{
    private readonly QdrantClient _client;
    private readonly QdrantOptions _options;

    public QdrantVectorSearchStore(IOptions<QdrantOptions> options)
    {
        _options = options.Value;
        _client = new QdrantClient(_options.Endpoint, _options.Port);
    }

    public async Task<IReadOnlyList<ScoredChunk>> SearchAsync(
        float[] queryVector,
        Department departments,
        int limit,
        float minimumScore,
        CancellationToken cancellationToken = default)
    {
        var departmentNames = ToDepartmentNames(departments);
        if (departmentNames.Length == 0)
        {
            return Array.Empty<ScoredChunk>();
        }

        var filter = Conditions.Match("authorizedDepartments", departmentNames);

        var points = await _client.SearchAsync(
            collectionName: _options.CollectionName,
            vector: queryVector,
            filter: filter,
            limit: (ulong)limit,
            scoreThreshold: minimumScore,
            cancellationToken: cancellationToken);

        return points.Select(ToScoredChunk).ToList();
    }

    private static ScoredChunk ToScoredChunk(ScoredPoint point)
    {
        var payload = point.Payload;

        return new ScoredChunk(
            ChunkId: Guid.Parse(point.Id.Uuid),
            DocumentId: payload["documentId"].StringValue,
            FileName: payload.TryGetValue("fileName", out var fileName) ? fileName.StringValue : string.Empty,
            ChunkIndex: (int)payload["chunkIndex"].IntegerValue,
            Content: payload["content"].StringValue,
            Departments: payload["authorizedDepartments"].ListValue.Values.Select(v => v.StringValue).ToArray(),
            CreatedAtUtc: DateTime.Parse(payload["createdAtUtc"].StringValue).ToUniversalTime(),
            Score: point.Score);
    }

    // Mirrors EmbeddingService's QdrantVectorStore.ToDepartmentNames — kept as a small, per-service
    // duplicate rather than shared code, per this repo's "no shared code between services" convention.
    private static string[] ToDepartmentNames(Department departments)
    {
        if (departments == Department.None)
        {
            return [];
        }

        return Enum.GetValues<Department>()
            .Where(d => d != Department.None && d != Department.All && departments.HasFlag(d))
            .Select(d => d.ToString())
            .ToArray();
    }
}
