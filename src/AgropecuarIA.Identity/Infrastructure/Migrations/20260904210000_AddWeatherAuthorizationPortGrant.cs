using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace AgropecuarIA.Identity.Infrastructure.Migrations;

[DbContext(typeof(IdentityDbContext))]
[Migration("20260904210000_AddWeatherAuthorizationPortGrant")]
public sealed class AddWeatherAuthorizationPortGrant : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) => migrationBuilder.Sql(
        """
        DO $role$
        BEGIN
            IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'agro_weather_app') THEN
                CREATE ROLE agro_weather_app NOLOGIN NOINHERIT NOSUPERUSER NOCREATEDB NOCREATEROLE NOREPLICATION NOBYPASSRLS;
            ELSIF EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'agro_weather_app'
                AND (rolcanlogin OR rolinherit OR rolsuper OR rolcreatedb OR rolcreaterole OR rolreplication OR rolbypassrls)) THEN
                RAISE EXCEPTION 'Weather application role has unsafe attributes';
            END IF;
        END $role$;
        GRANT USAGE ON SCHEMA identity TO agro_weather_app;
        GRANT EXECUTE ON FUNCTION identity.authorize_productive_owner() TO agro_weather_app;
        """);

    protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.Sql(
        "REVOKE EXECUTE ON FUNCTION identity.authorize_productive_owner() FROM agro_weather_app;");
}
