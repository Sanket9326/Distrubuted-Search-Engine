using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DocumentIngestionService.Persistence.Migrations;

/// <inheritdoc />
public partial class WidenDocumentIdColumn : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AlterColumn<string>(
            name: "DocumentId",
            table: "document_metadata",
            type: "character varying(600)",
            maxLength: 600,
            nullable: false,
            oldClrType: typeof(string),
            oldType: "character varying(64)",
            oldMaxLength: 64);

        migrationBuilder.AlterColumn<string>(
            name: "DocumentId",
            table: "document_chunks",
            type: "character varying(600)",
            maxLength: 600,
            nullable: false,
            oldClrType: typeof(string),
            oldType: "character varying(64)",
            oldMaxLength: 64);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AlterColumn<string>(
            name: "DocumentId",
            table: "document_metadata",
            type: "character varying(64)",
            maxLength: 64,
            nullable: false,
            oldClrType: typeof(string),
            oldType: "character varying(600)",
            oldMaxLength: 600);

        migrationBuilder.AlterColumn<string>(
            name: "DocumentId",
            table: "document_chunks",
            type: "character varying(64)",
            maxLength: 64,
            nullable: false,
            oldClrType: typeof(string),
            oldType: "character varying(600)",
            oldMaxLength: 600);
    }
}
