using Entities;
using Microsoft.EntityFrameworkCore;

namespace Persistence;

public sealed class DocumentIngestionDbContext : DbContext
{
    public DocumentIngestionDbContext(DbContextOptions<DocumentIngestionDbContext> options) : base(options)
    {
    }

    public DbSet<DocumentMetadata> DocumentMetadata => Set<DocumentMetadata>();

    public DbSet<DocumentChunk> DocumentChunks => Set<DocumentChunk>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DocumentMetadata>(entity =>
        {
            entity.ToTable("document_metadata");
            entity.HasKey(e => e.DocumentId);
            entity.Property(e => e.DocumentId).HasMaxLength(600);
            entity.Property(e => e.FileName).IsRequired().HasMaxLength(512);
            entity.Property(e => e.ContentType).IsRequired().HasMaxLength(256);
            entity.Property(e => e.AuthorizedDepartments).IsRequired().HasConversion<int>();
            entity.Property(e => e.UploadedAtUtc).IsRequired();
            entity.Property(e => e.IngestedAtUtc).IsRequired();
            entity.Property(e => e.Status).IsRequired().HasConversion<int>();
            entity.Property(e => e.ErrorMessage).HasMaxLength(2048);
        });

        modelBuilder.Entity<DocumentChunk>(entity =>
        {
            entity.ToTable("document_chunks");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.DocumentId).IsRequired().HasMaxLength(600);
            entity.Property(e => e.Content).IsRequired();
            entity.Property(e => e.CharCount).IsRequired();
            entity.Property(e => e.CreatedAtUtc).IsRequired();
            entity.HasIndex(e => new { e.DocumentId, e.ChunkIndex }).IsUnique();
            entity.HasOne<DocumentMetadata>()
                .WithMany()
                .HasForeignKey(e => e.DocumentId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}