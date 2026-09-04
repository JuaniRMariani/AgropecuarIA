using AgropecuarIA.Weather.Infrastructure;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace AgropecuarIA.Weather.Migrations;

[DbContext(typeof(WeatherDbContext))]
[Migration("20260904211000_EnforceWeatherTenantIsolation")]
public sealed class EnforceWeatherTenantIsolation : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            DO $roles$
            DECLARE role_name text;
            BEGIN
                FOREACH role_name IN ARRAY ARRAY['agro_weather_owner', 'agro_weather_editor', 'agro_weather_app'] LOOP
                    IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = role_name) THEN
                        EXECUTE format('CREATE ROLE %I NOLOGIN NOINHERIT NOSUPERUSER NOCREATEDB NOCREATEROLE NOREPLICATION NOBYPASSRLS', role_name);
                    ELSIF EXISTS (SELECT 1 FROM pg_roles WHERE rolname = role_name
                        AND (rolcanlogin OR rolinherit OR rolsuper OR rolcreatedb OR rolcreaterole OR rolreplication OR rolbypassrls)) THEN
                        RAISE EXCEPTION 'Weather role has unsafe attributes';
                    END IF;
                END LOOP;
            END $roles$;
            ALTER SCHEMA weather OWNER TO agro_weather_owner;
            REVOKE ALL ON SCHEMA weather FROM PUBLIC;
            GRANT USAGE ON SCHEMA weather TO agro_weather_app, agro_weather_editor;
            ALTER TABLE weather.observed_rains OWNER TO agro_weather_owner;
            ALTER TABLE weather.activity_rules OWNER TO agro_weather_owner;
            ALTER TABLE weather.forecast_snapshots OWNER TO agro_weather_owner;
            ALTER TABLE weather.weather_alerts OWNER TO agro_weather_owner;
            REVOKE ALL ON ALL TABLES IN SCHEMA weather FROM PUBLIC, agro_weather_app, agro_weather_editor;
            GRANT SELECT, INSERT ON weather.observed_rains, weather.activity_rules, weather.forecast_snapshots TO agro_weather_app;
            GRANT SELECT ON weather.weather_alerts TO agro_weather_app;
            GRANT SELECT, INSERT ON weather.weather_alerts TO agro_weather_editor;
            GRANT UPDATE ("Status") ON weather.weather_alerts TO agro_weather_editor;
            ALTER TABLE weather.observed_rains ENABLE ROW LEVEL SECURITY;
            ALTER TABLE weather.observed_rains FORCE ROW LEVEL SECURITY;
            ALTER TABLE weather.activity_rules ENABLE ROW LEVEL SECURITY;
            ALTER TABLE weather.activity_rules FORCE ROW LEVEL SECURITY;
            """);

        foreach ((string table, string actorColumn) in new[]
        {
            ("observed_rains", "RecordedByUserId"), ("activity_rules", "CreatedByUserId"),
        })
        {
            // Table and column identifiers are fixed migration constants, never request input.
            migrationBuilder.Sql($"""
                CREATE POLICY {table}_tenant_read ON weather.{table}
                FOR SELECT TO agro_weather_app
                USING (
                    nullif(current_setting('app.current_scope_kind', true), '') = 'tenant'
                    AND "OrganizationId"::text = nullif(current_setting('app.current_organization_id', true), '')
                    AND identity.authorize_productive_owner()::text = nullif(current_setting('app.current_authorization_version', true), '')
                );
                CREATE POLICY {table}_tenant_insert ON weather.{table}
                FOR INSERT TO agro_weather_app
                WITH CHECK (
                    nullif(current_setting('app.current_scope_kind', true), '') = 'tenant'
                    AND "OrganizationId"::text = nullif(current_setting('app.current_organization_id', true), '')
                    AND "{actorColumn}"::text = nullif(current_setting('app.current_actor_id', true), '')
                    AND identity.authorize_productive_owner()::text = nullif(current_setting('app.current_authorization_version', true), '')
                );
                """);
        }
    }

    protected override void Down(MigrationBuilder migrationBuilder) => throw new NotSupportedException(
        "Removing tenant isolation is not supported. Use a forward migration.");
}
