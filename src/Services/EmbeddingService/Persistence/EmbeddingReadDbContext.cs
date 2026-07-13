using Contracts;
using Entities;
using Microsoft.EntityFrameworkCore;

namespace Persistence;

public sealed class EmbeddingReadDbContext : DbContext
{
    public EmbeddingReadDbContext(DbContextOptions<EmbeddingReadDbContext> options) : base(options)
    {
    }

    public DbSet<DocumentChunk> DocumentChunks => Set<DocumentChunk>();

    public DbSet<DocumentMetadataStatus> DocumentMetadata => Set<DocumentMetadataStatus>();

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

        modelBuilder.Entity<DocumentMetadataStatus>(entity =>
        {
            entity.ToTable("document_metadata");
            entity.HasKey(e => e.DocumentId);
            entity.Property(e => e.DocumentId).HasMaxLength(600);
            entity.Property(e => e.FileName).HasMaxLength(512);
            entity.Property(e => e.AuthorizedDepartments).IsRequired().HasConversion<int>();
            entity.Property(e => e.Status).IsRequired().HasConversion<int>();
            entity.Property(e => e.ErrorMessage).HasMaxLength(2048);
        });
    }
}
