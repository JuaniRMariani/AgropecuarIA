using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1861 // EF Core scaffolds transient arrays for migration metadata.

namespace AgropecuarIA.ProductiveCore.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitializeProductiveCore : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DO $roles$
                DECLARE
                    role_name text;
                BEGIN
                    FOREACH role_name IN ARRAY ARRAY[
                        'agro_productive_owner',
                        'agro_productive_migrator',
                        'agro_productive_app',
                        'agro_productive_job'
                    ]
                    LOOP
                        IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = role_name) THEN
                            EXECUTE format(
                                'CREATE ROLE %I NOLOGIN NOINHERIT NOSUPERUSER NOCREATEDB NOCREATEROLE NOREPLICATION NOBYPASSRLS',
                                role_name);
                        ELSIF EXISTS (
                            SELECT 1
                            FROM pg_roles
                            WHERE rolname = role_name
                              AND (rolcanlogin OR rolinherit OR rolsuper OR rolcreatedb
                                   OR rolcreaterole OR rolreplication OR rolbypassrls)
                        ) THEN
                            RAISE EXCEPTION
                                'Productive Core privilege role % has unsafe attributes', role_name;
                        END IF;
                    END LOOP;
                END
                $roles$;

                GRANT agro_productive_owner TO agro_productive_migrator
                    WITH INHERIT FALSE, SET TRUE;
                """);

            migrationBuilder.EnsureSchema(
                name: "productive_core");

            migrationBuilder.CreateTable(
                name: "journal_entries",
                schema: "productive_core",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    ActorUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    SessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Action = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Outcome = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    CorrelationId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    OccurredAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_journal_entries", x => x.Id);
                    table.CheckConstraint("CK_journal_entries_Action", "\"Action\" = 'management_unit_created'");
                    table.CheckConstraint("CK_journal_entries_Outcome", "\"Outcome\" = 'succeeded'");
                });

            migrationBuilder.CreateTable(
                name: "management_units",
                schema: "productive_core",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    UnitType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    SpatialStatus = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Version = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_management_units", x => x.Id);
                    table.UniqueConstraint("AK_management_units_Id_OrganizationId", x => new { x.Id, x.OrganizationId });
                    table.CheckConstraint("CK_management_units_DisplayName", "char_length(\"DisplayName\") BETWEEN 2 AND 120");
                    table.CheckConstraint("CK_management_units_SpatialStatus", "\"SpatialStatus\" = 'not_configured'");
                    table.CheckConstraint("CK_management_units_Status", "\"Status\" = 'draft'");
                    table.CheckConstraint("CK_management_units_UnitType", "\"UnitType\" = 'field'");
                });

            migrationBuilder.CreateTable(
                name: "outbox_messages",
                schema: "productive_core",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    EventType = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    SchemaVersion = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    Source = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Scope = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    AggregateType = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    AggregateId = table.Column<Guid>(type: "uuid", nullable: false),
                    AggregateVersion = table.Column<long>(type: "bigint", nullable: false),
                    CorrelationId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    OccurredAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    AvailableAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    PayloadJson = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_outbox_messages", x => x.Id);
                    table.CheckConstraint("CK_productive_outbox_messages_AggregateType", "\"AggregateType\" = 'ManagementUnit'");
                    table.CheckConstraint("CK_productive_outbox_messages_EventType", "\"EventType\" = 'ManagementUnitCreated'");
                    table.CheckConstraint("CK_productive_outbox_messages_SourceScope", "\"Source\" = 'productive-core' AND \"Scope\" = 'tenant'");
                    table.CheckConstraint("CK_productive_outbox_messages_Versions", "\"SchemaVersion\" = '1.0.0' AND \"AggregateVersion\" = 1");
                });

            migrationBuilder.CreateTable(
                name: "management_unit_creation_ledgers",
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
                    RequestFingerprint = table.Column<byte[]>(type: "bytea", maxLength: 32, nullable: false),
                    State = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    ManagementUnitId = table.Column<Guid>(type: "uuid", nullable: true),
                    ResultVersion = table.Column<Guid>(type: "uuid", nullable: true),
                    LeaseOwner = table.Column<Guid>(type: "uuid", nullable: false),
                    FenceToken = table.Column<long>(type: "bigint", nullable: false),
                    LeaseUntilUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    StartedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CompletedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Version = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_management_unit_creation_ledgers", x => x.Id);
                    table.UniqueConstraint("AK_management_unit_creation_ledgers_Id_OrganizationId", x => new { x.Id, x.OrganizationId });
                    table.CheckConstraint("CK_management_unit_creation_ledgers_DigestLength", "octet_length(\"RequestFingerprint\") = 32");
                    table.CheckConstraint("CK_management_unit_creation_ledgers_Fence", "\"FenceToken\" > 0 AND \"LeaseUntilUtc\" > \"StartedAtUtc\"");
                    table.CheckConstraint("CK_management_unit_creation_ledgers_Protocol", "\"ScopeKind\" = 'tenant' AND \"Namespace\" = 'management_unit' AND \"Operation\" = 'create_field' AND \"ContractVersion\" = 1 AND \"CanonicalizationVersion\" = 1");
                    table.CheckConstraint("CK_management_unit_creation_ledgers_State", "(\"State\" = 'in_progress' AND \"ManagementUnitId\" IS NULL AND \"ResultVersion\" IS NULL AND \"CompletedAtUtc\" IS NULL) OR (\"State\" IN ('succeeded', 'response_expired') AND \"ManagementUnitId\" IS NOT NULL AND \"ResultVersion\" IS NOT NULL AND \"CompletedAtUtc\" IS NOT NULL) OR (\"State\" = 'failed_terminal' AND \"ManagementUnitId\" IS NULL AND \"ResultVersion\" IS NULL AND \"CompletedAtUtc\" IS NOT NULL)");
                    table.CheckConstraint("CK_management_unit_creation_ledgers_Times", "(\"CompletedAtUtc\" IS NULL OR \"CompletedAtUtc\" >= \"StartedAtUtc\")");
                    table.ForeignKey(
                        name: "FK_management_unit_creation_ledgers_management_units_Managemen~",
                        columns: x => new { x.ManagementUnitId, x.OrganizationId },
                        principalSchema: "productive_core",
                        principalTable: "management_units",
                        principalColumns: new[] { "Id", "OrganizationId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "management_unit_creation_key_aliases",
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
                    table.PrimaryKey("PK_management_unit_creation_key_aliases", x => x.Id);
                    table.CheckConstraint("CK_management_unit_creation_key_aliases_DigestLength", "octet_length(\"KeyDigest\") = 32");
                    table.CheckConstraint("CK_management_unit_creation_key_aliases_Protocol", "\"ScopeKind\" = 'tenant' AND \"Namespace\" = 'management_unit' AND \"Operation\" = 'create_field'");
                    table.ForeignKey(
                        name: "FK_management_unit_creation_key_aliases_management_unit_creati~",
                        columns: x => new { x.LedgerId, x.OrganizationId },
                        principalSchema: "productive_core",
                        principalTable: "management_unit_creation_ledgers",
                        principalColumns: new[] { "Id", "OrganizationId" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_journal_entries_OrganizationId_OccurredAtUtc",
                schema: "productive_core",
                table: "journal_entries",
                columns: new[] { "OrganizationId", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_management_unit_creation_key_aliases_LedgerId_KeyVersion",
                schema: "productive_core",
                table: "management_unit_creation_key_aliases",
                columns: new[] { "LedgerId", "KeyVersion" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_management_unit_creation_key_aliases_LedgerId_OrganizationId",
                schema: "productive_core",
                table: "management_unit_creation_key_aliases",
                columns: new[] { "LedgerId", "OrganizationId" });

            migrationBuilder.CreateIndex(
                name: "IX_management_unit_creation_key_aliases_OrganizationId_ScopeKi~",
                schema: "productive_core",
                table: "management_unit_creation_key_aliases",
                columns: new[] { "OrganizationId", "ScopeKind", "Namespace", "Operation", "KeyVersion", "KeyDigest" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_management_unit_creation_ledgers_ManagementUnitId_Organizat~",
                schema: "productive_core",
                table: "management_unit_creation_ledgers",
                columns: new[] { "ManagementUnitId", "OrganizationId" });

            migrationBuilder.CreateIndex(
                name: "IX_management_unit_creation_ledgers_OrganizationId_ActorUserId~",
                schema: "productive_core",
                table: "management_unit_creation_ledgers",
                columns: new[] { "OrganizationId", "ActorUserId", "StartedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_management_unit_creation_ledgers_OrganizationId_State_Lease~",
                schema: "productive_core",
                table: "management_unit_creation_ledgers",
                columns: new[] { "OrganizationId", "State", "LeaseUntilUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_management_units_OrganizationId_CreatedAtUtc_Id",
                schema: "productive_core",
                table: "management_units",
                columns: new[] { "OrganizationId", "CreatedAtUtc", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_outbox_messages_AvailableAtUtc_OccurredAtUtc",
                schema: "productive_core",
                table: "outbox_messages",
                columns: new[] { "AvailableAtUtc", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_outbox_messages_OrganizationId_AggregateType_AggregateId_Ag~",
                schema: "productive_core",
                table: "outbox_messages",
                columns: new[] { "OrganizationId", "AggregateType", "AggregateId", "AggregateVersion" },
                unique: true);

            migrationBuilder.Sql(
                """
                ALTER SCHEMA productive_core OWNER TO agro_productive_owner;
                REVOKE ALL ON SCHEMA productive_core FROM PUBLIC;
                GRANT USAGE ON SCHEMA productive_core
                    TO agro_productive_owner, agro_productive_migrator,
                       agro_productive_app, agro_productive_job;

                ALTER TABLE productive_core.management_units
                    OWNER TO agro_productive_owner;
                ALTER TABLE productive_core.management_unit_creation_ledgers
                    OWNER TO agro_productive_owner;
                ALTER TABLE productive_core.management_unit_creation_key_aliases
                    OWNER TO agro_productive_owner;
                ALTER TABLE productive_core.journal_entries
                    OWNER TO agro_productive_owner;
                ALTER TABLE productive_core.outbox_messages
                    OWNER TO agro_productive_owner;
                ALTER TABLE IF EXISTS productive_core."__EFMigrationsHistory"
                    OWNER TO agro_productive_owner;

                ALTER TABLE productive_core.management_units ENABLE ROW LEVEL SECURITY;
                ALTER TABLE productive_core.management_units FORCE ROW LEVEL SECURITY;
                ALTER TABLE productive_core.management_unit_creation_ledgers
                    ENABLE ROW LEVEL SECURITY;
                ALTER TABLE productive_core.management_unit_creation_ledgers
                    FORCE ROW LEVEL SECURITY;
                ALTER TABLE productive_core.management_unit_creation_key_aliases
                    ENABLE ROW LEVEL SECURITY;
                ALTER TABLE productive_core.management_unit_creation_key_aliases
                    FORCE ROW LEVEL SECURITY;
                ALTER TABLE productive_core.journal_entries ENABLE ROW LEVEL SECURITY;
                ALTER TABLE productive_core.journal_entries FORCE ROW LEVEL SECURITY;
                ALTER TABLE productive_core.outbox_messages ENABLE ROW LEVEL SECURITY;
                ALTER TABLE productive_core.outbox_messages FORCE ROW LEVEL SECURITY;

                REVOKE ALL ON ALL TABLES IN SCHEMA productive_core FROM PUBLIC;
                REVOKE ALL ON ALL TABLES IN SCHEMA productive_core
                    FROM agro_productive_app, agro_productive_job;

                GRANT SELECT, INSERT
                    ON TABLE productive_core.management_units TO agro_productive_app;
                GRANT SELECT, INSERT, UPDATE
                    ON TABLE productive_core.management_unit_creation_ledgers
                    TO agro_productive_app;
                GRANT SELECT, INSERT
                    ON TABLE productive_core.management_unit_creation_key_aliases
                    TO agro_productive_app;
                GRANT INSERT ON TABLE productive_core.journal_entries
                    TO agro_productive_app;
                GRANT INSERT ON TABLE productive_core.outbox_messages
                    TO agro_productive_app;

                CREATE POLICY management_units_app_read
                    ON productive_core.management_units
                    FOR SELECT TO agro_productive_app
                    USING (
                        nullif(current_setting('app.current_scope_kind', true), '') = 'tenant'
                        AND "OrganizationId"::text = nullif(
                            current_setting('app.current_organization_id', true), '')
                        AND identity.authorize_productive_owner()::text = nullif(
                            current_setting('app.current_authorization_version', true), '')
                    );
                CREATE POLICY management_units_app_insert
                    ON productive_core.management_units
                    FOR INSERT TO agro_productive_app
                    WITH CHECK (
                        nullif(current_setting('app.current_scope_kind', true), '') = 'tenant'
                        AND "OrganizationId"::text = nullif(
                            current_setting('app.current_organization_id', true), '')
                        AND identity.authorize_productive_owner()::text = nullif(
                            current_setting('app.current_authorization_version', true), '')
                        AND "UnitType" = 'field'
                        AND "Status" = 'draft'
                        AND "SpatialStatus" = 'not_configured'
                    );

                CREATE POLICY management_unit_creation_ledgers_app_access
                    ON productive_core.management_unit_creation_ledgers
                    FOR ALL TO agro_productive_app
                    USING (
                        nullif(current_setting('app.current_scope_kind', true), '') = 'tenant'
                        AND "OrganizationId"::text = nullif(
                            current_setting('app.current_organization_id', true), '')
                        AND "ActorUserId"::text = nullif(
                            current_setting('app.current_actor_id', true), '')
                        AND "SessionId"::text = nullif(
                            current_setting('app.current_session_id', true), '')
                        AND "AuthorizationVersion"::text = nullif(
                            current_setting('app.current_authorization_version', true), '')
                        AND identity.authorize_productive_owner() = "AuthorizationVersion"
                    )
                    WITH CHECK (
                        nullif(current_setting('app.current_scope_kind', true), '') = 'tenant'
                        AND "OrganizationId"::text = nullif(
                            current_setting('app.current_organization_id', true), '')
                        AND "ActorUserId"::text = nullif(
                            current_setting('app.current_actor_id', true), '')
                        AND "SessionId"::text = nullif(
                            current_setting('app.current_session_id', true), '')
                        AND "AuthorizationVersion"::text = nullif(
                            current_setting('app.current_authorization_version', true), '')
                        AND identity.authorize_productive_owner() = "AuthorizationVersion"
                    );

                CREATE POLICY management_unit_creation_aliases_app_access
                    ON productive_core.management_unit_creation_key_aliases
                    FOR ALL TO agro_productive_app
                    USING (
                        "OrganizationId"::text = nullif(
                            current_setting('app.current_organization_id', true), '')
                        AND identity.authorize_productive_owner()::text = nullif(
                            current_setting('app.current_authorization_version', true), '')
                        AND EXISTS (
                            SELECT 1
                            FROM productive_core.management_unit_creation_ledgers AS ledger
                            WHERE ledger."Id" = management_unit_creation_key_aliases."LedgerId"
                              AND ledger."OrganizationId" =
                                  management_unit_creation_key_aliases."OrganizationId")
                    )
                    WITH CHECK (
                        "OrganizationId"::text = nullif(
                            current_setting('app.current_organization_id', true), '')
                        AND identity.authorize_productive_owner()::text = nullif(
                            current_setting('app.current_authorization_version', true), '')
                        AND EXISTS (
                            SELECT 1
                            FROM productive_core.management_unit_creation_ledgers AS ledger
                            WHERE ledger."Id" = management_unit_creation_key_aliases."LedgerId"
                              AND ledger."OrganizationId" =
                                  management_unit_creation_key_aliases."OrganizationId")
                    );

                CREATE POLICY journal_entries_app_insert
                    ON productive_core.journal_entries
                    FOR INSERT TO agro_productive_app
                    WITH CHECK (
                        "OrganizationId"::text = nullif(
                            current_setting('app.current_organization_id', true), '')
                        AND "ActorUserId"::text = nullif(
                            current_setting('app.current_actor_id', true), '')
                        AND "SessionId"::text = nullif(
                            current_setting('app.current_session_id', true), '')
                        AND identity.authorize_productive_owner()::text = nullif(
                            current_setting('app.current_authorization_version', true), '')
                        AND "Action" = 'management_unit_created'
                        AND "Outcome" = 'succeeded'
                    );

                CREATE POLICY outbox_messages_app_insert
                    ON productive_core.outbox_messages
                    FOR INSERT TO agro_productive_app
                    WITH CHECK (
                        "OrganizationId"::text = nullif(
                            current_setting('app.current_organization_id', true), '')
                        AND identity.authorize_productive_owner()::text = nullif(
                            current_setting('app.current_authorization_version', true), '')
                        AND "EventType" = 'ManagementUnitCreated'
                        AND "Source" = 'productive-core'
                        AND "Scope" = 'tenant'
                        AND "AggregateType" = 'ManagementUnit'
                        AND "SchemaVersion" = '1.0.0'
                        AND "AggregateVersion" = 1
                    );

                CREATE POLICY management_unit_creation_ledgers_owner_key_coverage
                    ON productive_core.management_unit_creation_ledgers
                    FOR SELECT TO agro_productive_owner USING (true);
                CREATE POLICY management_unit_creation_aliases_owner_key_coverage
                    ON productive_core.management_unit_creation_key_aliases
                    FOR SELECT TO agro_productive_owner USING (true);

                CREATE FUNCTION productive_core.management_unit_creation_retained_key_covered(
                    retained_versions text[])
                RETURNS boolean
                LANGUAGE sql
                STABLE
                SECURITY DEFINER
                SET search_path = pg_catalog
                AS $retained_key_coverage$
                    SELECT retained_versions IS NOT NULL
                       AND cardinality(retained_versions) > 0
                       AND NOT EXISTS (
                            SELECT 1
                            FROM productive_core.management_unit_creation_ledgers AS ledger
                            WHERE NOT EXISTS (
                                SELECT 1
                                FROM productive_core.management_unit_creation_key_aliases AS alias
                                WHERE alias."LedgerId" = ledger."Id"
                                  AND alias."OrganizationId" = ledger."OrganizationId"
                                  AND alias."KeyVersion" = ANY(retained_versions)))
                $retained_key_coverage$;
                ALTER FUNCTION
                    productive_core.management_unit_creation_retained_key_covered(text[])
                    OWNER TO agro_productive_owner;
                REVOKE ALL ON FUNCTION
                    productive_core.management_unit_creation_retained_key_covered(text[])
                    FROM PUBLIC;
                GRANT EXECUTE ON FUNCTION
                    productive_core.management_unit_creation_retained_key_covered(text[])
                    TO agro_productive_app;

                CREATE FUNCTION productive_core.prevent_journal_entry_mutation()
                RETURNS trigger
                LANGUAGE plpgsql
                SET search_path = pg_catalog
                AS $prevent_journal_mutation$
                BEGIN
                    RAISE EXCEPTION 'Productive journal entries are append-only';
                END
                $prevent_journal_mutation$;
                ALTER FUNCTION productive_core.prevent_journal_entry_mutation()
                    OWNER TO agro_productive_owner;
                REVOKE ALL ON FUNCTION
                    productive_core.prevent_journal_entry_mutation() FROM PUBLIC;
                CREATE TRIGGER journal_entries_append_only
                    BEFORE UPDATE OR DELETE ON productive_core.journal_entries
                    FOR EACH ROW EXECUTE FUNCTION
                        productive_core.prevent_journal_entry_mutation();
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DO $ephemeral_only$
                BEGIN
                    IF current_database() !~ '^agro_(identity|productive)_[0-9a-f]{32}$' THEN
                        RAISE EXCEPTION
                            'The Productive Core down migration is allowed only on an ephemeral test database';
                    END IF;
                END
                $ephemeral_only$;

                DROP TRIGGER IF EXISTS journal_entries_append_only
                    ON productive_core.journal_entries;
                DROP FUNCTION IF EXISTS
                    productive_core.prevent_journal_entry_mutation();
                DROP FUNCTION IF EXISTS
                    productive_core.management_unit_creation_retained_key_covered(text[]);
                """);

            migrationBuilder.DropTable(
                name: "journal_entries",
                schema: "productive_core");

            migrationBuilder.DropTable(
                name: "management_unit_creation_key_aliases",
                schema: "productive_core");

            migrationBuilder.DropTable(
                name: "outbox_messages",
                schema: "productive_core");

            migrationBuilder.DropTable(
                name: "management_unit_creation_ledgers",
                schema: "productive_core");

            migrationBuilder.DropTable(
                name: "management_units",
                schema: "productive_core");
        }
    }
}
