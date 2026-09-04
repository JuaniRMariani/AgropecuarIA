using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1861 // EF-generated migration metadata uses local arrays.

namespace AgropecuarIA.Catalog.Migrations
{
    /// <inheritdoc />
    public partial class SecureCatalogPublication : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DO $legacy_guard$
                BEGIN
                    IF (SELECT count(*) FROM catalog.catalog_published_versions WHERE "IsActive") > 1 THEN
                        RAISE EXCEPTION 'Catalog migration requires an explicit reviewed repair of multiple active versions; no winner is selected';
                    END IF;
                    IF EXISTS (SELECT 1 FROM catalog.catalog_published_items GROUP BY "VersionId", "NormalizedCode" HAVING count(*) > 1)
                       OR EXISTS (SELECT 1 FROM catalog.catalog_published_items i WHERE NOT EXISTS
                            (SELECT 1 FROM catalog.catalog_published_versions v WHERE v."Id"=i."VersionId")) THEN
                        RAISE EXCEPTION 'Catalog migration requires an explicit reviewed repair of duplicate normalized codes or orphan items; history is not deleted';
                    END IF;
                END $legacy_guard$;
                """);
            migrationBuilder.DropIndex(
                name: "IX_catalog_published_versions_IsActive",
                schema: "catalog",
                table: "catalog_published_versions");

            migrationBuilder.AddColumn<string>(
                name: "Category",
                schema: "catalog",
                table: "catalog_staging_entries",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "NormalizedCode",
                schema: "catalog",
                table: "catalog_staging_entries",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "SourceSnapshotId",
                schema: "catalog",
                table: "catalog_staging_entries",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Synonyms",
                schema: "catalog",
                table: "catalog_staging_entries",
                type: "jsonb",
                nullable: false,
                defaultValue: "[]");

            migrationBuilder.AddColumn<int>(
                name: "EntryCount",
                schema: "catalog",
                table: "catalog_source_snapshots",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "IngestedBy",
                schema: "catalog",
                table: "catalog_source_snapshots",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "IngestionSequence",
                schema: "catalog",
                table: "catalog_source_snapshots",
                type: "bigint",
                nullable: false,
                defaultValue: 0L)
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn);

            migrationBuilder.AddColumn<bool>(
                name: "IsComplete",
                schema: "catalog",
                table: "catalog_source_snapshots",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<byte[]>(
                name: "RawContent",
                schema: "catalog",
                table: "catalog_source_snapshots",
                type: "bytea",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CandidateHash",
                schema: "catalog",
                table: "catalog_published_versions",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string[]>(
                name: "NormalizedSynonyms",
                schema: "catalog",
                table: "catalog_published_items",
                type: "text[]",
                nullable: false,
                defaultValue: Array.Empty<string>());

            migrationBuilder.AddColumn<Guid>(
                name: "SourceSnapshotId",
                schema: "catalog",
                table: "catalog_published_items",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "catalog_editorial_audits",
                schema: "catalog",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Action = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ActorUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    SessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    CorrelationId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    VersionId = table.Column<Guid>(type: "uuid", nullable: true),
                    SourceSnapshotId = table.Column<Guid>(type: "uuid", nullable: true),
                    OccurredAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_catalog_editorial_audits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_catalog_editorial_audits_catalog_published_versions_Version~",
                        column: x => x.VersionId,
                        principalSchema: "catalog",
                        principalTable: "catalog_published_versions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_catalog_editorial_audits_catalog_source_snapshots_SourceSna~",
                        column: x => x.SourceSnapshotId,
                        principalSchema: "catalog",
                        principalTable: "catalog_source_snapshots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "catalog_published_sources",
                schema: "catalog",
                columns: table => new
                {
                    VersionId = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceSnapshotId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_catalog_published_sources", x => new { x.VersionId, x.SourceSnapshotId });
                    table.ForeignKey(
                        name: "FK_catalog_published_sources_catalog_published_versions_Versio~",
                        column: x => x.VersionId,
                        principalSchema: "catalog",
                        principalTable: "catalog_published_versions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_catalog_published_sources_catalog_source_snapshots_SourceSn~",
                        column: x => x.SourceSnapshotId,
                        principalSchema: "catalog",
                        principalTable: "catalog_source_snapshots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "catalog_outbox_messages",
                schema: "catalog",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EventType = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    SchemaVersion = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    Source = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Scope = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    AggregateType = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    AggregateId = table.Column<Guid>(type: "uuid", nullable: false),
                    AuditId = table.Column<Guid>(type: "uuid", nullable: false),
                    ActorUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CorrelationId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    OccurredAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    PayloadJson = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_catalog_outbox_messages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_catalog_outbox_messages_catalog_editorial_audits_AuditId",
                        column: x => x.AuditId,
                        principalSchema: "catalog",
                        principalTable: "catalog_editorial_audits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_catalog_outbox_messages_catalog_published_versions_Aggregat~",
                        column: x => x.AggregateId,
                        principalSchema: "catalog",
                        principalTable: "catalog_published_versions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_catalog_staging_entries_SourceSnapshotId_NormalizedCode",
                schema: "catalog",
                table: "catalog_staging_entries",
                columns: new[] { "SourceSnapshotId", "NormalizedCode" },
                unique: true,
                filter: "\"SourceSnapshotId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_catalog_source_snapshots_IngestionSequence",
                schema: "catalog",
                table: "catalog_source_snapshots",
                column: "IngestionSequence",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_catalog_published_versions_IsActive",
                schema: "catalog",
                table: "catalog_published_versions",
                column: "IsActive",
                unique: true,
                filter: "\"IsActive\" = TRUE");

            migrationBuilder.CreateIndex(
                name: "IX_catalog_published_items_SourceSnapshotId",
                schema: "catalog",
                table: "catalog_published_items",
                column: "SourceSnapshotId");

            migrationBuilder.CreateIndex(
                name: "IX_catalog_published_items_VersionId_NormalizedCode",
                schema: "catalog",
                table: "catalog_published_items",
                columns: new[] { "VersionId", "NormalizedCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_catalog_editorial_audits_SourceSnapshotId",
                schema: "catalog",
                table: "catalog_editorial_audits",
                column: "SourceSnapshotId");

            migrationBuilder.CreateIndex(
                name: "IX_catalog_editorial_audits_VersionId",
                schema: "catalog",
                table: "catalog_editorial_audits",
                column: "VersionId");

            migrationBuilder.CreateIndex(
                name: "IX_catalog_outbox_messages_AggregateId",
                schema: "catalog",
                table: "catalog_outbox_messages",
                column: "AggregateId");

            migrationBuilder.CreateIndex(
                name: "IX_catalog_outbox_messages_AuditId",
                schema: "catalog",
                table: "catalog_outbox_messages",
                column: "AuditId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_catalog_published_sources_SourceSnapshotId",
                schema: "catalog",
                table: "catalog_published_sources",
                column: "SourceSnapshotId");

            migrationBuilder.AddForeignKey(
                name: "FK_catalog_published_items_catalog_published_versions_VersionId",
                schema: "catalog",
                table: "catalog_published_items",
                column: "VersionId",
                principalSchema: "catalog",
                principalTable: "catalog_published_versions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_catalog_published_items_catalog_source_snapshots_SourceSnap~",
                schema: "catalog",
                table: "catalog_published_items",
                column: "SourceSnapshotId",
                principalSchema: "catalog",
                principalTable: "catalog_source_snapshots",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_catalog_staging_entries_catalog_source_snapshots_SourceSnap~",
                schema: "catalog",
                table: "catalog_staging_entries",
                column: "SourceSnapshotId",
                principalSchema: "catalog",
                principalTable: "catalog_source_snapshots",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.Sql("""
                -- Legacy parents remain closed (NULL); new parents receive a database-owned,
                -- full transaction identity that is not changed by activation updates or VACUUM.
                ALTER TABLE catalog.catalog_source_snapshots ADD COLUMN "CreationTransaction" xid8 NULL;
                ALTER TABLE catalog.catalog_published_versions ADD COLUMN "CreationTransaction" xid8 NULL;
                ALTER TABLE catalog.catalog_published_versions ADD COLUMN "DeactivationTransaction" xid8 NULL;
                ALTER TABLE catalog.catalog_editorial_audits ADD COLUMN "CreationTransaction" xid8 NULL;
                ALTER TABLE catalog.catalog_outbox_messages ADD COLUMN "CreationTransaction" xid8 NULL;
                CREATE FUNCTION catalog.stamp_creation_transaction() RETURNS trigger LANGUAGE plpgsql SET search_path=pg_catalog AS $stamp$
                BEGIN
                    NEW."CreationTransaction" := pg_current_xact_id();
                    IF TG_TABLE_NAME='catalog_published_versions' THEN NEW."DeactivationTransaction" := NULL; END IF;
                    RETURN NEW;
                END $stamp$;
                CREATE TRIGGER catalog_creation_transaction BEFORE INSERT ON catalog.catalog_source_snapshots
                    FOR EACH ROW EXECUTE FUNCTION catalog.stamp_creation_transaction();
                CREATE TRIGGER catalog_creation_transaction BEFORE INSERT ON catalog.catalog_published_versions
                    FOR EACH ROW EXECUTE FUNCTION catalog.stamp_creation_transaction();
                CREATE TRIGGER catalog_creation_transaction BEFORE INSERT ON catalog.catalog_editorial_audits
                    FOR EACH ROW EXECUTE FUNCTION catalog.stamp_creation_transaction();
                CREATE TRIGGER catalog_creation_transaction BEFORE INSERT ON catalog.catalog_outbox_messages
                    FOR EACH ROW EXECUTE FUNCTION catalog.stamp_creation_transaction();
                CREATE FUNCTION catalog.stamp_deactivation_transaction() RETURNS trigger LANGUAGE plpgsql SET search_path=pg_catalog AS $deactivation_stamp$
                BEGIN
                    NEW."DeactivationTransaction" := CASE WHEN OLD."IsActive" AND NOT NEW."IsActive"
                        THEN pg_current_xact_id() ELSE OLD."DeactivationTransaction" END;
                    RETURN NEW;
                END $deactivation_stamp$;
                CREATE TRIGGER catalog_activation_stamp BEFORE UPDATE ON catalog.catalog_published_versions
                    FOR EACH ROW EXECUTE FUNCTION catalog.stamp_deactivation_transaction();
                CREATE FUNCTION catalog.enforce_activation_proof() RETURNS trigger LANGUAGE plpgsql SET search_path=pg_catalog AS $activation$
                DECLARE active_id uuid; previous_id uuid; previous_count integer;
                BEGIN
                    IF TG_OP='UPDATE' AND OLD."IsActive"=NEW."IsActive" THEN RETURN NULL; END IF;
                    SELECT v."Id" INTO active_id FROM catalog.catalog_published_versions v WHERE v."IsActive";
                    SELECT count(*) INTO previous_count FROM catalog.catalog_published_versions v WHERE v."DeactivationTransaction"=pg_current_xact_id();
                    SELECT v."Id" INTO previous_id FROM catalog.catalog_published_versions v WHERE v."DeactivationTransaction"=pg_current_xact_id();
                    IF active_id IS NULL OR previous_count>1 OR previous_id=active_id
                        OR (TG_OP='INSERT' AND (NOT NEW."IsActive" OR NEW."Id"<>active_id))
                        OR (TG_OP='UPDATE' AND NEW."IsActive" AND NEW."Id"<>active_id)
                        OR NOT EXISTS (
                            SELECT 1 FROM catalog.catalog_editorial_audits a JOIN catalog.catalog_outbox_messages o ON o."AuditId"=a."Id"
                            WHERE a."CreationTransaction"=pg_current_xact_id() AND o."CreationTransaction"=pg_current_xact_id()
                                AND a."VersionId"=active_id AND o."AggregateId"=active_id
                                AND ((a."Action"='catalog_published' AND o."EventType"='ProductCatalogPublished'
                                        AND EXISTS (SELECT 1 FROM catalog.catalog_published_versions v WHERE v."Id"=active_id AND v."CreationTransaction"=pg_current_xact_id()))
                                    OR (a."Action"='catalog_rolled_back' AND o."EventType"='ProductCatalogRolledBack'
                                        AND EXISTS (SELECT 1 FROM catalog.catalog_published_versions v WHERE v."Id"=active_id AND v."CreationTransaction" IS DISTINCT FROM pg_current_xact_id())))
                                AND a."ActorUserId"=o."ActorUserId" AND a."CorrelationId"=o."CorrelationId" AND a."OccurredAtUtc"=o."OccurredAtUtc"
                                AND o."PayloadJson" ? 'previousActiveVersionId'
                                AND (o."PayloadJson"->>'previousActiveVersionId') IS NOT DISTINCT FROM previous_id::text
                                AND (CASE WHEN o."EventType"='ProductCatalogPublished' THEN o."PayloadJson"->>'publishedAtUtc'
                                    ELSE o."PayloadJson"->>'rolledBackAtUtc' END)::timestamptz=o."OccurredAtUtc") THEN
                        RAISE EXCEPTION 'Catalog activation requires a new matching transaction audit and outbox for the previous and final active release';
                    END IF;
                    RETURN NULL;
                END $activation$;
                CREATE CONSTRAINT TRIGGER catalog_activation_proof AFTER INSERT OR UPDATE ON catalog.catalog_published_versions
                    DEFERRABLE INITIALLY DEFERRED FOR EACH ROW EXECUTE FUNCTION catalog.enforce_activation_proof();
                CREATE FUNCTION catalog.enforce_open_release() RETURNS trigger LANGUAGE plpgsql SET search_path=pg_catalog AS $open_release$
                BEGIN
                    IF NOT EXISTS (SELECT 1 FROM catalog.catalog_published_versions v WHERE v."Id"=NEW."VersionId"
                        AND v."CreationTransaction"=pg_current_xact_id()) THEN
                        RAISE EXCEPTION 'Catalog release children must be inserted in their parent creation transaction';
                    END IF;
                    RETURN NEW;
                END $open_release$;
                CREATE TRIGGER catalog_open_release BEFORE INSERT ON catalog.catalog_published_items
                    FOR EACH ROW EXECUTE FUNCTION catalog.enforce_open_release();
                CREATE TRIGGER catalog_open_release BEFORE INSERT ON catalog.catalog_published_sources
                    FOR EACH ROW EXECUTE FUNCTION catalog.enforce_open_release();
                ALTER TABLE catalog.catalog_source_snapshots ADD CONSTRAINT catalog_complete_source_facts CHECK (
                    NOT "IsComplete" OR ("RawContent" IS NOT NULL AND octet_length("RawContent") BETWEEN 2 AND 1048576
                        AND sha256("RawContent")="ContentHash" AND "IngestedBy" IS NOT NULL
                        AND "IngestedBy"<>'00000000-0000-0000-0000-000000000000'::uuid AND "EntryCount" BETWEEN 0 AND 10000));
                ALTER TABLE catalog.catalog_published_versions ADD CONSTRAINT catalog_candidate_hash CHECK ("CandidateHash" IS NULL OR "CandidateHash" ~ '^[0-9a-f]{64}$');
                ALTER TABLE catalog.catalog_editorial_audits ADD CONSTRAINT catalog_audit_shape CHECK (
                    "ActorUserId"<>'00000000-0000-0000-0000-000000000000'::uuid
                    AND "SessionId"<>'00000000-0000-0000-0000-000000000000'::uuid AND length("CorrelationId") BETWEEN 1 AND 128
                    AND (("Action"='source_ingested' AND "SourceSnapshotId" IS NOT NULL AND "VersionId" IS NULL)
                        OR ("Action" IN ('catalog_published','catalog_rolled_back') AND "VersionId" IS NOT NULL AND "SourceSnapshotId" IS NULL)));
                ALTER TABLE catalog.catalog_outbox_messages ADD CONSTRAINT catalog_outbox_shape CHECK (
                    "EventType" IN ('ProductCatalogPublished','ProductCatalogRolledBack') AND "SchemaVersion"='1.0.0'
                    AND "Source"='national-catalog' AND "Scope"='platform' AND "AggregateType"='NationalCatalogRelease'
                    AND ("PayloadJson"->>'versionId'="AggregateId"::text) IS TRUE);
                ALTER TABLE catalog.catalog_published_items ADD CONSTRAINT catalog_item_source_manifest
                    FOREIGN KEY ("VersionId","SourceSnapshotId") REFERENCES catalog.catalog_published_sources("VersionId","SourceSnapshotId")
                    DEFERRABLE INITIALLY DEFERRED;

                CREATE FUNCTION catalog.enforce_complete_source() RETURNS trigger LANGUAGE plpgsql SET search_path=pg_catalog AS $source$
                BEGIN
                    IF NEW."IsComplete" AND (
                        jsonb_array_length(convert_from(NEW."RawContent",'UTF8')::jsonb) <> NEW."EntryCount"
                        OR (SELECT count(*) FROM catalog.catalog_staging_entries s WHERE s."SourceSnapshotId"=NEW."Id")<>NEW."EntryCount"
                        OR NOT EXISTS (SELECT 1 FROM catalog.catalog_editorial_audits a WHERE a."SourceSnapshotId"=NEW."Id"
                            AND a."Action"='source_ingested' AND a."ActorUserId"=NEW."IngestedBy" AND a."OccurredAtUtc"=NEW."CreatedAtUtc")) THEN
                        RAISE EXCEPTION 'Complete catalog source requires every row and its ingestion audit atomically';
                    END IF;
                    RETURN NULL;
                END $source$;
                CREATE CONSTRAINT TRIGGER catalog_source_complete AFTER INSERT ON catalog.catalog_source_snapshots
                    DEFERRABLE INITIALLY DEFERRED FOR EACH ROW EXECUTE FUNCTION catalog.enforce_complete_source();

                CREATE FUNCTION catalog.enforce_staging_source() RETURNS trigger LANGUAGE plpgsql SET search_path=pg_catalog AS $staging$
                BEGIN
                    IF NEW."SourceSnapshotId" IS NULL OR NOT EXISTS (SELECT 1 FROM catalog.catalog_source_snapshots s
                        WHERE s."Id"=NEW."SourceSnapshotId" AND s."SourceId"=NEW."SourceId" AND s."ContentHash"=NEW."SourceHash" AND s."IsComplete"
                            AND s."CreationTransaction"=pg_current_xact_id()) THEN
                        RAISE EXCEPTION 'New catalog staging rows require their complete immutable source';
                    END IF;
                    RETURN NEW;
                END $staging$;
                CREATE TRIGGER catalog_staging_source BEFORE INSERT ON catalog.catalog_staging_entries FOR EACH ROW EXECUTE FUNCTION catalog.enforce_staging_source();

                CREATE FUNCTION catalog.enforce_catalog_publication() RETURNS trigger LANGUAGE plpgsql SET search_path=pg_catalog AS $publication$
                BEGIN
                    IF NEW."CandidateHash" IS NULL OR NEW."ItemsCount" NOT BETWEEN 1 AND 50000
                        OR (SELECT count(*) FROM catalog.catalog_published_items i WHERE i."VersionId"=NEW."Id")<>NEW."ItemsCount"
                        OR NOT EXISTS (SELECT 1 FROM catalog.catalog_editorial_audits a JOIN catalog.catalog_outbox_messages o ON o."AuditId"=a."Id"
                            WHERE a."VersionId"=NEW."Id" AND a."Action"='catalog_published' AND a."ActorUserId"::text=NEW."PublishedBy"
                              AND a."OccurredAtUtc"=NEW."PublishedAtUtc" AND o."AggregateId"=NEW."Id" AND o."EventType"='ProductCatalogPublished'
                              AND o."ActorUserId"=a."ActorUserId" AND o."CorrelationId"=a."CorrelationId" AND o."OccurredAtUtc"=NEW."PublishedAtUtc"
                              AND o."PayloadJson"->>'versionTag'=NEW."VersionTag" AND o."PayloadJson"->>'candidateHash'=NEW."CandidateHash"
                              AND (o."PayloadJson"->>'itemsCount')::int=NEW."ItemsCount"
                              AND (o."PayloadJson"->>'publishedAtUtc')::timestamptz=NEW."PublishedAtUtc"
                              AND jsonb_array_length(o."PayloadJson"->'sourceSnapshotIds')=(SELECT count(*) FROM catalog.catalog_published_sources p WHERE p."VersionId"=NEW."Id")
                              AND NOT EXISTS (SELECT 1 FROM catalog.catalog_published_sources p WHERE p."VersionId"=NEW."Id"
                                  AND NOT (o."PayloadJson"->'sourceSnapshotIds' @> to_jsonb(p."SourceSnapshotId"::text)))) THEN
                        RAISE EXCEPTION 'Catalog publication requires an atomic matching release, items, source manifest, audit and outbox';
                    END IF;
                    IF EXISTS (SELECT 1 FROM catalog.catalog_published_items i WHERE i."VersionId"=NEW."Id"
                        AND (i."SupportLevel"<>'FLUJO_GENERICO' OR i."SourceSnapshotId" IS NULL
                          OR NOT EXISTS (SELECT 1 FROM catalog.catalog_source_snapshots s WHERE s."Id"=i."SourceSnapshotId" AND s."IsComplete"))) THEN
                        RAISE EXCEPTION 'New catalog items require verified ingestion provenance and generic support';
                    END IF;
                    RETURN NULL;
                END $publication$;
                CREATE CONSTRAINT TRIGGER catalog_publication_atomic AFTER INSERT ON catalog.catalog_published_versions
                    DEFERRABLE INITIALLY DEFERRED FOR EACH ROW EXECUTE FUNCTION catalog.enforce_catalog_publication();

                CREATE FUNCTION catalog.prevent_history_rewrite() RETURNS trigger LANGUAGE plpgsql SET search_path=pg_catalog AS $immutable$
                BEGIN
                    IF TG_TABLE_NAME='catalog_published_versions' AND TG_OP='UPDATE'
                        AND (to_jsonb(NEW)-ARRAY['IsActive','DeactivationTransaction'])=(to_jsonb(OLD)-ARRAY['IsActive','DeactivationTransaction']) THEN RETURN NEW; END IF;
                    RAISE EXCEPTION 'Catalog historical facts are append-only; only release activation can change';
                END $immutable$;
                DO $guards$
                DECLARE target text;
                BEGIN
                    FOREACH target IN ARRAY ARRAY['catalog_source_snapshots','catalog_staging_entries','catalog_published_versions',
                        'catalog_published_items','catalog_published_sources','catalog_editorial_audits','catalog_outbox_messages'] LOOP
                        EXECUTE format('CREATE TRIGGER catalog_immutable BEFORE UPDATE OR DELETE ON catalog.%I FOR EACH ROW EXECUTE FUNCTION catalog.prevent_history_rewrite()',target);
                        EXECUTE format('CREATE TRIGGER catalog_no_truncate BEFORE TRUNCATE ON catalog.%I FOR EACH STATEMENT EXECUTE FUNCTION catalog.prevent_history_rewrite()',target);
                    END LOOP;
                END $guards$;
                REVOKE ALL ON FUNCTION catalog.enforce_complete_source(),catalog.enforce_staging_source(),catalog.enforce_catalog_publication(),catalog.prevent_history_rewrite(),catalog.stamp_creation_transaction(),catalog.enforce_open_release(),catalog.stamp_deactivation_transaction(),catalog.enforce_activation_proof() FROM PUBLIC;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DO $ephemeral_only$
                DECLARE target text;
                BEGIN
                    IF current_database() !~ '^agro_(identity|catalog)_[0-9a-f]{32}$' THEN
                        RAISE EXCEPTION 'Catalog rollback is limited to disposable test databases; preserve published history';
                    END IF;
                    FOREACH target IN ARRAY ARRAY['catalog_source_snapshots','catalog_staging_entries','catalog_published_versions',
                        'catalog_published_items','catalog_published_sources','catalog_editorial_audits','catalog_outbox_messages'] LOOP
                        EXECUTE format('DROP TRIGGER catalog_immutable ON catalog.%I',target);
                        EXECUTE format('DROP TRIGGER catalog_no_truncate ON catalog.%I',target);
                    END LOOP;
                END $ephemeral_only$;
                DROP TRIGGER catalog_source_complete ON catalog.catalog_source_snapshots;
                DROP TRIGGER catalog_staging_source ON catalog.catalog_staging_entries;
                DROP TRIGGER catalog_publication_atomic ON catalog.catalog_published_versions;
                DROP TRIGGER catalog_creation_transaction ON catalog.catalog_source_snapshots;
                DROP TRIGGER catalog_creation_transaction ON catalog.catalog_published_versions;
                DROP TRIGGER catalog_creation_transaction ON catalog.catalog_editorial_audits;
                DROP TRIGGER catalog_creation_transaction ON catalog.catalog_outbox_messages;
                DROP TRIGGER catalog_activation_stamp ON catalog.catalog_published_versions;
                DROP TRIGGER catalog_activation_proof ON catalog.catalog_published_versions;
                DROP TRIGGER catalog_open_release ON catalog.catalog_published_items;
                DROP TRIGGER catalog_open_release ON catalog.catalog_published_sources;
                DROP FUNCTION catalog.enforce_complete_source(),catalog.enforce_staging_source(),catalog.enforce_catalog_publication(),catalog.prevent_history_rewrite(),catalog.stamp_creation_transaction(),catalog.enforce_open_release(),catalog.stamp_deactivation_transaction(),catalog.enforce_activation_proof();
                ALTER TABLE catalog.catalog_source_snapshots DROP COLUMN "CreationTransaction";
                ALTER TABLE catalog.catalog_published_versions DROP COLUMN "CreationTransaction";
                ALTER TABLE catalog.catalog_published_versions DROP COLUMN "DeactivationTransaction";
                ALTER TABLE catalog.catalog_source_snapshots DROP CONSTRAINT catalog_complete_source_facts;
                ALTER TABLE catalog.catalog_published_versions DROP CONSTRAINT catalog_candidate_hash;
                ALTER TABLE catalog.catalog_published_items DROP CONSTRAINT catalog_item_source_manifest;
                """);
            migrationBuilder.DropForeignKey(
                name: "FK_catalog_published_items_catalog_published_versions_VersionId",
                schema: "catalog",
                table: "catalog_published_items");

            migrationBuilder.DropForeignKey(
                name: "FK_catalog_published_items_catalog_source_snapshots_SourceSnap~",
                schema: "catalog",
                table: "catalog_published_items");

            migrationBuilder.DropForeignKey(
                name: "FK_catalog_staging_entries_catalog_source_snapshots_SourceSnap~",
                schema: "catalog",
                table: "catalog_staging_entries");

            migrationBuilder.DropTable(
                name: "catalog_outbox_messages",
                schema: "catalog");

            migrationBuilder.DropTable(
                name: "catalog_published_sources",
                schema: "catalog");

            migrationBuilder.DropTable(
                name: "catalog_editorial_audits",
                schema: "catalog");

            migrationBuilder.DropIndex(
                name: "IX_catalog_staging_entries_SourceSnapshotId_NormalizedCode",
                schema: "catalog",
                table: "catalog_staging_entries");

            migrationBuilder.DropIndex(
                name: "IX_catalog_source_snapshots_IngestionSequence",
                schema: "catalog",
                table: "catalog_source_snapshots");

            migrationBuilder.DropIndex(
                name: "IX_catalog_published_versions_IsActive",
                schema: "catalog",
                table: "catalog_published_versions");

            migrationBuilder.DropIndex(
                name: "IX_catalog_published_items_SourceSnapshotId",
                schema: "catalog",
                table: "catalog_published_items");

            migrationBuilder.DropIndex(
                name: "IX_catalog_published_items_VersionId_NormalizedCode",
                schema: "catalog",
                table: "catalog_published_items");

            migrationBuilder.DropColumn(
                name: "Category",
                schema: "catalog",
                table: "catalog_staging_entries");

            migrationBuilder.DropColumn(
                name: "NormalizedCode",
                schema: "catalog",
                table: "catalog_staging_entries");

            migrationBuilder.DropColumn(
                name: "SourceSnapshotId",
                schema: "catalog",
                table: "catalog_staging_entries");

            migrationBuilder.DropColumn(
                name: "Synonyms",
                schema: "catalog",
                table: "catalog_staging_entries");

            migrationBuilder.DropColumn(
                name: "EntryCount",
                schema: "catalog",
                table: "catalog_source_snapshots");

            migrationBuilder.DropColumn(
                name: "IngestedBy",
                schema: "catalog",
                table: "catalog_source_snapshots");

            migrationBuilder.DropColumn(
                name: "IngestionSequence",
                schema: "catalog",
                table: "catalog_source_snapshots");

            migrationBuilder.DropColumn(
                name: "IsComplete",
                schema: "catalog",
                table: "catalog_source_snapshots");

            migrationBuilder.DropColumn(
                name: "RawContent",
                schema: "catalog",
                table: "catalog_source_snapshots");

            migrationBuilder.DropColumn(
                name: "CandidateHash",
                schema: "catalog",
                table: "catalog_published_versions");

            migrationBuilder.DropColumn(
                name: "NormalizedSynonyms",
                schema: "catalog",
                table: "catalog_published_items");

            migrationBuilder.DropColumn(
                name: "SourceSnapshotId",
                schema: "catalog",
                table: "catalog_published_items");

            migrationBuilder.CreateIndex(
                name: "IX_catalog_published_versions_IsActive",
                schema: "catalog",
                table: "catalog_published_versions",
                column: "IsActive");
        }
    }
}
