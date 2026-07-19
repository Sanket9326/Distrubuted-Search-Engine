public sealed class KafkaSettings
{
    public const string SectionName = "Kafka";

    public string BootstrapServers { get; init; } = string.Empty;

    public int DlqTopicPartitions { get; init; } = 1;

    public short DlqTopicReplicationFactor { get; init; } = 1;
}
