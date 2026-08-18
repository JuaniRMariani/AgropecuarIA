using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgropecuarIA.Identity.Infrastructure.Migrations;

[DbContext(typeof(IdentityDbContext))]
[Migration("20260818234500_AddRevokeAllOwnSessions")]
public sealed class AddRevokeAllOwnSessions : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            CREATE FUNCTION identity.revoke_all_own_sessions(
                p_strong_not_before timestamptz)
            RETURNS TABLE (
                outcome text,
                session_id uuid,
                revoked_at_utc timestamptz,
                version uuid)
            LANGUAGE plpgsql
            VOLATILE
            SECURITY DEFINER
            SET search_path = pg_catalog
            AS $revoke_all_own_sessions$
            DECLARE
                actor_id uuid;
                current_session_id uuid;
                authorization_version uuid;
                revoked_at timestamptz := statement_timestamp();
                current_version uuid;
                current_revoked_at timestamptz;
                current_expires_at timestamptz;
                current_assurance_verified boolean;
                current_strong_authenticated_at timestamptz;
                current_strong_purpose text;
                revoked_count integer;
            BEGIN
                outcome := 'not_available';
                IF p_strong_not_before IS NULL
                   OR p_strong_not_before > revoked_at
                   OR nullif(current_setting('app.current_scope_kind', true), '')
                        IS DISTINCT FROM 'platform' THEN
                    RETURN NEXT;
                    RETURN;
                END IF;
                BEGIN
                    actor_id := nullif(current_setting('app.current_actor_id', true), '')::uuid;
                    current_session_id := nullif(
                        current_setting('app.current_session_id', true), '')::uuid;
                    authorization_version := nullif(
                        current_setting('app.current_authorization_version', true), '')::uuid;
                EXCEPTION WHEN invalid_text_representation OR null_value_not_allowed THEN
                    RETURN NEXT;
                    RETURN;
                END;
                IF actor_id IS NULL OR current_session_id IS NULL
                   OR authorization_version IS NULL THEN
                    RETURN NEXT;
                    RETURN;
                END IF;

                PERFORM pg_advisory_xact_lock(hashtextextended(actor_id::text, 0));

                SELECT actor_session."Version",
                       actor_session."RevokedAtUtc",
                       actor_session."ExpiresAtUtc",
                       actor_session."IsAuthenticationAssuranceVerified",
                       actor_session."StrongAuthenticatedAtUtc",
                       actor_session."StrongAuthenticationPurpose"
                INTO current_version,
                     current_revoked_at,
                     current_expires_at,
                     current_assurance_verified,
                     current_strong_authenticated_at,
                     current_strong_purpose
                FROM identity.sessions AS actor_session
                WHERE actor_session."Id" = current_session_id
                  AND actor_session."UserId" = actor_id
                FOR UPDATE;
                IF NOT FOUND THEN
                    RETURN NEXT;
                    RETURN;
                END IF;
                IF current_revoked_at IS NOT NULL THEN
                    IF NOT EXISTS (
                        SELECT 1
                        FROM identity.sessions AS owned_session
                        WHERE owned_session."UserId" = actor_id
                          AND owned_session."RevokedAtUtc" IS NULL
                          AND owned_session."ExpiresAtUtc" > revoked_at) THEN
                        outcome := 'no_sessions';
                    END IF;
                    RETURN NEXT;
                    RETURN;
                END IF;
                IF current_version <> authorization_version
                   OR current_expires_at <= revoked_at
                   OR NOT current_assurance_verified THEN
                    RETURN NEXT;
                    RETURN;
                END IF;
                IF current_strong_authenticated_at IS NULL
                   OR current_strong_authenticated_at < p_strong_not_before
                   OR current_strong_authenticated_at > revoked_at
                   OR current_strong_purpose IS DISTINCT FROM 'manage_sessions' THEN
                    outcome := 'strong_authentication_required';
                    RETURN NEXT;
                    RETURN;
                END IF;

                RETURN QUERY
                WITH targets AS MATERIALIZED (
                    SELECT owned_session."Id"
                    FROM identity.sessions AS owned_session
                    WHERE owned_session."UserId" = actor_id
                      AND owned_session."RevokedAtUtc" IS NULL
                      AND owned_session."ExpiresAtUtc" > revoked_at
                    ORDER BY owned_session."Id"
                    FOR UPDATE
                ),
                revoked AS (
                    UPDATE identity.sessions AS revoked_session
                    SET "RevokedAtUtc" = revoked_at,
                        "Version" = gen_random_uuid()
                    FROM targets
                    WHERE revoked_session."Id" = targets."Id"
                    RETURNING revoked_session."Id",
                              revoked_session."RevokedAtUtc",
                              revoked_session."Version"
                )
                SELECT 'revoked'::text,
                       revoked."Id",
                       revoked."RevokedAtUtc",
                       revoked."Version"
                FROM revoked
                ORDER BY revoked."Id";
                GET DIAGNOSTICS revoked_count = ROW_COUNT;
                IF revoked_count = 0 THEN
                    outcome := 'not_available';
                    session_id := NULL;
                    revoked_at_utc := NULL;
                    version := NULL;
                    RETURN NEXT;
                END IF;
            END
            $revoke_all_own_sessions$;

            CREATE FUNCTION identity.revoke_current_own_session()
            RETURNS TABLE (
                outcome text,
                session_id uuid,
                revoked_at_utc timestamptz,
                version uuid)
            LANGUAGE plpgsql
            VOLATILE
            SECURITY DEFINER
            SET search_path = pg_catalog
            AS $revoke_current_own_session$
            DECLARE
                actor_id uuid;
                current_session_id uuid;
                authorization_version uuid;
                revoked_at timestamptz := statement_timestamp();
                current_version uuid;
                current_revoked_at timestamptz;
                current_expires_at timestamptz;
                current_assurance_verified boolean;
            BEGIN
                outcome := 'not_available';
                IF nullif(current_setting('app.current_scope_kind', true), '')
                    IS DISTINCT FROM 'platform' THEN
                    RETURN NEXT;
                    RETURN;
                END IF;
                BEGIN
                    actor_id := nullif(current_setting('app.current_actor_id', true), '')::uuid;
                    current_session_id := nullif(
                        current_setting('app.current_session_id', true), '')::uuid;
                    authorization_version := nullif(
                        current_setting('app.current_authorization_version', true), '')::uuid;
                EXCEPTION WHEN invalid_text_representation OR null_value_not_allowed THEN
                    RETURN NEXT;
                    RETURN;
                END;
                IF actor_id IS NULL OR current_session_id IS NULL
                   OR authorization_version IS NULL THEN
                    RETURN NEXT;
                    RETURN;
                END IF;

                PERFORM pg_advisory_xact_lock(hashtextextended(actor_id::text, 0));

                SELECT actor_session."Version",
                       actor_session."RevokedAtUtc",
                       actor_session."ExpiresAtUtc",
                       actor_session."IsAuthenticationAssuranceVerified"
                INTO current_version,
                     current_revoked_at,
                     current_expires_at,
                     current_assurance_verified
                FROM identity.sessions AS actor_session
                WHERE actor_session."Id" = current_session_id
                  AND actor_session."UserId" = actor_id
                FOR UPDATE;
                IF NOT FOUND THEN
                    RETURN NEXT;
                    RETURN;
                END IF;
                IF current_revoked_at IS NOT NULL THEN
                    outcome := 'already_revoked';
                    session_id := current_session_id;
                    revoked_at_utc := current_revoked_at;
                    version := current_version;
                    RETURN NEXT;
                    RETURN;
                END IF;
                IF current_version <> authorization_version
                   OR current_expires_at <= revoked_at
                   OR NOT current_assurance_verified THEN
                    RETURN NEXT;
                    RETURN;
                END IF;

                UPDATE identity.sessions AS revoked_session
                SET "RevokedAtUtc" = revoked_at,
                    "Version" = gen_random_uuid()
                WHERE revoked_session."Id" = current_session_id
                  AND revoked_session."UserId" = actor_id
                  AND revoked_session."RevokedAtUtc" IS NULL
                  AND revoked_session."ExpiresAtUtc" > revoked_at
                  AND revoked_session."Version" = authorization_version
                RETURNING revoked_session."Id",
                          revoked_session."RevokedAtUtc",
                          revoked_session."Version"
                INTO session_id, revoked_at_utc, version;
                IF NOT FOUND THEN
                    session_id := NULL;
                    revoked_at_utc := NULL;
                    version := NULL;
                    RETURN NEXT;
                    RETURN;
                END IF;
                outcome := 'revoked';
                RETURN NEXT;
            END
            $revoke_current_own_session$;

            ALTER FUNCTION identity.revoke_all_own_sessions(timestamptz)
                OWNER TO agro_identity_owner;
            ALTER FUNCTION identity.revoke_current_own_session()
                OWNER TO agro_identity_owner;
            REVOKE ALL ON FUNCTION
                identity.revoke_all_own_sessions(timestamptz) FROM PUBLIC;
            REVOKE ALL ON FUNCTION
                identity.revoke_current_own_session() FROM PUBLIC;
            REVOKE ALL ON FUNCTION
                identity.revoke_all_own_sessions(timestamptz)
                FROM agro_identity_app, agro_identity_job, agro_identity_discovery;
            REVOKE ALL ON FUNCTION
                identity.revoke_current_own_session()
                FROM agro_identity_app, agro_identity_job, agro_identity_discovery;
            GRANT EXECUTE ON FUNCTION
                identity.revoke_all_own_sessions(timestamptz)
                TO agro_identity_app;
            GRANT EXECUTE ON FUNCTION
                identity.revoke_current_own_session()
                TO agro_identity_app;
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
                        'The revoke-all-own-sessions down migration is allowed only on an ephemeral test database';
                END IF;
            END
            $ephemeral_only$;

            DROP FUNCTION IF EXISTS identity.revoke_current_own_session();
            DROP FUNCTION IF EXISTS identity.revoke_all_own_sessions(timestamptz);
            """);
    }
}
