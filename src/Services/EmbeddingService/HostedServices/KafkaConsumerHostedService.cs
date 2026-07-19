using System.Text;
using System.Text.Json;
using Confluent.Kafka;
using Consumers;
using Contracts.Events;
using Contracts.Reliability;
using Microsoft.Extensions.Options;
using SharedKernel;

namespace HostedServices;

public sealed class KafkaConsumerHostedService : BackgroundService
{
    private readonly KafkaConsumerSettings _settings;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<KafkaConsumerHostedService> _logger;
    private readonly IConsumer<string, string> _consumer;

    public KafkaConsumerHostedService(
        IOptions<KafkaConsumerSettings> settings,
        IServiceScopeFactory scopeFactory,
        ILogger<KafkaConsumerHostedService> logger)
    {
        _settings = settings.Value;
        _scopeFactory = scopeFactory;
        _logger = logger;

        _consumer = new ConsumerBuilder<string, string>(new ConsumerConfig
        {
            BootstrapServers = _settings.BootstrapServers,
            GroupId = _settings.GroupId,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false,
            MaxPollIntervalMs = _settings.MaxPollIntervalMs
        }).Build();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _consumer.Subscribe(Constants.KafkaTopics.ChunksCreated);

        _logger.LogInformation(
            "Subscribed to Kafka topic '{Topic}' with consumer group '{GroupId}'",
            Constants.KafkaTopics.ChunksCreated,
            _settings.GroupId);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var result = _consumer.Consume(stoppingToken);
                if (result?.Message is null)
                {
                    continue;
                }

                var message = JsonSerializer.Deserialize<ChunksCreatedEvent>(result.Message.Value);
                if (message is null)
                {
                    _logger.LogWarning(
                        "Failed to deserialize message from topic '{Topic}' at offset {Offset}",
                        result.Topic,
                        result.Offset);
                    _consumer.Commit(result);
                    continue;
                }

                var retryContext = ExtractRetryContext(result.Message.Headers);

                using var scope = _scopeFactory.CreateScope();
                var consumer = scope.ServiceProvider.GetRequiredService<IChunksCreatedConsumer>();
                await consumer.ConsumeAsync(message, retryContext, stoppingToken);

                _consumer.Commit(result);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (ConsumeException ex)
            {
                _logger.LogError(ex, "Error consuming message from Kafka topic '{Topic}'", Constants.KafkaTopics.ChunksCreated);
            }
            catch (KafkaException ex)
            {
                // e.g. "Unknown member" committing after a group rebalance evicted this consumer
                // (max.poll.interval.ms exceeded). Log and keep looping rather than letting this
                // crash the whole host - the un-committed message will simply be redelivered.
                _logger.LogError(ex, "Kafka broker error while processing/committing on topic '{Topic}'; message will be redelivered.", Constants.KafkaTopics.ChunksCreated);
            }
        }
    }

    /// <summary>
    /// Reads retry metadata off the inbound message's headers. A message not carrying these
    /// headers (the common case) is its first attempt.
    /// </summary>
    private static RetryContext ExtractRetryContext(Headers? headers)
    {
        if (headers is null)
        {
            return RetryContext.None;
        }

        var retryCount = 0;
        if (headers.TryGetLastBytes(Constants.KafkaHeaders.RetryCount, out var retryCountBytes)
            && int.TryParse(Encoding.UTF8.GetString(retryCountBytes), out var parsedRetryCount))
        {
            retryCount = parsedRetryCount;
        }

        DateTime? firstFailedAtUtc = null;
        if (headers.TryGetLastBytes(Constants.KafkaHeaders.FirstFailedAtUtc, out var firstFailedBytes)
            && DateTime.TryParse(Encoding.UTF8.GetString(firstFailedBytes), out var parsedFirstFailedAtUtc))
        {
            firstFailedAtUtc = DateTime.SpecifyKind(parsedFirstFailedAtUtc, DateTimeKind.Utc);
        }

        return new RetryContext(retryCount, firstFailedAtUtc);
    }

    public override void Dispose()
    {
        _consumer.Close();
        _consumer.Dispose();
        base.Dispose();
    }
}
