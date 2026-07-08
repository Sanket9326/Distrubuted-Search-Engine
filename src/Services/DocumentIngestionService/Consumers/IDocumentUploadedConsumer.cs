using Contracts.Events;

namespace Consumers;

public interface IDocumentUploadedConsumer
{
    Task ConsumeAsync(DocumentUploadedEvent message, CancellationToken cancellationToken = default);
}