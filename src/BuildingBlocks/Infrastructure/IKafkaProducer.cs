namespace Infrastructure;

public interface IKafkaProducer
{
    Task PublishAsync<TMessage>(string topic, TMessage message, CancellationToken cancellationToken = default);
}
