#pragma warning disable CA1861
using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgropecuarIA.Catalog.Migrations
{
    /// <inheritdoc />
    public partial class InitialCatalog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "catalog");

            migrationBuilder.CreateTable(
                name: "catalog_source_snapshots",
                schema: "catalog",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    ContentHash = table.Column<byte[]>(type: "bytea", maxLength: 32, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_catalog_source_snapshots", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "catalog_staging_entries",
                schema: "catalog",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    SourceHash = table.Column<byte[]>(type: "bytea", maxLength: 32, nullable: false),
                    Code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Jurisdiction = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_catalog_staging_entries", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_catalog_source_snapshots_SourceId_ContentHash",
                schema: "catalog",
                table: "catalog_source_snapshots",
                columns: new[] { "SourceId", "ContentHash" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_catalog_staging_entries_SourceId_SourceHash_Code",
                schema: "catalog",
                table: "catalog_staging_entries",
                columns: new[] { "SourceId", "SourceHash", "Code" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "catalog_source_snapshots",
                schema: "catalog");

            migrationBuilder.DropTable(
                name: "catalog_staging_entries",
                schema: "catalog");
        }
    }
}
