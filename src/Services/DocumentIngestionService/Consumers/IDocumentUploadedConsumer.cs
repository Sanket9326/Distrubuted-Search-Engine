using Contracts.Events;
using Contracts.Reliability;

namespace Consumers;

public interface IDocumentUploadedConsumer
{
    Task ConsumeAsync(DocumentUploadedEvent message, RetryContext retryContext, CancellationToken cancellationToken = default);
}