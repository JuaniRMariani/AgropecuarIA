\set ON_ERROR_STOP on

-- Deterministic synthetic fixtures. All identities use reserved .invalid
-- domains and all UUIDs are deliberately recognizable test values.
BEGIN;
SET LOCAL ROLE agro_schema_owner;

INSERT INTO identity_spike.platform_user (
    id, external_issuer, external_subject, contact_email, created_at
)
VALUES
    ('10000000-0000-0000-0000-000000000001', 'https://fake-idp.invalid/email', 'user-a', 'user-a@example.invalid', '2026-08-05T12:00:00Z'),
    ('10000000-0000-0000-0000-000000000002', 'https://fake-idp.invalid/email', 'user-b', 'user-b@example.invalid', '2026-08-05T12:00:00Z'),
    ('10000000-0000-0000-0000-000000000003', 'https://fake-idp.invalid/google', 'user-shared', 'shared@example.invalid', '2026-08-05T12:00:00Z'),
    ('10000000-0000-0000-0000-000000000004', 'https://fake-idp.invalid/email', 'user-zero', 'zero@example.invalid', '2026-08-05T12:00:00Z')
ON CONFLICT (id) DO UPDATE SET
    external_issuer = EXCLUDED.external_issuer,
    external_subject = EXCLUDED.external_subject,
    contact_email = EXCLUDED.contact_email,
    created_at = EXCLUDED.created_at;

SELECT set_config('app.current_organization_id', '00000000-0000-0000-0000-00000000000a', true);

INSERT INTO identity_spike.organization (id, display_name, is_active, created_at)
VALUES ('00000000-0000-0000-0000-00000000000a', 'Organización sintética A', true, '2026-08-05T12:00:00Z')
ON CONFLICT (id) DO UPDATE SET
    display_name = EXCLUDED.display_name,
    is_active = EXCLUDED.is_active,
    created_at = EXCLUDED.created_at;

INSERT INTO identity_spike.membership (
    id, organization_id, platform_user_id, permission_set, is_active, security_version, created_at
)
VALUES
    ('a1111111-1111-4111-8111-111111111111', '00000000-0000-0000-0000-00000000000a', '10000000-0000-0000-0000-000000000001', 'tenant-record.read', true, 1, '2026-08-05T12:01:00Z'),
    ('a2222222-2222-4222-8222-222222222221', '00000000-0000-0000-0000-00000000000a', '10000000-0000-0000-0000-000000000003', 'tenant-record.read', true, 1, '2026-08-05T12:01:00Z')
ON CONFLICT (organization_id, platform_user_id) DO UPDATE SET
    id = EXCLUDED.id,
    permission_set = EXCLUDED.permission_set,
    is_active = EXCLUDED.is_active,
    security_version = EXCLUDED.security_version,
    created_at = EXCLUDED.created_at;

INSERT INTO identity_spike.tenant_record (
    organization_id, id, record_name, record_value, created_by_user_id, created_at, version
)
VALUES (
    '00000000-0000-0000-0000-00000000000a',
    '30000000-0000-0000-0000-00000000000a',
    'Registro A',
    'visible-solo-en-A',
    '10000000-0000-0000-0000-000000000001',
    '2026-08-05T12:02:00Z',
    1
)
ON CONFLICT (organization_id, id) DO UPDATE SET
    record_name = EXCLUDED.record_name,
    record_value = EXCLUDED.record_value,
    created_by_user_id = EXCLUDED.created_by_user_id,
    created_at = EXCLUDED.created_at,
    version = EXCLUDED.version;

INSERT INTO identity_spike.audit_event (
    organization_id, id, event_type, actor_kind, actor_user_id, subject_ref, correlation_id, occurred_at
)
VALUES (
    '00000000-0000-0000-0000-00000000000a',
    '40000000-0000-0000-0000-00000000000a',
    'identity_spike.fixture_loaded',
    'user',
    '10000000-0000-0000-0000-000000000001',
    'tenant-record:REC-A',
    '50000000-0000-0000-0000-00000000000a',
    '2026-08-05T12:03:00Z'
)
ON CONFLICT (organization_id, id) DO NOTHING;

SELECT set_config('app.current_organization_id', '00000000-0000-0000-0000-00000000000b', true);

INSERT INTO identity_spike.organization (id, display_name, is_active, created_at)
VALUES ('00000000-0000-0000-0000-00000000000b', 'Organización sintética B', true, '2026-08-05T12:00:00Z')
ON CONFLICT (id) DO UPDATE SET
    display_name = EXCLUDED.display_name,
    is_active = EXCLUDED.is_active,
    created_at = EXCLUDED.created_at;

INSERT INTO identity_spike.membership (
    id, organization_id, platform_user_id, permission_set, is_active, security_version, created_at
)
VALUES
    ('a3333333-3333-4333-8333-333333333333', '00000000-0000-0000-0000-00000000000b', '10000000-0000-0000-0000-000000000002', 'identity.none', true, 1, '2026-08-05T12:01:00Z'),
    ('a2222222-2222-4222-8222-222222222222', '00000000-0000-0000-0000-00000000000b', '10000000-0000-0000-0000-000000000003', 'tenant-record.read', true, 1, '2026-08-05T12:01:00Z'),
    ('a4444444-4444-4444-8444-444444444444', '00000000-0000-0000-0000-00000000000b', '10000000-0000-0000-0000-000000000001', 'tenant-record.read', false, 2, '2026-08-05T12:01:00Z')
ON CONFLICT (organization_id, platform_user_id) DO UPDATE SET
    id = EXCLUDED.id,
    permission_set = EXCLUDED.permission_set,
    is_active = EXCLUDED.is_active,
    security_version = EXCLUDED.security_version,
    created_at = EXCLUDED.created_at;

INSERT INTO identity_spike.tenant_record (
    organization_id, id, record_name, record_value, created_by_user_id, created_at, version
)
VALUES (
    '00000000-0000-0000-0000-00000000000b',
    '30000000-0000-0000-0000-00000000000b',
    'Registro B',
    'visible-solo-en-B',
    '10000000-0000-0000-0000-000000000002',
    '2026-08-05T12:02:00Z',
    1
)
ON CONFLICT (organization_id, id) DO UPDATE SET
    record_name = EXCLUDED.record_name,
    record_value = EXCLUDED.record_value,
    created_by_user_id = EXCLUDED.created_by_user_id,
    created_at = EXCLUDED.created_at,
    version = EXCLUDED.version;

INSERT INTO identity_spike.audit_event (
    organization_id, id, event_type, actor_kind, actor_user_id, subject_ref, correlation_id, occurred_at
)
VALUES (
    '00000000-0000-0000-0000-00000000000b',
    '40000000-0000-0000-0000-00000000000b',
    'identity_spike.fixture_loaded',
    'system',
    NULL,
    'tenant-record:REC-B',
    '50000000-0000-0000-0000-00000000000b',
    '2026-08-05T12:03:00Z'
)
ON CONFLICT (organization_id, id) DO NOTHING;

COMMIT;
