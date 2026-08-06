using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KeywordIndexService.Persistence.Migrations;

/// <inheritdoc />
public partial class AddHybridSearchMetadataAndStats : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "index_chunk_stats",
            columns: table => new
            {
                ChunkId = table.Column<Guid>(type: "uuid", nullable: false),
                DocumentId = table.Column<string>(type: "character varying(600)", maxLength: 600, nullable: false),
                TokenCount = table.Column<int>(type: "integer", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_index_chunk_stats", x => x.ChunkId);
            });

        migrationBuilder.CreateTable(
            name: "index_document_metadata",
            columns: table => new
            {
                DocumentId = table.Column<string>(type: "character varying(600)", maxLength: 600, nullable: false),
                AuthorizedDepartments = table.Column<int>(type: "integer", nullable: false),
                FileName = table.Column<string>(type: "character varying(600)", maxLength: 600, nullable: false),
                UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_index_document_metadata", x => x.DocumentId);
            });

        migrationBuilder.CreateTable(
            name: "index_stats",
            columns: table => new
            {
                Id = table.Column<int>(type: "integer", nullable: false),
                TotalChunks = table.Column<long>(type: "bigint", nullable: false),
                TotalTokenLength = table.Column<long>(type: "bigint", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_index_stats", x => x.Id);
            });

        migrationBuilder.InsertData(
            table: "index_stats",
            columns: new[] { "Id", "TotalChunks", "TotalTokenLength" },
            values: new object[] { 1, 0L, 0L });

        migrationBuilder.CreateIndex(
            name: "IX_index_chunk_stats_DocumentId",
            table: "index_chunk_stats",
            column: "DocumentId");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "index_chunk_stats");

        migrationBuilder.DropTable(
            name: "index_document_metadata");

        migrationBuilder.DropTable(
            name: "index_stats");
    }
}
