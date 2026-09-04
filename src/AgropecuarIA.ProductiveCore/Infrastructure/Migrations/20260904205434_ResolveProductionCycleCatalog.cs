using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgropecuarIA.ProductiveCore.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ResolveProductionCycleCatalog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CatalogItemId",
                schema: "productive_core",
                table: "production_cycles",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CatalogProvenanceStatus",
                schema: "productive_core",
                table: "production_cycles",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CatalogReferenceStatus",
                schema: "productive_core",
                table: "production_cycles",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "legacy_unresolved");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CatalogResolvedAtUtc",
                schema: "productive_core",
                table: "production_cycles",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CatalogSourceHash",
                schema: "productive_core",
                table: "production_cycles",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CatalogSourceId",
                schema: "productive_core",
                table: "production_cycles",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CatalogSourceIngestedAtUtc",
                schema: "productive_core",
                table: "production_cycles",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CatalogSourceSnapshotId",
                schema: "productive_core",
                table: "production_cycles",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CatalogVersionId",
                schema: "productive_core",
                table: "production_cycles",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CatalogVersionTag",
                schema: "productive_core",
                table: "production_cycles",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeclaredCatalogSupportLevel",
                schema: "productive_core",
                table: "production_cycles",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.Sql("""
                ALTER TABLE productive_core.production_cycles ADD CONSTRAINT production_cycle_catalog_snapshot_shape CHECK ((
                    ("CatalogReferenceStatus"='legacy_unresolved'
                        AND "CatalogVersionId" IS NULL AND "CatalogItemId" IS NULL AND "CatalogVersionTag" IS NULL
                        AND "DeclaredCatalogSupportLevel" IS NULL AND "CatalogResolvedAtUtc" IS NULL
                        AND "CatalogSourceSnapshotId" IS NULL AND "CatalogSourceId" IS NULL AND "CatalogSourceHash" IS NULL
                        AND "CatalogSourceIngestedAtUtc" IS NULL AND "CatalogProvenanceStatus" IS NULL)
                    OR ("CatalogReferenceStatus"='resolved_publication' AND "SupportLevel"='FLUJO_GENERICO'
                        AND "CatalogVersionId" IS NOT NULL AND "CatalogVersionId"<>'00000000-0000-0000-0000-000000000000'::uuid
                        AND "CatalogItemId" IS NOT NULL AND "CatalogItemId"<>'00000000-0000-0000-0000-000000000000'::uuid
                        AND length(btrim("CatalogVersionTag")) BETWEEN 1 AND 64 AND length(btrim("DeclaredCatalogSupportLevel")) BETWEEN 1 AND 64
                        AND length(btrim("CatalogCode")) BETWEEN 1 AND 64 AND length(btrim("CatalogDisplayName")) BETWEEN 1 AND 256
                        AND "CatalogResolvedAtUtc" IS NOT NULL AND isfinite("CatalogResolvedAtUtc")
                        AND (("CatalogProvenanceStatus"='legacy_unavailable'
                                AND "CatalogSourceSnapshotId" IS NULL AND "CatalogSourceId" IS NULL AND "CatalogSourceHash" IS NULL AND "CatalogSourceIngestedAtUtc" IS NULL)
                            OR ("CatalogProvenanceStatus"='verified_snapshot'
                                AND "CatalogSourceSnapshotId" IS NOT NULL AND "CatalogSourceSnapshotId"<>'00000000-0000-0000-0000-000000000000'::uuid
                                AND length(btrim("CatalogSourceId")) BETWEEN 1 AND 128 AND "CatalogSourceHash" ~ '^[0-9a-f]{64}$'
                                AND "CatalogSourceIngestedAtUtc" IS NOT NULL AND isfinite("CatalogSourceIngestedAtUtc"))))) IS TRUE);

                CREATE FUNCTION productive_core.enforce_cycle_catalog_reference() RETURNS trigger LANGUAGE plpgsql SET search_path=pg_catalog AS $reference$
                BEGIN
                    IF TG_OP='INSERT' THEN
                        IF NEW."CatalogReferenceStatus"<>'resolved_publication' OR NEW."SupportLevel"<>'FLUJO_GENERICO'
                            OR NEW."Status"<>'active' OR NEW."EndDateUtc" IS NOT NULL THEN
                            RAISE EXCEPTION 'New production cycles require a resolved catalog snapshot and generic effective support';
                        END IF;
                        RETURN NEW;
                    END IF;
                    IF (to_jsonb(NEW)-ARRAY['Status','EndDateUtc'])<>(to_jsonb(OLD)-ARRAY['Status','EndDateUtc']) THEN
                        RAISE EXCEPTION 'Production cycle metadata and its catalog snapshot are immutable';
                    END IF;
                    IF NEW."Status"=OLD."Status" AND NEW."EndDateUtc" IS NOT DISTINCT FROM OLD."EndDateUtc" THEN RETURN NEW; END IF;
                    IF OLD."Status"='active' AND ((NEW."Status"='closed' AND NEW."EndDateUtc" IS NOT NULL AND NEW."EndDateUtc">=OLD."StartDateUtc")
                        OR (NEW."Status"='canceled' AND NEW."EndDateUtc" IS NULL)) THEN RETURN NEW; END IF;
                    RAISE EXCEPTION 'Production cycle transition is invalid';
                END $reference$;
                CREATE TRIGGER production_cycle_catalog_reference BEFORE INSERT OR UPDATE ON productive_core.production_cycles
                    FOR EACH ROW EXECUTE FUNCTION productive_core.enforce_cycle_catalog_reference();
                REVOKE ALL ON FUNCTION productive_core.enforce_cycle_catalog_reference() FROM PUBLIC;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DO $disposable$ BEGIN
                    IF current_database() !~ '^agro_(identity|catalog)_[0-9a-f]{32}$' THEN
                        RAISE EXCEPTION 'Catalog-reference rollback is limited to disposable databases; preserve cycle history';
                    END IF;
                END $disposable$;
                DROP TRIGGER production_cycle_catalog_reference ON productive_core.production_cycles;
                DROP FUNCTION productive_core.enforce_cycle_catalog_reference();
                ALTER TABLE productive_core.production_cycles DROP CONSTRAINT production_cycle_catalog_snapshot_shape;
                """);
            migrationBuilder.DropColumn(
                name: "CatalogItemId",
                schema: "productive_core",
                table: "production_cycles");

            migrationBuilder.DropColumn(
                name: "CatalogProvenanceStatus",
                schema: "productive_core",
                table: "production_cycles");

            migrationBuilder.DropColumn(
                name: "CatalogReferenceStatus",
                schema: "productive_core",
                table: "production_cycles");

            migrationBuilder.DropColumn(
                name: "CatalogResolvedAtUtc",
                schema: "productive_core",
                table: "production_cycles");

            migrationBuilder.DropColumn(
                name: "CatalogSourceHash",
                schema: "productive_core",
                table: "production_cycles");

            migrationBuilder.DropColumn(
                name: "CatalogSourceId",
                schema: "productive_core",
                table: "production_cycles");

            migrationBuilder.DropColumn(
                name: "CatalogSourceIngestedAtUtc",
                schema: "productive_core",
                table: "production_cycles");

            migrationBuilder.DropColumn(
                name: "CatalogSourceSnapshotId",
                schema: "productive_core",
                table: "production_cycles");

            migrationBuilder.DropColumn(
                name: "CatalogVersionId",
                schema: "productive_core",
                table: "production_cycles");

            migrationBuilder.DropColumn(
                name: "CatalogVersionTag",
                schema: "productive_core",
                table: "production_cycles");

            migrationBuilder.DropColumn(
                name: "DeclaredCatalogSupportLevel",
                schema: "productive_core",
                table: "production_cycles");
        }
    }
}
