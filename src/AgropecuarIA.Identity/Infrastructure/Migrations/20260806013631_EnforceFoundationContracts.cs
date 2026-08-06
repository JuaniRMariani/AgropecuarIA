using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1861

namespace AgropecuarIA.Identity.Infrastructure.Migrations;

/// <inheritdoc />
public partial class EnforceFoundationContracts : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<long>(
            name: "Version",
            schema: "identity",
            table: "users",
            type: "bigint",
            nullable: false,
            defaultValue: 0L);

        migrationBuilder.AddColumn<string>(
            name: "AggregateType",
            schema: "identity",
            table: "outbox_messages",
            type: "character varying(128)",
            maxLength: 128,
            nullable: true);

        migrationBuilder.AddColumn<Guid>(
            name: "ActorId",
            schema: "identity",
            table: "outbox_messages",
            type: "uuid",
            nullable: true);

        migrationBuilder.AddColumn<long>(
            name: "AggregateVersion",
            schema: "identity",
            table: "outbox_messages",
            type: "bigint",
            nullable: true);

        migrationBuilder.AddColumn<Guid>(
            name: "CausationId",
            schema: "identity",
            table: "outbox_messages",
            type: "uuid",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "CorrelationId",
            schema: "identity",
            table: "outbox_messages",
            type: "character varying(128)",
            maxLength: 128,
            nullable: true);

        migrationBuilder.AddColumn<DateTimeOffset>(
            name: "EffectiveAtUtc",
            schema: "identity",
            table: "outbox_messages",
            type: "timestamp with time zone",
            nullable: true);

        migrationBuilder.AddColumn<DateTimeOffset>(
            name: "RecordedAtUtc",
            schema: "identity",
            table: "outbox_messages",
            type: "timestamp with time zone",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "SchemaVersion",
            schema: "identity",
            table: "outbox_messages",
            type: "character varying(32)",
            maxLength: 32,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "ScopeKind",
            schema: "identity",
            table: "outbox_messages",
            type: "character varying(16)",
            maxLength: 16,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "Source",
            schema: "identity",
            table: "outbox_messages",
            type: "character varying(80)",
            maxLength: 80,
            nullable: true);

        migrationBuilder.AddColumn<Guid>(
            name: "TenantId",
            schema: "identity",
            table: "outbox_messages",
            type: "uuid",
            nullable: true);

        migrationBuilder.Sql(
            """
            WITH sequenced AS (
                SELECT "EventId",
                       row_number() OVER (
                           PARTITION BY "AggregateId"
                           ORDER BY "OccurredAtUtc", "EventId") AS aggregate_version
                FROM identity.outbox_messages
            )
            UPDATE identity.outbox_messages AS message
            SET "SchemaVersion" = CASE
                    WHEN message."Version" > 0 THEN message."Version"::text || '.0.0'
                    ELSE '1.0.0'
                END,
                "Source" = 'identity-tenancy',
                "ScopeKind" = 'platform',
                "EffectiveAtUtc" = message."OccurredAtUtc",
                "RecordedAtUtc" = message."OccurredAtUtc",
                "ActorId" = message."AggregateId",
                "CorrelationId" = 'legacy-' || message."EventId"::text,
                "AggregateType" = 'PlatformUser',
                "AggregateVersion" = sequenced.aggregate_version
            FROM sequenced
            WHERE message."EventId" = sequenced."EventId";

            UPDATE identity.users AS platform_user
            SET "Version" = latest.aggregate_version
            FROM (
                SELECT "AggregateId", max("AggregateVersion") AS aggregate_version
                FROM identity.outbox_messages
                GROUP BY "AggregateId"
            ) AS latest
            WHERE platform_user."Id" = latest."AggregateId";
            """);

        migrationBuilder.CreateIndex(
                name: "IX_outbox_messages_Source_ScopeKind_TenantId_AggregateType_Agg~",
                schema: "identity",
                table: "outbox_messages",
                columns: new[] { "Source", "ScopeKind", "TenantId", "AggregateType", "AggregateId", "AggregateVersion" },
                unique: true,
                filter: "\"Source\" IS NOT NULL AND \"ScopeKind\" IS NOT NULL AND " +
                    "\"AggregateType\" IS NOT NULL AND \"AggregateVersion\" IS NOT NULL")
            .Annotation("Npgsql:NullsDistinct", false);

        migrationBuilder.AddCheckConstraint(
            name: "CK_outbox_messages_Scope",
            schema: "identity",
            table: "outbox_messages",
            sql: "(\"ScopeKind\" = 'platform' AND \"TenantId\" IS NULL) OR (\"ScopeKind\" = 'tenant' AND \"TenantId\" IS NOT NULL)");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_outbox_messages_Source_ScopeKind_TenantId_AggregateType_Agg~",
            schema: "identity",
            table: "outbox_messages");

        migrationBuilder.DropCheckConstraint(
            name: "CK_outbox_messages_Scope",
            schema: "identity",
            table: "outbox_messages");

        migrationBuilder.DropColumn(name: "Version", schema: "identity", table: "users");
        migrationBuilder.DropColumn(name: "ActorId", schema: "identity", table: "outbox_messages");
        migrationBuilder.DropColumn(name: "AggregateType", schema: "identity", table: "outbox_messages");
        migrationBuilder.DropColumn(name: "AggregateVersion", schema: "identity", table: "outbox_messages");
        migrationBuilder.DropColumn(name: "CausationId", schema: "identity", table: "outbox_messages");
        migrationBuilder.DropColumn(name: "CorrelationId", schema: "identity", table: "outbox_messages");
        migrationBuilder.DropColumn(name: "EffectiveAtUtc", schema: "identity", table: "outbox_messages");
        migrationBuilder.DropColumn(name: "RecordedAtUtc", schema: "identity", table: "outbox_messages");
        migrationBuilder.DropColumn(name: "SchemaVersion", schema: "identity", table: "outbox_messages");
        migrationBuilder.DropColumn(name: "ScopeKind", schema: "identity", table: "outbox_messages");
        migrationBuilder.DropColumn(name: "Source", schema: "identity", table: "outbox_messages");
        migrationBuilder.DropColumn(name: "TenantId", schema: "identity", table: "outbox_messages");

    }
}

#pragma warning restore CA1861
