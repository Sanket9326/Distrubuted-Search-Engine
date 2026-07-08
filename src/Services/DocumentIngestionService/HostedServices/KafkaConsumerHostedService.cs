using System.Text.Json;
using Confluent.Kafka;
using Consumers;
using Contracts.Events;
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
            EnableAutoCommit = false
        }).Build();
    }

    /// <summary>
    /// Executes the Kafka consumer hosted service, subscribing to the specified Kafka topic and processing incoming messages.
    /// </summary>
    /// <param name="stoppingToken">The cancellation token to monitor for cancellation requests.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _consumer.Subscribe(Constants.KafkaTopics.DocumentIngestion);

        _logger.LogInformation(
            "Subscribed to Kafka topic '{Topic}' with consumer group '{GroupId}'",
            Constants.KafkaTopics.DocumentIngestion,
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

                var message = JsonSerializer.Deserialize<DocumentUploadedEvent>(result.Message.Value);
                if (message is null)
                {
                    _logger.LogWarning(
                        "Failed to deserialize message from topic '{Topic}' at offset {Offset}",
                        result.Topic,
                        result.Offset);
                    _consumer.Commit(result);
                    continue;
                }

                using var scope = _scopeFactory.CreateScope();
                var consumer = scope.ServiceProvider.GetRequiredService<IDocumentUploadedConsumer>();
                await consumer.ConsumeAsync(message, stoppingToken);

                _consumer.Commit(result);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (ConsumeException ex)
            {
                _logger.LogError(ex, "Error consuming message from Kafka topic '{Topic}'", Constants.KafkaTopics.DocumentIngestion);
            }
        }
    }

    public override void Dispose()
    {
        _consumer.Close();
        _consumer.Dispose();
        base.Dispose();
    }
}