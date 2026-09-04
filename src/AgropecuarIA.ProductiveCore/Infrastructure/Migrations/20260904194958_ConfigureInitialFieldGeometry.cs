using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgropecuarIA.ProductiveCore.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ConfigureInitialFieldGeometry : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DO $capability$
                BEGIN
                    IF NOT EXISTS (SELECT 1 FROM pg_extension e JOIN pg_namespace n ON n.oid = e.extnamespace
                                   WHERE e.extname = 'postgis' AND n.nspname = 'public') THEN
                        RAISE EXCEPTION 'Initial field geometry requires PostGIS pre-provisioned in schema public by the deployment operator; application migrations do not install extensions';
                    END IF;
                END
                $capability$;

                CREATE TABLE productive_core.management_unit_geometry_versions (
                    "Id" uuid PRIMARY KEY,
                    "OrganizationId" uuid NOT NULL,
                    "ManagementUnitId" uuid NOT NULL,
                    "ActorUserId" uuid NOT NULL,
                    "SessionId" uuid NOT NULL,
                    "Boundary" public.geometry(MultiPolygon,4326) NOT NULL,
                    "DeclaredAreaHectares" numeric(18,4) NOT NULL CHECK ("DeclaredAreaHectares" > 0),
                    "CalculatedAreaHectares" numeric(18,4) GENERATED ALWAYS AS
                        (round((public.ST_Area("Boundary"::public.geography, true) / 10000)::numeric, 4)) STORED,
                    "CentroidLatitude" double precision GENERATED ALWAYS AS
                        (public.ST_Y(public.ST_Centroid("Boundary"::public.geography, true)::public.geometry)) STORED,
                    "CentroidLongitude" double precision GENERATED ALWAYS AS
                        (public.ST_X(public.ST_Centroid("Boundary"::public.geography, true)::public.geometry)) STORED,
                    "CalculationMethod" text NOT NULL DEFAULT 'postgis-geography-spheroid'
                        CHECK ("CalculationMethod" = 'postgis-geography-spheroid'),
                    "Revision" bigint NOT NULL CHECK ("Revision" >= 2),
                    "ConfiguredAtUtc" timestamptz NOT NULL,
                    "JournalEntryId" uuid NOT NULL UNIQUE REFERENCES productive_core.journal_entries("Id") DEFERRABLE INITIALLY DEFERRED,
                    "OutboxMessageId" uuid NOT NULL UNIQUE REFERENCES productive_core.outbox_messages("Id") DEFERRABLE INITIALLY DEFERRED,
                    UNIQUE ("OrganizationId", "ManagementUnitId"),
                    FOREIGN KEY ("ManagementUnitId", "OrganizationId") REFERENCES productive_core.management_units("Id", "OrganizationId"),
                    CHECK (public.ST_NDims("Boundary") = 2 AND NOT public.ST_IsEmpty("Boundary")
                        AND public.ST_IsValid("Boundary", 0) AND public.ST_NPoints("Boundary") BETWEEN 4 AND 10000
                        AND public.ST_XMin(public.Box3D("Boundary")) >= -180 AND public.ST_XMax(public.Box3D("Boundary")) <= 180
                        AND public.ST_YMin(public.Box3D("Boundary")) >= -90 AND public.ST_YMax(public.Box3D("Boundary")) <= 90),
                    CHECK ("CalculatedAreaHectares" > 0)
                );
                CREATE INDEX "IX_geometry_versions_Boundary" ON productive_core.management_unit_geometry_versions USING gist("Boundary");
                ALTER TABLE productive_core.management_unit_geometry_versions OWNER TO agro_productive_owner;
                ALTER TABLE productive_core.management_unit_geometry_versions ENABLE ROW LEVEL SECURITY;
                ALTER TABLE productive_core.management_unit_geometry_versions FORCE ROW LEVEL SECURITY;
                REVOKE ALL ON TABLE productive_core.management_unit_geometry_versions FROM PUBLIC, agro_productive_app, agro_productive_job;
                GRANT SELECT, INSERT ON TABLE productive_core.management_unit_geometry_versions TO agro_productive_app;
                CREATE POLICY geometry_versions_app_select ON productive_core.management_unit_geometry_versions
                    FOR SELECT TO agro_productive_app USING (
                        nullif(current_setting('app.current_scope_kind', true), '') = 'tenant'
                        AND "OrganizationId"::text = nullif(current_setting('app.current_organization_id', true), '')
                        AND identity.authorize_productive_owner()::text = nullif(current_setting('app.current_authorization_version', true), ''));
                CREATE POLICY geometry_versions_app_insert ON productive_core.management_unit_geometry_versions
                    FOR INSERT TO agro_productive_app WITH CHECK (
                        nullif(current_setting('app.current_scope_kind', true), '') = 'tenant'
                        AND "OrganizationId"::text = nullif(current_setting('app.current_organization_id', true), '')
                        AND "ActorUserId"::text = nullif(current_setting('app.current_actor_id', true), '')
                        AND "SessionId"::text = nullif(current_setting('app.current_session_id', true), '')
                        AND identity.authorize_productive_owner()::text = nullif(current_setting('app.current_authorization_version', true), '')
                        AND EXISTS (SELECT 1 FROM productive_core.management_units f
                            WHERE f."OrganizationId" = management_unit_geometry_versions."OrganizationId"
                              AND f."Id" = management_unit_geometry_versions."ManagementUnitId"
                              AND f."Status" = 'draft' AND f."SpatialStatus" = 'not_configured'
                              AND f."Revision" + 1 = management_unit_geometry_versions."Revision"));

                GRANT UPDATE ("SpatialStatus", "BoundaryGeoJson", "DeclaredAreaHectares", "CalculatedAreaHectares",
                    "CentroidLatitude", "CentroidLongitude") ON productive_core.management_units TO agro_productive_app;
                ALTER POLICY management_units_app_update ON productive_core.management_units WITH CHECK (
                    "Status" = 'draft' AND "UnitType" = 'field' AND "SpatialStatus" IN ('not_configured', 'configured')
                    AND "Revision" >= 2 AND nullif(current_setting('app.current_scope_kind', true), '') = 'tenant'
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
                           (to_jsonb(OLD) - ARRAY['Status', 'Revision', 'Version']) THEN RETURN NEW; END IF;
                    IF NEW."DisplayName" IS DISTINCT FROM OLD."DisplayName"
                       AND (to_jsonb(NEW) - ARRAY['DisplayName', 'Revision', 'Version']) =
                           (to_jsonb(OLD) - ARRAY['DisplayName', 'Revision', 'Version']) THEN RETURN NEW; END IF;
                    IF OLD."SpatialStatus" = 'not_configured' AND NEW."SpatialStatus" = 'configured'
                       AND NEW."OfficialProvinceCode" IS NULL AND NEW."OfficialDepartmentCode" IS NULL
                       AND (to_jsonb(NEW) - ARRAY['SpatialStatus', 'BoundaryGeoJson', 'DeclaredAreaHectares',
                           'CalculatedAreaHectares', 'CentroidLatitude', 'CentroidLongitude', 'Revision', 'Version']) =
                           (to_jsonb(OLD) - ARRAY['SpatialStatus', 'BoundaryGeoJson', 'DeclaredAreaHectares',
                           'CalculatedAreaHectares', 'CentroidLatitude', 'CentroidLongitude', 'Revision', 'Version'])
                       AND EXISTS (SELECT 1 FROM productive_core.management_unit_geometry_versions s
                           WHERE s."OrganizationId" = NEW."OrganizationId" AND s."ManagementUnitId" = NEW."Id"
                             AND s."Revision" = NEW."Revision"
                             AND public.ST_AsGeoJSON(s."Boundary", 17, 0)::jsonb = NEW."BoundaryGeoJson"
                             AND s."DeclaredAreaHectares" = NEW."DeclaredAreaHectares"
                             AND s."CalculatedAreaHectares" = NEW."CalculatedAreaHectares"
                             AND s."CentroidLatitude" = NEW."CentroidLatitude" AND s."CentroidLongitude" = NEW."CentroidLongitude")
                    THEN RETURN NEW; END IF;
                    RAISE EXCEPTION 'Invalid Productive Core management unit transition';
                END
                $transition$;

                CREATE POLICY journal_entries_app_geometry ON productive_core.journal_entries
                    FOR INSERT TO agro_productive_app WITH CHECK (
                        "OrganizationId"::text = nullif(current_setting('app.current_organization_id', true), '')
                        AND "ActorUserId"::text = nullif(current_setting('app.current_actor_id', true), '')
                        AND "SessionId"::text = nullif(current_setting('app.current_session_id', true), '')
                        AND identity.authorize_productive_owner()::text = nullif(current_setting('app.current_authorization_version', true), '')
                        AND "Action" = 'management_unit_geometry_configured' AND "Outcome" = 'succeeded');
                CREATE POLICY outbox_messages_app_geometry ON productive_core.outbox_messages
                    FOR INSERT TO agro_productive_app WITH CHECK (
                        "OrganizationId"::text = nullif(current_setting('app.current_organization_id', true), '')
                        AND identity.authorize_productive_owner()::text = nullif(current_setting('app.current_authorization_version', true), '')
                        AND "EventType" = 'ManagementUnitGeometryConfigured' AND "Source" = 'productive-core'
                        AND "Scope" = 'tenant' AND "AggregateType" = 'ManagementUnit' AND "SchemaVersion" = '1.0.0'
                        AND EXISTS (SELECT 1 FROM productive_core.management_unit_geometry_versions s
                            WHERE s."OutboxMessageId" = outbox_messages."Id" AND s."OrganizationId" = outbox_messages."OrganizationId"
                              AND s."ManagementUnitId" = "AggregateId" AND s."Revision" = "AggregateVersion"
                              AND "PayloadJson"->>'organizationId' = s."OrganizationId"::text
                              AND "PayloadJson"->>'managementUnitId' = s."ManagementUnitId"::text
                              AND "PayloadJson"->>'geometryVersionId' = s."Id"::text
                              AND "PayloadJson"->>'revision' = s."Revision"::text
                              AND "PayloadJson"->>'spatialStatus' = 'configured'
                              AND ("PayloadJson"->>'declaredAreaHectares')::numeric = s."DeclaredAreaHectares"
                              AND ("PayloadJson"->>'calculatedAreaHectares')::numeric = s."CalculatedAreaHectares"
                              AND ("PayloadJson"->>'configuredAtUtc')::timestamptz = s."ConfiguredAtUtc"
                              AND "PayloadJson"->>'calculationMethod' = s."CalculationMethod"));

                CREATE POLICY geometry_versions_owner_integrity ON productive_core.management_unit_geometry_versions FOR SELECT TO agro_productive_owner USING (true);
                CREATE POLICY management_units_owner_geometry_integrity ON productive_core.management_units FOR SELECT TO agro_productive_owner USING (true);
                CREATE POLICY journal_entries_owner_geometry_integrity ON productive_core.journal_entries FOR SELECT TO agro_productive_owner USING (true);
                CREATE POLICY outbox_messages_owner_geometry_integrity ON productive_core.outbox_messages FOR SELECT TO agro_productive_owner USING (true);
                CREATE FUNCTION productive_core.enforce_initial_geometry_atomicity()
                RETURNS trigger LANGUAGE plpgsql SECURITY DEFINER SET search_path = pg_catalog AS $atomic$
                BEGIN
                    IF NOT EXISTS (SELECT 1 FROM productive_core.management_units f
                        JOIN productive_core.journal_entries j ON j."Id" = NEW."JournalEntryId"
                        JOIN productive_core.outbox_messages o ON o."Id" = NEW."OutboxMessageId"
                        WHERE f."Id" = NEW."ManagementUnitId" AND f."OrganizationId" = NEW."OrganizationId"
                          AND f."SpatialStatus" = 'configured' AND f."Revision" >= NEW."Revision"
                          AND f."BoundaryGeoJson" = public.ST_AsGeoJSON(NEW."Boundary", 17, 0)::jsonb
                          AND f."DeclaredAreaHectares" = NEW."DeclaredAreaHectares"
                          AND f."CalculatedAreaHectares" = NEW."CalculatedAreaHectares"
                          AND f."CentroidLatitude" = NEW."CentroidLatitude" AND f."CentroidLongitude" = NEW."CentroidLongitude"
                          AND j."OrganizationId" = NEW."OrganizationId" AND j."ActorUserId" = NEW."ActorUserId"
                          AND j."SessionId" = NEW."SessionId" AND j."Action" = 'management_unit_geometry_configured'
                          AND j."OccurredAtUtc" = NEW."ConfiguredAtUtc" AND j."Outcome" = 'succeeded'
                          AND o."OrganizationId" = NEW."OrganizationId" AND o."AggregateId" = NEW."ManagementUnitId"
                          AND o."AggregateVersion" = NEW."Revision" AND o."EventType" = 'ManagementUnitGeometryConfigured'
                          AND o."PayloadJson"->>'geometryVersionId' = NEW."Id"::text
                          AND o."OccurredAtUtc" = NEW."ConfiguredAtUtc" AND o."CorrelationId" = j."CorrelationId") THEN
                        RAISE EXCEPTION 'Initial geometry requires an atomic matching field, journal and outbox';
                    END IF;
                    RETURN NULL;
                END
                $atomic$;
                ALTER FUNCTION productive_core.enforce_initial_geometry_atomicity() OWNER TO agro_productive_owner;
                REVOKE ALL ON FUNCTION productive_core.enforce_initial_geometry_atomicity() FROM PUBLIC;
                CREATE CONSTRAINT TRIGGER initial_geometry_atomicity AFTER INSERT ON productive_core.management_unit_geometry_versions
                    DEFERRABLE INITIALLY DEFERRED FOR EACH ROW EXECUTE FUNCTION productive_core.enforce_initial_geometry_atomicity();
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
                sql: "\"EventType\" IN ('ManagementUnitCreated', 'ManagementUnitDisplayNameChanged', 'ManagementUnitArchived', 'ManagementUnitGeometryConfigured')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_productive_outbox_messages_Versions",
                schema: "productive_core",
                table: "outbox_messages",
                sql: "\"SchemaVersion\" = '1.0.0' AND ((\"EventType\" = 'ManagementUnitCreated' AND \"AggregateVersion\" = 1) OR (\"EventType\" IN ('ManagementUnitDisplayNameChanged', 'ManagementUnitArchived', 'ManagementUnitGeometryConfigured') AND \"AggregateVersion\" >= 2))");

            migrationBuilder.AddCheckConstraint(
                name: "CK_journal_entries_Action",
                schema: "productive_core",
                table: "journal_entries",
                sql: "\"Action\" IN ('management_unit_created', 'management_unit_display_name_changed', 'management_unit_archived', 'management_unit_geometry_configured')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DO $ephemeral_only$
                BEGIN
                    IF current_database() !~ '^agro_(identity|productive)_[0-9a-f]{32}$' THEN
                        RAISE EXCEPTION 'Initial geometry rollback is limited to disposable test databases; preserve immutable spatial history';
                    END IF;
                END
                $ephemeral_only$;
                DROP POLICY journal_entries_app_geometry ON productive_core.journal_entries;
                DROP POLICY outbox_messages_app_geometry ON productive_core.outbox_messages;
                DROP TABLE productive_core.management_unit_geometry_versions;
                DROP FUNCTION productive_core.enforce_initial_geometry_atomicity();
                DROP POLICY management_units_owner_geometry_integrity ON productive_core.management_units;
                DROP POLICY journal_entries_owner_geometry_integrity ON productive_core.journal_entries;
                DROP POLICY outbox_messages_owner_geometry_integrity ON productive_core.outbox_messages;
                REVOKE UPDATE ("SpatialStatus", "BoundaryGeoJson", "DeclaredAreaHectares", "CalculatedAreaHectares",
                    "CentroidLatitude", "CentroidLongitude") ON productive_core.management_units FROM agro_productive_app;
                ALTER POLICY management_units_app_update ON productive_core.management_units WITH CHECK (
                    "Status" = 'draft' AND "UnitType" = 'field' AND "SpatialStatus" = 'not_configured'
                    AND "Revision" >= 2 AND nullif(current_setting('app.current_scope_kind', true), '') = 'tenant'
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
                           (to_jsonb(OLD) - ARRAY['Status', 'Revision', 'Version']) THEN RETURN NEW; END IF;
                    IF NEW."DisplayName" IS DISTINCT FROM OLD."DisplayName"
                       AND (to_jsonb(NEW) - ARRAY['DisplayName', 'Revision', 'Version']) =
                           (to_jsonb(OLD) - ARRAY['DisplayName', 'Revision', 'Version']) THEN RETURN NEW; END IF;
                    RAISE EXCEPTION 'Invalid Productive Core management unit transition';
                END
                $transition$;
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
        }
    }
}
