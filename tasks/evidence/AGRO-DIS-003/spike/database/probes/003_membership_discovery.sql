\set ON_ERROR_STOP on

DO $catalog$
DECLARE
    role_row record;
    policy_count integer;
    membership_id_attnum smallint;
BEGIN
    SELECT * INTO role_row
    FROM pg_roles
    WHERE rolname = 'agro_membership_discovery';

    IF NOT FOUND THEN
        RAISE EXCEPTION 'Missing membership discovery role';
    END IF;

    IF NOT role_row.rolcanlogin
        OR role_row.rolsuper
        OR role_row.rolcreatedb
        OR role_row.rolcreaterole
        OR role_row.rolinherit
        OR role_row.rolreplication
        OR role_row.rolbypassrls THEN
        RAISE EXCEPTION 'Membership discovery role has unsafe attributes';
    END IF;

    IF EXISTS (
        SELECT 1
        FROM pg_auth_members membership
        JOIN pg_roles member_role ON member_role.oid = membership.member
        WHERE member_role.rolname = 'agro_membership_discovery'
    ) THEN
        RAISE EXCEPTION 'Membership discovery role must not inherit another role';
    END IF;

    IF EXISTS (
        SELECT 1
        FROM pg_class relation
        JOIN pg_namespace namespace ON namespace.oid = relation.relnamespace
        JOIN pg_roles owner_role ON owner_role.oid = relation.relowner
        WHERE namespace.nspname = 'identity_spike'
          AND owner_role.rolname = 'agro_membership_discovery'
    ) THEN
        RAISE EXCEPTION 'Membership discovery role must not own relations';
    END IF;

    IF NOT has_schema_privilege(
        'agro_membership_discovery', 'identity_spike', 'USAGE')
        OR has_schema_privilege(
            'agro_membership_discovery', 'identity_spike', 'CREATE') THEN
        RAISE EXCEPTION 'Membership discovery schema grants are unsafe';
    END IF;

    SELECT count(*) INTO policy_count
    FROM pg_policies
    WHERE schemaname = 'identity_spike'
      AND policyname IN (
          'membership_actor_discovery',
          'organization_actor_discovery')
      AND cmd = 'SELECT'
      AND roles @> ARRAY['agro_membership_discovery']::name[]
      AND position('app.current_actor_id' IN qual) > 0;

    IF policy_count <> 2 THEN
        RAISE EXCEPTION 'Expected two actor-scoped SELECT policies, found %', policy_count;
    END IF;

    IF EXISTS (
        SELECT 1
        FROM pg_class relation
        JOIN pg_namespace namespace ON namespace.oid = relation.relnamespace
        WHERE namespace.nspname = 'identity_spike'
          AND relation.relname IN ('membership', 'organization')
          AND (NOT relation.relrowsecurity OR NOT relation.relforcerowsecurity)
    ) THEN
        RAISE EXCEPTION 'Membership discovery tables must retain ENABLE and FORCE RLS';
    END IF;

    IF (
        SELECT count(*)
        FROM pg_policies
        WHERE schemaname = 'identity_spike'
          AND (
              (tablename = 'membership'
                  AND policyname IN (
                      'membership_tenant_isolation',
                      'membership_actor_discovery'))
              OR (tablename = 'organization'
                  AND policyname IN (
                      'organization_tenant_isolation',
                      'organization_actor_discovery'))
          )
    ) <> 4 THEN
        RAISE EXCEPTION 'Tenant and discovery RLS policies must coexist';
    END IF;

    IF NOT EXISTS (
        SELECT 1
        FROM pg_policies
        WHERE schemaname = 'identity_spike'
          AND tablename = 'membership'
          AND policyname = 'membership_actor_discovery'
          AND position('is_active' IN qual) > 0
    ) THEN
        RAISE EXCEPTION 'Membership discovery policy must exclude inactive rows';
    END IF;

    SELECT attribute.attnum
    INTO membership_id_attnum
    FROM pg_attribute attribute
    WHERE attribute.attrelid = 'identity_spike.membership'::regclass
      AND attribute.attname = 'id'
      AND NOT attribute.attisdropped;

    IF membership_id_attnum IS NULL OR NOT EXISTS (
        SELECT 1
        FROM pg_constraint
        WHERE conrelid = 'identity_spike.membership'::regclass
          AND conname = 'uq_membership_id'
          AND contype = 'u'
          AND conkey = ARRAY[membership_id_attnum]::smallint[]
    ) OR EXISTS (
        SELECT 1
        FROM information_schema.columns
        WHERE table_schema = 'identity_spike'
          AND table_name = 'membership'
          AND column_name = 'id'
          AND is_nullable = 'YES'
    ) THEN
        RAISE EXCEPTION 'Membership ID must be stable, unique and non-null';
    END IF;

    IF NOT has_column_privilege(
            'agro_membership_discovery',
            'identity_spike.membership',
            'id',
            'SELECT')
            OR NOT has_column_privilege(
                'agro_membership_discovery',
                'identity_spike.membership',
                'organization_id',
                'SELECT')
            OR NOT has_column_privilege(
                'agro_membership_discovery',
                'identity_spike.membership',
                'permission_set',
                'SELECT')
            OR NOT has_column_privilege(
                'agro_membership_discovery',
                'identity_spike.membership',
                'is_active',
                'SELECT')
            OR NOT has_column_privilege(
                'agro_membership_discovery',
                'identity_spike.membership',
                'security_version',
                'SELECT') THEN
        RAISE EXCEPTION 'Membership discovery is missing a required column grant';
    END IF;

    IF has_column_privilege(
            'agro_membership_discovery',
            'identity_spike.membership',
            'platform_user_id',
            'SELECT')
            OR has_column_privilege(
                'agro_membership_discovery',
                'identity_spike.membership',
                'created_at',
                'SELECT') THEN
        RAISE EXCEPTION 'Membership discovery can read a non-contract membership column';
    END IF;

    IF NOT has_column_privilege(
            'agro_membership_discovery',
            'identity_spike.organization',
            'id',
            'SELECT')
            OR NOT has_column_privilege(
                'agro_membership_discovery',
                'identity_spike.organization',
                'display_name',
                'SELECT')
            OR NOT has_column_privilege(
                'agro_membership_discovery',
                'identity_spike.organization',
                'is_active',
                'SELECT') THEN
        RAISE EXCEPTION 'Membership discovery is missing an organization column grant';
    END IF;

    IF has_column_privilege(
        'agro_membership_discovery',
        'identity_spike.organization',
        'created_at',
        'SELECT') THEN
        RAISE EXCEPTION 'Membership discovery can read a non-contract organization column';
    END IF;

    IF has_any_column_privilege(
        'agro_membership_discovery', 'identity_spike.platform_user', 'SELECT')
        OR has_any_column_privilege(
            'agro_membership_discovery', 'identity_spike.tenant_record', 'SELECT')
        OR has_any_column_privilege(
            'agro_membership_discovery', 'identity_spike.audit_event', 'SELECT') THEN
        RAISE EXCEPTION 'Membership discovery can read a forbidden table';
    END IF;

    IF has_table_privilege(
        'agro_membership_discovery', 'identity_spike.membership', 'INSERT')
        OR has_table_privilege(
            'agro_membership_discovery', 'identity_spike.membership', 'UPDATE')
        OR has_table_privilege(
            'agro_membership_discovery', 'identity_spike.membership', 'DELETE')
        OR has_table_privilege(
            'agro_membership_discovery', 'identity_spike.organization', 'INSERT')
        OR has_table_privilege(
            'agro_membership_discovery', 'identity_spike.organization', 'UPDATE')
        OR has_table_privilege(
            'agro_membership_discovery', 'identity_spike.organization', 'DELETE') THEN
        RAISE EXCEPTION 'Membership discovery role has write privileges';
    END IF;
END
$catalog$;

-- Missing actor context must expose neither memberships nor organizations.
BEGIN;
SET LOCAL ROLE agro_membership_discovery;
DO $missing_actor$
DECLARE
    visible_memberships integer;
    visible_organizations integer;
BEGIN
    SELECT count(*) INTO visible_memberships
    FROM identity_spike.membership;
    SELECT count(*) INTO visible_organizations
    FROM identity_spike.organization;
    IF visible_memberships <> 0 OR visible_organizations <> 0 THEN
        RAISE EXCEPTION
            'Missing actor exposed % memberships and % organizations',
            visible_memberships,
            visible_organizations;
    END IF;
END
$missing_actor$;
ROLLBACK;

-- User zero has no membership and cannot infer either organization.
BEGIN;
SET LOCAL ROLE agro_membership_discovery;
SELECT set_config(
    'app.current_actor_id',
    '10000000-0000-0000-0000-000000000004',
    true);
DO $zero$
DECLARE
    visible_memberships integer;
    visible_organizations integer;
BEGIN
    SELECT count(*) INTO visible_memberships
    FROM identity_spike.membership;
    SELECT count(*) INTO visible_organizations
    FROM identity_spike.organization;
    IF visible_memberships <> 0 OR visible_organizations <> 0 THEN
        RAISE EXCEPTION 'Zero-membership actor discovered tenant data';
    END IF;
END
$zero$;
COMMIT;

-- User A has one active membership. Its inactive B membership remains hidden.
BEGIN;
SET LOCAL ROLE agro_membership_discovery;
SELECT set_config(
    'app.current_actor_id',
    '10000000-0000-0000-0000-000000000001',
    true);
DO $one$
DECLARE
    visible_memberships integer;
    visible_organizations integer;
BEGIN
    SELECT count(*) INTO visible_memberships
    FROM identity_spike.membership
    WHERE organization_id = '00000000-0000-0000-0000-00000000000a'
      AND permission_set = 'tenant-record.read'
      AND id = 'a1111111-1111-4111-8111-111111111111';
    SELECT count(*) INTO visible_organizations
    FROM identity_spike.organization;
    IF visible_memberships <> 1 OR visible_organizations <> 1 THEN
        RAISE EXCEPTION 'One-membership actor did not discover exactly one tenant';
    END IF;
    IF EXISTS (
        SELECT 1
        FROM identity_spike.membership
        WHERE id = 'a4444444-4444-4444-8444-444444444444'
    ) THEN
        RAISE EXCEPTION 'Inactive membership is visible';
    END IF;
END
$one$;
COMMIT;

-- User B has one active tenant but no tenant-record.read permission.
BEGIN;
SET LOCAL ROLE agro_membership_discovery;
SELECT set_config(
    'app.current_actor_id',
    '10000000-0000-0000-0000-000000000002',
    true);
DO $no_permission$
DECLARE
    visible_count integer;
BEGIN
    SELECT count(*) INTO visible_count
    FROM identity_spike.membership
    WHERE organization_id = '00000000-0000-0000-0000-00000000000b'
      AND permission_set = 'identity.none';
    IF visible_count <> 1 THEN
        RAISE EXCEPTION 'No-permission membership was not represented safely';
    END IF;
END
$no_permission$;
COMMIT;

-- Shared user discovers A and B, and only the minimum contract columns can be
-- selected in a join.
BEGIN;
SET LOCAL ROLE agro_membership_discovery;
SELECT set_config(
    'app.current_actor_id',
    '10000000-0000-0000-0000-000000000003',
    true);
DO $many$
DECLARE
    visible_count integer;
BEGIN
    SELECT count(*) INTO visible_count
    FROM identity_spike.membership membership
    JOIN identity_spike.organization organization
      ON organization.id = membership.organization_id
    WHERE membership.permission_set = 'tenant-record.read'
      AND membership.is_active
      AND organization.is_active
      AND membership.security_version = 1
      AND length(organization.display_name) > 0;
    IF visible_count <> 2 THEN
        RAISE EXCEPTION 'Multi-organization actor expected 2 memberships, found %', visible_count;
    END IF;
END
$many$;

DO $forbidden_access$
BEGIN
    BEGIN
        PERFORM 1 FROM identity_spike.platform_user LIMIT 1;
        RAISE EXCEPTION 'Discovery role read platform_user';
    EXCEPTION
        WHEN insufficient_privilege THEN NULL;
    END;

    BEGIN
        PERFORM 1 FROM identity_spike.tenant_record LIMIT 1;
        RAISE EXCEPTION 'Discovery role read tenant_record';
    EXCEPTION
        WHEN insufficient_privilege THEN NULL;
    END;

    BEGIN
        UPDATE identity_spike.membership
        SET security_version = security_version + 1;
        RAISE EXCEPTION 'Discovery role updated membership';
    EXCEPTION
        WHEN insufficient_privilege THEN NULL;
    END;
END
$forbidden_access$;
COMMIT;

-- An inactive organization hides its otherwise-active memberships from the
-- adapter's exact join. The fixture change is transactional and rolls back.
BEGIN;
SET LOCAL ROLE agro_schema_owner;
SELECT set_config(
    'app.current_organization_id',
    '00000000-0000-0000-0000-00000000000b',
    true);
UPDATE identity_spike.organization
SET is_active = false
WHERE id = '00000000-0000-0000-0000-00000000000b';

RESET ROLE;
SET LOCAL ROLE agro_membership_discovery;
SELECT set_config(
    'app.current_actor_id',
    '10000000-0000-0000-0000-000000000003',
    true);
DO $inactive_organization$
DECLARE
    visible_count integer;
BEGIN
    SELECT count(*) INTO visible_count
    FROM identity_spike.membership membership
    JOIN identity_spike.organization organization
      ON organization.id = membership.organization_id
    WHERE membership.is_active
      AND organization.is_active;

    IF visible_count <> 1 THEN
        RAISE EXCEPTION
            'Inactive organization probe expected 1 visible membership, found %',
            visible_count;
    END IF;

    IF EXISTS (
        SELECT 1
        FROM identity_spike.organization
        WHERE id = '00000000-0000-0000-0000-00000000000b'
    ) THEN
        RAISE EXCEPTION 'Inactive organization remained visible to discovery';
    END IF;
END
$inactive_organization$;
ROLLBACK;

-- SET LOCAL must not survive commit on the same psql session.
BEGIN;
SET LOCAL ROLE agro_membership_discovery;
DO $after_commit$
DECLARE
    visible_count integer;
BEGIN
    SELECT count(*) INTO visible_count
    FROM identity_spike.membership;
    IF visible_count <> 0 THEN
        RAISE EXCEPTION 'Actor context leaked after commit';
    END IF;
END
$after_commit$;
ROLLBACK;

SELECT 'membership-discovery-pass' AS probe_result;
