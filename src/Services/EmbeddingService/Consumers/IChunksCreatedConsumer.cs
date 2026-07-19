using Contracts.Events;
using Contracts.Reliability;

namespace Consumers;

public interface IChunksCreatedConsumer
{
    Task ConsumeAsync(ChunksCreatedEvent message, RetryContext retryContext, CancellationToken cancellationToken = default);
}
