using Contracts.Events;

namespace Consumers;

public interface IChunksCreatedConsumer
{
    Task ConsumeAsync(ChunksCreatedEvent message, CancellationToken cancellationToken = default);
}
