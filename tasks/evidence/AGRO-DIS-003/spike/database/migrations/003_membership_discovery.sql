\set ON_ERROR_STOP on

-- R0 spike only. Membership discovery is intentionally isolated from the
-- tenant runtime roles: the caller can read only its own active memberships
-- and the minimum organization labels required by the effective-context v1
-- contract.

\getenv agro_discovery_password AGRO_DISCOVERY_PASSWORD

BEGIN;

DO $role$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_roles WHERE rolname = 'agro_membership_discovery'
    ) THEN
        CREATE ROLE agro_membership_discovery
            LOGIN
            NOSUPERUSER
            NOCREATEDB
            NOCREATEROLE
            NOINHERIT
            NOREPLICATION
            NOBYPASSRLS;
    END IF;
END
$role$;

ALTER ROLE agro_membership_discovery
    LOGIN NOSUPERUSER NOCREATEDB NOCREATEROLE NOINHERIT NOREPLICATION NOBYPASSRLS
    PASSWORD :'agro_discovery_password';

-- A rerun must not retain an accidental path to a schema/table owner.
DO $memberships$
DECLARE
    granted_role_name text;
BEGIN
    FOR granted_role_name IN
        SELECT granted_role.rolname
        FROM pg_auth_members membership
        JOIN pg_roles granted_role ON granted_role.oid = membership.roleid
        JOIN pg_roles member_role ON member_role.oid = membership.member
        WHERE member_role.rolname = 'agro_membership_discovery'
    LOOP
        EXECUTE format(
            'REVOKE %I FROM agro_membership_discovery',
            granted_role_name);
    END LOOP;
END
$memberships$;

REVOKE ALL ON SCHEMA identity_spike FROM agro_membership_discovery;
GRANT USAGE ON SCHEMA identity_spike TO agro_membership_discovery;

SET LOCAL ROLE agro_schema_owner;

ALTER TABLE identity_spike.membership
    ADD COLUMN IF NOT EXISTS id uuid;

-- This also makes an upgrade from the earlier disposable fixture deterministic
-- enough to enforce NOT NULL. Fixtures replace their own IDs with stable values.
UPDATE identity_spike.membership
SET id = gen_random_uuid()
WHERE id IS NULL;

ALTER TABLE identity_spike.membership
    ALTER COLUMN id SET NOT NULL;

DO $constraint$
DECLARE
    membership_id_attnum smallint;
BEGIN
    SELECT attribute.attnum
    INTO membership_id_attnum
    FROM pg_attribute attribute
    WHERE attribute.attrelid = 'identity_spike.membership'::regclass
      AND attribute.attname = 'id'
      AND NOT attribute.attisdropped;

    IF membership_id_attnum IS NULL THEN
        RAISE EXCEPTION 'Missing identity_spike.membership.id';
    END IF;

    IF EXISTS (
        SELECT 1
        FROM pg_constraint
        WHERE conrelid = 'identity_spike.membership'::regclass
          AND conname = 'uq_membership_id'
          AND (
              contype <> 'u'
              OR conkey <> ARRAY[membership_id_attnum]::smallint[]
          )
    ) THEN
        ALTER TABLE identity_spike.membership
            DROP CONSTRAINT uq_membership_id;
    END IF;

    IF NOT EXISTS (
        SELECT 1
        FROM pg_constraint
        WHERE conrelid = 'identity_spike.membership'::regclass
          AND conname = 'uq_membership_id'
          AND contype = 'u'
          AND conkey = ARRAY[membership_id_attnum]::smallint[]
    ) THEN
        ALTER TABLE identity_spike.membership
            ADD CONSTRAINT uq_membership_id UNIQUE (id);
    END IF;
END
$constraint$;

DROP POLICY IF EXISTS membership_actor_discovery
    ON identity_spike.membership;
CREATE POLICY membership_actor_discovery
    ON identity_spike.membership
    FOR SELECT
    TO agro_membership_discovery
    USING (
        is_active
        AND platform_user_id =
            nullif(current_setting('app.current_actor_id', true), '')::uuid
    );

DROP POLICY IF EXISTS organization_actor_discovery
    ON identity_spike.organization;
CREATE POLICY organization_actor_discovery
    ON identity_spike.organization
    FOR SELECT
    TO agro_membership_discovery
    USING (
        is_active
        AND nullif(current_setting('app.current_actor_id', true), '') IS NOT NULL
        AND EXISTS (
            SELECT 1
            FROM identity_spike.membership actor_membership
            WHERE actor_membership.organization_id = organization.id
              AND actor_membership.is_active
        )
    );

RESET ROLE;

REVOKE ALL ON ALL TABLES IN SCHEMA identity_spike
    FROM agro_membership_discovery;
GRANT SELECT (id, organization_id, permission_set, is_active, security_version)
    ON identity_spike.membership
    TO agro_membership_discovery;
GRANT SELECT (id, display_name, is_active)
    ON identity_spike.organization
    TO agro_membership_discovery;

COMMIT;
