using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgropecuarIA.Identity.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPurposeBoundStrongAuthentication : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "StrongAuthenticatedAtUtc",
                schema: "identity",
                table: "sessions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StrongAuthenticationPurpose",
                schema: "identity",
                table: "sessions",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "step_up_attempts",
                schema: "identity",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    InitiatingSessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Purpose = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    StartedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ExpiresAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ConsumedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Version = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_step_up_attempts", x => x.Id);
                    table.CheckConstraint("CK_step_up_attempts_Expiry", "\"ExpiresAtUtc\" > \"StartedAtUtc\"");
                    table.CheckConstraint("CK_step_up_attempts_Purpose", "\"Purpose\" = 'manage_authentication_methods'");
                    table.ForeignKey(
                        name: "FK_step_up_attempts_sessions_InitiatingSessionId",
                        column: x => x.InitiatingSessionId,
                        principalSchema: "identity",
                        principalTable: "sessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_step_up_attempts_users_UserId",
                        column: x => x.UserId,
                        principalSchema: "identity",
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.AddCheckConstraint(
                name: "CK_sessions_StrongAuthentication",
                schema: "identity",
                table: "sessions",
                sql: "(\"StrongAuthenticatedAtUtc\" IS NULL AND \"StrongAuthenticationPurpose\" IS NULL) OR (\"StrongAuthenticatedAtUtc\" IS NOT NULL AND \"StrongAuthenticationPurpose\" IS NOT NULL AND \"StrongAuthenticationPurpose\" = 'manage_authentication_methods')");

            migrationBuilder.CreateIndex(
                name: "IX_step_up_attempts_InitiatingSessionId",
                schema: "identity",
                table: "step_up_attempts",
                column: "InitiatingSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_step_up_attempts_UserId_ExpiresAtUtc",
                schema: "identity",
                table: "step_up_attempts",
                columns: ["UserId", "ExpiresAtUtc"]);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "step_up_attempts",
                schema: "identity");

            migrationBuilder.DropCheckConstraint(
                name: "CK_sessions_StrongAuthentication",
                schema: "identity",
                table: "sessions");

            migrationBuilder.DropColumn(
                name: "StrongAuthenticatedAtUtc",
                schema: "identity",
                table: "sessions");

            migrationBuilder.DropColumn(
                name: "StrongAuthenticationPurpose",
                schema: "identity",
                table: "sessions");
        }
    }
}
