public sealed class KafkaConsumerSettings
{
    public const string SectionName = "Kafka";

    public string BootstrapServers { get; init; } = string.Empty;

    public string GroupId { get; init; } = string.Empty;

    /// <summary>
    /// Max time allowed between poll cycles before the broker evicts this consumer from the
    /// group ("Unknown member" on the next commit). Must comfortably exceed the worst-case time
    /// to process a single message - large documents can take several minutes to embed.
    /// </summary>
    public int MaxPollIntervalMs { get; init; } = 1_800_000;
}
