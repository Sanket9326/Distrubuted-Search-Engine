using Entities;
using Microsoft.EntityFrameworkCore;

namespace Persistence;

/// <summary>
/// Read-only mapping onto index_terms/index_postings/index_chunk_stats/index_stats/index_document_metadata
/// (owned and migrated by KeywordIndexService) plus document_chunks (owned by DocumentIngestionService,
/// needed here to hydrate BM25 candidates with their chunk content). No migrations here - matches the
/// same cross-service read-only pattern KeywordIndexService itself already uses for document_chunks.
/// </summary>
public sealed class KeywordIndexReadDbContext : DbContext
{
    public KeywordIndexReadDbContext(DbContextOptions<KeywordIndexReadDbContext> options) : base(options)
    {
    }

    public DbSet<IndexTerm> IndexTerms => Set<IndexTerm>();

    public DbSet<IndexPosting> IndexPostings => Set<IndexPosting>();

    public DbSet<IndexChunkStats> IndexChunkStats => Set<IndexChunkStats>();

    public DbSet<IndexStats> IndexStats => Set<IndexStats>();

    public DbSet<IndexDocumentMetadata> IndexDocumentMetadata => Set<IndexDocumentMetadata>();

    public DbSet<DocumentChunk> DocumentChunks => Set<DocumentChunk>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<IndexTerm>(entity =>
        {
            entity.ToTable("index_terms");
            entity.HasKey(e => e.TermId);
            entity.Property(e => e.Term).IsRequired().HasMaxLength(100);
            entity.Property(e => e.DocumentFrequency).IsRequired();
        });

        modelBuilder.Entity<IndexPosting>(entity =>
        {
            entity.ToTable("index_postings");
            entity.HasKey(e => new { e.TermId, e.ChunkId });
            entity.Property(e => e.DocumentId).IsRequired().HasMaxLength(600);
            entity.Property(e => e.TermFrequency).IsRequired();
            entity.Property(e => e.Positions).IsRequired();
        });

        modelBuilder.Entity<IndexChunkStats>(entity =>
        {
            entity.ToTable("index_chunk_stats");
            entity.HasKey(e => e.ChunkId);
            entity.Property(e => e.DocumentId).IsRequired().HasMaxLength(600);
            entity.Property(e => e.TokenCount).IsRequired();
        });

        modelBuilder.Entity<IndexStats>(entity =>
        {
            entity.ToTable("index_stats");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TotalChunks).IsRequired();
            entity.Property(e => e.TotalTokenLength).IsRequired();
        });

        modelBuilder.Entity<IndexDocumentMetadata>(entity =>
        {
            entity.ToTable("index_document_metadata");
            entity.HasKey(e => e.DocumentId);
            entity.Property(e => e.DocumentId).HasMaxLength(600);
            entity.Property(e => e.AuthorizedDepartments).IsRequired();
            entity.Property(e => e.FileName).IsRequired().HasMaxLength(600);
            entity.Property(e => e.UpdatedAtUtc).IsRequired();
        });

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
