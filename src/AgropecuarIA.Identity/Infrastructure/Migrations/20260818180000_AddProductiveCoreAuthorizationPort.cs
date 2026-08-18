using AgropecuarIA.Identity.Infrastructure;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgropecuarIA.Identity.Infrastructure.Migrations;

[DbContext(typeof(IdentityDbContext))]
[Migration("20260818180000_AddProductiveCoreAuthorizationPort")]
public sealed class AddProductiveCoreAuthorizationPort : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            DO $consumer_role$
            BEGIN
                IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'agro_productive_app') THEN
                    CREATE ROLE agro_productive_app
                        NOLOGIN NOINHERIT NOSUPERUSER NOCREATEDB NOCREATEROLE
                        NOREPLICATION NOBYPASSRLS;
                ELSIF EXISTS (
                    SELECT 1
                    FROM pg_roles
                    WHERE rolname = 'agro_productive_app'
                      AND (rolcanlogin OR rolinherit OR rolsuper OR rolcreatedb
                           OR rolcreaterole OR rolreplication OR rolbypassrls)
                ) THEN
                    RAISE EXCEPTION
                        'Productive Core application role has unsafe attributes';
                END IF;
            END
            $consumer_role$;

            CREATE FUNCTION identity.authorize_productive_owner()
            RETURNS uuid
            LANGUAGE plpgsql
            VOLATILE
            SECURITY DEFINER
            SET search_path = pg_catalog
            AS $authorize_productive_owner$
            DECLARE
                actor_id uuid;
                organization_id uuid;
                session_id uuid;
                authorization_version uuid;
            BEGIN
                IF nullif(current_setting('app.current_scope_kind', true), '') <> 'tenant' THEN
                    RETURN NULL;
                END IF;

                BEGIN
                    actor_id := nullif(
                        current_setting('app.current_actor_id', true), '')::uuid;
                    organization_id := nullif(
                        current_setting('app.current_organization_id', true), '')::uuid;
                    session_id := nullif(
                        current_setting('app.current_session_id', true), '')::uuid;
                EXCEPTION WHEN invalid_text_representation OR null_value_not_allowed THEN
                    RETURN NULL;
                END;

                IF actor_id IS NULL OR organization_id IS NULL OR session_id IS NULL THEN
                    RETURN NULL;
                END IF;

                PERFORM 1
                FROM identity.organizations AS organization
                WHERE organization."Id" = organization_id
                  AND organization."Status" = 'active'
                FOR KEY SHARE;

                IF NOT FOUND THEN
                    RETURN NULL;
                END IF;

                SELECT actor_session."Version"
                INTO authorization_version
                FROM identity.sessions AS actor_session
                WHERE actor_session."Id" = session_id
                  AND actor_session."UserId" = actor_id
                  AND actor_session."RevokedAtUtc" IS NULL
                  AND actor_session."ExpiresAtUtc" > statement_timestamp()
                  AND actor_session."IsAuthenticationAssuranceVerified";

                IF authorization_version IS NULL OR NOT EXISTS (
                    SELECT 1
                    FROM identity.memberships AS actor_membership
                    WHERE actor_membership."OrganizationId" = organization_id
                      AND actor_membership."UserId" = actor_id
                      AND actor_membership."Role" = 'owner'
                      AND actor_membership."Status" = 'active') THEN
                    RETURN NULL;
                END IF;

                RETURN authorization_version;
            END
            $authorize_productive_owner$;

            ALTER FUNCTION identity.authorize_productive_owner()
                OWNER TO agro_identity_owner;
            REVOKE ALL ON FUNCTION identity.authorize_productive_owner() FROM PUBLIC;
            REVOKE ALL ON FUNCTION identity.authorize_productive_owner()
                FROM agro_identity_app, agro_identity_job, agro_identity_discovery;
            GRANT USAGE ON SCHEMA identity TO agro_productive_app;
            GRANT EXECUTE ON FUNCTION identity.authorize_productive_owner()
                TO agro_productive_app;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            DO $ephemeral_only$
            BEGIN
                IF current_database() !~ '^agro_identity_[0-9a-f]{32}$' THEN
                    RAISE EXCEPTION
                        'The Productive Core authorization port down migration is allowed only on an ephemeral test database';
                END IF;
            END
            $ephemeral_only$;

            DROP FUNCTION IF EXISTS identity.authorize_productive_owner();
            REVOKE USAGE ON SCHEMA identity FROM agro_productive_app;
            """);
    }
}
