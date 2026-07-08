using Entities;
using Microsoft.EntityFrameworkCore;

namespace Persistence;

public sealed class DocumentIngestionDbContext : DbContext
{
    public DocumentIngestionDbContext(DbContextOptions<DocumentIngestionDbContext> options) : base(options)
    {
    }

    public DbSet<DocumentMetadata> DocumentMetadata => Set<DocumentMetadata>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DocumentMetadata>(entity =>
        {
            entity.ToTable("document_metadata");
            entity.HasKey(e => e.DocumentId);
            entity.Property(e => e.DocumentId).HasMaxLength(64);
            entity.Property(e => e.FileName).IsRequired().HasMaxLength(512);
            entity.Property(e => e.ContentType).IsRequired().HasMaxLength(256);
            entity.Property(e => e.AuthorizedDepartments).IsRequired().HasConversion<int>();
            entity.Property(e => e.UploadedAtUtc).IsRequired();
            entity.Property(e => e.IngestedAtUtc).IsRequired();
        });
    }
}