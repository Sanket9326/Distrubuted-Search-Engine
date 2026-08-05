using Entities;

namespace Repositories;

public interface IKeywordIndexStatusRepository
{
    Task UpdateStatusAsync(string documentId, KeywordIndexStatus status, string? errorMessage, CancellationToken cancellationToken = default);
}
