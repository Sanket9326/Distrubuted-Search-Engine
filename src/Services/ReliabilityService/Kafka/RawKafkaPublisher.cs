using System.Text;
using Confluent.Kafka;
using Microsoft.Extensions.Options;

namespace Kafka;

public sealed class RawKafkaPublisher : IRawKafkaPublisher, IAsyncDisposable
{
    private readonly IProducer<string, string> _producer;
    private readonly ILogger<RawKafkaPublisher> _logger;

    public RawKafkaPublisher(IOptions<KafkaSettings> settings, ILogger<RawKafkaPublisher> logger)
    {
        _logger = logger;
        _producer = new ProducerBuilder<string, string>(new ProducerConfig
        {
            BootstrapServers = settings.Value.BootstrapServers
        }).Build();
    }

    public async Task PublishAsync(
        string topic,
        string key,
        string payloadJson,
        IReadOnlyDictionary<string, string> headers,
        CancellationToken cancellationToken = default)
    {
        var kafkaHeaders = new Headers();
        foreach (var (name, value) in headers)
        {
            kafkaHeaders.Add(name, Encoding.UTF8.GetBytes(value));
        }

        var message = new Message<string, string>
        {
            Key = key,
            Value = payloadJson,
            Headers = kafkaHeaders
        };

        try
        {
            var result = await _producer.ProduceAsync(topic, message, cancellationToken);
            _logger.LogInformation(
                "Published message to '{Topic}' at partition {Partition}, offset {Offset}",
                topic, result.Partition.Value, result.Offset.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish message to '{Topic}'", topic);
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
