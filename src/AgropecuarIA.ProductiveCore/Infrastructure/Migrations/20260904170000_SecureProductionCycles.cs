using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace AgropecuarIA.ProductiveCore.Infrastructure.Migrations;

[DbContext(typeof(ProductiveCoreDbContext))]
[Migration("20260904170000_SecureProductionCycles")]
public sealed class SecureProductionCycles : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            ALTER TABLE productive_core.production_cycles OWNER TO agro_productive_owner;
            ALTER TABLE productive_core.production_events OWNER TO agro_productive_owner;
            ALTER TABLE productive_core.production_cycles ENABLE ROW LEVEL SECURITY;
            ALTER TABLE productive_core.production_cycles FORCE ROW LEVEL SECURITY;
            ALTER TABLE productive_core.production_events ENABLE ROW LEVEL SECURITY;
            ALTER TABLE productive_core.production_events FORCE ROW LEVEL SECURITY;
            REVOKE ALL ON productive_core.production_cycles, productive_core.production_events
                FROM PUBLIC, agro_productive_app, agro_productive_job;
            GRANT SELECT, INSERT ON productive_core.production_cycles TO agro_productive_app;
            GRANT UPDATE ("Status", "EndDateUtc") ON productive_core.production_cycles TO agro_productive_app;
            GRANT SELECT, INSERT ON productive_core.production_events TO agro_productive_app;

            CREATE POLICY production_cycles_app_access ON productive_core.production_cycles
                FOR ALL TO agro_productive_app
                USING (
                    nullif(current_setting('app.current_scope_kind', true), '') = 'tenant'
                    AND "OrganizationId"::text = nullif(current_setting('app.current_organization_id', true), '')
                    AND identity.authorize_productive_owner()::text = nullif(
                        current_setting('app.current_authorization_version', true), '')
                    AND EXISTS (
                        SELECT 1 FROM productive_core.management_units field
                        WHERE field."Id" = production_cycles."ManagementUnitId"
                          AND field."OrganizationId" = production_cycles."OrganizationId"
                    )
                )
                WITH CHECK (
                    nullif(current_setting('app.current_scope_kind', true), '') = 'tenant'
                    AND "OrganizationId"::text = nullif(current_setting('app.current_organization_id', true), '')
                    AND identity.authorize_productive_owner()::text = nullif(
                        current_setting('app.current_authorization_version', true), '')
                    AND EXISTS (
                        SELECT 1 FROM productive_core.management_units field
                        WHERE field."Id" = production_cycles."ManagementUnitId"
                          AND field."OrganizationId" = production_cycles."OrganizationId"
                    )
                );

            CREATE POLICY production_cycles_app_active_field_insert ON productive_core.production_cycles
                AS RESTRICTIVE FOR INSERT TO agro_productive_app
                WITH CHECK (
                    EXISTS (
                        SELECT 1 FROM productive_core.management_units field
                        WHERE field."Id" = production_cycles."ManagementUnitId"
                          AND field."OrganizationId" = production_cycles."OrganizationId"
                          AND field."Status" <> 'archived'
                    )
                );

            CREATE POLICY production_events_app_read ON productive_core.production_events
                FOR SELECT TO agro_productive_app
                USING (
                    nullif(current_setting('app.current_scope_kind', true), '') = 'tenant'
                    AND "OrganizationId"::text = nullif(current_setting('app.current_organization_id', true), '')
                    AND identity.authorize_productive_owner()::text = nullif(
                        current_setting('app.current_authorization_version', true), '')
                    AND EXISTS (
                        SELECT 1 FROM productive_core.production_cycles cycle
                        WHERE cycle."Id" = production_events."ProductionCycleId"
                          AND cycle."OrganizationId" = production_events."OrganizationId"
                    )
                );
            CREATE POLICY production_events_app_insert ON productive_core.production_events
                FOR INSERT TO agro_productive_app
                WITH CHECK (
                    nullif(current_setting('app.current_scope_kind', true), '') = 'tenant'
                    AND "OrganizationId"::text = nullif(current_setting('app.current_organization_id', true), '')
                    AND identity.authorize_productive_owner()::text = nullif(
                        current_setting('app.current_authorization_version', true), '')
                    AND EXISTS (
                        SELECT 1 FROM productive_core.production_cycles cycle
                        WHERE cycle."Id" = production_events."ProductionCycleId"
                          AND cycle."OrganizationId" = production_events."OrganizationId"
                          AND cycle."Status" = 'active'
                    )
                );
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Reverting the additive feature removes app access; it never restores unscoped reads.
        migrationBuilder.Sql(
            """
            REVOKE ALL ON productive_core.production_cycles, productive_core.production_events
                FROM agro_productive_app, agro_productive_job;
            REVOKE UPDATE ("Status", "EndDateUtc") ON productive_core.production_cycles FROM agro_productive_app;
            DROP POLICY production_events_app_insert ON productive_core.production_events;
            DROP POLICY production_events_app_read ON productive_core.production_events;
            DROP POLICY production_cycles_app_access ON productive_core.production_cycles;
            DROP POLICY production_cycles_app_active_field_insert ON productive_core.production_cycles;
            """);
    }
}
