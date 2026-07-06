using Confluent.Kafka;
using Confluent.Kafka.Admin;
using Microsoft.Extensions.Options;
using SharedKernel;

public sealed class KafkaTopicInitializer : IHostedService
{
    private readonly KafkaSettings _settings;
    private readonly ILogger<KafkaTopicInitializer> _logger;

    public KafkaTopicInitializer(IOptions<KafkaSettings> settings, ILogger<KafkaTopicInitializer> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var adminClient = new AdminClientBuilder(new AdminClientConfig
        {
            BootstrapServers = _settings.BootstrapServers
        }).Build();

        var metadata = adminClient.GetMetadata(TimeSpan.FromSeconds(10));
        var topicExists = metadata.Topics.Exists(t =>
            t.Topic == Constants.KafkaTopics.DocumentIngestion && t.Error.Code == ErrorCode.NoError);

        if (topicExists)
        {
            _logger.LogInformation("Kafka topic '{Topic}' already exists", Constants.KafkaTopics.DocumentIngestion);
            return;
        }

        try
        {
            await adminClient.CreateTopicsAsync(new[]
            {
                new TopicSpecification
                {
                    Name = Constants.KafkaTopics.DocumentIngestion,
                    NumPartitions = _settings.TopicPartitions,
                    ReplicationFactor = _settings.TopicReplicationFactor
                }
            });

            _logger.LogInformation("Created Kafka topic '{Topic}'", Constants.KafkaTopics.DocumentIngestion);
        }
        catch (CreateTopicsException ex) when (ex.Results.Any(r => r.Error.Code == ErrorCode.TopicAlreadyExists))
        {
            _logger.LogInformation("Kafka topic '{Topic}' already exists", Constants.KafkaTopics.DocumentIngestion);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
