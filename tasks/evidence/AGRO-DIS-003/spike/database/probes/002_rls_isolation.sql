\set ON_ERROR_STOP on

-- One psql session deliberately performs A -> rollback -> no context -> B ->
-- commit -> no context. This is a deterministic proxy for reuse of the same
-- physical connection; the Npgsql pool stress test remains an application test.

BEGIN;
SET LOCAL ROLE agro_app;
SELECT set_config('app.current_organization_id', '00000000-0000-0000-0000-00000000000a', true);

DO $tenant_a$
DECLARE
    visible_count integer;
    affected_count integer;
BEGIN
    SELECT count(*) INTO visible_count FROM identity_spike.tenant_record;
    IF visible_count <> 1 THEN
        RAISE EXCEPTION 'Tenant A expected 1 visible record, found %', visible_count;
    END IF;

    IF EXISTS (
        SELECT 1 FROM identity_spike.tenant_record
        WHERE id = '30000000-0000-0000-0000-00000000000b'
    ) THEN
        RAISE EXCEPTION 'Tenant A can read tenant B';
    END IF;

    UPDATE identity_spike.tenant_record
    SET record_value = 'cross-tenant-update-must-not-happen'
    WHERE organization_id = '00000000-0000-0000-0000-00000000000b';
    GET DIAGNOSTICS affected_count = ROW_COUNT;
    IF affected_count <> 0 THEN
        RAISE EXCEPTION 'Tenant A updated % tenant B rows', affected_count;
    END IF;

    DELETE FROM identity_spike.tenant_record
    WHERE organization_id = '00000000-0000-0000-0000-00000000000b';
    GET DIAGNOSTICS affected_count = ROW_COUNT;
    IF affected_count <> 0 THEN
        RAISE EXCEPTION 'Tenant A deleted % tenant B rows', affected_count;
    END IF;

    BEGIN
        INSERT INTO identity_spike.tenant_record (
            organization_id, id, record_name, record_value,
            created_by_user_id, created_at, version
        ) VALUES (
            '00000000-0000-0000-0000-00000000000b',
            '30000000-0000-0000-0000-000000000099',
            'Inserción cruzada',
            'must-fail',
            '10000000-0000-0000-0000-000000000002',
            '2026-08-05T13:00:00Z',
            1
        );
        RAISE EXCEPTION 'Tenant A cross-tenant INSERT unexpectedly succeeded';
    EXCEPTION
        WHEN insufficient_privilege THEN NULL;
    END;

    INSERT INTO identity_spike.tenant_record (
        organization_id, id, record_name, record_value,
        created_by_user_id, created_at, version
    ) VALUES (
        '00000000-0000-0000-0000-00000000000a',
        '30000000-0000-0000-0000-000000000098',
        'Inserción permitida A',
        'rolled-back-after-probe',
        '10000000-0000-0000-0000-000000000001',
        '2026-08-05T13:00:00Z',
        1
    );
END
$tenant_a$;

-- Simulates an application exception: both data and SET LOCAL context rollback.
ROLLBACK;

BEGIN;
SET LOCAL ROLE agro_app;
DO $after_rollback$
DECLARE
    visible_count integer;
BEGIN
    SELECT count(*) INTO visible_count FROM identity_spike.tenant_record;
    IF visible_count <> 0 THEN
        RAISE EXCEPTION 'Tenant context leaked after rollback: % rows visible', visible_count;
    END IF;

    IF EXISTS (
        SELECT 1 FROM identity_spike.tenant_record
        WHERE id = '30000000-0000-0000-0000-000000000098'
    ) THEN
        RAISE EXCEPTION 'Rolled-back tenant A insert persisted';
    END IF;
END
$after_rollback$;
ROLLBACK;

BEGIN;
SET LOCAL ROLE agro_app;
SELECT set_config('app.current_organization_id', '00000000-0000-0000-0000-00000000000b', true);
DO $tenant_b$
DECLARE
    visible_count integer;
BEGIN
    SELECT count(*) INTO visible_count FROM identity_spike.tenant_record;
    IF visible_count <> 1 THEN
        RAISE EXCEPTION 'Tenant B expected 1 visible record, found %', visible_count;
    END IF;

    IF EXISTS (
        SELECT 1 FROM identity_spike.tenant_record
        WHERE id = '30000000-0000-0000-0000-00000000000a'
    ) THEN
        RAISE EXCEPTION 'Tenant B can read tenant A';
    END IF;
END
$tenant_b$;
COMMIT;

BEGIN;
SET LOCAL ROLE agro_app;
DO $no_context$
DECLARE
    visible_count integer;
BEGIN
    SELECT count(*) INTO visible_count FROM identity_spike.tenant_record;
    IF visible_count <> 0 THEN
        RAISE EXCEPTION 'Tenant context leaked after commit: % rows visible', visible_count;
    END IF;

    BEGIN
        INSERT INTO identity_spike.tenant_record (
            organization_id, id, record_name, record_value,
            created_by_user_id, created_at, version
        ) VALUES (
            '00000000-0000-0000-0000-00000000000a',
            '30000000-0000-0000-0000-000000000097',
            'Sin contexto',
            'must-fail',
            '10000000-0000-0000-0000-000000000001',
            '2026-08-05T13:00:00Z',
            1
        );
        RAISE EXCEPTION 'INSERT without tenant context unexpectedly succeeded';
    EXCEPTION
        WHEN insufficient_privilege THEN NULL;
    END;

    BEGIN
        PERFORM 1 FROM identity_spike.platform_user LIMIT 1;
        RAISE EXCEPTION 'Runtime role unexpectedly read global platform_user';
    EXCEPTION
        WHEN insufficient_privilege THEN NULL;
    END;
END
$no_context$;
ROLLBACK;

BEGIN;
SET LOCAL ROLE agro_job;
SELECT set_config('app.current_organization_id', '00000000-0000-0000-0000-00000000000b', true);
DO $job_b$
DECLARE
    visible_count integer;
BEGIN
    SELECT count(*) INTO visible_count FROM identity_spike.tenant_record;
    IF visible_count <> 1 THEN
        RAISE EXCEPTION 'Tenant B job expected 1 visible record, found %', visible_count;
    END IF;
END
$job_b$;
COMMIT;

BEGIN;
SET LOCAL ROLE agro_job;
DO $job_without_context$
DECLARE
    visible_count integer;
BEGIN
    SELECT count(*) INTO visible_count FROM identity_spike.tenant_record;
    IF visible_count <> 0 THEN
        RAISE EXCEPTION 'Job tenant context leaked after commit: % rows visible', visible_count;
    END IF;
END
$job_without_context$;
ROLLBACK;

SELECT 'rls-isolation-pass' AS probe_result;
