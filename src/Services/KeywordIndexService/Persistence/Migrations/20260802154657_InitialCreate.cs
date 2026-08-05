using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace KeywordIndexService.Persistence.Migrations;

/// <inheritdoc />
public partial class InitialCreate : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "index_terms",
            columns: table => new
            {
                TermId = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                Term = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                DocumentFrequency = table.Column<int>(type: "integer", nullable: false, defaultValue: 0)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_index_terms", x => x.TermId);
            });

        migrationBuilder.CreateTable(
            name: "keyword_index_status",
            columns: table => new
            {
                DocumentId = table.Column<string>(type: "character varying(600)", maxLength: 600, nullable: false),
                Status = table.Column<int>(type: "integer", nullable: false),
                ErrorMessage = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                IndexedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_keyword_index_status", x => x.DocumentId);
            });

        migrationBuilder.CreateTable(
            name: "index_postings",
            columns: table => new
            {
                TermId = table.Column<int>(type: "integer", nullable: false),
                ChunkId = table.Column<Guid>(type: "uuid", nullable: false),
                DocumentId = table.Column<string>(type: "character varying(600)", maxLength: 600, nullable: false),
                TermFrequency = table.Column<int>(type: "integer", nullable: false),
                Positions = table.Column<int[]>(type: "integer[]", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_index_postings", x => new { x.TermId, x.ChunkId });
                table.ForeignKey(
                    name: "FK_index_postings_index_terms_TermId",
                    column: x => x.TermId,
                    principalTable: "index_terms",
                    principalColumn: "TermId",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_index_postings_ChunkId",
            table: "index_postings",
            column: "ChunkId");

        migrationBuilder.CreateIndex(
            name: "IX_index_postings_DocumentId",
            table: "index_postings",
            column: "DocumentId");

        migrationBuilder.CreateIndex(
            name: "IX_index_terms_Term",
            table: "index_terms",
            column: "Term",
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "index_postings");

        migrationBuilder.DropTable(
            name: "keyword_index_status");

        migrationBuilder.DropTable(
            name: "index_terms");
    }
}
