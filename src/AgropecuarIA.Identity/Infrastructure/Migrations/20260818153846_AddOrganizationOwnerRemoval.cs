using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1861 // EF Core scaffolds transient arrays for migration metadata.

namespace AgropecuarIA.Identity.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOrganizationOwnerRemoval : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_memberships_Status",
                schema: "identity",
                table: "memberships");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "RemovedAtUtc",
                schema: "identity",
                table: "memberships",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "RemovedByUserId",
                schema: "identity",
                table: "memberships",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "Version",
                schema: "identity",
                table: "memberships",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateTable(
                name: "organization_owner_removal_ledgers",
                schema: "identity",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    ScopeKind = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    Namespace = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Operation = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ContractVersion = table.Column<int>(type: "integer", nullable: false),
                    CanonicalizationVersion = table.Column<int>(type: "integer", nullable: false),
                    ActorUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    SessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    AuthorizationVersion = table.Column<Guid>(type: "uuid", nullable: false),
                    MembershipId = table.Column<Guid>(type: "uuid", nullable: false),
                    ExpectedMembershipVersion = table.Column<Guid>(type: "uuid", nullable: false),
                    RequestFingerprint = table.Column<byte[]>(type: "bytea", maxLength: 32, nullable: false),
                    State = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    ResultMembershipVersion = table.Column<Guid>(type: "uuid", nullable: true),
                    ResultAuthorizationVersion = table.Column<long>(type: "bigint", nullable: true),
                    RemovedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LeaseOwner = table.Column<Guid>(type: "uuid", nullable: false),
                    FenceToken = table.Column<long>(type: "bigint", nullable: false),
                    LeaseUntilUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    StartedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CompletedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Version = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_organization_owner_removal_ledgers", x => x.Id);
                    table.CheckConstraint("CK_organization_owner_removal_ledgers_Fence", "\"FenceToken\" > 0 AND \"LeaseUntilUtc\" > \"StartedAtUtc\"");
                    table.CheckConstraint("CK_organization_owner_removal_ledgers_Protocol", "\"ScopeKind\" = 'tenant' AND \"Namespace\" = 'organization-owner-membership' AND \"Operation\" = 'remove_owner' AND \"ContractVersion\" > 0 AND \"CanonicalizationVersion\" > 0");
                    table.CheckConstraint("CK_organization_owner_removal_ledgers_Result", "(\"ResultAuthorizationVersion\" IS NULL OR \"ResultAuthorizationVersion\" > 1) AND (\"RemovedAtUtc\" IS NULL OR \"RemovedAtUtc\" >= \"StartedAtUtc\") AND (\"CompletedAtUtc\" IS NULL OR \"CompletedAtUtc\" >= \"StartedAtUtc\") AND (\"RemovedAtUtc\" IS NULL OR \"CompletedAtUtc\" IS NULL OR \"CompletedAtUtc\" >= \"RemovedAtUtc\")");
                    table.CheckConstraint("CK_organization_owner_removal_ledgers_State", "(\"State\" = 'in_progress' AND \"ResultMembershipVersion\" IS NULL AND \"ResultAuthorizationVersion\" IS NULL AND \"RemovedAtUtc\" IS NULL AND \"CompletedAtUtc\" IS NULL) OR (\"State\" = 'succeeded' AND \"ResultMembershipVersion\" IS NOT NULL AND \"ResultAuthorizationVersion\" IS NOT NULL AND \"RemovedAtUtc\" IS NOT NULL AND \"CompletedAtUtc\" IS NOT NULL) OR (\"State\" = 'failed_terminal' AND \"ResultMembershipVersion\" IS NULL AND \"ResultAuthorizationVersion\" IS NULL AND \"RemovedAtUtc\" IS NULL AND \"CompletedAtUtc\" IS NOT NULL) OR (\"State\" = 'response_expired' AND \"ResultMembershipVersion\" IS NOT NULL AND \"ResultAuthorizationVersion\" IS NOT NULL AND \"RemovedAtUtc\" IS NOT NULL AND \"CompletedAtUtc\" IS NOT NULL)");
                    table.ForeignKey(
                        name: "FK_organization_owner_removal_ledgers_memberships_MembershipId",
                        column: x => x.MembershipId,
                        principalSchema: "identity",
                        principalTable: "memberships",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_organization_owner_removal_ledgers_organizations_Organizati~",
                        column: x => x.OrganizationId,
                        principalSchema: "identity",
                        principalTable: "organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_organization_owner_removal_ledgers_sessions_SessionId",
                        column: x => x.SessionId,
                        principalSchema: "identity",
                        principalTable: "sessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_organization_owner_removal_ledgers_users_ActorUserId",
                        column: x => x.ActorUserId,
                        principalSchema: "identity",
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "organization_owner_removal_key_aliases",
                schema: "identity",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LedgerId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    ScopeKind = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    Namespace = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Operation = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    KeyVersion = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    KeyDigest = table.Column<byte[]>(type: "bytea", maxLength: 32, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_organization_owner_removal_key_aliases", x => x.Id);
                    table.CheckConstraint("CK_organization_owner_removal_key_aliases_Protocol", "\"ScopeKind\" = 'tenant' AND \"Namespace\" = 'organization-owner-membership' AND \"Operation\" = 'remove_owner'");
                    table.ForeignKey(
                        name: "FK_organization_owner_removal_key_aliases_organization_owner_r~",
                        column: x => x.LedgerId,
                        principalSchema: "identity",
                        principalTable: "organization_owner_removal_ledgers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_organization_owner_removal_key_aliases_organizations_Organi~",
                        column: x => x.OrganizationId,
                        principalSchema: "identity",
                        principalTable: "organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_memberships_RemovedByUserId",
                schema: "identity",
                table: "memberships",
                column: "RemovedByUserId");

            migrationBuilder.AddCheckConstraint(
                name: "CK_memberships_Status",
                schema: "identity",
                table: "memberships",
                sql: "(\"Status\" = 'active' AND \"RemovedAtUtc\" IS NULL AND \"RemovedByUserId\" IS NULL) OR (\"Status\" = 'removed' AND \"RemovedAtUtc\" >= \"CreatedAtUtc\" AND \"RemovedByUserId\" IS NOT NULL)");

            migrationBuilder.CreateIndex(
                name: "IX_organization_owner_removal_key_aliases_LedgerId_KeyVersion",
                schema: "identity",
                table: "organization_owner_removal_key_aliases",
                columns: new[] { "LedgerId", "KeyVersion" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_organization_owner_removal_key_aliases_OrganizationId_Scope~",
                schema: "identity",
                table: "organization_owner_removal_key_aliases",
                columns: new[] { "OrganizationId", "ScopeKind", "Namespace", "Operation", "KeyVersion", "KeyDigest" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_organization_owner_removal_ledgers_ActorUserId",
                schema: "identity",
                table: "organization_owner_removal_ledgers",
                column: "ActorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_organization_owner_removal_ledgers_MembershipId",
                schema: "identity",
                table: "organization_owner_removal_ledgers",
                column: "MembershipId");

            migrationBuilder.CreateIndex(
                name: "IX_organization_owner_removal_ledgers_OrganizationId_ActorUser~",
                schema: "identity",
                table: "organization_owner_removal_ledgers",
                columns: new[] { "OrganizationId", "ActorUserId", "StartedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_organization_owner_removal_ledgers_OrganizationId_State_Lea~",
                schema: "identity",
                table: "organization_owner_removal_ledgers",
                columns: new[] { "OrganizationId", "State", "LeaseUntilUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_organization_owner_removal_ledgers_SessionId",
                schema: "identity",
                table: "organization_owner_removal_ledgers",
                column: "SessionId");

            migrationBuilder.AddForeignKey(
                name: "FK_memberships_users_RemovedByUserId",
                schema: "identity",
                table: "memberships",
                column: "RemovedByUserId",
                principalSchema: "identity",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.Sql(
                """
                ALTER TABLE identity.memberships
                    ALTER COLUMN "Version" SET DEFAULT gen_random_uuid();
                UPDATE identity.memberships
                SET "Version" = gen_random_uuid()
                WHERE "Version" = '00000000-0000-0000-0000-000000000000'::uuid;

                ALTER TABLE identity.memberships
                    ADD CONSTRAINT "CK_memberships_Version"
                    CHECK ("Version" <> '00000000-0000-0000-0000-000000000000'::uuid);
                ALTER TABLE identity.organization_owner_removal_ledgers
                    ADD CONSTRAINT "CK_owner_removal_ledgers_FingerprintLength"
                    CHECK (octet_length("RequestFingerprint") = 32);
                ALTER TABLE identity.organization_owner_removal_key_aliases
                    ADD CONSTRAINT "CK_owner_removal_key_aliases_DigestLength"
                    CHECK (octet_length("KeyDigest") = 32);

                CREATE UNIQUE INDEX "UX_memberships_OrganizationId_Id"
                    ON identity.memberships ("OrganizationId", "Id");
                CREATE UNIQUE INDEX "UX_owner_removal_ledgers_OrganizationId_Id"
                    ON identity.organization_owner_removal_ledgers ("OrganizationId", "Id");
                ALTER TABLE identity.organization_owner_removal_ledgers
                    ADD CONSTRAINT "FK_owner_removal_ledgers_sessions_Actor"
                    FOREIGN KEY ("SessionId", "ActorUserId")
                    REFERENCES identity.sessions ("Id", "UserId")
                    ON DELETE RESTRICT;
                ALTER TABLE identity.organization_owner_removal_ledgers
                    ADD CONSTRAINT "FK_owner_removal_ledgers_memberships_Tenant"
                    FOREIGN KEY ("OrganizationId", "MembershipId")
                    REFERENCES identity.memberships ("OrganizationId", "Id")
                    ON DELETE RESTRICT;
                ALTER TABLE identity.organization_owner_removal_key_aliases
                    ADD CONSTRAINT "FK_owner_removal_aliases_ledgers_Tenant"
                    FOREIGN KEY ("OrganizationId", "LedgerId")
                    REFERENCES identity.organization_owner_removal_ledgers ("OrganizationId", "Id")
                    ON DELETE CASCADE;

                ALTER TABLE identity.organization_owner_removal_ledgers
                    OWNER TO agro_identity_owner;
                ALTER TABLE identity.organization_owner_removal_key_aliases
                    OWNER TO agro_identity_owner;
                ALTER TABLE identity.organization_owner_removal_ledgers
                    ENABLE ROW LEVEL SECURITY;
                ALTER TABLE identity.organization_owner_removal_ledgers
                    FORCE ROW LEVEL SECURITY;
                ALTER TABLE identity.organization_owner_removal_key_aliases
                    ENABLE ROW LEVEL SECURITY;
                ALTER TABLE identity.organization_owner_removal_key_aliases
                    FORCE ROW LEVEL SECURITY;

                REVOKE ALL ON TABLE identity.organization_owner_removal_ledgers FROM PUBLIC;
                REVOKE ALL ON TABLE identity.organization_owner_removal_key_aliases FROM PUBLIC;
                REVOKE ALL ON TABLE identity.organization_owner_removal_ledgers
                    FROM agro_identity_app, agro_identity_job, agro_identity_discovery;
                REVOKE ALL ON TABLE identity.organization_owner_removal_key_aliases
                    FROM agro_identity_app, agro_identity_job, agro_identity_discovery;
                GRANT SELECT, INSERT, UPDATE
                    ON TABLE identity.organization_owner_removal_ledgers TO agro_identity_app;
                GRANT SELECT, INSERT
                    ON TABLE identity.organization_owner_removal_key_aliases TO agro_identity_app;
                GRANT SELECT ("Id", "DisplayName")
                    ON TABLE identity.users TO agro_identity_owner;
                GRANT SELECT (
                    "Id", "UserId", "ExpiresAtUtc", "IsAuthenticationAssuranceVerified",
                    "StrongAuthenticatedAtUtc", "StrongAuthenticationPurpose", "RevokedAtUtc", "Version")
                    ON TABLE identity.sessions TO agro_identity_owner;
                GRANT DELETE, SELECT ("UserId", "OrganizationId")
                    ON TABLE identity.organization_memberships TO agro_identity_owner;

                CREATE POLICY organizations_owner_owner_management
                    ON identity.organizations
                    FOR SELECT TO agro_identity_owner USING (true);
                CREATE POLICY organizations_owner_owner_management_update
                    ON identity.organizations
                    FOR UPDATE TO agro_identity_owner
                    USING (true) WITH CHECK (true);
                CREATE POLICY memberships_owner_owner_management_read
                    ON identity.memberships
                    FOR SELECT TO agro_identity_owner USING (true);
                CREATE POLICY memberships_owner_owner_management_update
                    ON identity.memberships
                    FOR UPDATE TO agro_identity_owner
                    USING (true) WITH CHECK (true);
                CREATE POLICY sessions_owner_owner_removal_read
                    ON identity.sessions
                    FOR SELECT TO agro_identity_owner USING (true);
                CREATE POLICY owner_invitations_owner_owner_removal_update
                    ON identity.organization_owner_invitations
                    FOR UPDATE TO agro_identity_owner
                    USING (true) WITH CHECK (true);

                CREATE POLICY owner_removal_ledgers_app_access
                    ON identity.organization_owner_removal_ledgers
                    FOR ALL TO agro_identity_app
                    USING (
                        nullif(current_setting('app.current_scope_kind', true), '') = 'tenant'
                        AND "OrganizationId"::text = nullif(
                            current_setting('app.current_organization_id', true), '')
                        AND "ActorUserId"::text = nullif(
                            current_setting('app.current_actor_id', true), '')
                        AND "SessionId"::text = nullif(
                            current_setting('app.current_session_id', true), '')
                        AND "AuthorizationVersion"::text = nullif(
                            current_setting('app.current_authorization_version', true), '')
                    )
                    WITH CHECK (
                        nullif(current_setting('app.current_scope_kind', true), '') = 'tenant'
                        AND "OrganizationId"::text = nullif(
                            current_setting('app.current_organization_id', true), '')
                        AND "ActorUserId"::text = nullif(
                            current_setting('app.current_actor_id', true), '')
                        AND "SessionId"::text = nullif(
                            current_setting('app.current_session_id', true), '')
                        AND "AuthorizationVersion"::text = nullif(
                            current_setting('app.current_authorization_version', true), '')
                    );
                CREATE POLICY owner_removal_aliases_app_access
                    ON identity.organization_owner_removal_key_aliases
                    FOR ALL TO agro_identity_app
                    USING (
                        "OrganizationId"::text = nullif(
                            current_setting('app.current_organization_id', true), '')
                        AND EXISTS (
                            SELECT 1
                            FROM identity.organization_owner_removal_ledgers AS ledger
                            WHERE ledger."Id" = organization_owner_removal_key_aliases."LedgerId"
                              AND ledger."OrganizationId" =
                                  organization_owner_removal_key_aliases."OrganizationId"
                        )
                    )
                    WITH CHECK (
                        "OrganizationId"::text = nullif(
                            current_setting('app.current_organization_id', true), '')
                        AND EXISTS (
                            SELECT 1
                            FROM identity.organization_owner_removal_ledgers AS ledger
                            WHERE ledger."Id" = organization_owner_removal_key_aliases."LedgerId"
                              AND ledger."OrganizationId" =
                                  organization_owner_removal_key_aliases."OrganizationId"
                        )
                    );
                CREATE POLICY owner_removal_ledgers_owner_key_coverage
                    ON identity.organization_owner_removal_ledgers
                    FOR SELECT TO agro_identity_owner USING (true);
                CREATE POLICY owner_removal_aliases_owner_key_coverage
                    ON identity.organization_owner_removal_key_aliases
                    FOR SELECT TO agro_identity_owner USING (true);

                CREATE FUNCTION identity.lock_owner_management_organization()
                RETURNS boolean
                LANGUAGE plpgsql
                VOLATILE
                SECURITY DEFINER
                SET search_path = pg_catalog
                AS $lock_owner_management$
                DECLARE
                    actor_id uuid;
                    organization_id uuid;
                BEGIN
                    IF nullif(current_setting('app.current_scope_kind', true), '') <> 'tenant' THEN
                        RETURN false;
                    END IF;
                    BEGIN
                        actor_id := nullif(current_setting('app.current_actor_id', true), '')::uuid;
                        organization_id := nullif(
                            current_setting('app.current_organization_id', true), '')::uuid;
                    EXCEPTION WHEN invalid_text_representation OR null_value_not_allowed THEN
                        RETURN false;
                    END;
                    IF actor_id IS NULL OR organization_id IS NULL THEN
                        RETURN false;
                    END IF;
                    PERFORM 1
                    FROM identity.organizations AS organization
                    WHERE organization."Id" = organization_id
                      AND organization."Status" = 'active'
                    FOR UPDATE;
                    IF NOT FOUND THEN
                        RETURN false;
                    END IF;
                    RETURN EXISTS (
                        SELECT 1
                        FROM identity.memberships AS membership
                        WHERE membership."OrganizationId" = organization_id
                          AND membership."UserId" = actor_id
                          AND membership."Role" = 'owner'
                          AND membership."Status" = 'active');
                END
                $lock_owner_management$;

                CREATE FUNCTION identity.lock_owner_invitation_acceptance_organization(
                    p_invitation_id uuid)
                RETURNS boolean
                LANGUAGE plpgsql
                VOLATILE
                SECURITY DEFINER
                SET search_path = pg_catalog
                AS $lock_owner_acceptance$
                DECLARE
                    actor_id uuid;
                    organization_id uuid;
                BEGIN
                    IF p_invitation_id IS NULL
                       OR nullif(current_setting('app.current_scope_kind', true), '') <> 'tenant'
                       OR p_invitation_id::text <> nullif(
                           current_setting('app.current_invitation_id', true), '') THEN
                        RETURN false;
                    END IF;
                    BEGIN
                        actor_id := nullif(current_setting('app.current_actor_id', true), '')::uuid;
                        organization_id := nullif(
                            current_setting('app.current_organization_id', true), '')::uuid;
                    EXCEPTION WHEN invalid_text_representation OR null_value_not_allowed THEN
                        RETURN false;
                    END;
                    IF actor_id IS NULL OR organization_id IS NULL THEN
                        RETURN false;
                    END IF;
                    PERFORM 1
                    FROM identity.organizations AS organization
                    WHERE organization."Id" = organization_id
                      AND organization."Status" = 'active'
                    FOR UPDATE;
                    IF NOT FOUND THEN
                        RETURN false;
                    END IF;
                    RETURN EXISTS (
                        SELECT 1
                        FROM identity.organization_owner_invitations AS invitation
                        WHERE invitation."Id" = p_invitation_id
                          AND invitation."OrganizationId" = organization_id
                          AND (
                              invitation."Status" = 'pending'
                              OR
                              (invitation."Status" = 'accepted'
                               AND invitation."AcceptedByUserId" = actor_id)
                          ));
                END
                $lock_owner_acceptance$;

                CREATE FUNCTION identity.list_active_owner_memberships()
                RETURNS TABLE (
                    membership_id uuid,
                    display_name text,
                    role text,
                    status text,
                    security_version bigint,
                    created_at_utc timestamptz,
                    version uuid,
                    is_current_user boolean)
                LANGUAGE plpgsql
                STABLE
                SECURITY DEFINER
                SET search_path = pg_catalog
                AS $list_active_owners$
                DECLARE
                    actor_id uuid;
                    organization_id uuid;
                BEGIN
                    IF nullif(current_setting('app.current_scope_kind', true), '') <> 'tenant' THEN
                        RETURN;
                    END IF;
                    BEGIN
                        actor_id := nullif(current_setting('app.current_actor_id', true), '')::uuid;
                        organization_id := nullif(
                            current_setting('app.current_organization_id', true), '')::uuid;
                    EXCEPTION WHEN invalid_text_representation OR null_value_not_allowed THEN
                        RETURN;
                    END;
                    IF actor_id IS NULL OR organization_id IS NULL
                       OR NOT EXISTS (
                            SELECT 1
                            FROM identity.memberships AS actor_membership
                            WHERE actor_membership."OrganizationId" = organization_id
                              AND actor_membership."UserId" = actor_id
                              AND actor_membership."Role" = 'owner'
                              AND actor_membership."Status" = 'active') THEN
                        RETURN;
                    END IF;
                    RETURN QUERY
                    SELECT membership."Id", platform_user."DisplayName"::text,
                           membership."Role"::text, membership."Status"::text,
                           membership."SecurityVersion", membership."CreatedAtUtc",
                           membership."Version", membership."UserId" = actor_id
                    FROM identity.memberships AS membership
                    JOIN identity.users AS platform_user
                      ON platform_user."Id" = membership."UserId"
                    WHERE membership."OrganizationId" = organization_id
                      AND membership."Role" = 'owner'
                      AND membership."Status" = 'active'
                    ORDER BY membership."CreatedAtUtc", membership."Id";
                END
                $list_active_owners$;

                CREATE FUNCTION identity.remove_active_owner(
                    p_membership_id uuid,
                    p_expected_version uuid,
                    p_removed_at timestamptz,
                    p_new_version uuid)
                RETURNS TABLE (
                    outcome text,
                    membership_id uuid,
                    display_name text,
                    role text,
                    status text,
                    security_version bigint,
                    created_at_utc timestamptz,
                    removed_at_utc timestamptz,
                    version uuid,
                    revoked_invitation_ids uuid[],
                    is_current_user boolean)
                LANGUAGE plpgsql
                VOLATILE
                SECURITY DEFINER
                SET search_path = pg_catalog
                AS $remove_active_owner$
                DECLARE
                    actor_id uuid;
                    organization_id uuid;
                    session_id uuid;
                    authorization_version uuid;
                    target identity.memberships%ROWTYPE;
                BEGIN
                    outcome := 'not_available';
                    revoked_invitation_ids := ARRAY[]::uuid[];
                    is_current_user := false;
                    IF p_membership_id IS NULL OR p_expected_version IS NULL
                       OR p_removed_at IS NULL OR p_new_version IS NULL
                       OR p_expected_version = '00000000-0000-0000-0000-000000000000'::uuid
                       OR p_new_version = '00000000-0000-0000-0000-000000000000'::uuid
                       OR nullif(current_setting('app.current_scope_kind', true), '') <> 'tenant' THEN
                        RETURN NEXT;
                        RETURN;
                    END IF;
                    BEGIN
                        actor_id := nullif(current_setting('app.current_actor_id', true), '')::uuid;
                        organization_id := nullif(
                            current_setting('app.current_organization_id', true), '')::uuid;
                        session_id := nullif(current_setting('app.current_session_id', true), '')::uuid;
                        authorization_version := nullif(
                            current_setting('app.current_authorization_version', true), '')::uuid;
                    EXCEPTION WHEN invalid_text_representation OR null_value_not_allowed THEN
                        RETURN NEXT;
                        RETURN;
                    END;
                    IF actor_id IS NULL OR organization_id IS NULL OR session_id IS NULL
                       OR authorization_version IS NULL THEN
                        RETURN NEXT;
                        RETURN;
                    END IF;
                    PERFORM 1
                    FROM identity.organizations AS organization
                    WHERE organization."Id" = organization_id
                      AND organization."Status" = 'active'
                    FOR UPDATE;
                    IF NOT FOUND
                       OR NOT EXISTS (
                            SELECT 1
                            FROM identity.sessions AS actor_session
                            WHERE actor_session."Id" = session_id
                              AND actor_session."UserId" = actor_id
                              AND actor_session."Version" = authorization_version
                              AND actor_session."RevokedAtUtc" IS NULL
                              AND actor_session."ExpiresAtUtc" > statement_timestamp()
                              AND actor_session."IsAuthenticationAssuranceVerified"
                              AND actor_session."StrongAuthenticatedAtUtc" IS NOT NULL
                              AND actor_session."StrongAuthenticatedAtUtc" <= statement_timestamp()
                              AND actor_session."StrongAuthenticationPurpose" =
                                  'manage_organization_owners')
                       OR NOT EXISTS (
                            SELECT 1
                            FROM identity.memberships AS actor_membership
                            WHERE actor_membership."OrganizationId" = organization_id
                              AND actor_membership."UserId" = actor_id
                              AND actor_membership."Role" = 'owner'
                              AND actor_membership."Status" = 'active') THEN
                        RETURN NEXT;
                        RETURN;
                    END IF;
                    SELECT membership.*
                    INTO target
                    FROM identity.memberships AS membership
                    WHERE membership."OrganizationId" = organization_id
                      AND membership."Id" = p_membership_id
                      AND membership."Role" = 'owner'
                      AND membership."Status" = 'active';
                    IF NOT FOUND OR target."UserId" = actor_id
                       OR p_removed_at < target."CreatedAtUtc" THEN
                        RETURN NEXT;
                        RETURN;
                    END IF;
                    IF target."Version" <> p_expected_version THEN
                        outcome := 'version_mismatch';
                        RETURN NEXT;
                        RETURN;
                    END IF;
                    IF NOT EXISTS (
                        SELECT 1
                        FROM identity.memberships AS remaining_owner
                        WHERE remaining_owner."OrganizationId" = organization_id
                          AND remaining_owner."Role" = 'owner'
                          AND remaining_owner."Status" = 'active'
                          AND remaining_owner."Id" <> target."Id") THEN
                        outcome := 'last_owner';
                        RETURN NEXT;
                        RETURN;
                    END IF;
                    UPDATE identity.memberships AS removed_membership
                    SET "Status" = 'removed',
                        "SecurityVersion" = removed_membership."SecurityVersion" + 1,
                        "RemovedAtUtc" = p_removed_at,
                        "RemovedByUserId" = actor_id,
                        "Version" = p_new_version
                    WHERE removed_membership."Id" = target."Id"
                      AND removed_membership."OrganizationId" = organization_id
                      AND removed_membership."Status" = 'active'
                      AND removed_membership."Version" = p_expected_version
                    RETURNING removed_membership.* INTO target;
                    IF NOT FOUND THEN
                        outcome := 'version_mismatch';
                        RETURN NEXT;
                        RETURN;
                    END IF;
                    DELETE FROM identity.organization_memberships AS legacy_membership
                    WHERE legacy_membership."OrganizationId" = organization_id
                      AND legacy_membership."UserId" = target."UserId";
                    WITH revoked AS (
                        UPDATE identity.organization_owner_invitations AS invitation
                        SET "Status" = 'revoked',
                            "RevokedAtUtc" = p_removed_at,
                            "RevokedByUserId" = actor_id,
                            "Version" = gen_random_uuid()
                        WHERE invitation."OrganizationId" = organization_id
                          AND invitation."CreatedByUserId" = target."UserId"
                          AND invitation."Status" = 'pending'
                          AND invitation."ExpiresAtUtc" > p_removed_at
                        RETURNING invitation."Id")
                    SELECT coalesce(array_agg(revoked."Id" ORDER BY revoked."Id"), ARRAY[]::uuid[])
                    INTO revoked_invitation_ids
                    FROM revoked;
                    SELECT platform_user."DisplayName"::text
                    INTO display_name
                    FROM identity.users AS platform_user
                    WHERE platform_user."Id" = target."UserId";
                    outcome := 'removed';
                    membership_id := target."Id";
                    role := target."Role";
                    status := target."Status";
                    security_version := target."SecurityVersion";
                    created_at_utc := target."CreatedAtUtc";
                    removed_at_utc := target."RemovedAtUtc";
                    version := target."Version";
                    RETURN NEXT;
                END
                $remove_active_owner$;

                CREATE FUNCTION identity.resolve_owner_removal_result(p_ledger_id uuid)
                RETURNS TABLE (
                    membership_id uuid,
                    display_name text,
                    role text,
                    status text,
                    security_version bigint,
                    created_at_utc timestamptz,
                    removed_at_utc timestamptz,
                    version uuid,
                    is_current_user boolean)
                LANGUAGE sql
                STABLE
                SECURITY DEFINER
                SET search_path = pg_catalog
                AS $resolve_owner_removal$
                    SELECT membership."Id", platform_user."DisplayName"::text,
                           membership."Role"::text, membership."Status"::text,
                           membership."SecurityVersion", membership."CreatedAtUtc",
                           membership."RemovedAtUtc", membership."Version",
                           membership."UserId"::text = nullif(
                               current_setting('app.current_actor_id', true), '')
                    FROM identity.organization_owner_removal_ledgers AS ledger
                    JOIN identity.memberships AS membership
                      ON membership."OrganizationId" = ledger."OrganizationId"
                     AND membership."Id" = ledger."MembershipId"
                    JOIN identity.users AS platform_user
                      ON platform_user."Id" = membership."UserId"
                    WHERE p_ledger_id IS NOT NULL
                      AND ledger."Id" = p_ledger_id
                      AND ledger."State" = 'succeeded'
                      AND ledger."OrganizationId"::text = nullif(
                          current_setting('app.current_organization_id', true), '')
                      AND ledger."ActorUserId"::text = nullif(
                          current_setting('app.current_actor_id', true), '')
                      AND ledger."SessionId"::text = nullif(
                          current_setting('app.current_session_id', true), '')
                      AND ledger."AuthorizationVersion"::text = nullif(
                          current_setting('app.current_authorization_version', true), '')
                      AND membership."Status" = 'removed'
                      AND membership."Version" = ledger."ResultMembershipVersion"
                      AND membership."SecurityVersion" = ledger."ResultAuthorizationVersion"
                $resolve_owner_removal$;

                CREATE FUNCTION identity.owner_removal_retained_key_covered(
                    retained_versions text[])
                RETURNS boolean
                LANGUAGE sql
                STABLE
                SECURITY DEFINER
                SET search_path = pg_catalog
                AS $owner_removal_key_coverage$
                    SELECT retained_versions IS NOT NULL
                       AND cardinality(retained_versions) > 0
                       AND NOT EXISTS (
                            SELECT 1
                            FROM identity.organization_owner_removal_ledgers AS ledger
                            WHERE NOT EXISTS (
                                SELECT 1
                                FROM identity.organization_owner_removal_key_aliases AS alias
                                WHERE alias."LedgerId" = ledger."Id"
                                  AND alias."OrganizationId" = ledger."OrganizationId"
                                  AND alias."KeyVersion" = ANY(retained_versions)))
                $owner_removal_key_coverage$;

                ALTER FUNCTION identity.lock_owner_management_organization()
                    OWNER TO agro_identity_owner;
                ALTER FUNCTION identity.lock_owner_invitation_acceptance_organization(uuid)
                    OWNER TO agro_identity_owner;
                ALTER FUNCTION identity.list_active_owner_memberships()
                    OWNER TO agro_identity_owner;
                ALTER FUNCTION identity.remove_active_owner(uuid, uuid, timestamptz, uuid)
                    OWNER TO agro_identity_owner;
                ALTER FUNCTION identity.resolve_owner_removal_result(uuid)
                    OWNER TO agro_identity_owner;
                ALTER FUNCTION identity.owner_removal_retained_key_covered(text[])
                    OWNER TO agro_identity_owner;
                REVOKE ALL ON FUNCTION identity.lock_owner_management_organization() FROM PUBLIC;
                REVOKE ALL ON FUNCTION
                    identity.lock_owner_invitation_acceptance_organization(uuid) FROM PUBLIC;
                REVOKE ALL ON FUNCTION identity.list_active_owner_memberships() FROM PUBLIC;
                REVOKE ALL ON FUNCTION
                    identity.remove_active_owner(uuid, uuid, timestamptz, uuid) FROM PUBLIC;
                REVOKE ALL ON FUNCTION identity.resolve_owner_removal_result(uuid) FROM PUBLIC;
                REVOKE ALL ON FUNCTION
                    identity.owner_removal_retained_key_covered(text[]) FROM PUBLIC;
                GRANT EXECUTE ON FUNCTION identity.lock_owner_management_organization()
                    TO agro_identity_app;
                GRANT EXECUTE ON FUNCTION
                    identity.lock_owner_invitation_acceptance_organization(uuid)
                    TO agro_identity_app;
                GRANT EXECUTE ON FUNCTION identity.list_active_owner_memberships()
                    TO agro_identity_app;
                GRANT EXECUTE ON FUNCTION
                    identity.remove_active_owner(uuid, uuid, timestamptz, uuid)
                    TO agro_identity_app;
                GRANT EXECUTE ON FUNCTION identity.resolve_owner_removal_result(uuid)
                    TO agro_identity_app;
                GRANT EXECUTE ON FUNCTION
                    identity.owner_removal_retained_key_covered(text[])
                    TO agro_identity_app;
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
                            'The organization owner removal down migration is allowed only on an ephemeral test database';
                    END IF;
                END
                $ephemeral_only$;

                DROP FUNCTION IF EXISTS identity.owner_removal_retained_key_covered(text[]);
                DROP FUNCTION IF EXISTS identity.resolve_owner_removal_result(uuid);
                DROP FUNCTION IF EXISTS
                    identity.remove_active_owner(uuid, uuid, timestamptz, uuid);
                DROP FUNCTION IF EXISTS identity.list_active_owner_memberships();
                DROP FUNCTION IF EXISTS
                    identity.lock_owner_invitation_acceptance_organization(uuid);
                DROP FUNCTION IF EXISTS identity.lock_owner_management_organization();

                DROP POLICY IF EXISTS owner_invitations_owner_owner_removal_update
                    ON identity.organization_owner_invitations;
                DROP POLICY IF EXISTS memberships_owner_owner_management_update
                    ON identity.memberships;
                DROP POLICY IF EXISTS memberships_owner_owner_management_read
                    ON identity.memberships;
                DROP POLICY IF EXISTS organizations_owner_owner_management
                    ON identity.organizations;
                DROP POLICY IF EXISTS organizations_owner_owner_management_update
                    ON identity.organizations;
                DROP POLICY IF EXISTS sessions_owner_owner_removal_read
                    ON identity.sessions;
                REVOKE DELETE, SELECT ("UserId", "OrganizationId")
                    ON TABLE identity.organization_memberships
                    FROM agro_identity_owner;
                REVOKE SELECT (
                    "Id", "UserId", "ExpiresAtUtc", "IsAuthenticationAssuranceVerified",
                    "StrongAuthenticatedAtUtc", "StrongAuthenticationPurpose", "RevokedAtUtc", "Version")
                    ON TABLE identity.sessions FROM agro_identity_owner;
                REVOKE SELECT ("Id", "DisplayName") ON TABLE identity.users
                    FROM agro_identity_owner;

                INSERT INTO identity.organization_memberships
                    ("UserId", "OrganizationId", "OrganizationName", "Role")
                SELECT membership."UserId", membership."OrganizationId",
                       organization."DisplayName", membership."Role"
                FROM identity.memberships AS membership
                JOIN identity.organizations AS organization
                  ON organization."Id" = membership."OrganizationId"
                WHERE membership."Status" = 'removed'
                ON CONFLICT ("UserId", "OrganizationId") DO NOTHING;
                UPDATE identity.memberships
                SET "Status" = 'active',
                    "RemovedAtUtc" = NULL,
                    "RemovedByUserId" = NULL;

                ALTER TABLE identity.memberships
                    DROP CONSTRAINT IF EXISTS "CK_memberships_Version";
                """);

            migrationBuilder.DropForeignKey(
                name: "FK_memberships_users_RemovedByUserId",
                schema: "identity",
                table: "memberships");

            migrationBuilder.DropTable(
                name: "organization_owner_removal_key_aliases",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "organization_owner_removal_ledgers",
                schema: "identity");

            migrationBuilder.Sql(
                """
                DROP INDEX IF EXISTS identity."UX_memberships_OrganizationId_Id";
                """);

            migrationBuilder.DropIndex(
                name: "IX_memberships_RemovedByUserId",
                schema: "identity",
                table: "memberships");

            migrationBuilder.DropCheckConstraint(
                name: "CK_memberships_Status",
                schema: "identity",
                table: "memberships");

            migrationBuilder.DropColumn(
                name: "RemovedAtUtc",
                schema: "identity",
                table: "memberships");

            migrationBuilder.DropColumn(
                name: "RemovedByUserId",
                schema: "identity",
                table: "memberships");

            migrationBuilder.DropColumn(
                name: "Version",
                schema: "identity",
                table: "memberships");

            migrationBuilder.AddCheckConstraint(
                name: "CK_memberships_Status",
                schema: "identity",
                table: "memberships",
                sql: "\"Status\" = 'active'");
        }
    }
}
