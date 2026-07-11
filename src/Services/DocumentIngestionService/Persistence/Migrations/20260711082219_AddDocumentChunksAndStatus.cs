using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DocumentIngestionService.Persistence.Migrations;

/// <inheritdoc />
public partial class AddDocumentChunksAndStatus : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "ErrorMessage",
            table: "document_metadata",
            type: "character varying(2048)",
            maxLength: 2048,
            nullable: true);

        migrationBuilder.AddColumn<int>(
            name: "Status",
            table: "document_metadata",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.CreateTable(
            name: "document_chunks",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                DocumentId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                ChunkIndex = table.Column<int>(type: "integer", nullable: false),
                Content = table.Column<string>(type: "text", nullable: false),
                CharCount = table.Column<int>(type: "integer", nullable: false),
                CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_document_chunks", x => x.Id);
                table.ForeignKey(
                    name: "FK_document_chunks_document_metadata_DocumentId",
                    column: x => x.DocumentId,
                    principalTable: "document_metadata",
                    principalColumn: "DocumentId",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_document_chunks_DocumentId_ChunkIndex",
            table: "document_chunks",
            columns: new[] { "DocumentId", "ChunkIndex" },
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "document_chunks");

        migrationBuilder.DropColumn(
            name: "ErrorMessage",
            table: "document_metadata");

        migrationBuilder.DropColumn(
            name: "Status",
            table: "document_metadata");
    }
}
