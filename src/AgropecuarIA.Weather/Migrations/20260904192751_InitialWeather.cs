using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1861 // EF Core scaffolds transient arrays for migration metadata.

namespace AgropecuarIA.Weather.Migrations
{
    /// <inheritdoc />
    public partial class InitialWeather : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "weather");

            migrationBuilder.CreateTable(
                name: "activity_rules",
                schema: "weather",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    FieldId = table.Column<Guid>(type: "uuid", nullable: true),
                    ActivityType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    RuleName = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    MaxWindSpeedKmh = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                    MinTemperatureCelsius = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                    MaxTemperatureCelsius = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                    MaxPrecipitationProbability = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                    MaxPrecipitationMm = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                    MinRelativeHumidity = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                    MaxRelativeHumidity = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_activity_rules", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "forecast_snapshots",
                schema: "weather",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CentroidLatitude = table.Column<double>(type: "double precision", nullable: false),
                    CentroidLongitude = table.Column<double>(type: "double precision", nullable: false),
                    Provider = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ModelName = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    IssuedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ValidUntilUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    HourlyVariablesJson = table.Column<string>(type: "jsonb", nullable: false),
                    DailyVariablesJson = table.Column<string>(type: "jsonb", nullable: false),
                    SnapshotHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_forecast_snapshots", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "observed_rains",
                schema: "weather",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    FieldId = table.Column<Guid>(type: "uuid", nullable: false),
                    ObservedDateUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    AmountMillimeters = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    Method = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Notes = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    RecordedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    RecordedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    RectifiedFromId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_observed_rains", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "weather_alerts",
                schema: "weather",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Identifier = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Sender = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    SentUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    EventName = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Severity = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Certainty = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Headline = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    Description = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                    Instruction = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    AreaDescription = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    PolygonCoordinatesJson = table.Column<string>(type: "jsonb", nullable: false),
                    MinLatitude = table.Column<double>(type: "double precision", nullable: false),
                    MaxLatitude = table.Column<double>(type: "double precision", nullable: false),
                    MinLongitude = table.Column<double>(type: "double precision", nullable: false),
                    MaxLongitude = table.Column<double>(type: "double precision", nullable: false),
                    EffectiveUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ExpiresUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_weather_alerts", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_activity_rules_OrganizationId_FieldId_ActivityType",
                schema: "weather",
                table: "activity_rules",
                columns: new[] { "OrganizationId", "FieldId", "ActivityType" });

            migrationBuilder.CreateIndex(
                name: "IX_forecast_snapshots_CentroidLatitude_CentroidLongitude_Valid~",
                schema: "weather",
                table: "forecast_snapshots",
                columns: new[] { "CentroidLatitude", "CentroidLongitude", "ValidUntilUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_forecast_snapshots_SnapshotHash",
                schema: "weather",
                table: "forecast_snapshots",
                column: "SnapshotHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_observed_rains_OrganizationId_FieldId_ObservedDateUtc",
                schema: "weather",
                table: "observed_rains",
                columns: new[] { "OrganizationId", "FieldId", "ObservedDateUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_weather_alerts_Identifier",
                schema: "weather",
                table: "weather_alerts",
                column: "Identifier",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_weather_alerts_MinLatitude_MaxLatitude_MinLongitude_MaxLong~",
                schema: "weather",
                table: "weather_alerts",
                columns: new[] { "MinLatitude", "MaxLatitude", "MinLongitude", "MaxLongitude" });

            migrationBuilder.CreateIndex(
                name: "IX_weather_alerts_Status_EffectiveUtc_ExpiresUtc",
                schema: "weather",
                table: "weather_alerts",
                columns: new[] { "Status", "EffectiveUtc", "ExpiresUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "activity_rules",
                schema: "weather");

            migrationBuilder.DropTable(
                name: "forecast_snapshots",
                schema: "weather");

            migrationBuilder.DropTable(
                name: "observed_rains",
                schema: "weather");

            migrationBuilder.DropTable(
                name: "weather_alerts",
                schema: "weather");
        }
    }
}
