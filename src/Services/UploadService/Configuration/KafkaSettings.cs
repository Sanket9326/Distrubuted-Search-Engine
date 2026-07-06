public sealed class KafkaSettings
{
    public const string SectionName = "Kafka";

    public string BootstrapServers { get; init; } = string.Empty;

    public int TopicPartitions { get; init; } = 1;

    public short TopicReplicationFactor { get; init; } = 1;
}
