using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1861 // EF Core scaffolds transient arrays for migration metadata.

namespace AgropecuarIA.Territory.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitializeOfficialTerritory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DO $roles$
                BEGIN
                    IF NOT EXISTS (SELECT 1 FROM pg_catalog.pg_roles WHERE rolname = 'agro_territory_owner') THEN
                        CREATE ROLE agro_territory_owner NOLOGIN NOINHERIT NOSUPERUSER
                            NOCREATEDB NOCREATEROLE NOREPLICATION NOBYPASSRLS;
                    END IF;
                    IF NOT EXISTS (SELECT 1 FROM pg_catalog.pg_roles WHERE rolname = 'agro_territory_app') THEN
                        CREATE ROLE agro_territory_app NOLOGIN NOINHERIT NOSUPERUSER
                            NOCREATEDB NOCREATEROLE NOREPLICATION NOBYPASSRLS;
                    END IF;
                    IF NOT EXISTS (SELECT 1 FROM pg_catalog.pg_roles WHERE rolname = 'agro_territory_importer') THEN
                        CREATE ROLE agro_territory_importer NOLOGIN NOINHERIT NOSUPERUSER
                            NOCREATEDB NOCREATEROLE NOREPLICATION NOBYPASSRLS;
                    END IF;
                END
                $roles$;

                ALTER ROLE agro_territory_owner WITH NOLOGIN NOINHERIT NOSUPERUSER
                    NOCREATEDB NOCREATEROLE NOREPLICATION NOBYPASSRLS;
                ALTER ROLE agro_territory_app WITH NOLOGIN NOINHERIT NOSUPERUSER
                    NOCREATEDB NOCREATEROLE NOREPLICATION NOBYPASSRLS;
                ALTER ROLE agro_territory_importer WITH NOLOGIN NOINHERIT NOSUPERUSER
                    NOCREATEDB NOCREATEROLE NOREPLICATION NOBYPASSRLS;

                GRANT agro_territory_app, agro_territory_importer, agro_territory_owner
                    TO CURRENT_USER;
                """);

            migrationBuilder.EnsureSchema(
                name: "territory");

            migrationBuilder.CreateTable(
                name: "snapshots",
                schema: "territory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Provider = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Version = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    CapturedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ContentHash = table.Column<byte[]>(type: "bytea", maxLength: 32, nullable: false),
                    Status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    ImportedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ActivatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_snapshots", x => x.Id);
                    table.CheckConstraint("CK_snapshots_Activation", "(\"Status\" = 'staging' AND \"ActivatedAtUtc\" IS NULL) OR (\"Status\" IN ('active', 'retired') AND \"ActivatedAtUtc\" IS NOT NULL)");
                    table.CheckConstraint("CK_snapshots_ContentHash", "octet_length(\"ContentHash\") = 32");
                    table.CheckConstraint("CK_snapshots_Source", "length(btrim(\"Provider\")) > 0 AND length(btrim(\"Version\")) > 0");
                    table.CheckConstraint("CK_snapshots_Status", "\"Status\" IN ('staging', 'active', 'retired')");
                });

            migrationBuilder.CreateTable(
                name: "official_units",
                schema: "territory",
                columns: table => new
                {
                    SnapshotId = table.Column<Guid>(type: "uuid", nullable: false),
                    OfficialCode = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    NormalizedName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Level = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    ParentCode = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true),
                    CentroidLatitude = table.Column<double>(type: "double precision", nullable: true),
                    CentroidLongitude = table.Column<double>(type: "double precision", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_official_units", x => new { x.SnapshotId, x.OfficialCode });
                    table.CheckConstraint("CK_official_units_Centroid", "(\"CentroidLatitude\" IS NULL AND \"CentroidLongitude\" IS NULL) OR (\"CentroidLatitude\" IS NOT NULL AND \"CentroidLongitude\" IS NOT NULL AND \"CentroidLatitude\" BETWEEN -90 AND 90 AND \"CentroidLongitude\" BETWEEN -180 AND 180)");
                    table.CheckConstraint("CK_official_units_Identity", "length(btrim(\"OfficialCode\")) > 0 AND length(btrim(\"Name\")) > 0 AND length(btrim(\"NormalizedName\")) > 0");
                    table.CheckConstraint("CK_official_units_Level", "\"Level\" IN ('province', 'department', 'municipality', 'locality')");
                    table.CheckConstraint("CK_official_units_NotSelfParent", "\"ParentCode\" IS NULL OR \"ParentCode\" <> \"OfficialCode\"");
                    table.CheckConstraint("CK_official_units_Parent", "(\"Level\" = 'province' AND \"ParentCode\" IS NULL) OR (\"Level\" <> 'province' AND \"ParentCode\" IS NOT NULL)");
                    table.ForeignKey(
                        name: "FK_official_units_official_units_SnapshotId_ParentCode",
                        columns: x => new { x.SnapshotId, x.ParentCode },
                        principalSchema: "territory",
                        principalTable: "official_units",
                        principalColumns: new[] { "SnapshotId", "OfficialCode" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_official_units_snapshots_SnapshotId",
                        column: x => x.SnapshotId,
                        principalSchema: "territory",
                        principalTable: "snapshots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_official_units_SnapshotId_Level_ParentCode",
                schema: "territory",
                table: "official_units",
                columns: new[] { "SnapshotId", "Level", "ParentCode" });

            migrationBuilder.CreateIndex(
                name: "IX_official_units_SnapshotId_NormalizedName_OfficialCode",
                schema: "territory",
                table: "official_units",
                columns: new[] { "SnapshotId", "NormalizedName", "OfficialCode" });

            migrationBuilder.CreateIndex(
                name: "IX_official_units_SnapshotId_ParentCode",
                schema: "territory",
                table: "official_units",
                columns: new[] { "SnapshotId", "ParentCode" });

            migrationBuilder.CreateIndex(
                name: "IX_snapshots_Provider_Version",
                schema: "territory",
                table: "snapshots",
                columns: new[] { "Provider", "Version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_snapshots_Status",
                schema: "territory",
                table: "snapshots",
                column: "Status",
                unique: true,
                filter: "\"Status\" = 'active'");

            migrationBuilder.Sql(
                """
                ALTER SCHEMA territory OWNER TO agro_territory_owner;
                ALTER TABLE territory.snapshots OWNER TO agro_territory_owner;
                ALTER TABLE territory.official_units OWNER TO agro_territory_owner;

                REVOKE ALL ON SCHEMA territory FROM PUBLIC;
                REVOKE ALL ON ALL TABLES IN SCHEMA territory FROM PUBLIC;
                REVOKE ALL ON TABLE territory.snapshots, territory.official_units
                    FROM agro_territory_app, agro_territory_importer;
                GRANT USAGE ON SCHEMA territory TO agro_territory_app, agro_territory_importer;
                GRANT SELECT ON TABLE territory.snapshots, territory.official_units
                    TO agro_territory_app, agro_territory_importer;
                GRANT INSERT, DELETE ON TABLE territory.snapshots, territory.official_units
                    TO agro_territory_importer;

                ALTER TABLE territory.snapshots ENABLE ROW LEVEL SECURITY;
                ALTER TABLE territory.snapshots FORCE ROW LEVEL SECURITY;
                ALTER TABLE territory.official_units ENABLE ROW LEVEL SECURITY;
                ALTER TABLE territory.official_units FORCE ROW LEVEL SECURITY;

                CREATE POLICY territory_snapshots_owner_all
                    ON territory.snapshots
                    FOR ALL TO agro_territory_owner
                    USING (true)
                    WITH CHECK (true);
                CREATE POLICY territory_snapshots_app_active_read
                    ON territory.snapshots
                    FOR SELECT TO agro_territory_app
                    USING ("Status" = 'active');
                CREATE POLICY territory_snapshots_importer_read
                    ON territory.snapshots
                    FOR SELECT TO agro_territory_importer
                    USING (true);
                CREATE POLICY territory_snapshots_importer_insert
                    ON territory.snapshots
                    FOR INSERT TO agro_territory_importer
                    WITH CHECK ("Status" = 'staging' AND "ActivatedAtUtc" IS NULL);
                CREATE POLICY territory_snapshots_importer_delete_staging
                    ON territory.snapshots
                    FOR DELETE TO agro_territory_importer
                    USING ("Status" = 'staging');

                CREATE POLICY territory_units_owner_all
                    ON territory.official_units
                    FOR ALL TO agro_territory_owner
                    USING (true)
                    WITH CHECK (true);
                CREATE POLICY territory_units_app_active_read
                    ON territory.official_units
                    FOR SELECT TO agro_territory_app
                    USING (
                        EXISTS (
                            SELECT 1
                            FROM territory.snapshots AS snapshot
                            WHERE snapshot."Id" = official_units."SnapshotId"
                              AND snapshot."Status" = 'active'
                        )
                    );
                CREATE POLICY territory_units_importer_read
                    ON territory.official_units
                    FOR SELECT TO agro_territory_importer
                    USING (true);
                CREATE POLICY territory_units_importer_insert
                    ON territory.official_units
                    FOR INSERT TO agro_territory_importer
                    WITH CHECK (
                        EXISTS (
                            SELECT 1
                            FROM territory.snapshots AS snapshot
                            WHERE snapshot."Id" = official_units."SnapshotId"
                              AND snapshot."Status" = 'staging'
                        )
                    );
                CREATE POLICY territory_units_importer_delete_staging
                    ON territory.official_units
                    FOR DELETE TO agro_territory_importer
                    USING (
                        EXISTS (
                            SELECT 1
                            FROM territory.snapshots AS snapshot
                            WHERE snapshot."Id" = official_units."SnapshotId"
                              AND snapshot."Status" = 'staging'
                        )
                    );

                CREATE FUNCTION territory.enforce_snapshot_immutability()
                RETURNS trigger
                LANGUAGE plpgsql
                SECURITY DEFINER
                SET search_path = pg_catalog
                AS $snapshot_immutability$
                BEGIN
                    IF TG_OP = 'DELETE' THEN
                        IF OLD."Status" <> 'staging' THEN
                            RAISE EXCEPTION 'Published territory snapshots are immutable.'
                                USING ERRCODE = '23514';
                        END IF;
                        RETURN OLD;
                    END IF;

                    IF OLD."Id" IS DISTINCT FROM NEW."Id"
                       OR OLD."Provider" IS DISTINCT FROM NEW."Provider"
                       OR OLD."Version" IS DISTINCT FROM NEW."Version"
                       OR OLD."CapturedAtUtc" IS DISTINCT FROM NEW."CapturedAtUtc"
                       OR OLD."ContentHash" IS DISTINCT FROM NEW."ContentHash"
                       OR OLD."ImportedAtUtc" IS DISTINCT FROM NEW."ImportedAtUtc"
                       OR OLD."ActivatedAtUtc" IS DISTINCT FROM NEW."ActivatedAtUtc"
                          AND NOT (
                              OLD."Status" = 'staging'
                              AND NEW."Status" = 'active'
                              AND OLD."ActivatedAtUtc" IS NULL
                              AND NEW."ActivatedAtUtc" IS NOT NULL)
                       OR NOT (
                           (OLD."Status" = 'staging' AND NEW."Status" = 'active')
                           OR (OLD."Status" = 'active' AND NEW."Status" = 'retired'))
                       OR CURRENT_USER <> 'agro_territory_owner' THEN
                        RAISE EXCEPTION 'Territory snapshot state can only advance through activation.'
                            USING ERRCODE = '23514';
                    END IF;

                    RETURN NEW;
                END
                $snapshot_immutability$;
                ALTER FUNCTION territory.enforce_snapshot_immutability()
                    OWNER TO agro_territory_owner;
                REVOKE ALL ON FUNCTION territory.enforce_snapshot_immutability() FROM PUBLIC;

                CREATE TRIGGER snapshots_immutable
                    BEFORE UPDATE OR DELETE ON territory.snapshots
                    FOR EACH ROW EXECUTE FUNCTION territory.enforce_snapshot_immutability();

                CREATE FUNCTION territory.enforce_official_unit_staging_write()
                RETURNS trigger
                LANGUAGE plpgsql
                SECURITY DEFINER
                SET search_path = pg_catalog
                AS $unit_staging_write$
                DECLARE
                    snapshot_status text;
                BEGIN
                    SELECT snapshot."Status"
                    INTO snapshot_status
                    FROM territory.snapshots AS snapshot
                    WHERE snapshot."Id" = COALESCE(NEW."SnapshotId", OLD."SnapshotId");

                    IF snapshot_status IS DISTINCT FROM 'staging' THEN
                        RAISE EXCEPTION 'Official territory units are mutable only while staging.'
                            USING ERRCODE = '23514';
                    END IF;

                    IF TG_OP = 'UPDATE' THEN
                        RAISE EXCEPTION 'Official territory units are append-only.'
                            USING ERRCODE = '23514';
                    END IF;

                    RETURN CASE WHEN TG_OP = 'DELETE' THEN OLD ELSE NEW END;
                END
                $unit_staging_write$;
                ALTER FUNCTION territory.enforce_official_unit_staging_write()
                    OWNER TO agro_territory_owner;
                REVOKE ALL ON FUNCTION territory.enforce_official_unit_staging_write() FROM PUBLIC;

                CREATE TRIGGER official_units_staging_write
                    BEFORE INSERT OR UPDATE OR DELETE ON territory.official_units
                    FOR EACH ROW EXECUTE FUNCTION territory.enforce_official_unit_staging_write();

                CREATE FUNCTION territory.activate_official_snapshot(p_snapshot_id uuid)
                RETURNS void
                LANGUAGE plpgsql
                VOLATILE
                SECURITY DEFINER
                SET search_path = pg_catalog
                AS $activate_snapshot$
                DECLARE
                    target_status text;
                    province_codes text[];
                    expected_hash bytea;
                    calculated_hash bytea;
                BEGIN
                    IF p_snapshot_id IS NULL THEN
                        RAISE EXCEPTION 'Snapshot ID is required.' USING ERRCODE = '22023';
                    END IF;

                    PERFORM pg_catalog.pg_advisory_xact_lock(
                        pg_catalog.hashtextextended('territory.activate_official_snapshot', 0));
                    LOCK TABLE territory.snapshots IN SHARE ROW EXCLUSIVE MODE;
                    LOCK TABLE territory.official_units IN SHARE ROW EXCLUSIVE MODE;

                    SELECT snapshot."Status"
                    INTO target_status
                    FROM territory.snapshots AS snapshot
                    WHERE snapshot."Id" = p_snapshot_id
                    FOR UPDATE;

                    IF NOT FOUND OR target_status <> 'staging' THEN
                        RAISE EXCEPTION 'Snapshot must exist in staging before activation.'
                            USING ERRCODE = '23514';
                    END IF;

                    SELECT pg_catalog.array_agg(unit."OfficialCode" ORDER BY unit."OfficialCode")
                    INTO province_codes
                    FROM territory.official_units AS unit
                    WHERE unit."SnapshotId" = p_snapshot_id
                      AND unit."Level" = 'province';

                    IF province_codes IS DISTINCT FROM ARRAY[
                        '02', '06', '10', '14', '18', '22', '26', '30',
                        '34', '38', '42', '46', '50', '54', '58', '62',
                        '66', '70', '74', '78', '82', '86', '90', '94']::text[] THEN
                        RAISE EXCEPTION 'Snapshot must contain the 24 official provinces and CABA.'
                            USING ERRCODE = '23514';
                    END IF;

                    SELECT snapshot."ContentHash"
                    INTO expected_hash
                    FROM territory.snapshots AS snapshot
                    WHERE snapshot."Id" = p_snapshot_id;

                    SELECT pg_catalog.sha256(
                        pg_catalog.convert_to(
                            pg_catalog.string_agg(
                                unit."OfficialCode" || pg_catalog.chr(31) ||
                                unit."Level" || pg_catalog.chr(31) ||
                                unit."Name" || pg_catalog.chr(31) ||
                                unit."NormalizedName" || pg_catalog.chr(31) ||
                                coalesce(unit."ParentCode", '') || pg_catalog.chr(31) ||
                                coalesce(
                                    pg_catalog.replace(unit."CentroidLatitude"::text, 'e', 'E'),
                                    '') ||
                                pg_catalog.chr(31) ||
                                coalesce(
                                    pg_catalog.replace(unit."CentroidLongitude"::text, 'e', 'E'),
                                    '') || E'\n',
                                '' ORDER BY unit."OfficialCode" COLLATE "C"),
                            'UTF8'))
                    INTO calculated_hash
                    FROM territory.official_units AS unit
                    WHERE unit."SnapshotId" = p_snapshot_id;

                    IF expected_hash IS DISTINCT FROM calculated_hash THEN
                        RAISE EXCEPTION 'Snapshot content hash does not match its canonical units.'
                            USING ERRCODE = '23514';
                    END IF;

                    IF EXISTS (
                        SELECT 1
                        FROM territory.official_units AS child
                        JOIN territory.official_units AS parent
                          ON parent."SnapshotId" = child."SnapshotId"
                         AND parent."OfficialCode" = child."ParentCode"
                        WHERE child."SnapshotId" = p_snapshot_id
                          AND CASE child."Level"
                              WHEN 'province' THEN 0
                              WHEN 'department' THEN 1
                              WHEN 'municipality' THEN 2
                              WHEN 'locality' THEN 3
                              ELSE -1
                              END <= CASE parent."Level"
                              WHEN 'province' THEN 0
                              WHEN 'department' THEN 1
                              WHEN 'municipality' THEN 2
                              WHEN 'locality' THEN 3
                              ELSE 4
                              END
                    ) THEN
                        RAISE EXCEPTION 'Territory parents must have a shallower level.'
                            USING ERRCODE = '23514';
                    END IF;

                    IF EXISTS (
                        WITH RECURSIVE ancestry AS (
                            SELECT unit."OfficialCode" AS start_code,
                                   unit."ParentCode" AS current_code,
                                   ARRAY[unit."OfficialCode"]::text[] AS path,
                                   false AS cycle
                            FROM territory.official_units AS unit
                            WHERE unit."SnapshotId" = p_snapshot_id
                              AND unit."ParentCode" IS NOT NULL
                            UNION ALL
                            SELECT ancestry.start_code,
                                   parent."ParentCode",
                                   ancestry.path || parent."OfficialCode",
                                   parent."OfficialCode" = ANY(ancestry.path)
                            FROM ancestry
                            JOIN territory.official_units AS parent
                              ON parent."SnapshotId" = p_snapshot_id
                             AND parent."OfficialCode" = ancestry.current_code
                            WHERE ancestry.current_code IS NOT NULL
                              AND NOT ancestry.cycle
                        )
                        SELECT 1 FROM ancestry WHERE cycle
                    ) THEN
                        RAISE EXCEPTION 'Territory hierarchy contains a cycle.'
                            USING ERRCODE = '23514';
                    END IF;

                    UPDATE territory.snapshots
                    SET "Status" = 'retired'
                    WHERE "Status" = 'active';

                    UPDATE territory.snapshots
                    SET "Status" = 'active',
                        "ActivatedAtUtc" = pg_catalog.statement_timestamp()
                    WHERE "Id" = p_snapshot_id;
                END
                $activate_snapshot$;
                ALTER FUNCTION territory.activate_official_snapshot(uuid)
                    OWNER TO agro_territory_owner;
                REVOKE ALL ON FUNCTION territory.activate_official_snapshot(uuid) FROM PUBLIC;
                GRANT EXECUTE ON FUNCTION territory.activate_official_snapshot(uuid)
                    TO agro_territory_importer;
                """);

            migrationBuilder.Sql(
                """
                INSERT INTO territory.snapshots
                    ("Id", "Provider", "Version", "CapturedAtUtc", "ContentHash",
                     "Status", "ImportedAtUtc", "ActivatedAtUtc")
                VALUES
                    ('00000000-0000-4000-8000-000000000001', 'georef', '1.0.0',
                     '2026-08-05T16:33:00Z',
                     decode('ee27e73d27b1fe45a5010b758e97f073fcf8909f0d8bb46541b8bb4eb9eb6fe7', 'hex'),
                     'staging', '2026-08-11T00:00:00Z', NULL);

                INSERT INTO territory.official_units
                    ("SnapshotId", "OfficialCode", "Name", "NormalizedName", "Level",
                     "ParentCode", "CentroidLatitude", "CentroidLongitude")
                VALUES
                    ('00000000-0000-4000-8000-000000000001', '02', 'Ciudad Autónoma de Buenos Aires', 'ciudad autonoma de buenos aires', 'province', NULL, -34.6144420654301, -58.4458763250916),
                    ('00000000-0000-4000-8000-000000000001', '06', 'Buenos Aires', 'buenos aires', 'province', NULL, -36.6773920760823, -60.5584771084959),
                    ('00000000-0000-4000-8000-000000000001', '10', 'Catamarca', 'catamarca', 'province', NULL, -27.3359537960762, -66.9478972451295),
                    ('00000000-0000-4000-8000-000000000001', '14', 'Córdoba', 'cordoba', 'province', NULL, -32.1447993873859, -63.801973466573),
                    ('00000000-0000-4000-8000-000000000001', '18', 'Corrientes', 'corrientes', 'province', NULL, -28.7742044813623, -57.8010818603331),
                    ('00000000-0000-4000-8000-000000000001', '22', 'Chaco', 'chaco', 'province', NULL, -26.3869871835867, -60.765116260356),
                    ('00000000-0000-4000-8000-000000000001', '26', 'Chubut', 'chubut', 'province', NULL, -43.7886271389083, -68.5267363339818),
                    ('00000000-0000-4000-8000-000000000001', '30', 'Entre Ríos', 'entre rios', 'province', NULL, -32.0589278938558, -59.201262616496),
                    ('00000000-0000-4000-8000-000000000001', '34', 'Formosa', 'formosa', 'province', NULL, -24.8950871761481, -59.9321901121647),
                    ('00000000-0000-4000-8000-000000000001', '38', 'Jujuy', 'jujuy', 'province', NULL, -23.3199750616583, -65.764423919292),
                    ('00000000-0000-4000-8000-000000000001', '42', 'La Pampa', 'la pampa', 'province', NULL, -37.1350652212898, -65.4476439990213),
                    ('00000000-0000-4000-8000-000000000001', '46', 'La Rioja', 'la rioja', 'province', NULL, -29.6849372775783, -67.1817575814487),
                    ('00000000-0000-4000-8000-000000000001', '50', 'Mendoza', 'mendoza', 'province', NULL, -34.6303887067166, -68.5829456019867),
                    ('00000000-0000-4000-8000-000000000001', '54', 'Misiones', 'misiones', 'province', NULL, -26.8753025989034, -54.6515705627219),
                    ('00000000-0000-4000-8000-000000000001', '58', 'Neuquén', 'neuquen', 'province', NULL, -38.6419828626673, -70.1198972237318),
                    ('00000000-0000-4000-8000-000000000001', '62', 'Río Negro', 'rio negro', 'province', NULL, -40.4050796306359, -67.2296757996036),
                    ('00000000-0000-4000-8000-000000000001', '66', 'Salta', 'salta', 'province', NULL, -24.2992838957201, -64.8141586574346),
                    ('00000000-0000-4000-8000-000000000001', '70', 'San Juan', 'san juan', 'province', NULL, -30.8656607015096, -68.8881597071776),
                    ('00000000-0000-4000-8000-000000000001', '74', 'San Luis', 'san luis', 'province', NULL, -33.7611035381154, -66.0252312714021),
                    ('00000000-0000-4000-8000-000000000001', '78', 'Santa Cruz', 'santa cruz', 'province', NULL, -48.8155471830527, -69.9557619144913),
                    ('00000000-0000-4000-8000-000000000001', '82', 'Santa Fe', 'santa fe', 'province', NULL, -30.7088227091528, -60.9506872769706),
                    ('00000000-0000-4000-8000-000000000001', '86', 'Santiago del Estero', 'santiago del estero', 'province', NULL, -27.7834318817521, -63.2526268856462),
                    ('00000000-0000-4000-8000-000000000001', '90', 'Tucumán', 'tucuman', 'province', NULL, -26.948283501723, -65.3647655803683),
                    ('00000000-0000-4000-8000-000000000001', '94', 'Tierra del Fuego, Antártida e Islas del Atlántico Sur', 'tierra del fuego, antartida e islas del atlantico sur', 'province', NULL, -82.5211345211545, -50.7428606764691);

                SELECT territory.activate_official_snapshot(
                    '00000000-0000-4000-8000-000000000001'::uuid);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DO $ephemeral_only$
                BEGIN
                    IF pg_catalog.current_database() NOT LIKE 'agro_territory_%'
                       AND pg_catalog.current_database() <> 'agropecuaria_design' THEN
                        RAISE EXCEPTION 'Destructive territory rollback is restricted to ephemeral databases.';
                    END IF;
                END
                $ephemeral_only$;

                DROP TRIGGER IF EXISTS official_units_staging_write
                    ON territory.official_units;
                DROP TRIGGER IF EXISTS snapshots_immutable ON territory.snapshots;
                DROP FUNCTION IF EXISTS territory.activate_official_snapshot(uuid);
                DROP FUNCTION IF EXISTS territory.enforce_official_unit_staging_write();
                DROP FUNCTION IF EXISTS territory.enforce_snapshot_immutability();
                ALTER TABLE territory.official_units OWNER TO CURRENT_USER;
                ALTER TABLE territory.snapshots OWNER TO CURRENT_USER;
                ALTER SCHEMA territory OWNER TO CURRENT_USER;
                """);

            migrationBuilder.DropTable(
                name: "official_units",
                schema: "territory");

            migrationBuilder.DropTable(
                name: "snapshots",
                schema: "territory");
        }
    }
}
