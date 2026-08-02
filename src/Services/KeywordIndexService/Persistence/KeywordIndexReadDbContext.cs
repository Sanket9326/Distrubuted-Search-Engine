using Entities;
using Microsoft.EntityFrameworkCore;

namespace Persistence;

/// <summary>
/// Read-only mapping onto document_chunks, owned and migrated by DocumentIngestionService.
/// No migrations here - matches EmbeddingService's read model over the same shared table.
/// </summary>
public sealed class KeywordIndexReadDbContext : DbContext
{
    public KeywordIndexReadDbContext(DbContextOptions<KeywordIndexReadDbContext> options) : base(options)
    {
    }

    public DbSet<DocumentChunk> DocumentChunks => Set<DocumentChunk>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DocumentChunk>(entity =>
        {
            entity.ToTable("document_chunks");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.DocumentId).IsRequired().HasMaxLength(600);
            entity.Property(e => e.Content).IsRequired();
            entity.Property(e => e.CharCount).IsRequired();
            entity.Property(e => e.CreatedAtUtc).IsRequired();
        });
    }
}
