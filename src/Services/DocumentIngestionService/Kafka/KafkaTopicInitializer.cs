using Confluent.Kafka;
using Confluent.Kafka.Admin;
using Microsoft.Extensions.Options;
using SharedKernel;

namespace Kafka;

public sealed class KafkaTopicInitializer : IHostedService
{
    private readonly KafkaConsumerSettings _settings;
    private readonly ILogger<KafkaTopicInitializer> _logger;

    public KafkaTopicInitializer(IOptions<KafkaConsumerSettings> settings, ILogger<KafkaTopicInitializer> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    private static readonly string[] TopicsToCreate =
    [
        Constants.KafkaTopics.ChunksCreated,
        Constants.KafkaTopics.KeywordIndexing
    ];

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var adminClient = new AdminClientBuilder(new AdminClientConfig
        {
            BootstrapServers = _settings.BootstrapServers
        }).Build();

        var metadata = adminClient.GetMetadata(TimeSpan.FromSeconds(10));

        var topicsToCreate = TopicsToCreate
            .Where(topic => !metadata.Topics.Exists(t => t.Topic == topic && t.Error.Code == ErrorCode.NoError))
            .Select(topic => new TopicSpecification
            {
                Name = topic,
                NumPartitions = _settings.TopicPartitions,
                ReplicationFactor = _settings.TopicReplicationFactor
            })
            .ToArray();

        if (topicsToCreate.Length == 0)
        {
            _logger.LogInformation("All Kafka topics already exist.");
            return;
        }

        try
        {
            await adminClient.CreateTopicsAsync(topicsToCreate);
            _logger.LogInformation("Created Kafka topics: {Topics}", string.Join(", ", topicsToCreate.Select(t => t.Name)));
        }
        catch (CreateTopicsException ex) when (ex.Results.All(r => r.Error.Code is ErrorCode.NoError or ErrorCode.TopicAlreadyExists))
        {
            _logger.LogInformation("Kafka topics already exist (created concurrently by another instance).");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
