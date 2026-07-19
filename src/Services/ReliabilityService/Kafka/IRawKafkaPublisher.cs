namespace Kafka;

/// <summary>
/// Publishes an already-serialized JSON payload to an arbitrary topic with custom headers.
/// Unlike <c>Infrastructure.IKafkaProducer</c> (used elsewhere to publish a typed event to a
/// fixed topic), the reliability worker only ever handles pre-serialized retry envelopes and
/// needs to attach retry-tracking headers, so it gets its own small producer rather than
/// reusing/extending the typed contract used by the rest of the system.
/// </summary>
public interface IRawKafkaPublisher
{
    Task PublishAsync(
        string topic,
        string key,
        string payloadJson,
        IReadOnlyDictionary<string, string> headers,
        CancellationToken cancellationToken = default);
}
