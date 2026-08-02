using Contracts.Events;
using Contracts.Reliability;

namespace Consumers;

public interface IKeywordIndexingConsumer
{
    Task ConsumeAsync(ChunksCreatedEvent message, RetryContext retryContext, CancellationToken cancellationToken = default);
}
