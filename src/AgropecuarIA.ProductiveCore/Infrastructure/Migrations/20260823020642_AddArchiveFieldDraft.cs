using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgropecuarIA.ProductiveCore.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddArchiveFieldDraft : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "management_unit_archive_ledgers",
                schema: "productive_core",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    ScopeKind = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    Namespace = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Operation = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ContractVersion = table.Column<int>(type: "integer", nullable: false),
                    CanonicalizationVersion = table.Column<int>(type: "integer", nullable: false),
                    ActorUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    SessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    AuthorizationVersion = table.Column<Guid>(type: "uuid", nullable: false),
                    ManagementUnitId = table.Column<Guid>(type: "uuid", nullable: false),
                    ExpectedVersion = table.Column<Guid>(type: "uuid", nullable: false),
                    RequestFingerprint = table.Column<byte[]>(type: "bytea", maxLength: 32, nullable: false),
                    State = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    ResultVersion = table.Column<Guid>(type: "uuid", nullable: true),
                    ResultRevision = table.Column<long>(type: "bigint", nullable: true),
                    LeaseOwner = table.Column<Guid>(type: "uuid", nullable: false),
                    FenceToken = table.Column<long>(type: "bigint", nullable: false),
                    LeaseUntilUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    StartedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CompletedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Version = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_management_unit_archive_ledgers", x => x.Id);
                    table.UniqueConstraint("AK_management_unit_archive_ledgers_Id_OrganizationId", x => new { x.Id, x.OrganizationId });
                    table.CheckConstraint("CK_management_unit_archive_ledgers_DigestLength", "octet_length(\"RequestFingerprint\") = 32");
                    table.CheckConstraint("CK_management_unit_archive_ledgers_Fence", "\"FenceToken\" > 0 AND \"LeaseUntilUtc\" > \"StartedAtUtc\"");
                    table.CheckConstraint("CK_management_unit_archive_ledgers_Protocol", "\"ScopeKind\" = 'tenant' AND \"Namespace\" = 'management_unit' AND \"Operation\" = 'archive_field' AND \"ContractVersion\" = 1 AND \"CanonicalizationVersion\" = 1");
                    table.CheckConstraint("CK_management_unit_archive_ledgers_State", "(\"State\" = 'in_progress' AND \"ResultVersion\" IS NULL AND \"ResultRevision\" IS NULL AND \"CompletedAtUtc\" IS NULL) OR (\"State\" IN ('succeeded', 'response_expired') AND \"ResultVersion\" IS NOT NULL AND \"ResultRevision\" >= 2 AND \"CompletedAtUtc\" IS NOT NULL) OR (\"State\" = 'failed_terminal' AND \"ResultVersion\" IS NULL AND \"ResultRevision\" IS NULL AND \"CompletedAtUtc\" IS NOT NULL)");
                    table.CheckConstraint("CK_management_unit_archive_ledgers_Times", "(\"CompletedAtUtc\" IS NULL OR \"CompletedAtUtc\" >= \"StartedAtUtc\")");
                    table.ForeignKey(
                        name: "FK_management_unit_archive_ledgers_management_units_Management~",
                        columns: x => new { x.ManagementUnitId, x.OrganizationId },
                        principalSchema: "productive_core",
                        principalTable: "management_units",
                        principalColumns: new[] { "Id", "OrganizationId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "management_unit_archive_key_aliases",
                schema: "productive_core",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LedgerId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    ScopeKind = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    Namespace = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Operation = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    KeyVersion = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    KeyDigest = table.Column<byte[]>(type: "bytea", maxLength: 32, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_management_unit_archive_key_aliases", x => x.Id);
                    table.CheckConstraint("CK_management_unit_archive_key_aliases_DigestLength", "octet_length(\"KeyDigest\") = 32");
                    table.CheckConstraint("CK_management_unit_archive_key_aliases_Protocol", "\"ScopeKind\" = 'tenant' AND \"Namespace\" = 'management_unit' AND \"Operation\" = 'archive_field'");
                    table.ForeignKey(
                        name: "FK_management_unit_archive_key_aliases_management_unit_archive~",
                        columns: x => new { x.LedgerId, x.OrganizationId },
                        principalSchema: "productive_core",
                        principalTable: "management_unit_archive_ledgers",
                        principalColumns: new[] { "Id", "OrganizationId" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_management_unit_archive_key_aliases_LedgerId_KeyVersion",
                schema: "productive_core",
                table: "management_unit_archive_key_aliases",
                columns: new[] { "LedgerId", "KeyVersion" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_management_unit_archive_key_aliases_LedgerId_OrganizationId",
                schema: "productive_core",
                table: "management_unit_archive_key_aliases",
                columns: new[] { "LedgerId", "OrganizationId" });

            migrationBuilder.CreateIndex(
                name: "IX_management_unit_archive_key_aliases_OrganizationId_ScopeKin~",
                schema: "productive_core",
                table: "management_unit_archive_key_aliases",
                columns: new[] { "OrganizationId", "ScopeKind", "Namespace", "Operation", "KeyVersion", "KeyDigest" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_management_unit_archive_ledgers_ManagementUnitId_Organizati~",
                schema: "productive_core",
                table: "management_unit_archive_ledgers",
                columns: new[] { "ManagementUnitId", "OrganizationId" });

            migrationBuilder.CreateIndex(
                name: "IX_management_unit_archive_ledgers_OrganizationId_ActorUserId_~",
                schema: "productive_core",
                table: "management_unit_archive_ledgers",
                columns: new[] { "OrganizationId", "ActorUserId", "StartedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_management_unit_archive_ledgers_OrganizationId_ManagementUn~",
                schema: "productive_core",
                table: "management_unit_archive_ledgers",
                columns: new[] { "OrganizationId", "ManagementUnitId", "ExpectedVersion" });

            migrationBuilder.CreateIndex(
                name: "IX_management_unit_archive_ledgers_OrganizationId_State_LeaseU~",
                schema: "productive_core",
                table: "management_unit_archive_ledgers",
                columns: new[] { "OrganizationId", "State", "LeaseUntilUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "management_unit_archive_key_aliases",
                schema: "productive_core");

            migrationBuilder.DropTable(
                name: "management_unit_archive_ledgers",
                schema: "productive_core");
        }
    }
}
