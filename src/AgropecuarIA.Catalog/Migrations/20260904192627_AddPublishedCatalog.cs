using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1861 // EF Core scaffolds transient arrays for migration metadata.

namespace AgropecuarIA.Catalog.Migrations
{
    /// <inheritdoc />
    public partial class AddPublishedCatalog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "catalog_published_items",
                schema: "catalog",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    VersionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    NormalizedCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    NormalizedDisplayName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Jurisdiction = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    SupportLevel = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Category = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Synonyms = table.Column<string>(type: "jsonb", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_catalog_published_items", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "catalog_published_versions",
                schema: "catalog",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    VersionTag = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    PublishedBy = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    ItemsCount = table.Column<int>(type: "integer", nullable: false),
                    PublishedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_catalog_published_versions", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_catalog_published_items_Jurisdiction",
                schema: "catalog",
                table: "catalog_published_items",
                column: "Jurisdiction");

            migrationBuilder.CreateIndex(
                name: "IX_catalog_published_items_NormalizedCode",
                schema: "catalog",
                table: "catalog_published_items",
                column: "NormalizedCode");

            migrationBuilder.CreateIndex(
                name: "IX_catalog_published_items_NormalizedDisplayName",
                schema: "catalog",
                table: "catalog_published_items",
                column: "NormalizedDisplayName");

            migrationBuilder.CreateIndex(
                name: "IX_catalog_published_items_SupportLevel",
                schema: "catalog",
                table: "catalog_published_items",
                column: "SupportLevel");

            migrationBuilder.CreateIndex(
                name: "IX_catalog_published_items_VersionId_Code",
                schema: "catalog",
                table: "catalog_published_items",
                columns: new[] { "VersionId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_catalog_published_versions_IsActive",
                schema: "catalog",
                table: "catalog_published_versions",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_catalog_published_versions_VersionTag",
                schema: "catalog",
                table: "catalog_published_versions",
                column: "VersionTag",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "catalog_published_items",
                schema: "catalog");

            migrationBuilder.DropTable(
                name: "catalog_published_versions",
                schema: "catalog");
        }
    }
}
