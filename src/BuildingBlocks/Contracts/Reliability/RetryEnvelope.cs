namespace Contracts.Reliability;

/// <summary>
/// A failed Kafka message parked in the Redis retry sorted set. This is the JSON payload
/// stored as the sorted-set member itself (score = the Unix-ms timestamp it becomes due).
/// </summary>
public sealed class RetryEnvelope
{
    public Guid MessageId { get; init; }

    public string OriginalTopic { get; init; } = string.Empty;

    public string MessageKey { get; init; } = string.Empty;

    public string Payload { get; init; } = string.Empty;

    public int RetryCount { get; init; }

    public DateTime FirstFailedAtUtc { get; init; }

    public DateTime LastFailedAtUtc { get; init; }

    public string? LastError { get; init; }
}
