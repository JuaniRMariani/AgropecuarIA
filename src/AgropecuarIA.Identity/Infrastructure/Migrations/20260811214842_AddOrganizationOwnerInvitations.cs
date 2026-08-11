using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1861 // EF Core scaffolds transient arrays for migration metadata.

namespace AgropecuarIA.Identity.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOrganizationOwnerInvitations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                ALTER TABLE identity.sessions
                    DROP CONSTRAINT "CK_sessions_StrongAuthentication";
                ALTER TABLE identity.sessions
                    ADD CONSTRAINT "CK_sessions_StrongAuthentication"
                    CHECK (
                        ("StrongAuthenticatedAtUtc" IS NULL
                         AND "StrongAuthenticationPurpose" IS NULL)
                        OR
                        ("StrongAuthenticatedAtUtc" IS NOT NULL
                         AND "StrongAuthenticationPurpose" IS NOT NULL
                         AND "StrongAuthenticationPurpose" IN (
                             'manage_authentication_methods',
                             'manage_organization_owners'))
                    );
                ALTER TABLE identity.step_up_attempts
                    DROP CONSTRAINT "CK_step_up_attempts_Purpose";
                ALTER TABLE identity.step_up_attempts
                    ADD CONSTRAINT "CK_step_up_attempts_Purpose"
                    CHECK (
                        "Purpose" IN (
                            'manage_authentication_methods',
                            'manage_organization_owners')
                    );
                """);

            migrationBuilder.CreateTable(
                name: "organization_owner_invitations",
                schema: "identity",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreationSessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreationKeyVersion = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    CreationKeyDigest = table.Column<byte[]>(type: "bytea", maxLength: 32, nullable: false),
                    TokenKeyVersion = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    TokenDigest = table.Column<byte[]>(type: "bytea", maxLength: 32, nullable: false),
                    Status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ExpiresAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    AcceptedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    AcceptedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    AcceptedMembershipId = table.Column<Guid>(type: "uuid", nullable: true),
                    RevokedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    RevokedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    Version = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_organization_owner_invitations", x => x.Id);
                    table.CheckConstraint("CK_organization_owner_invitations_Digests", "octet_length(\"CreationKeyDigest\") = 32 AND octet_length(\"TokenDigest\") = 32");
                    table.CheckConstraint("CK_organization_owner_invitations_Expiry", "\"ExpiresAtUtc\" > \"CreatedAtUtc\"");
                    table.CheckConstraint("CK_organization_owner_invitations_State", "(\"Status\" = 'pending' AND \"AcceptedAtUtc\" IS NULL AND \"AcceptedByUserId\" IS NULL AND \"AcceptedMembershipId\" IS NULL AND \"RevokedAtUtc\" IS NULL AND \"RevokedByUserId\" IS NULL) OR (\"Status\" = 'accepted' AND \"AcceptedAtUtc\" >= \"CreatedAtUtc\" AND \"AcceptedAtUtc\" < \"ExpiresAtUtc\" AND \"AcceptedByUserId\" IS NOT NULL AND \"AcceptedMembershipId\" IS NOT NULL AND \"RevokedAtUtc\" IS NULL AND \"RevokedByUserId\" IS NULL) OR (\"Status\" = 'revoked' AND \"RevokedAtUtc\" >= \"CreatedAtUtc\" AND \"RevokedAtUtc\" < \"ExpiresAtUtc\" AND \"RevokedByUserId\" IS NOT NULL AND \"AcceptedAtUtc\" IS NULL AND \"AcceptedByUserId\" IS NULL AND \"AcceptedMembershipId\" IS NULL)");
                    table.ForeignKey(
                        name: "FK_organization_owner_invitations_memberships_AcceptedMembersh~",
                        column: x => x.AcceptedMembershipId,
                        principalSchema: "identity",
                        principalTable: "memberships",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_organization_owner_invitations_organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalSchema: "identity",
                        principalTable: "organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_organization_owner_invitations_sessions_CreationSessionId",
                        column: x => x.CreationSessionId,
                        principalSchema: "identity",
                        principalTable: "sessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_organization_owner_invitations_users_AcceptedByUserId",
                        column: x => x.AcceptedByUserId,
                        principalSchema: "identity",
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_organization_owner_invitations_users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalSchema: "identity",
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_organization_owner_invitations_users_RevokedByUserId",
                        column: x => x.RevokedByUserId,
                        principalSchema: "identity",
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_organization_owner_invitations_AcceptedByUserId",
                schema: "identity",
                table: "organization_owner_invitations",
                column: "AcceptedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_organization_owner_invitations_AcceptedMembershipId",
                schema: "identity",
                table: "organization_owner_invitations",
                column: "AcceptedMembershipId");

            migrationBuilder.CreateIndex(
                name: "IX_organization_owner_invitations_CreatedByUserId",
                schema: "identity",
                table: "organization_owner_invitations",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_organization_owner_invitations_CreationSessionId",
                schema: "identity",
                table: "organization_owner_invitations",
                column: "CreationSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_organization_owner_invitations_OrganizationId_CreationKeyVe~",
                schema: "identity",
                table: "organization_owner_invitations",
                columns: new[] { "OrganizationId", "CreationKeyVersion", "CreationKeyDigest" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_organization_owner_invitations_OrganizationId_Status_Expire~",
                schema: "identity",
                table: "organization_owner_invitations",
                columns: new[] { "OrganizationId", "Status", "ExpiresAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_organization_owner_invitations_RevokedByUserId",
                schema: "identity",
                table: "organization_owner_invitations",
                column: "RevokedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_organization_owner_invitations_TokenKeyVersion_TokenDigest",
                schema: "identity",
                table: "organization_owner_invitations",
                columns: new[] { "TokenKeyVersion", "TokenDigest" },
                unique: true);

            migrationBuilder.Sql(
                """
                ALTER TABLE identity.organization_owner_invitations
                    OWNER TO agro_identity_owner;

                ALTER TABLE identity.organization_owner_invitations
                    ADD CONSTRAINT "FK_owner_invitations_sessions_Creator"
                    FOREIGN KEY ("CreationSessionId", "CreatedByUserId")
                    REFERENCES identity.sessions ("Id", "UserId")
                    ON DELETE RESTRICT;
                ALTER TABLE identity.organization_owner_invitations
                    ADD CONSTRAINT "FK_owner_invitations_memberships_AcceptedOwner"
                    FOREIGN KEY ("OrganizationId", "AcceptedMembershipId", "AcceptedByUserId")
                    REFERENCES identity.memberships ("OrganizationId", "Id", "UserId")
                    ON DELETE RESTRICT;

                ALTER TABLE identity.organization_owner_invitations ENABLE ROW LEVEL SECURITY;
                ALTER TABLE identity.organization_owner_invitations FORCE ROW LEVEL SECURITY;

                REVOKE ALL ON TABLE identity.organization_owner_invitations FROM PUBLIC;
                REVOKE ALL ON TABLE identity.organization_owner_invitations
                    FROM agro_identity_app, agro_identity_job, agro_identity_discovery;
                GRANT SELECT, INSERT, UPDATE
                    ON TABLE identity.organization_owner_invitations TO agro_identity_app;
                GRANT SELECT ("StrongAuthenticatedAtUtc", "StrongAuthenticationPurpose")
                    ON TABLE identity.sessions TO agro_identity_app;

                CREATE POLICY sessions_app_owner_invitation_read
                    ON identity.sessions
                    FOR SELECT
                    TO agro_identity_app
                    USING (
                        nullif(current_setting('app.current_scope_kind', true), '') = 'tenant'
                        AND "UserId"::text = nullif(
                            current_setting('app.current_actor_id', true), '')
                    );

                CREATE POLICY organization_owner_invitations_owner_token_lookup
                    ON identity.organization_owner_invitations
                    FOR SELECT
                    TO agro_identity_owner
                    USING (true);

                CREATE FUNCTION identity.resolve_owner_invitation_by_token(
                    p_token_key_version text,
                    p_token_digest bytea)
                RETURNS uuid
                LANGUAGE plpgsql
                VOLATILE
                SECURITY DEFINER
                SET search_path = pg_catalog
                AS $resolve_invitation$
                DECLARE
                    resolved_id uuid;
                BEGIN
                    IF p_token_key_version IS NULL
                       OR length(p_token_key_version) NOT BETWEEN 1 AND 32
                       OR p_token_digest IS NULL
                       OR octet_length(p_token_digest) <> 32 THEN
                        PERFORM pg_catalog.set_config(
                            'app.current_invitation_id',
                            '',
                            true);
                        RETURN NULL;
                    END IF;

                    SELECT invitation."Id"
                    INTO resolved_id
                    FROM identity.organization_owner_invitations AS invitation
                    WHERE invitation."TokenKeyVersion" = p_token_key_version
                      AND invitation."TokenDigest" = p_token_digest;

                    PERFORM pg_catalog.set_config(
                        'app.current_invitation_id',
                        coalesce(resolved_id::text, ''),
                        true);
                    RETURN resolved_id;
                END
                $resolve_invitation$;
                ALTER FUNCTION identity.resolve_owner_invitation_by_token(text, bytea)
                    OWNER TO agro_identity_owner;
                REVOKE ALL ON FUNCTION
                    identity.resolve_owner_invitation_by_token(text, bytea) FROM PUBLIC;
                GRANT EXECUTE ON FUNCTION
                    identity.resolve_owner_invitation_by_token(text, bytea) TO agro_identity_app;

                CREATE FUNCTION identity.owner_invitation_retained_key_covered(
                    retained_versions text[])
                RETURNS boolean
                LANGUAGE sql
                STABLE
                SECURITY DEFINER
                SET search_path = pg_catalog
                AS $retained_key_coverage$
                    SELECT retained_versions IS NOT NULL
                       AND NOT EXISTS (
                            SELECT 1
                            FROM identity.organization_owner_invitations AS invitation
                            WHERE NOT (
                                  invitation."TokenKeyVersion" = ANY(retained_versions)
                              )
                       )
                $retained_key_coverage$;
                ALTER FUNCTION identity.owner_invitation_retained_key_covered(text[])
                    OWNER TO agro_identity_owner;
                REVOKE ALL ON FUNCTION
                    identity.owner_invitation_retained_key_covered(text[]) FROM PUBLIC;
                GRANT EXECUTE ON FUNCTION
                    identity.owner_invitation_retained_key_covered(text[])
                    TO agro_identity_app;

                CREATE FUNCTION identity.owner_invitation_context_allows_organization(
                    p_organization_id uuid)
                RETURNS boolean
                LANGUAGE sql
                STABLE
                SECURITY DEFINER
                SET search_path = pg_catalog
                AS $invitation_context$
                    SELECT coalesce(p_organization_id IS NOT NULL, false)
                       AND EXISTS (
                            SELECT 1
                            FROM identity.organization_owner_invitations AS invitation
                            WHERE invitation."Id"::text = nullif(
                                      pg_catalog.current_setting(
                                          'app.current_invitation_id', true),
                                      '')
                              AND invitation."OrganizationId" = p_organization_id
                              AND (
                                  (
                                      invitation."Status" = 'pending'
                                      AND invitation."ExpiresAtUtc" > statement_timestamp()
                                  )
                                  OR (
                                      invitation."Status" = 'accepted'
                                      AND invitation."AcceptedByUserId"::text = nullif(
                                          pg_catalog.current_setting(
                                              'app.current_actor_id', true),
                                          '')
                                  )
                              )
                       )
                $invitation_context$;
                ALTER FUNCTION identity.owner_invitation_context_allows_organization(uuid)
                    OWNER TO agro_identity_owner;
                REVOKE ALL ON FUNCTION
                    identity.owner_invitation_context_allows_organization(uuid) FROM PUBLIC;
                GRANT EXECUTE ON FUNCTION
                    identity.owner_invitation_context_allows_organization(uuid)
                    TO agro_identity_app;

                CREATE POLICY organization_owner_invitations_app_tenant_read
                    ON identity.organization_owner_invitations
                    FOR SELECT
                    TO agro_identity_app
                    USING (
                        nullif(current_setting('app.current_scope_kind', true), '') = 'tenant'
                        AND "OrganizationId"::text = nullif(
                            current_setting('app.current_organization_id', true), '')
                        AND EXISTS (
                            SELECT 1
                            FROM identity.memberships AS actor_membership
                            WHERE actor_membership."OrganizationId" =
                                  organization_owner_invitations."OrganizationId"
                              AND actor_membership."UserId"::text = nullif(
                                  current_setting('app.current_actor_id', true), '')
                              AND actor_membership."Role" = 'owner'
                              AND actor_membership."Status" = 'active'
                        )
                    );

                CREATE POLICY organization_owner_invitations_app_resolved_read
                    ON identity.organization_owner_invitations
                    FOR SELECT
                    TO agro_identity_app
                    USING (
                        nullif(current_setting('app.current_scope_kind', true), '') = 'platform'
                        AND "Id"::text = nullif(
                            current_setting('app.current_invitation_id', true), '')
                        AND (
                            "Status" = 'pending'
                            OR (
                                "Status" = 'accepted'
                                AND "AcceptedByUserId"::text = nullif(
                                    current_setting('app.current_actor_id', true), '')
                            )
                        )
                    );

                CREATE POLICY organization_owner_invitations_app_resolved_tenant_read
                    ON identity.organization_owner_invitations
                    FOR SELECT
                    TO agro_identity_app
                    USING (
                        nullif(current_setting('app.current_scope_kind', true), '') = 'tenant'
                        AND "OrganizationId"::text = nullif(
                            current_setting('app.current_organization_id', true), '')
                        AND "Id"::text = nullif(
                            current_setting('app.current_invitation_id', true), '')
                        AND (
                            (
                                "Status" = 'pending'
                                AND "ExpiresAtUtc" > statement_timestamp()
                            )
                            OR (
                                "Status" = 'accepted'
                                AND "AcceptedByUserId"::text = nullif(
                                    current_setting('app.current_actor_id', true), '')
                            )
                        )
                    );

                CREATE POLICY organization_owner_invitations_app_create
                    ON identity.organization_owner_invitations
                    FOR INSERT
                    TO agro_identity_app
                    WITH CHECK (
                        nullif(current_setting('app.current_scope_kind', true), '') = 'tenant'
                        AND "OrganizationId"::text = nullif(
                            current_setting('app.current_organization_id', true), '')
                        AND "CreatedByUserId"::text = nullif(
                            current_setting('app.current_actor_id', true), '')
                        AND "Status" = 'pending'
                        AND EXISTS (
                            SELECT 1
                            FROM identity.memberships AS actor_membership
                            WHERE actor_membership."OrganizationId" =
                                  organization_owner_invitations."OrganizationId"
                              AND actor_membership."UserId" =
                                  organization_owner_invitations."CreatedByUserId"
                              AND actor_membership."Role" = 'owner'
                              AND actor_membership."Status" = 'active'
                        )
                    );

                CREATE POLICY organization_owner_invitations_app_revoke
                    ON identity.organization_owner_invitations
                    FOR UPDATE
                    TO agro_identity_app
                    USING (
                        nullif(current_setting('app.current_scope_kind', true), '') = 'tenant'
                        AND "OrganizationId"::text = nullif(
                            current_setting('app.current_organization_id', true), '')
                        AND EXISTS (
                            SELECT 1
                            FROM identity.memberships AS actor_membership
                            WHERE actor_membership."OrganizationId" =
                                  organization_owner_invitations."OrganizationId"
                              AND actor_membership."UserId"::text = nullif(
                                  current_setting('app.current_actor_id', true), '')
                              AND actor_membership."Role" = 'owner'
                              AND actor_membership."Status" = 'active'
                        )
                    )
                    WITH CHECK (
                        "OrganizationId"::text = nullif(
                            current_setting('app.current_organization_id', true), '')
                        AND "Status" = 'revoked'
                        AND "RevokedByUserId"::text = nullif(
                            current_setting('app.current_actor_id', true), '')
                    );

                CREATE POLICY organization_owner_invitations_app_accept
                    ON identity.organization_owner_invitations
                    FOR UPDATE
                    TO agro_identity_app
                    USING (
                        nullif(current_setting('app.current_scope_kind', true), '') = 'tenant'
                        AND "OrganizationId"::text = nullif(
                            current_setting('app.current_organization_id', true), '')
                        AND "Id"::text = nullif(
                            current_setting('app.current_invitation_id', true), '')
                        AND "Status" = 'pending'
                        AND "ExpiresAtUtc" > statement_timestamp()
                    )
                    WITH CHECK (
                        "OrganizationId"::text = nullif(
                            current_setting('app.current_organization_id', true), '')
                        AND "Id"::text = nullif(
                            current_setting('app.current_invitation_id', true), '')
                        AND "Status" = 'accepted'
                        AND "AcceptedByUserId"::text = nullif(
                            current_setting('app.current_actor_id', true), '')
                        AND "AcceptedMembershipId" IS NOT NULL
                        AND "RevokedAtUtc" IS NULL
                        AND "RevokedByUserId" IS NULL
                    );

                CREATE POLICY organizations_app_invitation_accept_read
                    ON identity.organizations
                    FOR SELECT
                    TO agro_identity_app
                    USING (
                        nullif(current_setting('app.current_scope_kind', true), '') = 'tenant'
                        AND "Id"::text = nullif(
                            current_setting('app.current_organization_id', true), '')
                        AND identity.owner_invitation_context_allows_organization("Id")
                    );

                CREATE POLICY memberships_app_invitation_accept_insert
                    ON identity.memberships
                    FOR INSERT
                    TO agro_identity_app
                    WITH CHECK (
                        nullif(current_setting('app.current_scope_kind', true), '') = 'tenant'
                        AND "OrganizationId"::text = nullif(
                            current_setting('app.current_organization_id', true), '')
                        AND "UserId"::text = nullif(
                            current_setting('app.current_actor_id', true), '')
                        AND "Role" = 'owner'
                        AND "Status" = 'active'
                        AND identity.owner_invitation_context_allows_organization(
                            "OrganizationId")
                    );
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
                            'The owner invitation down migration is allowed only on an ephemeral test database';
                    END IF;
                END
                $ephemeral_only$;

                DROP POLICY IF EXISTS organizations_app_invitation_accept_read
                    ON identity.organizations;
                DROP POLICY IF EXISTS memberships_app_invitation_accept_insert
                    ON identity.memberships;
                DROP POLICY IF EXISTS sessions_app_owner_invitation_read
                    ON identity.sessions;
                REVOKE SELECT ("StrongAuthenticatedAtUtc", "StrongAuthenticationPurpose")
                    ON TABLE identity.sessions FROM agro_identity_app;
                DROP FUNCTION IF EXISTS
                    identity.owner_invitation_context_allows_organization(uuid);
                DROP FUNCTION IF EXISTS
                    identity.owner_invitation_retained_key_covered(text[]);
                DROP FUNCTION IF EXISTS
                    identity.resolve_owner_invitation_by_token(text, bytea);
                """);

            migrationBuilder.DropTable(
                name: "organization_owner_invitations",
                schema: "identity");

            migrationBuilder.Sql(
                """
                ALTER TABLE identity.sessions
                    DROP CONSTRAINT "CK_sessions_StrongAuthentication";
                ALTER TABLE identity.sessions
                    ADD CONSTRAINT "CK_sessions_StrongAuthentication"
                    CHECK (
                        ("StrongAuthenticatedAtUtc" IS NULL
                         AND "StrongAuthenticationPurpose" IS NULL)
                        OR
                        ("StrongAuthenticatedAtUtc" IS NOT NULL
                         AND "StrongAuthenticationPurpose" IS NOT NULL
                         AND "StrongAuthenticationPurpose" =
                             'manage_authentication_methods')
                    );
                ALTER TABLE identity.step_up_attempts
                    DROP CONSTRAINT "CK_step_up_attempts_Purpose";
                ALTER TABLE identity.step_up_attempts
                    ADD CONSTRAINT "CK_step_up_attempts_Purpose"
                    CHECK ("Purpose" = 'manage_authentication_methods');
                """);
        }
    }
}

#pragma warning restore CA1861
