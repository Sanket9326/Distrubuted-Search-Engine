using Confluent.Kafka;
using Confluent.Kafka.Admin;
using Microsoft.Extensions.Options;
using SharedKernel;

namespace Kafka;

public sealed class DlqTopicInitializer : IHostedService
{
    private static readonly string[] DlqTopics =
    [
        Constants.KafkaTopics.DocumentIngestionDlq,
        Constants.KafkaTopics.ChunksCreatedDlq
    ];

    private readonly KafkaSettings _settings;
    private readonly ILogger<DlqTopicInitializer> _logger;

    public DlqTopicInitializer(IOptions<KafkaSettings> settings, ILogger<DlqTopicInitializer> logger)
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

        var topicsToCreate = DlqTopics
            .Where(topic => !metadata.Topics.Exists(t => t.Topic == topic && t.Error.Code == ErrorCode.NoError))
            .Select(topic => new TopicSpecification
            {
                Name = topic,
                NumPartitions = _settings.DlqTopicPartitions,
                ReplicationFactor = _settings.DlqTopicReplicationFactor
            })
            .ToArray();

        if (topicsToCreate.Length == 0)
        {
            _logger.LogInformation("All DLQ topics already exist.");
            return;
        }

        try
        {
            await adminClient.CreateTopicsAsync(topicsToCreate);
            _logger.LogInformation("Created DLQ topics: {Topics}", string.Join(", ", topicsToCreate.Select(t => t.Name)));
        }
        catch (CreateTopicsException ex) when (ex.Results.All(r => r.Error.Code is ErrorCode.NoError or ErrorCode.TopicAlreadyExists))
        {
            _logger.LogInformation("DLQ topics already exist (created concurrently by another instance).");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
