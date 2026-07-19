using System.Text.Json;
using Contracts.Reliability;
using Infrastructure;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Prometheus;
using StackExchange.Redis;

namespace Common.Reliability;

/// <summary>
/// Redis sorted-set backed retry queue. A single global key holds every pending retry
/// across all topics/services; the envelope JSON is the sorted-set member itself (it embeds
/// a GUID so members are always unique), scored by the Unix-ms timestamp it becomes due.
///
/// Popping due entries uses a Lua script so the "find what's due" + "remove it" pair is
/// atomic from Redis's point of view — safe even if multiple ReliabilityService replicas
/// (or overlapping loop iterations) call PopDueAsync at the same moment, since Redis
/// executes the whole script single-threaded and no two callers can claim the same member.
/// </summary>
public sealed class RedisRetryQueue : IRetryQueue
{
    private const string RetryQueueKey = "retry:queue";

    private const string PopDueScript = """
        local due = redis.call('ZRANGEBYSCORE', KEYS[1], '-inf', ARGV[1], 'LIMIT', 0, ARGV[2])
        if #due == 0 then
            return {}
        end
        for i = 1, #due do
            redis.call('ZREM', KEYS[1], due[i])
        end
        return due
        """;

    private static readonly Counter RetryScheduledTotal = Metrics.CreateCounter(
        "retry_scheduled_total", "Number of messages scheduled onto the Redis retry queue", "topic");

    private const int LastErrorMaxLength = 2000;

    private readonly IConnectionMultiplexer _redis;
    private readonly RetrySettings _settings;
    private readonly ILogger<RedisRetryQueue> _logger;

    public RedisRetryQueue(IConnectionMultiplexer redis, IOptions<RetrySettings> settings, ILogger<RedisRetryQueue> logger)
    {
        _redis = redis;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<bool> ScheduleAsync(
        string topic,
        string key,
        string payloadJson,
        int currentRetryCount,
        DateTime? firstFailedAtUtc,
        string? lastError,
        CancellationToken cancellationToken = default)
    {
        var newRetryCount = currentRetryCount + 1;
        var exceeded = newRetryCount > _settings.MaxRetryCount;
        var now = DateTime.UtcNow;

        var envelope = new RetryEnvelope
        {
            MessageId = Guid.NewGuid(),
            OriginalTopic = topic,
            MessageKey = key,
            Payload = payloadJson,
            RetryCount = newRetryCount,
            FirstFailedAtUtc = firstFailedAtUtc ?? now,
            LastFailedAtUtc = now,
            LastError = Truncate(lastError, LastErrorMaxLength)
        };

        var delayMinutes = exceeded
            ? 0
            : _settings.BackoffMinutes[Math.Min(newRetryCount - 1, _settings.BackoffMinutes.Length - 1)];
        var dueAtUnixMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + delayMinutes * 60_000L;

        var db = _redis.GetDatabase();
        await db.SortedSetAddAsync(RetryQueueKey, JsonSerializer.Serialize(envelope), dueAtUnixMs);

        RetryScheduledTotal.WithLabels(topic).Inc();

        if (exceeded)
        {
            _logger.LogWarning(
                "Message '{MessageId}' for topic '{Topic}' exceeded max retry count ({MaxRetryCount}); queued for immediate dead-letter routing.",
                envelope.MessageId, topic, _settings.MaxRetryCount);
        }
        else
        {
            _logger.LogInformation(
                "Scheduled retry {RetryCount}/{MaxRetryCount} for message '{MessageId}' on topic '{Topic}', due in {DelayMinutes} min.",
                newRetryCount, _settings.MaxRetryCount, envelope.MessageId, topic, delayMinutes);
        }

        return !exceeded;
    }

    public async Task<IReadOnlyList<RetryEnvelope>> PopDueAsync(int batchSize, CancellationToken cancellationToken = default)
    {
        var db = _redis.GetDatabase();
        var nowUnixMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        var result = await db.ScriptEvaluateAsync(
            PopDueScript,
            [RetryQueueKey],
            [nowUnixMs, batchSize]);

        var members = (RedisValue[])result!;
        if (members.Length == 0)
        {
            return [];
        }

        var envelopes = new List<RetryEnvelope>(members.Length);
        foreach (var member in members)
        {
            var envelope = JsonSerializer.Deserialize<RetryEnvelope>((string)member!);
            if (envelope is not null)
            {
                envelopes.Add(envelope);
            }
            else
            {
                _logger.LogError("Failed to deserialize a retry envelope popped from Redis: {Raw}", member);
            }
        }

        return envelopes;
    }

    public async Task<long?> PeekNextDueAtUnixMsAsync(CancellationToken cancellationToken = default)
    {
        var db = _redis.GetDatabase();
        var entries = await db.SortedSetRangeByRankWithScoresAsync(RetryQueueKey, 0, 0);
        return entries.Length == 0 ? null : (long)entries[0].Score;
    }

    public async Task<long> GetQueueDepthAsync(CancellationToken cancellationToken = default)
    {
        var db = _redis.GetDatabase();
        return await db.SortedSetLengthAsync(RetryQueueKey);
    }

    private static string? Truncate(string? value, int maxLength) =>
        value is null || value.Length <= maxLength ? value : value[..maxLength];
}
