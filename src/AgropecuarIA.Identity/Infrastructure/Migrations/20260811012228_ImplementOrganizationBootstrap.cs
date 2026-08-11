using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgropecuarIA.Identity.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ImplementOrganizationBootstrap : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DO $roles$
                DECLARE
                    role_name text;
                BEGIN
                    FOREACH role_name IN ARRAY ARRAY[
                        'agro_identity_owner',
                        'agro_identity_migrator',
                        'agro_identity_app',
                        'agro_identity_job',
                        'agro_identity_discovery'
                    ]
                    LOOP
                        IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = role_name) THEN
                            EXECUTE format(
                                'CREATE ROLE %I NOLOGIN NOINHERIT NOSUPERUSER NOCREATEDB NOCREATEROLE NOREPLICATION NOBYPASSRLS',
                                role_name);
                        ELSIF EXISTS (
                            SELECT 1
                            FROM pg_roles
                            WHERE rolname = role_name
                              AND (rolcanlogin OR rolinherit OR rolsuper OR rolcreatedb
                                   OR rolcreaterole OR rolreplication OR rolbypassrls)
                        ) THEN
                            RAISE EXCEPTION 'Identity privilege role % has unsafe attributes', role_name;
                        END IF;
                    END LOOP;
                END
                $roles$;

                GRANT agro_identity_owner TO agro_identity_migrator
                    WITH INHERIT FALSE, SET TRUE;
                """);

            migrationBuilder.CreateTable(
                name: "organization_creation_ledgers",
                schema: "identity",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ScopeKind = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    Namespace = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Operation = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ContractVersion = table.Column<int>(type: "integer", nullable: false),
                    CanonicalizationVersion = table.Column<int>(type: "integer", nullable: false),
                    ActorUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    SessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    AuthorizationVersion = table.Column<Guid>(type: "uuid", nullable: false),
                    RequestFingerprint = table.Column<byte[]>(type: "bytea", maxLength: 32, nullable: false),
                    State = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: true),
                    MembershipId = table.Column<Guid>(type: "uuid", nullable: true),
                    LeaseOwner = table.Column<Guid>(type: "uuid", nullable: false),
                    FenceToken = table.Column<long>(type: "bigint", nullable: false),
                    LeaseUntilUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    StartedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CompletedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Version = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_organization_creation_ledgers", x => x.Id);
                    table.CheckConstraint("CK_organization_creation_ledgers_Fence", "\"FenceToken\" > 0 AND \"LeaseUntilUtc\" > \"StartedAtUtc\"");
                    table.CheckConstraint("CK_organization_creation_ledgers_Protocol", "\"ScopeKind\" = 'platform' AND \"Namespace\" = 'organization-bootstrap' AND \"Operation\" = 'create_organization' AND \"ContractVersion\" > 0 AND \"CanonicalizationVersion\" > 0");
                    table.CheckConstraint("CK_organization_creation_ledgers_State", "(\"State\" = 'in_progress' AND \"OrganizationId\" IS NULL AND \"MembershipId\" IS NULL AND \"CompletedAtUtc\" IS NULL) OR (\"State\" = 'succeeded' AND \"OrganizationId\" IS NOT NULL AND \"MembershipId\" IS NOT NULL AND \"CompletedAtUtc\" IS NOT NULL) OR (\"State\" = 'failed_terminal' AND \"OrganizationId\" IS NULL AND \"MembershipId\" IS NULL AND \"CompletedAtUtc\" IS NOT NULL) OR (\"State\" = 'response_expired' AND \"OrganizationId\" IS NOT NULL AND \"MembershipId\" IS NOT NULL AND \"CompletedAtUtc\" IS NOT NULL)");
                    table.ForeignKey(
                        name: "FK_organization_creation_ledgers_sessions_SessionId",
                        column: x => x.SessionId,
                        principalSchema: "identity",
                        principalTable: "sessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_organization_creation_ledgers_users_ActorUserId",
                        column: x => x.ActorUserId,
                        principalSchema: "identity",
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "organizations",
                schema: "identity",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Version = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_organizations", x => x.Id);
                    table.CheckConstraint("CK_organizations_Status", "\"Status\" = 'active'");
                    table.ForeignKey(
                        name: "FK_organizations_users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalSchema: "identity",
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "organization_creation_key_aliases",
                schema: "identity",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LedgerId = table.Column<Guid>(type: "uuid", nullable: false),
                    ScopeKind = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    Namespace = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Operation = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    KeyVersion = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    KeyDigest = table.Column<byte[]>(type: "bytea", maxLength: 32, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_organization_creation_key_aliases", x => x.Id);
                    table.CheckConstraint("CK_organization_creation_key_aliases_Protocol", "\"ScopeKind\" = 'platform' AND \"Namespace\" = 'organization-bootstrap' AND \"Operation\" = 'create_organization'");
                    table.ForeignKey(
                        name: "FK_organization_creation_key_aliases_organization_creation_led~",
                        column: x => x.LedgerId,
                        principalSchema: "identity",
                        principalTable: "organization_creation_ledgers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "memberships",
                schema: "identity",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Role = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    SecurityVersion = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_memberships", x => x.Id);
                    table.CheckConstraint("CK_memberships_Role", "\"Role\" = 'owner'");
                    table.CheckConstraint("CK_memberships_SecurityVersion", "\"SecurityVersion\" > 0");
                    table.CheckConstraint("CK_memberships_Status", "\"Status\" = 'active'");
                    table.ForeignKey(
                        name: "FK_memberships_organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalSchema: "identity",
                        principalTable: "organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_memberships_users_UserId",
                        column: x => x.UserId,
                        principalSchema: "identity",
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_memberships_OrganizationId_Status",
                schema: "identity",
                table: "memberships",
                columns: ["OrganizationId", "Status"]);

            migrationBuilder.CreateIndex(
                name: "IX_memberships_OrganizationId_UserId",
                schema: "identity",
                table: "memberships",
                columns: ["OrganizationId", "UserId"],
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_memberships_UserId_Status",
                schema: "identity",
                table: "memberships",
                columns: ["UserId", "Status"]);

            migrationBuilder.CreateIndex(
                name: "IX_organization_creation_key_aliases_LedgerId_KeyVersion",
                schema: "identity",
                table: "organization_creation_key_aliases",
                columns: ["LedgerId", "KeyVersion"],
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_organization_creation_key_aliases_ScopeKind_Namespace_Opera~",
                schema: "identity",
                table: "organization_creation_key_aliases",
                columns: ["ScopeKind", "Namespace", "Operation", "KeyVersion", "KeyDigest"],
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_organization_creation_ledgers_ActorUserId_StartedAtUtc",
                schema: "identity",
                table: "organization_creation_ledgers",
                columns: ["ActorUserId", "StartedAtUtc"]);

            migrationBuilder.CreateIndex(
                name: "IX_organization_creation_ledgers_SessionId",
                schema: "identity",
                table: "organization_creation_ledgers",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_organization_creation_ledgers_State_LeaseUntilUtc",
                schema: "identity",
                table: "organization_creation_ledgers",
                columns: ["State", "LeaseUntilUtc"]);

            migrationBuilder.CreateIndex(
                name: "IX_organizations_CreatedByUserId_CreatedAtUtc",
                schema: "identity",
                table: "organizations",
                columns: ["CreatedByUserId", "CreatedAtUtc"]);

            migrationBuilder.Sql(
                """
                ALTER TABLE identity.organizations OWNER TO agro_identity_owner;
                ALTER TABLE identity.memberships OWNER TO agro_identity_owner;
                ALTER TABLE identity.organization_creation_ledgers OWNER TO agro_identity_owner;
                ALTER TABLE identity.organization_creation_key_aliases OWNER TO agro_identity_owner;

                ALTER TABLE identity.organization_creation_ledgers
                    ADD CONSTRAINT "CK_organization_creation_ledgers_FingerprintLength"
                    CHECK (octet_length("RequestFingerprint") = 32);
                ALTER TABLE identity.organization_creation_key_aliases
                    ADD CONSTRAINT "CK_organization_creation_key_aliases_DigestLength"
                    CHECK (octet_length("KeyDigest") = 32);

                CREATE UNIQUE INDEX "UX_sessions_Id_UserId"
                    ON identity.sessions ("Id", "UserId");
                CREATE UNIQUE INDEX "UX_memberships_OrganizationId_Id_UserId"
                    ON identity.memberships ("OrganizationId", "Id", "UserId");
                ALTER TABLE identity.organization_creation_ledgers
                    ADD CONSTRAINT "FK_organization_creation_ledgers_sessions_SessionId_ActorUserId"
                    FOREIGN KEY ("SessionId", "ActorUserId")
                    REFERENCES identity.sessions ("Id", "UserId")
                    ON DELETE RESTRICT;
                ALTER TABLE identity.organization_creation_ledgers
                    ADD CONSTRAINT "FK_organization_creation_ledgers_memberships_ResultActor"
                    FOREIGN KEY ("OrganizationId", "MembershipId", "ActorUserId")
                    REFERENCES identity.memberships ("OrganizationId", "Id", "UserId")
                    ON DELETE RESTRICT;

                ALTER TABLE identity.organizations ENABLE ROW LEVEL SECURITY;
                ALTER TABLE identity.organizations FORCE ROW LEVEL SECURITY;
                ALTER TABLE identity.memberships ENABLE ROW LEVEL SECURITY;
                ALTER TABLE identity.memberships FORCE ROW LEVEL SECURITY;
                ALTER TABLE identity.organization_creation_ledgers ENABLE ROW LEVEL SECURITY;
                ALTER TABLE identity.organization_creation_ledgers FORCE ROW LEVEL SECURITY;
                ALTER TABLE identity.organization_creation_key_aliases ENABLE ROW LEVEL SECURITY;
                ALTER TABLE identity.organization_creation_key_aliases FORCE ROW LEVEL SECURITY;

                REVOKE ALL ON TABLE identity.organizations FROM PUBLIC;
                REVOKE ALL ON TABLE identity.memberships FROM PUBLIC;
                REVOKE ALL ON TABLE identity.organization_creation_ledgers FROM PUBLIC;
                REVOKE ALL ON TABLE identity.organization_creation_key_aliases FROM PUBLIC;
                REVOKE ALL ON TABLE identity.organizations
                    FROM agro_identity_app, agro_identity_job, agro_identity_discovery;
                REVOKE ALL ON TABLE identity.memberships
                    FROM agro_identity_app, agro_identity_job, agro_identity_discovery;
                REVOKE ALL ON TABLE identity.organization_creation_ledgers
                    FROM agro_identity_app, agro_identity_job, agro_identity_discovery;
                REVOKE ALL ON TABLE identity.organization_creation_key_aliases
                    FROM agro_identity_app, agro_identity_job, agro_identity_discovery;
                REVOKE ALL ON TABLE identity.users FROM agro_identity_app;
                REVOKE ALL ON TABLE identity.sessions FROM agro_identity_app;

                ALTER TABLE identity.sessions ENABLE ROW LEVEL SECURITY;

                GRANT USAGE ON SCHEMA identity
                    TO agro_identity_owner, agro_identity_migrator,
                       agro_identity_app, agro_identity_job, agro_identity_discovery;
                GRANT SELECT, INSERT ON TABLE identity.organizations TO agro_identity_app;
                GRANT SELECT, INSERT ON TABLE identity.memberships TO agro_identity_app;
                GRANT SELECT, INSERT, UPDATE
                    ON TABLE identity.organization_creation_ledgers TO agro_identity_app;
                GRANT SELECT, INSERT
                    ON TABLE identity.organization_creation_key_aliases TO agro_identity_app;
                GRANT SELECT (
                    "Id", "UserId", "AuthenticatedAtUtc", "ExpiresAtUtc",
                    "IsAuthenticationAssuranceVerified", "RevokedAtUtc", "Version")
                    ON TABLE identity.sessions TO agro_identity_app;
                GRANT INSERT ON TABLE identity.organization_memberships TO agro_identity_app;
                GRANT INSERT ON TABLE identity.audit_events TO agro_identity_app;
                GRANT INSERT ON TABLE identity.outbox_messages TO agro_identity_app;
                GRANT SELECT ("Id", "DisplayName", "Status")
                    ON TABLE identity.organizations TO agro_identity_discovery;
                GRANT SELECT ("Id", "OrganizationId", "UserId", "Role", "Status", "SecurityVersion")
                    ON TABLE identity.memberships TO agro_identity_discovery;

                CREATE POLICY sessions_app_organization_bootstrap_read
                    ON identity.sessions
                    FOR SELECT
                    TO agro_identity_app
                    USING (
                        nullif(current_setting('app.current_scope_kind', true), '') = 'platform'
                        AND "UserId" =
                            nullif(current_setting('app.current_actor_id', true), '')::uuid
                    );

                CREATE POLICY organizations_app_platform_read
                    ON identity.organizations
                    FOR SELECT
                    TO agro_identity_app
                    USING (
                        nullif(current_setting('app.current_scope_kind', true), '') = 'platform'
                        AND "Id" =
                            nullif(current_setting('app.current_organization_id', true), '')::uuid
                        AND "CreatedByUserId" =
                            nullif(current_setting('app.current_actor_id', true), '')::uuid
                    );

                CREATE POLICY organizations_app_tenant_read
                    ON identity.organizations
                    FOR SELECT
                    TO agro_identity_app
                    USING (
                        nullif(current_setting('app.current_scope_kind', true), '') = 'tenant'
                        AND "Id" =
                            nullif(current_setting('app.current_organization_id', true), '')::uuid
                        AND EXISTS (
                            SELECT 1
                            FROM identity.memberships AS actor_membership
                            WHERE actor_membership."OrganizationId" = organizations."Id"
                              AND actor_membership."UserId" =
                                  nullif(current_setting('app.current_actor_id', true), '')::uuid
                              AND actor_membership."Role" = 'owner'
                              AND actor_membership."Status" = 'active'
                        )
                    );

                CREATE POLICY organizations_app_bootstrap_insert
                    ON identity.organizations
                    FOR INSERT
                    TO agro_identity_app
                    WITH CHECK (
                        nullif(current_setting('app.current_scope_kind', true), '') = 'platform'
                        AND "Id" =
                            nullif(current_setting('app.current_organization_id', true), '')::uuid
                        AND "CreatedByUserId" =
                            nullif(current_setting('app.current_actor_id', true), '')::uuid
                        AND "Status" = 'active'
                    );

                CREATE POLICY organizations_discovery_read
                    ON identity.organizations
                    FOR SELECT
                    TO agro_identity_discovery
                    USING (
                        "Status" = 'active'
                        AND EXISTS (
                            SELECT 1
                            FROM identity.memberships AS actor_membership
                            WHERE actor_membership."OrganizationId" = organizations."Id"
                              AND actor_membership."UserId" =
                                  nullif(current_setting('app.current_actor_id', true), '')::uuid
                              AND actor_membership."Role" = 'owner'
                              AND actor_membership."Status" = 'active'
                        )
                    );

                CREATE POLICY memberships_app_tenant_read
                    ON identity.memberships
                    FOR SELECT
                    TO agro_identity_app
                    USING (
                        nullif(current_setting('app.current_scope_kind', true), '') = 'tenant'
                        AND "OrganizationId" =
                            nullif(current_setting('app.current_organization_id', true), '')::uuid
                        AND "UserId" =
                            nullif(current_setting('app.current_actor_id', true), '')::uuid
                        AND "Status" = 'active'
                    );

                CREATE POLICY memberships_app_platform_replay_read
                    ON identity.memberships
                    FOR SELECT
                    TO agro_identity_app
                    USING (
                        nullif(current_setting('app.current_scope_kind', true), '') = 'platform'
                        AND "OrganizationId" =
                            nullif(current_setting('app.current_organization_id', true), '')::uuid
                        AND "UserId" =
                            nullif(current_setting('app.current_actor_id', true), '')::uuid
                        AND "Role" = 'owner'
                        AND "Status" = 'active'
                    );

                CREATE POLICY memberships_app_bootstrap_insert
                    ON identity.memberships
                    FOR INSERT
                    TO agro_identity_app
                    WITH CHECK (
                        nullif(current_setting('app.current_scope_kind', true), '') = 'platform'
                        AND "OrganizationId" =
                            nullif(current_setting('app.current_organization_id', true), '')::uuid
                        AND "UserId" =
                            nullif(current_setting('app.current_actor_id', true), '')::uuid
                        AND "Role" = 'owner'
                        AND "Status" = 'active'
                        AND EXISTS (
                            SELECT 1
                            FROM identity.organizations AS created_organization
                            WHERE created_organization."Id" = memberships."OrganizationId"
                              AND created_organization."CreatedByUserId" =
                                  nullif(current_setting('app.current_actor_id', true), '')::uuid
                              AND created_organization."Status" = 'active'
                        )
                    );

                CREATE POLICY memberships_discovery_read
                    ON identity.memberships
                    FOR SELECT
                    TO agro_identity_discovery
                    USING (
                        "UserId" = nullif(current_setting('app.current_actor_id', true), '')::uuid
                        AND "Status" = 'active'
                    );

                CREATE POLICY organization_creation_ledgers_app_access
                    ON identity.organization_creation_ledgers
                    FOR ALL
                    TO agro_identity_app
                    USING (
                        nullif(current_setting('app.current_scope_kind', true), '') = 'platform'
                        AND "ActorUserId" =
                            nullif(current_setting('app.current_actor_id', true), '')::uuid
                    )
                    WITH CHECK (
                        nullif(current_setting('app.current_scope_kind', true), '') = 'platform'
                        AND "ActorUserId" =
                            nullif(current_setting('app.current_actor_id', true), '')::uuid
                    );

                CREATE POLICY organization_creation_key_aliases_app_access
                    ON identity.organization_creation_key_aliases
                    FOR ALL
                    TO agro_identity_app
                    USING (
                        nullif(current_setting('app.current_scope_kind', true), '') = 'platform'
                        AND EXISTS (
                            SELECT 1
                            FROM identity.organization_creation_ledgers AS actor_ledger
                            WHERE actor_ledger."Id" = organization_creation_key_aliases."LedgerId"
                              AND actor_ledger."ActorUserId" =
                                  nullif(current_setting('app.current_actor_id', true), '')::uuid
                        )
                    )
                    WITH CHECK (
                        nullif(current_setting('app.current_scope_kind', true), '') = 'platform'
                        AND EXISTS (
                            SELECT 1
                            FROM identity.organization_creation_ledgers AS actor_ledger
                            WHERE actor_ledger."Id" = organization_creation_key_aliases."LedgerId"
                              AND actor_ledger."ActorUserId" =
                                  nullif(current_setting('app.current_actor_id', true), '')::uuid
                        )
                    );

                CREATE POLICY organization_creation_ledgers_owner_key_coverage
                    ON identity.organization_creation_ledgers
                    FOR SELECT
                    TO agro_identity_owner
                    USING (true);

                CREATE POLICY organization_creation_key_aliases_owner_key_coverage
                    ON identity.organization_creation_key_aliases
                    FOR SELECT
                    TO agro_identity_owner
                    USING (true);

                CREATE FUNCTION identity.organization_creation_current_key_covered(version text)
                RETURNS boolean
                LANGUAGE sql
                STABLE
                SECURITY DEFINER
                SET search_path = pg_catalog
                AS $key_coverage$
                    SELECT coalesce(length(version) BETWEEN 1 AND 32, false)
                       AND NOT EXISTS (
                            SELECT 1
                            FROM identity.organization_creation_ledgers AS ledger
                            WHERE NOT EXISTS (
                                SELECT 1
                                FROM identity.organization_creation_key_aliases AS alias
                                WHERE alias."LedgerId" = ledger."Id"
                                  AND alias."KeyVersion" = version
                            )
                       )
                $key_coverage$;
                ALTER FUNCTION identity.organization_creation_current_key_covered(text)
                    OWNER TO agro_identity_owner;
                REVOKE ALL ON FUNCTION
                    identity.organization_creation_current_key_covered(text) FROM PUBLIC;
                GRANT EXECUTE ON FUNCTION
                    identity.organization_creation_current_key_covered(text) TO agro_identity_app;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DO $ephemeral_only$
                BEGIN
                    IF current_database() !~ '^agro_identity_[0-9a-f]{32}$' THEN
                        RAISE EXCEPTION
                            'The organization bootstrap down migration is allowed only on an ephemeral test database';
                    END IF;
                END
                $ephemeral_only$;

                DROP FUNCTION IF EXISTS
                    identity.organization_creation_current_key_covered(text);

                DROP POLICY IF EXISTS organizations_app_tenant_read
                    ON identity.organizations;
                DROP POLICY IF EXISTS organizations_discovery_read
                    ON identity.organizations;
                DROP POLICY IF EXISTS memberships_app_bootstrap_insert
                    ON identity.memberships;
                DROP POLICY IF EXISTS sessions_app_organization_bootstrap_read
                    ON identity.sessions;
                ALTER TABLE identity.sessions DISABLE ROW LEVEL SECURITY;
                REVOKE ALL ON TABLE identity.users FROM agro_identity_app;
                REVOKE ALL ON TABLE identity.sessions FROM agro_identity_app;
                """);

            migrationBuilder.DropTable(
                name: "organization_creation_key_aliases",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "organization_creation_ledgers",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "memberships",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "organizations",
                schema: "identity");

            migrationBuilder.Sql(
                """
                DROP INDEX IF EXISTS identity."UX_sessions_Id_UserId";
                """);
        }
    }
}
