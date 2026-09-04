using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgropecuarIA.ProductiveCore.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddProductionHistoryPagingIndexes : Migration
    {
        private static readonly string[] EventColumns = ["OrganizationId", "ProductionCycleId", "RecordedAtUtc", "Id"];
        private static readonly string[] CycleColumns = ["OrganizationId", "ManagementUnitId", "CreatedAtUtc", "Id"];
        private static readonly bool[] Descending = [false, false, true, true];

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_production_events_history_page",
                schema: "productive_core",
                table: "production_events",
                columns: EventColumns,
                descending: Descending);

            migrationBuilder.CreateIndex(
                name: "IX_production_cycles_history_page",
                schema: "productive_core",
                table: "production_cycles",
                columns: CycleColumns,
                descending: Descending);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_production_events_history_page",
                schema: "productive_core",
                table: "production_events");

            migrationBuilder.DropIndex(
                name: "IX_production_cycles_history_page",
                schema: "productive_core",
                table: "production_cycles");
        }
    }
}
