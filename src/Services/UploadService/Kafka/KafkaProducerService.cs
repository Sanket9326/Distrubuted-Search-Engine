using System.Text.Json;
using Common.Utilities;
using Confluent.Kafka;
using Infrastructure;
using Microsoft.Extensions.Options;

public sealed class KafkaProducerService : IKafkaProducer, IAsyncDisposable
{
    private readonly IProducer<string, string> _producer;
    private readonly ILogger<KafkaProducerService> _logger;
    private readonly IGuidGenerator _guidGenerator;

    public KafkaProducerService(IOptions<KafkaSettings> settings, ILogger<KafkaProducerService> logger, IGuidGenerator guidGenerator)
    {
        _logger = logger;
        _guidGenerator = guidGenerator;
        _producer = new ProducerBuilder<string, string>(new ProducerConfig
        {
            BootstrapServers = settings.Value.BootstrapServers
        }).Build();
    }

    public async Task PublishAsync<TMessage>(string topic, TMessage message, CancellationToken cancellationToken = default)
    {
        try
        {
            var payload = JsonSerializer.Serialize(message);
            var kafkaMessage = new Message<string, string>
            {
                Key = _guidGenerator.NewGuid().ToString(),
                Value = payload
            };

            var result = await _producer.ProduceAsync(topic, kafkaMessage, cancellationToken);

            _logger.LogInformation(
                "Published message to Kafka topic '{Topic}' at partition {Partition}, offset {Offset}",
                topic,
                result.Partition.Value,
                result.Offset.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish message to Kafka topic '{Topic}'", topic);
            throw;
        }
    }

    public ValueTask DisposeAsync()
    {
        _producer.Flush(TimeSpan.FromSeconds(5));
        _producer.Dispose();
        return ValueTask.CompletedTask;
    }
}
