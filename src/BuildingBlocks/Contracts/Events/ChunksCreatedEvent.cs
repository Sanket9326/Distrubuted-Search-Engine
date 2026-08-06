namespace Contracts.Events;

public sealed class ChunksCreatedEvent
{
    public string DocumentId { get; init; } = string.Empty;

    public string FileName { get; init; } = string.Empty;

    public Department AuthorizedDepartments { get; init; } = Department.None;

    public int ChunkCount { get; init; }

    public DateTime CreatedAtUtc { get; init; } = DateTime.UtcNow;
}
