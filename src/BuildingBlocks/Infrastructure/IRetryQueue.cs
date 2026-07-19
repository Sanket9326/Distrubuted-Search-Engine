using Contracts.Reliability;

namespace Infrastructure;

/// <summary>
/// Abstraction over the Redis-backed scheduled-retry queue: consuming services schedule a
/// failed message for a later attempt, and the reliability worker pops whatever is due.
/// </summary>
public interface IRetryQueue
{
    /// <summary>
    /// Schedules a failed message for retry, computing the next-attempt delay from the
    /// configured backoff schedule. Returns <c>false</c> once the retry count has exceeded
    /// the configured maximum — the message is still enqueued (due immediately) so the
    /// worker can route it to the dead-letter topic instead of retrying it again.
    /// </summary>
    Task<bool> ScheduleAsync(
        string topic,
        string key,
        string payloadJson,
        int currentRetryCount,
        DateTime? firstFailedAtUtc,
        string? lastError,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Atomically pops up to <paramref name="batchSize"/> envelopes whose due time has
    /// elapsed. Safe to call concurrently from multiple worker instances.
    /// </summary>
    Task<IReadOnlyList<RetryEnvelope>> PopDueAsync(int batchSize, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the due-timestamp (Unix milliseconds) of the earliest-scheduled entry, or
    /// <c>null</c> if the queue is empty. Lets the worker sleep until that time instead of
    /// polling the whole queue on a fixed interval.
    /// </summary>
    Task<long?> PeekNextDueAtUnixMsAsync(CancellationToken cancellationToken = default);

    Task<long> GetQueueDepthAsync(CancellationToken cancellationToken = default);
}
