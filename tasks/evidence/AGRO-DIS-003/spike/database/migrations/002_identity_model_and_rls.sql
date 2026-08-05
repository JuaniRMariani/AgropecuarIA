\set ON_ERROR_STOP on

BEGIN;
SET LOCAL ROLE agro_schema_owner;

CREATE TABLE identity_spike.organization (
    id uuid PRIMARY KEY,
    display_name varchar(120) NOT NULL CHECK (length(btrim(display_name)) BETWEEN 1 AND 120),
    is_active boolean NOT NULL DEFAULT true,
    created_at timestamptz NOT NULL
);

CREATE TABLE identity_spike.platform_user (
    id uuid PRIMARY KEY,
    external_issuer varchar(512) NOT NULL CHECK (length(btrim(external_issuer)) BETWEEN 1 AND 512),
    external_subject varchar(255) NOT NULL CHECK (length(btrim(external_subject)) BETWEEN 1 AND 255),
    contact_email varchar(320) NOT NULL CHECK (length(btrim(contact_email)) BETWEEN 3 AND 320),
    created_at timestamptz NOT NULL,
    CONSTRAINT uq_platform_user_external_identity UNIQUE (external_issuer, external_subject)
);

-- Composite tenant keys prevent a resource or actor reference from silently
-- crossing organization boundaries.
CREATE TABLE identity_spike.membership (
    organization_id uuid NOT NULL,
    platform_user_id uuid NOT NULL,
    permission_set varchar(80) NOT NULL CHECK (length(btrim(permission_set)) BETWEEN 1 AND 80),
    is_active boolean NOT NULL DEFAULT true,
    security_version integer NOT NULL DEFAULT 1 CHECK (security_version > 0),
    created_at timestamptz NOT NULL,
    PRIMARY KEY (organization_id, platform_user_id),
    CONSTRAINT fk_membership_organization
        FOREIGN KEY (organization_id)
        REFERENCES identity_spike.organization (id),
    CONSTRAINT fk_membership_platform_user
        FOREIGN KEY (platform_user_id)
        REFERENCES identity_spike.platform_user (id)
);

CREATE TABLE identity_spike.tenant_record (
    organization_id uuid NOT NULL,
    id uuid NOT NULL,
    record_name varchar(120) NOT NULL CHECK (length(btrim(record_name)) BETWEEN 1 AND 120),
    record_value varchar(200) NOT NULL CHECK (length(record_value) <= 200),
    created_by_user_id uuid NOT NULL,
    created_at timestamptz NOT NULL,
    version integer NOT NULL DEFAULT 1 CHECK (version > 0),
    PRIMARY KEY (organization_id, id),
    CONSTRAINT fk_tenant_record_organization
        FOREIGN KEY (organization_id)
        REFERENCES identity_spike.organization (id),
    CONSTRAINT fk_tenant_record_creator_membership
        FOREIGN KEY (organization_id, created_by_user_id)
        REFERENCES identity_spike.membership (organization_id, platform_user_id)
);

CREATE TABLE identity_spike.audit_event (
    organization_id uuid NOT NULL,
    id uuid NOT NULL,
    event_type varchar(100) NOT NULL CHECK (length(btrim(event_type)) BETWEEN 1 AND 100),
    actor_kind varchar(16) NOT NULL CHECK (actor_kind IN ('user', 'system')),
    actor_user_id uuid NULL,
    subject_ref varchar(100) NOT NULL CHECK (length(btrim(subject_ref)) BETWEEN 1 AND 100),
    correlation_id uuid NOT NULL,
    occurred_at timestamptz NOT NULL,
    PRIMARY KEY (organization_id, id),
    CONSTRAINT ck_audit_event_actor
        CHECK (
            (actor_kind = 'user' AND actor_user_id IS NOT NULL)
            OR (actor_kind = 'system' AND actor_user_id IS NULL)
        ),
    CONSTRAINT fk_audit_event_organization
        FOREIGN KEY (organization_id)
        REFERENCES identity_spike.organization (id),
    CONSTRAINT fk_audit_event_actor_membership
        FOREIGN KEY (organization_id, actor_user_id)
        REFERENCES identity_spike.membership (organization_id, platform_user_id)
);

-- Supports owner-side membership discovery and selected-tenant validation by
-- user. Runtime access remains policy-constrained and exposes no email lookup.
CREATE INDEX ix_membership_active_user
    ON identity_spike.membership (platform_user_id, organization_id)
    WHERE is_active;

-- Supports the demonstrated per-tenant recent-record query.
CREATE INDEX ix_tenant_record_recent
    ON identity_spike.tenant_record (organization_id, created_at DESC, id);

-- Supports chronological audit review without granting update/delete rights.
CREATE INDEX ix_audit_event_tenant_time
    ON identity_spike.audit_event (organization_id, occurred_at DESC, id);

ALTER TABLE identity_spike.organization ENABLE ROW LEVEL SECURITY;
ALTER TABLE identity_spike.organization FORCE ROW LEVEL SECURITY;
ALTER TABLE identity_spike.membership ENABLE ROW LEVEL SECURITY;
ALTER TABLE identity_spike.membership FORCE ROW LEVEL SECURITY;
ALTER TABLE identity_spike.tenant_record ENABLE ROW LEVEL SECURITY;
ALTER TABLE identity_spike.tenant_record FORCE ROW LEVEL SECURITY;
ALTER TABLE identity_spike.audit_event ENABLE ROW LEVEL SECURITY;
ALTER TABLE identity_spike.audit_event FORCE ROW LEVEL SECURITY;

CREATE POLICY organization_tenant_isolation
    ON identity_spike.organization
    TO agro_schema_owner, agro_app, agro_job
    USING (
        id = nullif(current_setting('app.current_organization_id', true), '')::uuid
    )
    WITH CHECK (
        id = nullif(current_setting('app.current_organization_id', true), '')::uuid
    );

CREATE POLICY membership_tenant_isolation
    ON identity_spike.membership
    TO agro_schema_owner, agro_app, agro_job
    USING (
        organization_id = nullif(current_setting('app.current_organization_id', true), '')::uuid
    )
    WITH CHECK (
        organization_id = nullif(current_setting('app.current_organization_id', true), '')::uuid
    );

CREATE POLICY tenant_record_tenant_isolation
    ON identity_spike.tenant_record
    TO agro_schema_owner, agro_app, agro_job
    USING (
        organization_id = nullif(current_setting('app.current_organization_id', true), '')::uuid
    )
    WITH CHECK (
        organization_id = nullif(current_setting('app.current_organization_id', true), '')::uuid
    );

CREATE POLICY audit_event_tenant_isolation
    ON identity_spike.audit_event
    TO agro_schema_owner, agro_app, agro_job
    USING (
        organization_id = nullif(current_setting('app.current_organization_id', true), '')::uuid
    )
    WITH CHECK (
        organization_id = nullif(current_setting('app.current_organization_id', true), '')::uuid
    );

REVOKE ALL ON ALL TABLES IN SCHEMA identity_spike FROM PUBLIC;
REVOKE ALL ON ALL TABLES IN SCHEMA identity_spike FROM agro_app, agro_job;

-- Global identities remain owner-only in this spike. Application flows receive
-- an internal user identifier from the authenticated server-side session.
GRANT SELECT ON identity_spike.organization, identity_spike.membership
    TO agro_app, agro_job;
GRANT SELECT, INSERT, UPDATE, DELETE ON identity_spike.tenant_record
    TO agro_app, agro_job;
GRANT SELECT, INSERT ON identity_spike.audit_event
    TO agro_app, agro_job;

ALTER DEFAULT PRIVILEGES IN SCHEMA identity_spike
    REVOKE ALL ON TABLES FROM PUBLIC;

COMMIT;
