using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgropecuarIA.Identity.Infrastructure.Migrations;

[DbContext(typeof(IdentityDbContext))]
[Migration("20260818213000_AddOwnSessionManagement")]
public sealed class AddOwnSessionManagement : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            ALTER TABLE identity.sessions
                DROP CONSTRAINT "CK_sessions_StrongAuthentication";
            ALTER TABLE identity.sessions
                ADD CONSTRAINT "CK_sessions_StrongAuthentication"
                CHECK (
                    ("StrongAuthenticatedAtUtc" IS NULL
                     AND "StrongAuthenticationPurpose" IS NULL)
                    OR
                    ("StrongAuthenticatedAtUtc" IS NOT NULL
                     AND "StrongAuthenticationPurpose" IS NOT NULL
                     AND "StrongAuthenticationPurpose" IN (
                         'manage_authentication_methods',
                         'manage_organization_owners',
                         'manage_sessions'))
                );
            ALTER TABLE identity.step_up_attempts
                DROP CONSTRAINT "CK_step_up_attempts_Purpose";
            ALTER TABLE identity.step_up_attempts
                ADD CONSTRAINT "CK_step_up_attempts_Purpose"
                CHECK (
                    "Purpose" IN (
                        'manage_authentication_methods',
                        'manage_organization_owners',
                        'manage_sessions')
                );

            GRANT SELECT ("AuthenticatedAtUtc")
                ON TABLE identity.sessions TO agro_identity_owner;
            GRANT UPDATE ("RevokedAtUtc", "Version")
                ON TABLE identity.sessions TO agro_identity_owner;
            CREATE POLICY sessions_owner_session_management_update
                ON identity.sessions
                FOR UPDATE TO agro_identity_owner
                USING (true) WITH CHECK (true);

            CREATE FUNCTION identity.count_own_active_sessions()
            RETURNS bigint
            LANGUAGE plpgsql
            STABLE
            SECURITY DEFINER
            SET search_path = pg_catalog
            AS $count_own_active_sessions$
            DECLARE
                actor_id uuid;
                current_session_id uuid;
                authorization_version uuid;
                active_count bigint;
            BEGIN
                IF nullif(current_setting('app.current_scope_kind', true), '') <> 'platform' THEN
                    RETURN 0;
                END IF;
                BEGIN
                    actor_id := nullif(current_setting('app.current_actor_id', true), '')::uuid;
                    current_session_id := nullif(
                        current_setting('app.current_session_id', true), '')::uuid;
                    authorization_version := nullif(
                        current_setting('app.current_authorization_version', true), '')::uuid;
                EXCEPTION WHEN invalid_text_representation OR null_value_not_allowed THEN
                    RETURN 0;
                END;
                IF actor_id IS NULL OR current_session_id IS NULL
                   OR authorization_version IS NULL
                   OR NOT EXISTS (
                        SELECT 1
                        FROM identity.sessions AS actor_session
                        WHERE actor_session."Id" = current_session_id
                          AND actor_session."UserId" = actor_id
                          AND actor_session."Version" = authorization_version
                          AND actor_session."RevokedAtUtc" IS NULL
                          AND actor_session."ExpiresAtUtc" > transaction_timestamp()
                          AND actor_session."IsAuthenticationAssuranceVerified") THEN
                    RETURN 0;
                END IF;
                SELECT count(*)
                INTO active_count
                FROM identity.sessions AS owned_session
                WHERE owned_session."UserId" = actor_id
                  AND owned_session."RevokedAtUtc" IS NULL
                  AND owned_session."ExpiresAtUtc" > transaction_timestamp();
                RETURN active_count;
            END
            $count_own_active_sessions$;

            CREATE FUNCTION identity.list_own_active_sessions(
                p_offset integer,
                p_limit integer)
            RETURNS TABLE (
                session_id uuid,
                authenticated_at_utc timestamptz,
                expires_at_utc timestamptz,
                version uuid,
                is_current boolean,
                total_count bigint)
            LANGUAGE plpgsql
            STABLE
            SECURITY DEFINER
            SET search_path = pg_catalog
            AS $list_own_active_sessions$
            DECLARE
                actor_id uuid;
                current_session_id uuid;
                authorization_version uuid;
            BEGIN
                IF p_offset IS NULL OR p_offset < 0
                   OR p_limit IS NULL OR p_limit < 1 OR p_limit > 50
                   OR nullif(current_setting('app.current_scope_kind', true), '') <> 'platform' THEN
                    RETURN;
                END IF;
                BEGIN
                    actor_id := nullif(current_setting('app.current_actor_id', true), '')::uuid;
                    current_session_id := nullif(
                        current_setting('app.current_session_id', true), '')::uuid;
                    authorization_version := nullif(
                        current_setting('app.current_authorization_version', true), '')::uuid;
                EXCEPTION WHEN invalid_text_representation OR null_value_not_allowed THEN
                    RETURN;
                END;
                IF actor_id IS NULL OR current_session_id IS NULL
                   OR authorization_version IS NULL
                   OR NOT EXISTS (
                        SELECT 1
                        FROM identity.sessions AS actor_session
                        WHERE actor_session."Id" = current_session_id
                          AND actor_session."UserId" = actor_id
                          AND actor_session."Version" = authorization_version
                          AND actor_session."RevokedAtUtc" IS NULL
                          AND actor_session."ExpiresAtUtc" > transaction_timestamp()
                          AND actor_session."IsAuthenticationAssuranceVerified") THEN
                    RETURN;
                END IF;
                RETURN QUERY
                SELECT owned_session."Id",
                       owned_session."AuthenticatedAtUtc",
                       owned_session."ExpiresAtUtc",
                       owned_session."Version",
                       owned_session."Id" = current_session_id,
                       count(*) OVER ()
                FROM identity.sessions AS owned_session
                WHERE owned_session."UserId" = actor_id
                  AND owned_session."RevokedAtUtc" IS NULL
                  AND owned_session."ExpiresAtUtc" > transaction_timestamp()
                ORDER BY owned_session."AuthenticatedAtUtc" DESC,
                         owned_session."Id" ASC
                OFFSET p_offset
                LIMIT p_limit;
            END
            $list_own_active_sessions$;

            CREATE FUNCTION identity.revoke_other_own_session(
                p_target_session_id uuid,
                p_expected_version uuid,
                p_strong_not_before timestamptz,
                p_new_version uuid)
            RETURNS TABLE (
                outcome text,
                session_id uuid,
                revoked_at_utc timestamptz,
                version uuid)
            LANGUAGE plpgsql
            VOLATILE
            SECURITY DEFINER
            SET search_path = pg_catalog
            AS $revoke_other_own_session$
            DECLARE
                actor_id uuid;
                current_session_id uuid;
                authorization_version uuid;
                revoked_at timestamptz := statement_timestamp();
                target_id uuid;
                target_expires_at timestamptz;
                target_revoked_at timestamptz;
                target_version uuid;
            BEGIN
                outcome := 'not_available';
                IF p_target_session_id IS NULL OR p_expected_version IS NULL
                   OR p_strong_not_before IS NULL OR p_new_version IS NULL
                   OR p_expected_version = '00000000-0000-0000-0000-000000000000'::uuid
                   OR p_new_version = '00000000-0000-0000-0000-000000000000'::uuid
                   OR p_expected_version = p_new_version
                   OR p_strong_not_before > revoked_at
                   OR nullif(current_setting('app.current_scope_kind', true), '') <> 'platform' THEN
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
                   OR authorization_version IS NULL
                   OR NOT EXISTS (
                        SELECT 1
                        FROM identity.sessions AS actor_session
                        WHERE actor_session."Id" = current_session_id
                          AND actor_session."UserId" = actor_id
                          AND actor_session."Version" = authorization_version
                          AND actor_session."RevokedAtUtc" IS NULL
                          AND actor_session."ExpiresAtUtc" > revoked_at
                          AND actor_session."IsAuthenticationAssuranceVerified") THEN
                    RETURN NEXT;
                    RETURN;
                END IF;
                IF NOT EXISTS (
                    SELECT 1
                    FROM identity.sessions AS actor_session
                    WHERE actor_session."Id" = current_session_id
                      AND actor_session."UserId" = actor_id
                      AND actor_session."Version" = authorization_version
                      AND actor_session."StrongAuthenticatedAtUtc" IS NOT NULL
                      AND actor_session."StrongAuthenticatedAtUtc" >= p_strong_not_before
                      AND actor_session."StrongAuthenticatedAtUtc" <= revoked_at
                      AND actor_session."StrongAuthenticationPurpose" = 'manage_sessions') THEN
                    outcome := 'strong_authentication_required';
                    RETURN NEXT;
                    RETURN;
                END IF;
                IF p_target_session_id = current_session_id THEN
                    outcome := 'current_session';
                    RETURN NEXT;
                    RETURN;
                END IF;
                SELECT owned_session."Id", owned_session."ExpiresAtUtc",
                       owned_session."RevokedAtUtc", owned_session."Version"
                INTO target_id, target_expires_at, target_revoked_at, target_version
                FROM identity.sessions AS owned_session
                WHERE owned_session."Id" = p_target_session_id
                  AND owned_session."UserId" = actor_id
                FOR UPDATE;
                IF NOT FOUND THEN
                    RETURN NEXT;
                    RETURN;
                END IF;
                IF target_revoked_at IS NOT NULL THEN
                    outcome := 'already_revoked';
                    session_id := target_id;
                    revoked_at_utc := target_revoked_at;
                    version := target_version;
                    RETURN NEXT;
                    RETURN;
                END IF;
                IF target_expires_at <= revoked_at THEN
                    RETURN NEXT;
                    RETURN;
                END IF;
                IF target_version <> p_expected_version THEN
                    outcome := 'version_mismatch';
                    RETURN NEXT;
                    RETURN;
                END IF;
                UPDATE identity.sessions AS revoked_session
                SET "RevokedAtUtc" = revoked_at,
                    "Version" = p_new_version
                WHERE revoked_session."Id" = target_id
                  AND revoked_session."UserId" = actor_id
                  AND revoked_session."RevokedAtUtc" IS NULL
                  AND revoked_session."ExpiresAtUtc" > revoked_at
                  AND revoked_session."Version" = p_expected_version
                RETURNING revoked_session."Id", revoked_session."RevokedAtUtc",
                          revoked_session."Version"
                INTO session_id, revoked_at_utc, version;
                IF NOT FOUND THEN
                    RETURN NEXT;
                    RETURN;
                END IF;
                outcome := 'revoked';
                RETURN NEXT;
            END
            $revoke_other_own_session$;

            ALTER FUNCTION identity.count_own_active_sessions()
                OWNER TO agro_identity_owner;
            ALTER FUNCTION identity.list_own_active_sessions(integer, integer)
                OWNER TO agro_identity_owner;
            ALTER FUNCTION identity.revoke_other_own_session(
                uuid, uuid, timestamptz, uuid)
                OWNER TO agro_identity_owner;
            REVOKE ALL ON FUNCTION identity.count_own_active_sessions() FROM PUBLIC;
            REVOKE ALL ON FUNCTION identity.list_own_active_sessions(integer, integer)
                FROM PUBLIC;
            REVOKE ALL ON FUNCTION identity.revoke_other_own_session(
                uuid, uuid, timestamptz, uuid) FROM PUBLIC;
            REVOKE ALL ON FUNCTION identity.count_own_active_sessions()
                FROM agro_identity_app, agro_identity_job, agro_identity_discovery;
            REVOKE ALL ON FUNCTION identity.list_own_active_sessions(integer, integer)
                FROM agro_identity_app, agro_identity_job, agro_identity_discovery;
            REVOKE ALL ON FUNCTION identity.revoke_other_own_session(
                uuid, uuid, timestamptz, uuid)
                FROM agro_identity_app, agro_identity_job, agro_identity_discovery;
            GRANT EXECUTE ON FUNCTION identity.count_own_active_sessions()
                TO agro_identity_app;
            GRANT EXECUTE ON FUNCTION identity.list_own_active_sessions(integer, integer)
                TO agro_identity_app;
            GRANT EXECUTE ON FUNCTION identity.revoke_other_own_session(
                uuid, uuid, timestamptz, uuid)
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
                        'The own-session management down migration is allowed only on an ephemeral test database';
                END IF;
            END
            $ephemeral_only$;

            DROP FUNCTION IF EXISTS identity.revoke_other_own_session(
                uuid, uuid, timestamptz, uuid);
            DROP FUNCTION IF EXISTS identity.list_own_active_sessions(integer, integer);
            DROP FUNCTION IF EXISTS identity.count_own_active_sessions();

            DROP POLICY IF EXISTS sessions_owner_session_management_update
                ON identity.sessions;
            REVOKE UPDATE ("RevokedAtUtc", "Version")
                ON TABLE identity.sessions FROM agro_identity_owner;
            REVOKE SELECT ("AuthenticatedAtUtc")
                ON TABLE identity.sessions FROM agro_identity_owner;

            ALTER TABLE identity.sessions
                DROP CONSTRAINT "CK_sessions_StrongAuthentication";
            ALTER TABLE identity.sessions
                ADD CONSTRAINT "CK_sessions_StrongAuthentication"
                CHECK (
                    ("StrongAuthenticatedAtUtc" IS NULL
                     AND "StrongAuthenticationPurpose" IS NULL)
                    OR
                    ("StrongAuthenticatedAtUtc" IS NOT NULL
                     AND "StrongAuthenticationPurpose" IS NOT NULL
                     AND "StrongAuthenticationPurpose" IN (
                         'manage_authentication_methods',
                         'manage_organization_owners'))
                );
            ALTER TABLE identity.step_up_attempts
                DROP CONSTRAINT "CK_step_up_attempts_Purpose";
            ALTER TABLE identity.step_up_attempts
                ADD CONSTRAINT "CK_step_up_attempts_Purpose"
                CHECK (
                    "Purpose" IN (
                        'manage_authentication_methods',
                        'manage_organization_owners')
                );
            """);
    }
}
