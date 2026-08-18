using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1861 // EF Core scaffolds transient arrays for migration metadata.

namespace AgropecuarIA.ProductiveCore.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddManagementUnitRename : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_productive_outbox_messages_EventType",
                schema: "productive_core",
                table: "outbox_messages");

            migrationBuilder.DropCheckConstraint(
                name: "CK_productive_outbox_messages_Versions",
                schema: "productive_core",
                table: "outbox_messages");

            migrationBuilder.DropCheckConstraint(
                name: "CK_journal_entries_Action",
                schema: "productive_core",
                table: "journal_entries");

            migrationBuilder.AddColumn<long>(
                name: "Revision",
                schema: "productive_core",
                table: "management_units",
                type: "bigint",
                nullable: false,
                defaultValue: 1L);

            migrationBuilder.AddCheckConstraint(
                name: "CK_management_units_Revision",
                schema: "productive_core",
                table: "management_units",
                sql: "\"Revision\" >= 1");

            migrationBuilder.CreateTable(
                name: "management_unit_rename_ledgers",
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
                    ResultDisplayName = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
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
                    table.PrimaryKey("PK_management_unit_rename_ledgers", x => x.Id);
                    table.UniqueConstraint("AK_management_unit_rename_ledgers_Id_OrganizationId", x => new { x.Id, x.OrganizationId });
                    table.CheckConstraint("CK_management_unit_rename_ledgers_DigestLength", "octet_length(\"RequestFingerprint\") = 32");
                    table.CheckConstraint("CK_management_unit_rename_ledgers_Fence", "\"FenceToken\" > 0 AND \"LeaseUntilUtc\" > \"StartedAtUtc\"");
                    table.CheckConstraint("CK_management_unit_rename_ledgers_Protocol", "\"ScopeKind\" = 'tenant' AND \"Namespace\" = 'management_unit' AND \"Operation\" = 'rename_field' AND \"ContractVersion\" = 1 AND \"CanonicalizationVersion\" = 1");
                    table.CheckConstraint("CK_management_unit_rename_ledgers_State", "(\"State\" = 'in_progress' AND \"ResultDisplayName\" IS NULL AND \"ResultVersion\" IS NULL AND \"ResultRevision\" IS NULL AND \"CompletedAtUtc\" IS NULL) OR (\"State\" IN ('succeeded', 'response_expired') AND \"ResultDisplayName\" IS NOT NULL AND \"ResultVersion\" IS NOT NULL AND \"ResultRevision\" >= 2 AND \"CompletedAtUtc\" IS NOT NULL) OR (\"State\" = 'failed_terminal' AND \"ResultDisplayName\" IS NULL AND \"ResultVersion\" IS NULL AND \"ResultRevision\" IS NULL AND \"CompletedAtUtc\" IS NOT NULL)");
                    table.CheckConstraint("CK_management_unit_rename_ledgers_Times", "(\"CompletedAtUtc\" IS NULL OR \"CompletedAtUtc\" >= \"StartedAtUtc\")");
                    table.ForeignKey(
                        name: "FK_management_unit_rename_ledgers_management_units_ManagementU~",
                        columns: x => new { x.ManagementUnitId, x.OrganizationId },
                        principalSchema: "productive_core",
                        principalTable: "management_units",
                        principalColumns: new[] { "Id", "OrganizationId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "management_unit_rename_key_aliases",
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
                    table.PrimaryKey("PK_management_unit_rename_key_aliases", x => x.Id);
                    table.CheckConstraint("CK_management_unit_rename_key_aliases_DigestLength", "octet_length(\"KeyDigest\") = 32");
                    table.CheckConstraint("CK_management_unit_rename_key_aliases_Protocol", "\"ScopeKind\" = 'tenant' AND \"Namespace\" = 'management_unit' AND \"Operation\" = 'rename_field'");
                    table.ForeignKey(
                        name: "FK_management_unit_rename_key_aliases_management_unit_rename_l~",
                        columns: x => new { x.LedgerId, x.OrganizationId },
                        principalSchema: "productive_core",
                        principalTable: "management_unit_rename_ledgers",
                        principalColumns: new[] { "Id", "OrganizationId" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.AddCheckConstraint(
                name: "CK_productive_outbox_messages_EventType",
                schema: "productive_core",
                table: "outbox_messages",
                sql: "\"EventType\" IN ('ManagementUnitCreated', 'ManagementUnitDisplayNameChanged')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_productive_outbox_messages_Versions",
                schema: "productive_core",
                table: "outbox_messages",
                sql: "\"SchemaVersion\" = '1.0.0' AND ((\"EventType\" = 'ManagementUnitCreated' AND \"AggregateVersion\" = 1) OR (\"EventType\" = 'ManagementUnitDisplayNameChanged' AND \"AggregateVersion\" >= 2))");

            migrationBuilder.AddCheckConstraint(
                name: "CK_journal_entries_Action",
                schema: "productive_core",
                table: "journal_entries",
                sql: "\"Action\" IN ('management_unit_created', 'management_unit_display_name_changed')");

            migrationBuilder.CreateIndex(
                name: "IX_management_unit_rename_key_aliases_LedgerId_KeyVersion",
                schema: "productive_core",
                table: "management_unit_rename_key_aliases",
                columns: new[] { "LedgerId", "KeyVersion" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_management_unit_rename_key_aliases_LedgerId_OrganizationId",
                schema: "productive_core",
                table: "management_unit_rename_key_aliases",
                columns: new[] { "LedgerId", "OrganizationId" });

            migrationBuilder.CreateIndex(
                name: "IX_management_unit_rename_key_aliases_OrganizationId_ScopeKind~",
                schema: "productive_core",
                table: "management_unit_rename_key_aliases",
                columns: new[] { "OrganizationId", "ScopeKind", "Namespace", "Operation", "KeyVersion", "KeyDigest" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_management_unit_rename_ledgers_ManagementUnitId_Organizatio~",
                schema: "productive_core",
                table: "management_unit_rename_ledgers",
                columns: new[] { "ManagementUnitId", "OrganizationId" });

            migrationBuilder.CreateIndex(
                name: "IX_management_unit_rename_ledgers_OrganizationId_ActorUserId_S~",
                schema: "productive_core",
                table: "management_unit_rename_ledgers",
                columns: new[] { "OrganizationId", "ActorUserId", "StartedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_management_unit_rename_ledgers_OrganizationId_ManagementUni~",
                schema: "productive_core",
                table: "management_unit_rename_ledgers",
                columns: new[] { "OrganizationId", "ManagementUnitId", "ExpectedVersion" });

            migrationBuilder.CreateIndex(
                name: "IX_management_unit_rename_ledgers_OrganizationId_State_LeaseUn~",
                schema: "productive_core",
                table: "management_unit_rename_ledgers",
                columns: new[] { "OrganizationId", "State", "LeaseUntilUtc" });

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
                            RAISE EXCEPTION 'Required Productive Core privilege role % is missing', role_name;
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

                    IF pg_has_role('agro_productive_app', 'agro_productive_owner', 'MEMBER')
                       OR pg_has_role('agro_productive_job', 'agro_productive_owner', 'MEMBER') THEN
                        RAISE EXCEPTION
                            'Productive application and job roles must not belong to the owner role';
                    END IF;
                END
                $roles$;

                ALTER TABLE productive_core.management_unit_rename_ledgers
                    OWNER TO agro_productive_owner;
                ALTER TABLE productive_core.management_unit_rename_key_aliases
                    OWNER TO agro_productive_owner;

                ALTER TABLE productive_core.management_unit_rename_ledgers
                    ENABLE ROW LEVEL SECURITY;
                ALTER TABLE productive_core.management_unit_rename_ledgers
                    FORCE ROW LEVEL SECURITY;
                ALTER TABLE productive_core.management_unit_rename_key_aliases
                    ENABLE ROW LEVEL SECURITY;
                ALTER TABLE productive_core.management_unit_rename_key_aliases
                    FORCE ROW LEVEL SECURITY;

                REVOKE ALL ON TABLE productive_core.management_unit_rename_ledgers
                    FROM PUBLIC, agro_productive_app, agro_productive_job;
                REVOKE ALL ON TABLE productive_core.management_unit_rename_key_aliases
                    FROM PUBLIC, agro_productive_app, agro_productive_job;
                GRANT SELECT, INSERT
                    ON TABLE productive_core.management_unit_rename_ledgers
                    TO agro_productive_app;
                GRANT SELECT, INSERT
                    ON TABLE productive_core.management_unit_rename_key_aliases
                    TO agro_productive_app;
                GRANT UPDATE ("DisplayName", "Revision", "Version")
                    ON TABLE productive_core.management_units
                    TO agro_productive_app;

                CREATE POLICY management_units_app_update
                    ON productive_core.management_units
                    FOR UPDATE TO agro_productive_app
                    USING (
                        nullif(current_setting('app.current_scope_kind', true), '') = 'tenant'
                        AND "OrganizationId"::text = nullif(
                            current_setting('app.current_organization_id', true), '')
                        AND identity.authorize_productive_owner()::text = nullif(
                            current_setting('app.current_authorization_version', true), '')
                    )
                    WITH CHECK (
                        nullif(current_setting('app.current_scope_kind', true), '') = 'tenant'
                        AND "OrganizationId"::text = nullif(
                            current_setting('app.current_organization_id', true), '')
                        AND identity.authorize_productive_owner()::text = nullif(
                            current_setting('app.current_authorization_version', true), '')
                        AND "UnitType" = 'field'
                        AND "Status" = 'draft'
                        AND "SpatialStatus" = 'not_configured'
                        AND "Revision" >= 2
                    );

                CREATE POLICY management_unit_rename_ledgers_app_select
                    ON productive_core.management_unit_rename_ledgers
                    FOR SELECT TO agro_productive_app
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
                    );
                CREATE POLICY management_unit_rename_ledgers_app_insert
                    ON productive_core.management_unit_rename_ledgers
                    FOR INSERT TO agro_productive_app
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

                CREATE POLICY management_unit_rename_aliases_app_access
                    ON productive_core.management_unit_rename_key_aliases
                    FOR ALL TO agro_productive_app
                    USING (
                        "OrganizationId"::text = nullif(
                            current_setting('app.current_organization_id', true), '')
                        AND identity.authorize_productive_owner()::text = nullif(
                            current_setting('app.current_authorization_version', true), '')
                        AND EXISTS (
                            SELECT 1
                            FROM productive_core.management_unit_rename_ledgers AS ledger
                            WHERE ledger."Id" = management_unit_rename_key_aliases."LedgerId"
                              AND ledger."OrganizationId" =
                                  management_unit_rename_key_aliases."OrganizationId")
                    )
                    WITH CHECK (
                        "OrganizationId"::text = nullif(
                            current_setting('app.current_organization_id', true), '')
                        AND identity.authorize_productive_owner()::text = nullif(
                            current_setting('app.current_authorization_version', true), '')
                        AND EXISTS (
                            SELECT 1
                            FROM productive_core.management_unit_rename_ledgers AS ledger
                            WHERE ledger."Id" = management_unit_rename_key_aliases."LedgerId"
                              AND ledger."OrganizationId" =
                                  management_unit_rename_key_aliases."OrganizationId")
                    );

                CREATE FUNCTION productive_core.enforce_management_unit_rename_transition()
                RETURNS trigger
                LANGUAGE plpgsql
                SET search_path = pg_catalog
                AS $rename_transition$
                BEGIN
                    IF NEW."OrganizationId" IS DISTINCT FROM OLD."OrganizationId"
                       OR NEW."UnitType" IS DISTINCT FROM OLD."UnitType"
                       OR NEW."Status" IS DISTINCT FROM OLD."Status"
                       OR NEW."SpatialStatus" IS DISTINCT FROM OLD."SpatialStatus"
                       OR NEW."CreatedAtUtc" IS DISTINCT FROM OLD."CreatedAtUtc"
                       OR NEW."DisplayName" IS NOT DISTINCT FROM OLD."DisplayName"
                       OR NEW."Revision" <> OLD."Revision" + 1
                       OR NEW."Version" IS NOT DISTINCT FROM OLD."Version" THEN
                        RAISE EXCEPTION
                            'Invalid Productive Core management unit rename transition';
                    END IF;
                    RETURN NEW;
                END
                $rename_transition$;
                ALTER FUNCTION productive_core.enforce_management_unit_rename_transition()
                    OWNER TO agro_productive_owner;
                REVOKE ALL ON FUNCTION
                    productive_core.enforce_management_unit_rename_transition()
                    FROM PUBLIC;
                CREATE TRIGGER management_units_rename_transition
                    BEFORE UPDATE ON productive_core.management_units
                    FOR EACH ROW EXECUTE FUNCTION
                        productive_core.enforce_management_unit_rename_transition();

                DROP POLICY journal_entries_app_insert
                    ON productive_core.journal_entries;
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
                        AND "Action" IN (
                            'management_unit_created',
                            'management_unit_display_name_changed')
                        AND "Outcome" = 'succeeded'
                    );

                DROP POLICY outbox_messages_app_insert
                    ON productive_core.outbox_messages;
                CREATE POLICY outbox_messages_app_insert
                    ON productive_core.outbox_messages
                    FOR INSERT TO agro_productive_app
                    WITH CHECK (
                        "OrganizationId"::text = nullif(
                            current_setting('app.current_organization_id', true), '')
                        AND identity.authorize_productive_owner()::text = nullif(
                            current_setting('app.current_authorization_version', true), '')
                        AND "EventType" IN (
                            'ManagementUnitCreated',
                            'ManagementUnitDisplayNameChanged')
                        AND "Source" = 'productive-core'
                        AND "Scope" = 'tenant'
                        AND "AggregateType" = 'ManagementUnit'
                        AND "SchemaVersion" = '1.0.0'
                        AND (("EventType" = 'ManagementUnitCreated' AND "AggregateVersion" = 1)
                             OR ("EventType" = 'ManagementUnitDisplayNameChanged'
                                 AND "AggregateVersion" >= 2))
                    );

                CREATE POLICY management_unit_rename_ledgers_owner_key_coverage
                    ON productive_core.management_unit_rename_ledgers
                    FOR SELECT TO agro_productive_owner USING (true);
                CREATE POLICY management_unit_rename_aliases_owner_key_coverage
                    ON productive_core.management_unit_rename_key_aliases
                    FOR SELECT TO agro_productive_owner USING (true);

                CREATE FUNCTION productive_core.management_unit_rename_retained_key_covered(
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
                            FROM productive_core.management_unit_rename_ledgers AS ledger
                            WHERE NOT EXISTS (
                                SELECT 1
                                FROM productive_core.management_unit_rename_key_aliases AS alias
                                WHERE alias."LedgerId" = ledger."Id"
                                  AND alias."OrganizationId" = ledger."OrganizationId"
                                  AND alias."KeyVersion" = ANY(retained_versions)))
                $retained_key_coverage$;
                ALTER FUNCTION
                    productive_core.management_unit_rename_retained_key_covered(text[])
                    OWNER TO agro_productive_owner;
                REVOKE ALL ON FUNCTION
                    productive_core.management_unit_rename_retained_key_covered(text[])
                    FROM PUBLIC;
                GRANT EXECUTE ON FUNCTION
                    productive_core.management_unit_rename_retained_key_covered(text[])
                    TO agro_productive_app;
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

                DROP FUNCTION IF EXISTS
                    productive_core.management_unit_rename_retained_key_covered(text[]);
                DROP TRIGGER IF EXISTS management_units_rename_transition
                    ON productive_core.management_units;
                DROP FUNCTION IF EXISTS
                    productive_core.enforce_management_unit_rename_transition();
                DROP POLICY IF EXISTS management_units_app_update
                    ON productive_core.management_units;
                DROP POLICY IF EXISTS journal_entries_app_insert
                    ON productive_core.journal_entries;
                DROP POLICY IF EXISTS outbox_messages_app_insert
                    ON productive_core.outbox_messages;
                REVOKE UPDATE ("DisplayName", "Revision", "Version")
                    ON TABLE productive_core.management_units
                    FROM agro_productive_app;
                """);

            migrationBuilder.DropTable(
                name: "management_unit_rename_key_aliases",
                schema: "productive_core");

            migrationBuilder.DropTable(
                name: "management_unit_rename_ledgers",
                schema: "productive_core");

            migrationBuilder.DropCheckConstraint(
                name: "CK_productive_outbox_messages_EventType",
                schema: "productive_core",
                table: "outbox_messages");

            migrationBuilder.DropCheckConstraint(
                name: "CK_productive_outbox_messages_Versions",
                schema: "productive_core",
                table: "outbox_messages");

            migrationBuilder.DropCheckConstraint(
                name: "CK_journal_entries_Action",
                schema: "productive_core",
                table: "journal_entries");

            migrationBuilder.DropColumn(
                name: "Revision",
                schema: "productive_core",
                table: "management_units");

            migrationBuilder.AddCheckConstraint(
                name: "CK_productive_outbox_messages_EventType",
                schema: "productive_core",
                table: "outbox_messages",
                sql: "\"EventType\" = 'ManagementUnitCreated'");

            migrationBuilder.AddCheckConstraint(
                name: "CK_productive_outbox_messages_Versions",
                schema: "productive_core",
                table: "outbox_messages",
                sql: "\"SchemaVersion\" = '1.0.0' AND \"AggregateVersion\" = 1");

            migrationBuilder.AddCheckConstraint(
                name: "CK_journal_entries_Action",
                schema: "productive_core",
                table: "journal_entries",
                sql: "\"Action\" = 'management_unit_created'");

            migrationBuilder.Sql(
                """
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
                """);
        }
    }
}
