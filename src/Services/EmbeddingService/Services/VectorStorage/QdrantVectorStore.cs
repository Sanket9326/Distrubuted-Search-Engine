using Contracts;
using Microsoft.Extensions.Options;
using Qdrant.Client;
using Qdrant.Client.Grpc;

namespace Services.VectorStorage;

public sealed class QdrantVectorStore : IVectorStore
{
    private readonly QdrantClient _client;
    private readonly QdrantOptions _options;
    private readonly ILogger<QdrantVectorStore> _logger;

    public QdrantVectorStore(IOptions<QdrantOptions> options, ILogger<QdrantVectorStore> logger)
    {
        _options = options.Value;
        _logger = logger;
        _client = new QdrantClient(_options.Endpoint, _options.Port);
    }

    public async Task EnsureCollectionAsync(CancellationToken cancellationToken = default)
    {
        var exists = await _client.CollectionExistsAsync(_options.CollectionName, cancellationToken);
        if (exists)
        {
            return;
        }

        await _client.CreateCollectionAsync(
            _options.CollectionName,
            new VectorParams { Size = (ulong)_options.VectorSize, Distance = Distance.Cosine },
            cancellationToken: cancellationToken);

        _logger.LogInformation("Created Qdrant collection '{Collection}'", _options.CollectionName);
    }

    public async Task UpsertAsync(IEnumerable<EmbeddedChunk> chunks, CancellationToken cancellationToken = default)
    {
        var points = chunks.Select(chunk => new PointStruct
        {
            Id = chunk.Id,
            Vectors = chunk.Embedding,
            Payload =
            {
                ["documentId"] = chunk.DocumentId,
                ["chunkIndex"] = chunk.ChunkIndex,
                ["content"] = chunk.Content,
                ["createdAtUtc"] = chunk.CreatedAtUtc.ToString("o"),
                ["authorizedDepartments"] = ToDepartmentNames(chunk.AuthorizedDepartments)
            }
        }).ToList();

        if (points.Count == 0)
        {
            return;
        }

        await _client.UpsertAsync(_options.CollectionName, points, cancellationToken: cancellationToken);
    }

    // Stored as an array of flag names (not the raw int bitmask) so a search request can filter with
    // a Qdrant MatchAny clause on the caller's department, e.g. authorizedDepartments MatchAny ["Finance"].
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
