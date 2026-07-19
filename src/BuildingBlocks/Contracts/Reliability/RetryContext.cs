namespace Contracts.Reliability;

/// <summary>
/// Retry metadata extracted from inbound Kafka message headers, threaded down to the
/// processing service so it knows how many times this message has already failed.
/// </summary>
public sealed record RetryContext(int RetryCount, DateTime? FirstFailedAtUtc)
{
    public static RetryContext None { get; } = new(0, null);
}
