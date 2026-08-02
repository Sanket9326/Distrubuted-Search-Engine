using Entities;
using Microsoft.EntityFrameworkCore;

namespace Persistence;

/// <summary>
/// Owns the inverted-index tables (index_terms, index_postings, keyword_index_status) - this
/// service's own migrations, no cross-service FK constraints into document_chunks/document_metadata
/// (owned by DocumentIngestionService), matching the "shared database, no shared code" convention.
/// </summary>
public sealed class KeywordIndexDbContext : DbContext
{
    public KeywordIndexDbContext(DbContextOptions<KeywordIndexDbContext> options) : base(options)
    {
    }

    public DbSet<IndexTerm> IndexTerms => Set<IndexTerm>();

    public DbSet<IndexPosting> IndexPostings => Set<IndexPosting>();

    public DbSet<DocumentKeywordIndexStatus> KeywordIndexStatuses => Set<DocumentKeywordIndexStatus>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<IndexTerm>(entity =>
        {
            entity.ToTable("index_terms");
            entity.HasKey(e => e.TermId);
            entity.Property(e => e.TermId).ValueGeneratedOnAdd();
            entity.Property(e => e.Term).IsRequired().HasMaxLength(100);
            entity.Property(e => e.DocumentFrequency).IsRequired().HasDefaultValue(0);
            entity.HasIndex(e => e.Term).IsUnique();
        });

        modelBuilder.Entity<IndexPosting>(entity =>
        {
            entity.ToTable("index_postings");
            entity.HasKey(e => new { e.TermId, e.ChunkId });
            entity.Property(e => e.DocumentId).IsRequired().HasMaxLength(600);
            entity.Property(e => e.TermFrequency).IsRequired();
            entity.Property(e => e.Positions).IsRequired();
            entity.HasIndex(e => e.ChunkId);
            entity.HasIndex(e => e.DocumentId);
            entity.HasOne<IndexTerm>()
                .WithMany()
                .HasForeignKey(e => e.TermId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<DocumentKeywordIndexStatus>(entity =>
        {
            entity.ToTable("keyword_index_status");
            entity.HasKey(e => e.DocumentId);
            entity.Property(e => e.DocumentId).HasMaxLength(600);
            entity.Property(e => e.Status).IsRequired().HasConversion<int>();
            entity.Property(e => e.ErrorMessage).HasMaxLength(2048);
            entity.Property(e => e.UpdatedAtUtc).IsRequired();
        });
    }
}
