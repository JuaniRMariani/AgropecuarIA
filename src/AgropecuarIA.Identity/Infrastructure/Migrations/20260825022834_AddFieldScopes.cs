using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1861 // EF Core scaffolds transient arrays for migration metadata.

namespace AgropecuarIA.Identity.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddFieldScopes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_memberships_Role",
                schema: "identity",
                table: "memberships");

            migrationBuilder.CreateTable(
                name: "organization_field_scopes",
                schema: "identity",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    MembershipId = table.Column<Guid>(type: "uuid", nullable: false),
                    FieldId = table.Column<Guid>(type: "uuid", nullable: false),
                    GrantedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    GrantedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_organization_field_scopes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_organization_field_scopes_memberships_MembershipId",
                        column: x => x.MembershipId,
                        principalSchema: "identity",
                        principalTable: "memberships",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.AddCheckConstraint(
                name: "CK_memberships_Role",
                schema: "identity",
                table: "memberships",
                sql: "\"Role\" IN ('owner', 'admin', 'agronomist', 'operator', 'accountant', 'viewer')");

            migrationBuilder.CreateIndex(
                name: "IX_organization_field_scopes_MembershipId",
                schema: "identity",
                table: "organization_field_scopes",
                column: "MembershipId");

            migrationBuilder.CreateIndex(
                name: "IX_organization_field_scopes_OrganizationId_MembershipId_Field~",
                schema: "identity",
                table: "organization_field_scopes",
                columns: new[] { "OrganizationId", "MembershipId", "FieldId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "organization_field_scopes",
                schema: "identity");

            migrationBuilder.DropCheckConstraint(
                name: "CK_memberships_Role",
                schema: "identity",
                table: "memberships");

            migrationBuilder.AddCheckConstraint(
                name: "CK_memberships_Role",
                schema: "identity",
                table: "memberships",
                sql: "\"Role\" = 'owner'");
        }
    }
}
