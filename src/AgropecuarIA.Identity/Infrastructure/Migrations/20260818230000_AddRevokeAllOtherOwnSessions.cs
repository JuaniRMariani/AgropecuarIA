using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgropecuarIA.Identity.Infrastructure.Migrations;

[DbContext(typeof(IdentityDbContext))]
[Migration("20260818230000_AddRevokeAllOtherOwnSessions")]
public sealed class AddRevokeAllOtherOwnSessions : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            CREATE OR REPLACE FUNCTION identity.revoke_other_own_session(
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
                   OR authorization_version IS NULL THEN
                    RETURN NEXT;
                    RETURN;
                END IF;

                PERFORM pg_advisory_xact_lock(hashtextextended(actor_id::text, 0));

                PERFORM 1
                FROM identity.sessions AS actor_session
                WHERE actor_session."Id" = current_session_id
                  AND actor_session."UserId" = actor_id
                  AND actor_session."Version" = authorization_version
                  AND actor_session."RevokedAtUtc" IS NULL
                  AND actor_session."ExpiresAtUtc" > revoked_at
                  AND actor_session."IsAuthenticationAssuranceVerified"
                FOR UPDATE;
                IF NOT FOUND THEN
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

            CREATE FUNCTION identity.revoke_all_other_own_sessions(
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
            AS $revoke_all_other_own_sessions$
            DECLARE
                actor_id uuid;
                current_session_id uuid;
                authorization_version uuid;
                revoked_at timestamptz := statement_timestamp();
                revoked_count integer;
            BEGIN
                outcome := 'not_available';
                IF p_strong_not_before IS NULL
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
                   OR authorization_version IS NULL THEN
                    RETURN NEXT;
                    RETURN;
                END IF;

                PERFORM pg_advisory_xact_lock(hashtextextended(actor_id::text, 0));

                PERFORM 1
                FROM identity.sessions AS actor_session
                WHERE actor_session."Id" = current_session_id
                  AND actor_session."UserId" = actor_id
                  AND actor_session."Version" = authorization_version
                  AND actor_session."RevokedAtUtc" IS NULL
                  AND actor_session."ExpiresAtUtc" > revoked_at
                  AND actor_session."IsAuthenticationAssuranceVerified"
                FOR UPDATE;
                IF NOT FOUND THEN
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

                RETURN QUERY
                WITH targets AS MATERIALIZED (
                    SELECT owned_session."Id"
                    FROM identity.sessions AS owned_session
                    WHERE owned_session."UserId" = actor_id
                      AND owned_session."Id" <> current_session_id
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
                    outcome := 'no_sessions';
                    session_id := NULL;
                    revoked_at_utc := NULL;
                    version := NULL;
                    RETURN NEXT;
                END IF;
            END
            $revoke_all_other_own_sessions$;

            ALTER FUNCTION identity.revoke_all_other_own_sessions(timestamptz)
                OWNER TO agro_identity_owner;
            REVOKE ALL ON FUNCTION
                identity.revoke_all_other_own_sessions(timestamptz) FROM PUBLIC;
            REVOKE ALL ON FUNCTION
                identity.revoke_all_other_own_sessions(timestamptz)
                FROM agro_identity_app, agro_identity_job, agro_identity_discovery;
            GRANT EXECUTE ON FUNCTION
                identity.revoke_all_other_own_sessions(timestamptz)
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
                        'The revoke-all sessions down migration is allowed only on an ephemeral test database';
                END IF;
            END
            $ephemeral_only$;

            DROP FUNCTION IF EXISTS
                identity.revoke_all_other_own_sessions(timestamptz);

            CREATE OR REPLACE FUNCTION identity.revoke_other_own_session(
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
            """);
    }
}
