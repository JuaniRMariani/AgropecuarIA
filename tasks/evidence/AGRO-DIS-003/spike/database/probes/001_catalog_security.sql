\set ON_ERROR_STOP on

DO $catalog$
DECLARE
    role_name text;
    table_name text;
    role_row record;
    relation_row record;
    policy_row record;
BEGIN
    FOREACH role_name IN ARRAY ARRAY['agro_schema_owner', 'agro_app', 'agro_job']
    LOOP
        SELECT * INTO role_row FROM pg_roles WHERE rolname = role_name;
        IF NOT FOUND THEN
            RAISE EXCEPTION 'Missing expected role: %', role_name;
        END IF;

        IF role_row.rolsuper
            OR role_row.rolcreatedb
            OR role_row.rolcreaterole
            OR role_row.rolinherit
            OR role_row.rolreplication
            OR role_row.rolbypassrls THEN
            RAISE EXCEPTION 'Role % has an unsafe attribute', role_name;
        END IF;

        IF role_name = 'agro_schema_owner' AND role_row.rolcanlogin THEN
            RAISE EXCEPTION 'Schema owner must be NOLOGIN';
        END IF;

        IF role_name <> 'agro_schema_owner' AND NOT role_row.rolcanlogin THEN
            RAISE EXCEPTION 'Runtime role % must be LOGIN in this isolated spike', role_name;
        END IF;
    END LOOP;

    IF EXISTS (
        SELECT 1
        FROM pg_auth_members membership
        JOIN pg_roles granted_role ON granted_role.oid = membership.roleid
        JOIN pg_roles member_role ON member_role.oid = membership.member
        WHERE granted_role.rolname = 'agro_schema_owner'
          AND member_role.rolname IN ('agro_app', 'agro_job')
    ) THEN
        RAISE EXCEPTION 'A runtime role is a member of agro_schema_owner';
    END IF;

    IF NOT EXISTS (
        SELECT 1
        FROM pg_namespace namespace
        JOIN pg_roles owner_role ON owner_role.oid = namespace.nspowner
        WHERE namespace.nspname = 'identity_spike'
          AND owner_role.rolname = 'agro_schema_owner'
    ) THEN
        RAISE EXCEPTION 'identity_spike schema owner is incorrect';
    END IF;

    FOREACH table_name IN ARRAY ARRAY[
        'organization', 'platform_user', 'membership', 'tenant_record', 'audit_event'
    ]
    LOOP
        SELECT
            relation.relrowsecurity,
            relation.relforcerowsecurity,
            owner_role.rolname AS owner_name
        INTO relation_row
        FROM pg_class relation
        JOIN pg_namespace namespace ON namespace.oid = relation.relnamespace
        JOIN pg_roles owner_role ON owner_role.oid = relation.relowner
        WHERE namespace.nspname = 'identity_spike'
          AND relation.relname = table_name
          AND relation.relkind = 'r';

        IF NOT FOUND THEN
            RAISE EXCEPTION 'Missing expected table: identity_spike.%', table_name;
        END IF;

        IF relation_row.owner_name <> 'agro_schema_owner' THEN
            RAISE EXCEPTION 'Unexpected owner % for table %', relation_row.owner_name, table_name;
        END IF;

        IF table_name = 'platform_user' THEN
            IF relation_row.relrowsecurity OR relation_row.relforcerowsecurity THEN
                RAISE EXCEPTION 'Global platform_user must not masquerade as a tenant table';
            END IF;
        ELSIF NOT relation_row.relrowsecurity OR NOT relation_row.relforcerowsecurity THEN
            RAISE EXCEPTION 'Tenant table % must have ENABLE and FORCE RLS', table_name;
        END IF;
    END LOOP;

    FOR policy_row IN
        SELECT tablename, roles, qual, with_check
        FROM pg_policies
        WHERE schemaname = 'identity_spike'
          AND tablename IN ('organization', 'membership', 'tenant_record', 'audit_event')
    LOOP
        IF NOT policy_row.roles @> ARRAY['agro_schema_owner', 'agro_app', 'agro_job']::name[]
            OR policy_row.qual IS NULL
            OR policy_row.with_check IS NULL
            OR position('current_setting' IN policy_row.qual) = 0
            OR position('current_setting' IN policy_row.with_check) = 0 THEN
            RAISE EXCEPTION 'Incomplete tenant policy on %', policy_row.tablename;
        END IF;
    END LOOP;

    IF (
        SELECT count(*)
        FROM pg_policies
        WHERE schemaname = 'identity_spike'
          AND tablename IN ('organization', 'membership', 'tenant_record', 'audit_event')
    ) <> 4 THEN
        RAISE EXCEPTION 'Expected exactly one explicit tenant policy on each tenant table';
    END IF;

    IF has_table_privilege('agro_app', 'identity_spike.platform_user', 'SELECT')
        OR has_table_privilege('agro_job', 'identity_spike.platform_user', 'SELECT') THEN
        RAISE EXCEPTION 'Runtime roles must not scan the global identity table';
    END IF;

    IF has_table_privilege('agro_app', 'identity_spike.audit_event', 'UPDATE')
        OR has_table_privilege('agro_app', 'identity_spike.audit_event', 'DELETE')
        OR has_table_privilege('agro_job', 'identity_spike.audit_event', 'UPDATE')
        OR has_table_privilege('agro_job', 'identity_spike.audit_event', 'DELETE') THEN
        RAISE EXCEPTION 'Audit events must be append-only for runtime roles';
    END IF;

    IF NOT has_table_privilege('agro_app', 'identity_spike.tenant_record', 'SELECT')
        OR NOT has_table_privilege('agro_app', 'identity_spike.tenant_record', 'INSERT')
        OR NOT has_table_privilege('agro_app', 'identity_spike.tenant_record', 'UPDATE')
        OR NOT has_table_privilege('agro_app', 'identity_spike.tenant_record', 'DELETE')
        OR NOT has_table_privilege('agro_job', 'identity_spike.tenant_record', 'SELECT')
        OR NOT has_table_privilege('agro_job', 'identity_spike.tenant_record', 'INSERT')
        OR NOT has_table_privilege('agro_job', 'identity_spike.tenant_record', 'UPDATE')
        OR NOT has_table_privilege('agro_job', 'identity_spike.tenant_record', 'DELETE') THEN
        RAISE EXCEPTION 'Tenant record runtime grants are incomplete';
    END IF;

    IF EXISTS (
        SELECT 1
        FROM information_schema.columns AS tenant_column
        WHERE tenant_column.table_schema = 'identity_spike'
          AND tenant_column.table_name IN ('organization', 'membership', 'tenant_record', 'audit_event')
          AND tenant_column.column_name IN ('id', 'organization_id')
          AND tenant_column.is_nullable = 'YES'
    ) THEN
        RAISE EXCEPTION 'A tenant identity column is nullable';
    END IF;
END
$catalog$;

SELECT 'catalog-security-pass' AS probe_result;
