namespace SharedKernel;

public static class Constants
{
    public static class KafkaTopics
    {
        public const string DocumentIngestion = "DocumentIngestion";
        public const string ChunksCreated = "ChunksCreated";
        public const string DocumentIngestionDlq = "DocumentIngestion.DLQ";
        public const string ChunksCreatedDlq = "ChunksCreated.DLQ";

        public static string ToDlqTopic(string originalTopic) => originalTopic switch
        {
            DocumentIngestion => DocumentIngestionDlq,
            ChunksCreated => ChunksCreatedDlq,
            _ => $"{originalTopic}.DLQ"
        };
    }

    /// <summary>
    /// Header names used to carry retry metadata alongside a republished Kafka message,
    /// so the original event payload/contract never needs to change to support retries.
    /// </summary>
    public static class KafkaHeaders
    {
        public const string RetryCount = "x-retry-count";
        public const string FirstFailedAtUtc = "x-first-failed-at-utc";
        public const string LastFailedAtUtc = "x-last-failed-at-utc";
    }
}
