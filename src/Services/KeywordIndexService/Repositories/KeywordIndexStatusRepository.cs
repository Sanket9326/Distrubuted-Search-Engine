using Entities;
using Microsoft.EntityFrameworkCore;
using Persistence;

namespace Repositories;

/// <summary>
/// Unlike document_metadata (created upfront at upload time), no other service creates a row here
/// ahead of time, so this upserts: the first status transition for a document creates its row.
/// </summary>
public sealed class KeywordIndexStatusRepository : IKeywordIndexStatusRepository
{
    private readonly KeywordIndexDbContext _dbContext;

    public KeywordIndexStatusRepository(KeywordIndexDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task UpdateStatusAsync(string documentId, KeywordIndexStatus status, string? errorMessage, CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.KeywordIndexStatuses
            .FirstOrDefaultAsync(s => s.DocumentId == documentId, cancellationToken);

        var now = DateTime.UtcNow;

        if (entity is null)
        {
            entity = new DocumentKeywordIndexStatus { DocumentId = documentId };
            _dbContext.KeywordIndexStatuses.Add(entity);
        }

        entity.Status = status;
        entity.ErrorMessage = errorMessage;
        entity.UpdatedAtUtc = now;

        if (status == KeywordIndexStatus.Indexed)
        {
            entity.IndexedAtUtc = now;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
