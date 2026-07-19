using Common.Reliability;
using Contracts.Reliability;
using Infrastructure;
using Kafka;
using Microsoft.Extensions.Options;
using Prometheus;
using SharedKernel;

namespace HostedServices;

/// <summary>
/// Drains the shared Redis retry sorted set: sleeps until the earliest-scheduled entry is
/// due (instead of scanning the whole queue on a fixed interval), then atomically pops
/// whatever has become due and either republishes it to its original Kafka topic (with
/// retry-tracking headers) or, once it has exceeded the configured max retry count, routes
/// it to that topic's dead-letter topic.
/// </summary>
public sealed class RetryQueuePollingHostedService : BackgroundService
{
    private static readonly Counter RetryRepublishedTotal = Metrics.CreateCounter(
        "retry_republished_total", "Number of messages republished to their original Kafka topic after a scheduled retry delay", "topic");

    private static readonly Counter RetryExhaustedTotal = Metrics.CreateCounter(
        "retry_exhausted_total", "Number of messages routed to the dead-letter topic after exceeding max retry count", "topic");

    private static readonly Gauge RetryQueueDepth = Metrics.CreateGauge(
        "retry_queue_depth", "Current number of envelopes waiting in the Redis retry queue");

    private readonly IRetryQueue _retryQueue;
    private readonly IRawKafkaPublisher _publisher;
    private readonly RetrySettings _settings;
    private readonly ILogger<RetryQueuePollingHostedService> _logger;

    public RetryQueuePollingHostedService(
        IRetryQueue retryQueue,
        IRawKafkaPublisher publisher,
        IOptions<RetrySettings> settings,
        ILogger<RetryQueuePollingHostedService> logger)
    {
        _retryQueue = retryQueue;
        _publisher = publisher;
        _settings = settings.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                RetryQueueDepth.Set(await _retryQueue.GetQueueDepthAsync(stoppingToken));

                var nextDueAtUnixMs = await _retryQueue.PeekNextDueAtUnixMsAsync(stoppingToken);
                if (nextDueAtUnixMs is null)
                {
                    await Task.Delay(TimeSpan.FromSeconds(_settings.IdlePollSeconds), stoppingToken);
                    continue;
                }

                var nowUnixMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                if (nextDueAtUnixMs.Value > nowUnixMs)
                {
                    var waitMs = Math.Min(nextDueAtUnixMs.Value - nowUnixMs, _settings.MaxWaitSeconds * 1000L);
                    await Task.Delay(TimeSpan.FromMilliseconds(Math.Max(waitMs, 0)), stoppingToken);
                    continue;
                }

                var due = await _retryQueue.PopDueAsync(_settings.PopBatchSize, stoppingToken);
                if (due.Count == 0)
                {
                    // Another replica (or the previous loop iteration) already claimed
                    // whatever was due between our peek and pop; nothing to do this tick.
                    continue;
                }

                foreach (var envelope in due)
                {
                    await RouteAsync(envelope, stoppingToken);
                }

                // Don't sleep here: PopDueAsync may have been capped by PopBatchSize, so
                // loop straight back around to drain any remaining due entries immediately.
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in retry queue polling loop.");
                await Task.Delay(TimeSpan.FromSeconds(_settings.IdlePollSeconds), stoppingToken);
            }
        }
    }

    private async Task RouteAsync(RetryEnvelope envelope, CancellationToken cancellationToken)
    {
        var headers = new Dictionary<string, string>
        {
            [Constants.KafkaHeaders.RetryCount] = envelope.RetryCount.ToString(),
            [Constants.KafkaHeaders.FirstFailedAtUtc] = envelope.FirstFailedAtUtc.ToString("O"),
            [Constants.KafkaHeaders.LastFailedAtUtc] = envelope.LastFailedAtUtc.ToString("O")
        };

        try
        {
            if (envelope.RetryCount > _settings.MaxRetryCount)
            {
                var dlqTopic = Constants.KafkaTopics.ToDlqTopic(envelope.OriginalTopic);
                await _publisher.PublishAsync(dlqTopic, envelope.MessageKey, envelope.Payload, headers, cancellationToken);

                RetryExhaustedTotal.WithLabels(envelope.OriginalTopic).Inc();
                _logger.LogWarning(
                    "Message '{MessageId}' for topic '{Topic}' exhausted {RetryCount} retries; routed to '{DlqTopic}'.",
                    envelope.MessageId, envelope.OriginalTopic, envelope.RetryCount, dlqTopic);
            }
            else
            {
                await _publisher.PublishAsync(envelope.OriginalTopic, envelope.MessageKey, envelope.Payload, headers, cancellationToken);

                RetryRepublishedTotal.WithLabels(envelope.OriginalTopic).Inc();
                _logger.LogInformation(
                    "Republished message '{MessageId}' (attempt {RetryCount}) to topic '{Topic}'.",
                    envelope.MessageId, envelope.RetryCount, envelope.OriginalTopic);
            }
        }
        catch (Exception ex)
        {
            // The atomic pop already removed this envelope from Redis. If the publish above
            // fails (broker unreachable, etc.), re-schedule it rather than lose it - this is
            // the one window where a worker crash between pop and publish could drop a
            // message; re-scheduling here covers the far more common "publish threw" case.
            _logger.LogError(ex, "Failed to route popped envelope '{MessageId}' for topic '{Topic}'; re-scheduling.", envelope.MessageId, envelope.OriginalTopic);
            await _retryQueue.ScheduleAsync(
                envelope.OriginalTopic,
                envelope.MessageKey,
                envelope.Payload,
                envelope.RetryCount - 1,
                envelope.FirstFailedAtUtc,
                ex.Message,
                cancellationToken);
        }
    }
}
