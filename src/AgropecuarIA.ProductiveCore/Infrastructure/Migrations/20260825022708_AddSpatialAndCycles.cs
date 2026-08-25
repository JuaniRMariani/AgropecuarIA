using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1861 // EF Core scaffolds transient arrays for migration metadata.

namespace AgropecuarIA.ProductiveCore.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSpatialAndCycles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_management_units_SpatialStatus",
                schema: "productive_core",
                table: "management_units");

            migrationBuilder.DropCheckConstraint(
                name: "CK_management_units_Status",
                schema: "productive_core",
                table: "management_units");

            migrationBuilder.AddColumn<string>(
                name: "BoundaryGeoJson",
                schema: "productive_core",
                table: "management_units",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "CalculatedAreaHectares",
                schema: "productive_core",
                table: "management_units",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "CentroidLatitude",
                schema: "productive_core",
                table: "management_units",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "CentroidLongitude",
                schema: "productive_core",
                table: "management_units",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "DeclaredAreaHectares",
                schema: "productive_core",
                table: "management_units",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OfficialDepartmentCode",
                schema: "productive_core",
                table: "management_units",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OfficialProvinceCode",
                schema: "productive_core",
                table: "management_units",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "inbox_entries",
                schema: "productive_core",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MessageId = table.Column<Guid>(type: "uuid", nullable: false),
                    ConsumerName = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProcessedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_inbox_entries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "production_cycles",
                schema: "productive_core",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    ManagementUnitId = table.Column<Guid>(type: "uuid", nullable: false),
                    CatalogCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    CatalogDisplayName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Purpose = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    System = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    SupportLevel = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    StartDateUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    EndDateUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_production_cycles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "production_events",
                schema: "productive_core",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductionCycleId = table.Column<Guid>(type: "uuid", nullable: false),
                    EventType = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    EffectiveDateUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    RecordedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    Unit = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    Notes = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    Origin = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_production_events", x => x.Id);
                });

            migrationBuilder.AddCheckConstraint(
                name: "CK_management_units_SpatialStatus",
                schema: "productive_core",
                table: "management_units",
                sql: "\"SpatialStatus\" IN ('not_configured', 'configured')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_management_units_Status",
                schema: "productive_core",
                table: "management_units",
                sql: "\"Status\" IN ('draft', 'archived')");

            migrationBuilder.CreateIndex(
                name: "IX_inbox_entries_OrganizationId_ConsumerName_MessageId",
                schema: "productive_core",
                table: "inbox_entries",
                columns: new[] { "OrganizationId", "ConsumerName", "MessageId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_production_cycles_OrganizationId_ManagementUnitId_Status",
                schema: "productive_core",
                table: "production_cycles",
                columns: new[] { "OrganizationId", "ManagementUnitId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_production_events_OrganizationId_ProductionCycleId_Effectiv~",
                schema: "productive_core",
                table: "production_events",
                columns: new[] { "OrganizationId", "ProductionCycleId", "EffectiveDateUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "inbox_entries",
                schema: "productive_core");

            migrationBuilder.DropTable(
                name: "production_cycles",
                schema: "productive_core");

            migrationBuilder.DropTable(
                name: "production_events",
                schema: "productive_core");

            migrationBuilder.DropCheckConstraint(
                name: "CK_management_units_SpatialStatus",
                schema: "productive_core",
                table: "management_units");

            migrationBuilder.DropCheckConstraint(
                name: "CK_management_units_Status",
                schema: "productive_core",
                table: "management_units");

            migrationBuilder.DropColumn(
                name: "BoundaryGeoJson",
                schema: "productive_core",
                table: "management_units");

            migrationBuilder.DropColumn(
                name: "CalculatedAreaHectares",
                schema: "productive_core",
                table: "management_units");

            migrationBuilder.DropColumn(
                name: "CentroidLatitude",
                schema: "productive_core",
                table: "management_units");

            migrationBuilder.DropColumn(
                name: "CentroidLongitude",
                schema: "productive_core",
                table: "management_units");

            migrationBuilder.DropColumn(
                name: "DeclaredAreaHectares",
                schema: "productive_core",
                table: "management_units");

            migrationBuilder.DropColumn(
                name: "OfficialDepartmentCode",
                schema: "productive_core",
                table: "management_units");

            migrationBuilder.DropColumn(
                name: "OfficialProvinceCode",
                schema: "productive_core",
                table: "management_units");

            migrationBuilder.AddCheckConstraint(
                name: "CK_management_units_SpatialStatus",
                schema: "productive_core",
                table: "management_units",
                sql: "\"SpatialStatus\" = 'not_configured'");

            migrationBuilder.AddCheckConstraint(
                name: "CK_management_units_Status",
                schema: "productive_core",
                table: "management_units",
                sql: "\"Status\" = 'draft'");
        }
    }
}
