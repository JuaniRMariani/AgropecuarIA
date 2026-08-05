\set ON_ERROR_STOP on

CREATE EXTENSION postgis;

CREATE TABLE tenants (
    tenant_id uuid PRIMARY KEY,
    tenant_ref text NOT NULL UNIQUE CHECK (tenant_ref ~ '^[a-f0-9]{16}$'),
    UNIQUE (tenant_id, tenant_ref)
);

INSERT INTO tenants (tenant_id, tenant_ref) VALUES
('aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa', '8e4aa79b0e4e8c2f'),
('bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb', '6d188822a6966e53');

CREATE TABLE file_objects (
    tenant_id uuid NOT NULL,
    tenant_ref text NOT NULL CHECK (tenant_ref ~ '^[a-f0-9]{16}$'),
    file_id uuid NOT NULL,
    version integer NOT NULL CHECK (version > 0),
    object_key text NOT NULL,
    sha256 text NOT NULL CHECK (sha256 ~ '^[a-f0-9]{64}$'),
    size_bytes bigint NOT NULL CHECK (size_bytes > 0),
    state text NOT NULL CHECK (state IN ('available', 'quarantined')),
    legal_hold boolean NOT NULL,
    resource_type text NOT NULL CHECK (resource_type ~ '^[a-z][a-z0-9_]{0,31}$'),
    resource_id uuid NOT NULL,
    location geometry(Point, 4326) NOT NULL,
    created_at timestamptz NOT NULL,
    PRIMARY KEY (tenant_id, file_id, version),
    UNIQUE (tenant_id, object_key),
    FOREIGN KEY (tenant_id, tenant_ref) REFERENCES tenants (tenant_id, tenant_ref)
);

CREATE OR REPLACE FUNCTION prevent_held_file_delete()
RETURNS trigger LANGUAGE plpgsql AS $$
BEGIN
    IF OLD.legal_hold THEN
        RAISE EXCEPTION 'legal_hold prevents purge' USING ERRCODE = '23514';
    END IF;
    RETURN OLD;
END;
$$;

CREATE TRIGGER file_hold_before_delete
BEFORE DELETE ON file_objects
FOR EACH ROW EXECUTE FUNCTION prevent_held_file_delete();

CREATE TABLE audit_entries (
    sequence bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    tenant_id uuid NOT NULL,
    tenant_ref text NOT NULL CHECK (tenant_ref ~ '^[a-f0-9]{16}$'),
    resource_id uuid NOT NULL,
    action text NOT NULL,
    occurred_at timestamptz NOT NULL,
    previous_sha256 text,
    entry_sha256 text NOT NULL CHECK (entry_sha256 ~ '^[a-f0-9]{64}$')
    ,FOREIGN KEY (tenant_id, tenant_ref) REFERENCES tenants (tenant_id, tenant_ref)
);

CREATE OR REPLACE FUNCTION prevent_audit_mutation()
RETURNS trigger LANGUAGE plpgsql AS $$
BEGIN
    RAISE EXCEPTION 'audit is append-only' USING ERRCODE = '23514';
END;
$$;

CREATE TRIGGER audit_no_update_delete
BEFORE UPDATE OR DELETE ON audit_entries
FOR EACH ROW EXECUTE FUNCTION prevent_audit_mutation();

INSERT INTO file_objects (
    tenant_id, tenant_ref, file_id, version, object_key, sha256, size_bytes,
    state, legal_hold, resource_type, resource_id, location, created_at
) VALUES
(
    'aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa', '8e4aa79b0e4e8c2f',
    '10000000-0000-4000-8000-000000000001', 1,
    'tenants/8e4aa79b0e4e8c2f/quarantine/10000000000040008000000000000001/v1',
    :'object_one_hash', :object_one_size, 'available', true, 'field',
    '20000000-0000-4000-8000-000000000001', ST_SetSRID(ST_Point(-60.6393, -32.9442), 4326),
    '2026-08-05T12:00:00Z'
),
(
    'bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb', '6d188822a6966e53',
    '10000000-0000-4000-8000-000000000002', 1,
    'tenants/6d188822a6966e53/quarantine/10000000000040008000000000000002/v1',
    :'object_two_hash', :object_two_size, 'quarantined', false, 'field',
    '20000000-0000-4000-8000-000000000002', ST_SetSRID(ST_Point(-68.8458, -32.8895), 4326),
    '2026-08-05T12:01:00Z'
);

INSERT INTO audit_entries (tenant_id, tenant_ref, resource_id, action, occurred_at, previous_sha256, entry_sha256) VALUES
('aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa', '8e4aa79b0e4e8c2f', '10000000-0000-4000-8000-000000000001', 'upload_completed', '2026-08-05T12:00:01Z', NULL, :'audit_one_hash'),
('aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa', '8e4aa79b0e4e8c2f', '10000000-0000-4000-8000-000000000001', 'scan_clean', '2026-08-05T12:00:02Z', :'audit_one_hash', :'audit_two_hash'),
('bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb', '6d188822a6966e53', '10000000-0000-4000-8000-000000000002', 'upload_completed', '2026-08-05T12:01:01Z', NULL, :'audit_three_hash'),
('bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb', '6d188822a6966e53', '10000000-0000-4000-8000-000000000002', 'scan_threat', '2026-08-05T12:01:02Z', :'audit_three_hash', :'audit_four_hash');
