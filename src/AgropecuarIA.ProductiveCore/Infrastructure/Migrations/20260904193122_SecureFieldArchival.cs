using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgropecuarIA.ProductiveCore.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SecureFieldArchival : Migration
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

            migrationBuilder.AddCheckConstraint(
                name: "CK_productive_outbox_messages_EventType",
                schema: "productive_core",
                table: "outbox_messages",
                sql: "\"EventType\" IN ('ManagementUnitCreated', 'ManagementUnitDisplayNameChanged', 'ManagementUnitArchived')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_productive_outbox_messages_Versions",
                schema: "productive_core",
                table: "outbox_messages",
                sql: "\"SchemaVersion\" = '1.0.0' AND ((\"EventType\" = 'ManagementUnitCreated' AND \"AggregateVersion\" = 1) OR (\"EventType\" IN ('ManagementUnitDisplayNameChanged', 'ManagementUnitArchived') AND \"AggregateVersion\" >= 2))");

            migrationBuilder.AddCheckConstraint(
                name: "CK_journal_entries_Action",
                schema: "productive_core",
                table: "journal_entries",
                sql: "\"Action\" IN ('management_unit_created', 'management_unit_display_name_changed', 'management_unit_archived')");

            migrationBuilder.Sql("""
                ALTER TABLE productive_core.management_unit_archive_ledgers OWNER TO agro_productive_owner;
                ALTER TABLE productive_core.management_unit_archive_key_aliases OWNER TO agro_productive_owner;
                ALTER TABLE productive_core.management_unit_archive_ledgers ENABLE ROW LEVEL SECURITY;
                ALTER TABLE productive_core.management_unit_archive_ledgers FORCE ROW LEVEL SECURITY;
                ALTER TABLE productive_core.management_unit_archive_key_aliases ENABLE ROW LEVEL SECURITY;
                ALTER TABLE productive_core.management_unit_archive_key_aliases FORCE ROW LEVEL SECURITY;
                REVOKE ALL ON TABLE productive_core.management_unit_archive_ledgers,
                    productive_core.management_unit_archive_key_aliases
                    FROM PUBLIC, agro_productive_app, agro_productive_job;
                GRANT SELECT, INSERT ON TABLE productive_core.management_unit_archive_ledgers,
                    productive_core.management_unit_archive_key_aliases TO agro_productive_app;
                GRANT UPDATE ("Status") ON productive_core.management_units TO agro_productive_app;

                CREATE POLICY management_unit_archive_ledgers_app_access
                    ON productive_core.management_unit_archive_ledgers
                    FOR ALL TO agro_productive_app
                    USING (
                        nullif(current_setting('app.current_scope_kind', true), '') = 'tenant'
                        AND "OrganizationId"::text = nullif(current_setting('app.current_organization_id', true), '')
                        AND "ActorUserId"::text = nullif(current_setting('app.current_actor_id', true), '')
                        AND "SessionId"::text = nullif(current_setting('app.current_session_id', true), '')
                        AND "AuthorizationVersion"::text = nullif(current_setting('app.current_authorization_version', true), '')
                        AND identity.authorize_productive_owner() = "AuthorizationVersion")
                    WITH CHECK (
                        nullif(current_setting('app.current_scope_kind', true), '') = 'tenant'
                        AND "OrganizationId"::text = nullif(current_setting('app.current_organization_id', true), '')
                        AND "ActorUserId"::text = nullif(current_setting('app.current_actor_id', true), '')
                        AND "SessionId"::text = nullif(current_setting('app.current_session_id', true), '')
                        AND "AuthorizationVersion"::text = nullif(current_setting('app.current_authorization_version', true), '')
                        AND identity.authorize_productive_owner() = "AuthorizationVersion");
                CREATE POLICY management_unit_archive_aliases_app_access
                    ON productive_core.management_unit_archive_key_aliases
                    FOR ALL TO agro_productive_app
                    USING (
                        "OrganizationId"::text = nullif(current_setting('app.current_organization_id', true), '')
                        AND identity.authorize_productive_owner()::text = nullif(current_setting('app.current_authorization_version', true), '')
                        AND EXISTS (SELECT 1 FROM productive_core.management_unit_archive_ledgers AS ledger
                            WHERE ledger."Id" = management_unit_archive_key_aliases."LedgerId"
                              AND ledger."OrganizationId" = management_unit_archive_key_aliases."OrganizationId"))
                    WITH CHECK (
                        "OrganizationId"::text = nullif(current_setting('app.current_organization_id', true), '')
                        AND identity.authorize_productive_owner()::text = nullif(current_setting('app.current_authorization_version', true), '')
                        AND EXISTS (SELECT 1 FROM productive_core.management_unit_archive_ledgers AS ledger
                            WHERE ledger."Id" = management_unit_archive_key_aliases."LedgerId"
                              AND ledger."OrganizationId" = management_unit_archive_key_aliases."OrganizationId"));

                CREATE POLICY management_units_app_archive ON productive_core.management_units
                    FOR UPDATE TO agro_productive_app
                    USING (
                        "Status" = 'draft'
                        AND nullif(current_setting('app.current_scope_kind', true), '') = 'tenant'
                        AND "OrganizationId"::text = nullif(current_setting('app.current_organization_id', true), '')
                        AND identity.authorize_productive_owner()::text = nullif(current_setting('app.current_authorization_version', true), ''))
                    WITH CHECK (
                        "Status" = 'archived' AND "UnitType" = 'field' AND "Revision" >= 2
                        AND nullif(current_setting('app.current_scope_kind', true), '') = 'tenant'
                        AND "OrganizationId"::text = nullif(current_setting('app.current_organization_id', true), '')
                        AND identity.authorize_productive_owner()::text = nullif(current_setting('app.current_authorization_version', true), ''));

                CREATE OR REPLACE FUNCTION productive_core.enforce_management_unit_rename_transition()
                RETURNS trigger LANGUAGE plpgsql SET search_path = pg_catalog AS $transition$
                BEGIN
                    IF OLD."Status" <> 'draft' OR NEW."Revision" <> OLD."Revision" + 1
                       OR NEW."Version" IS NOT DISTINCT FROM OLD."Version" THEN
                        RAISE EXCEPTION 'Invalid Productive Core management unit transition';
                    END IF;
                    IF NEW."Status" = 'archived'
                       AND (to_jsonb(NEW) - ARRAY['Status', 'Revision', 'Version']) =
                           (to_jsonb(OLD) - ARRAY['Status', 'Revision', 'Version']) THEN
                        RETURN NEW;
                    END IF;
                    IF NEW."DisplayName" IS DISTINCT FROM OLD."DisplayName"
                       AND (to_jsonb(NEW) - ARRAY['DisplayName', 'Revision', 'Version']) =
                           (to_jsonb(OLD) - ARRAY['DisplayName', 'Revision', 'Version']) THEN
                        RETURN NEW;
                    END IF;
                    RAISE EXCEPTION 'Invalid Productive Core management unit transition';
                END
                $transition$;

                CREATE POLICY journal_entries_app_archive ON productive_core.journal_entries
                    FOR INSERT TO agro_productive_app
                    WITH CHECK (
                        "OrganizationId"::text = nullif(current_setting('app.current_organization_id', true), '')
                        AND "ActorUserId"::text = nullif(current_setting('app.current_actor_id', true), '')
                        AND "SessionId"::text = nullif(current_setting('app.current_session_id', true), '')
                        AND identity.authorize_productive_owner()::text = nullif(current_setting('app.current_authorization_version', true), '')
                        AND "Action" = 'management_unit_archived' AND "Outcome" = 'succeeded');
                CREATE POLICY outbox_messages_app_archive ON productive_core.outbox_messages
                    FOR INSERT TO agro_productive_app
                    WITH CHECK (
                        "OrganizationId"::text = nullif(current_setting('app.current_organization_id', true), '')
                        AND identity.authorize_productive_owner()::text = nullif(current_setting('app.current_authorization_version', true), '')
                        AND "EventType" = 'ManagementUnitArchived'
                        AND "Source" = 'productive-core' AND "Scope" = 'tenant'
                        AND "AggregateType" = 'ManagementUnit' AND "SchemaVersion" = '1.0.0'
                        AND "AggregateVersion" >= 2
                        AND "PayloadJson"->>'organizationId' = "OrganizationId"::text
                        AND "PayloadJson"->>'managementUnitId' = "AggregateId"::text
                        AND "PayloadJson"->>'revision' = "AggregateVersion"::text
                        AND "PayloadJson"->>'status' = 'archived'
                        AND "PayloadJson" ? 'archivedAtUtc');

                CREATE POLICY management_unit_archive_ledgers_owner_key_coverage
                    ON productive_core.management_unit_archive_ledgers
                    FOR SELECT TO agro_productive_owner USING (true);
                CREATE POLICY management_unit_archive_aliases_owner_key_coverage
                    ON productive_core.management_unit_archive_key_aliases
                    FOR SELECT TO agro_productive_owner USING (true);
                CREATE FUNCTION productive_core.management_unit_archive_retained_key_covered(retained_versions text[])
                RETURNS boolean LANGUAGE sql STABLE SECURITY DEFINER SET search_path = pg_catalog
                AS $coverage$
                    SELECT retained_versions IS NOT NULL AND cardinality(retained_versions) > 0
                       AND NOT EXISTS (
                            SELECT 1 FROM productive_core.management_unit_archive_ledgers AS ledger
                            WHERE NOT EXISTS (
                                SELECT 1 FROM productive_core.management_unit_archive_key_aliases AS alias
                                WHERE alias."LedgerId" = ledger."Id"
                                  AND alias."OrganizationId" = ledger."OrganizationId"
                                  AND alias."KeyVersion" = ANY(retained_versions)))
                $coverage$;
                ALTER FUNCTION productive_core.management_unit_archive_retained_key_covered(text[])
                    OWNER TO agro_productive_owner;
                REVOKE ALL ON FUNCTION productive_core.management_unit_archive_retained_key_covered(text[]) FROM PUBLIC;
                GRANT EXECUTE ON FUNCTION productive_core.management_unit_archive_retained_key_covered(text[]) TO agro_productive_app;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DO $ephemeral_only$
                BEGIN
                    IF current_database() !~ '^agro_(identity|productive)_[0-9a-f]{32}$' THEN
                        RAISE EXCEPTION 'Field archival rollback is limited to disposable test databases';
                    END IF;
                END
                $ephemeral_only$;
                DROP FUNCTION productive_core.management_unit_archive_retained_key_covered(text[]);
                DROP POLICY management_unit_archive_ledgers_owner_key_coverage ON productive_core.management_unit_archive_ledgers;
                DROP POLICY management_unit_archive_aliases_owner_key_coverage ON productive_core.management_unit_archive_key_aliases;
                DROP POLICY management_unit_archive_ledgers_app_access ON productive_core.management_unit_archive_ledgers;
                DROP POLICY management_unit_archive_aliases_app_access ON productive_core.management_unit_archive_key_aliases;
                DROP POLICY management_units_app_archive ON productive_core.management_units;
                DROP POLICY journal_entries_app_archive ON productive_core.journal_entries;
                DROP POLICY outbox_messages_app_archive ON productive_core.outbox_messages;
                REVOKE UPDATE ("Status") ON productive_core.management_units FROM agro_productive_app;
                REVOKE ALL ON TABLE productive_core.management_unit_archive_ledgers,
                    productive_core.management_unit_archive_key_aliases FROM agro_productive_app;
                ALTER TABLE productive_core.management_unit_archive_ledgers DISABLE ROW LEVEL SECURITY;
                ALTER TABLE productive_core.management_unit_archive_ledgers NO FORCE ROW LEVEL SECURITY;
                ALTER TABLE productive_core.management_unit_archive_key_aliases DISABLE ROW LEVEL SECURITY;
                ALTER TABLE productive_core.management_unit_archive_key_aliases NO FORCE ROW LEVEL SECURITY;
                CREATE OR REPLACE FUNCTION productive_core.enforce_management_unit_rename_transition()
                RETURNS trigger LANGUAGE plpgsql SET search_path = pg_catalog AS $rename_transition$
                BEGIN
                    IF NEW."OrganizationId" IS DISTINCT FROM OLD."OrganizationId"
                       OR NEW."UnitType" IS DISTINCT FROM OLD."UnitType"
                       OR NEW."Status" IS DISTINCT FROM OLD."Status"
                       OR NEW."SpatialStatus" IS DISTINCT FROM OLD."SpatialStatus"
                       OR NEW."CreatedAtUtc" IS DISTINCT FROM OLD."CreatedAtUtc"
                       OR NEW."DisplayName" IS NOT DISTINCT FROM OLD."DisplayName"
                       OR NEW."Revision" <> OLD."Revision" + 1
                       OR NEW."Version" IS NOT DISTINCT FROM OLD."Version" THEN
                        RAISE EXCEPTION 'Invalid Productive Core management unit rename transition';
                    END IF;
                    RETURN NEW;
                END
                $rename_transition$;
                """);
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
        }
    }
}
