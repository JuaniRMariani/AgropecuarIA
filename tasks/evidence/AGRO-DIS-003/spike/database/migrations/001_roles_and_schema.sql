\set ON_ERROR_STOP on

-- R0 spike only. Run as the owner of an isolated PostgreSQL 17 database.
-- The ephemeral harness supplies distinct random passwords through process
-- environment variables. psql variables avoid placing those values in SQL or
-- command output.

\getenv agro_app_password AGRO_APP_PASSWORD
\getenv agro_job_password AGRO_JOB_PASSWORD

BEGIN;

DO $roles$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'agro_schema_owner') THEN
        CREATE ROLE agro_schema_owner
            NOLOGIN
            NOSUPERUSER
            NOCREATEDB
            NOCREATEROLE
            NOINHERIT
            NOREPLICATION
            NOBYPASSRLS;
    END IF;

    IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'agro_app') THEN
        CREATE ROLE agro_app
            LOGIN
            NOSUPERUSER
            NOCREATEDB
            NOCREATEROLE
            NOINHERIT
            NOREPLICATION
            NOBYPASSRLS;
    END IF;

    IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'agro_job') THEN
        CREATE ROLE agro_job
            LOGIN
            NOSUPERUSER
            NOCREATEDB
            NOCREATEROLE
            NOINHERIT
            NOREPLICATION
            NOBYPASSRLS;
    END IF;
END
$roles$;

ALTER ROLE agro_schema_owner
    NOLOGIN NOSUPERUSER NOCREATEDB NOCREATEROLE NOINHERIT NOREPLICATION NOBYPASSRLS;
ALTER ROLE agro_app
    LOGIN NOSUPERUSER NOCREATEDB NOCREATEROLE NOINHERIT NOREPLICATION NOBYPASSRLS
    PASSWORD :'agro_app_password';
ALTER ROLE agro_job
    LOGIN NOSUPERUSER NOCREATEDB NOCREATEROLE NOINHERIT NOREPLICATION NOBYPASSRLS
    PASSWORD :'agro_job_password';

-- Re-running the spike cannot accidentally retain owner membership, while a
-- clean cluster remains warning-free.
DO $memberships$
DECLARE
    runtime_role text;
BEGIN
    FOREACH runtime_role IN ARRAY ARRAY['agro_app', 'agro_job']
    LOOP
        IF EXISTS (
            SELECT 1
            FROM pg_auth_members membership
            JOIN pg_roles granted_role ON granted_role.oid = membership.roleid
            JOIN pg_roles member_role ON member_role.oid = membership.member
            WHERE granted_role.rolname = 'agro_schema_owner'
              AND member_role.rolname = runtime_role
        ) THEN
            EXECUTE format('REVOKE agro_schema_owner FROM %I', runtime_role);
        END IF;
    END LOOP;
END
$memberships$;

CREATE SCHEMA IF NOT EXISTS identity_spike AUTHORIZATION agro_schema_owner;
ALTER SCHEMA identity_spike OWNER TO agro_schema_owner;
REVOKE ALL ON SCHEMA identity_spike FROM PUBLIC;
GRANT USAGE ON SCHEMA identity_spike TO agro_app, agro_job;

COMMIT;
