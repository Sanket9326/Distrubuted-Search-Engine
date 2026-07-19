namespace Common.Reliability;

public sealed class RetrySettings
{
    public const string SectionName = "Retry";

    /// <summary>Attempts beyond this count are routed to the dead-letter topic instead of retried again.</summary>
    public int MaxRetryCount { get; init; } = 5;

    /// <summary>Backoff delay (minutes) for the Nth retry, indexed by retryCount - 1 and clamped to the last entry.</summary>
    public int[] BackoffMinutes { get; init; } = [2, 5, 15, 30, 60];

    /// <summary>How often the worker re-checks Redis when the queue is empty.</summary>
    public int IdlePollSeconds { get; init; } = 5;

    /// <summary>Upper bound on how long the worker sleeps while waiting for the next known due item, so a newly scheduled item with an earlier due time is never missed for too long.</summary>
    public int MaxWaitSeconds { get; init; } = 30;

    /// <summary>Max envelopes popped per due batch.</summary>
    public int PopBatchSize { get; init; } = 50;
}
