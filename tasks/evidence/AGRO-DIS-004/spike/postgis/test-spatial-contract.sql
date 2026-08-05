\set ON_ERROR_STOP on
\timing on

CREATE EXTENSION postgis;

CREATE SCHEMA spike;

CREATE OR REPLACE FUNCTION spike.assert_true(condition boolean, failure_message text)
RETURNS void
LANGUAGE plpgsql
AS $$
BEGIN
    IF condition IS DISTINCT FROM true THEN
        RAISE EXCEPTION 'ASSERTION FAILED: %', failure_message;
    END IF;
END;
$$;

CREATE TABLE spike.test_result (
    test_name text PRIMARY KEY,
    status text NOT NULL CHECK (status = 'PASS'),
    detail text NOT NULL
);

CREATE TABLE spike.geometry_fixture (
    case_id text PRIMARY KEY,
    wkt text NOT NULL,
    srid integer NOT NULL,
    expected_acceptable boolean NOT NULL
);

\ir ../../fixtures/geometry/geometry-fixtures.sql

CREATE OR REPLACE FUNCTION spike.is_acceptable_boundary(candidate geometry, raw_payload_bytes bigint)
RETURNS boolean
LANGUAGE sql
IMMUTABLE
STRICT
AS $$
    SELECT
        raw_payload_bytes BETWEEN 1 AND 1048576
        AND NOT ST_IsEmpty(candidate)
        AND ST_SRID(candidate) = 4326
        AND ST_NDims(candidate) = 2
        AND GeometryType(candidate) IN ('POLYGON', 'MULTIPOLYGON')
        AND ST_NPoints(candidate) BETWEEN 4 AND 10000
        AND ST_IsValid(candidate);
$$;

CREATE OR REPLACE FUNCTION spike.make_regular_polygon(vertex_count integer)
RETURNS geometry
LANGUAGE sql
IMMUTABLE
STRICT
AS $$
    WITH ordered_points AS (
        SELECT
            point_number,
            ST_SetSRID(ST_MakePoint(
                -65.0 + 0.1 * cos(2 * pi() * point_number / (vertex_count - 1)),
                -35.0 + 0.1 * sin(2 * pi() * point_number / (vertex_count - 1))
            ), 4326) AS point
        FROM generate_series(0, vertex_count - 1) AS point_number
    )
    SELECT ST_MakePolygon(ST_MakeLine(point ORDER BY point_number))
    FROM ordered_points;
$$;

SELECT spike.assert_true(
    (SELECT extversion = '3.6.2' FROM pg_extension WHERE extname = 'postgis'),
    'The isolated database must load PostGIS 3.6.2.'
);

SELECT spike.assert_true(
    current_setting('listen_addresses') = '127.0.0.1',
    'The ephemeral server must listen only on IPv4 loopback.'
);

SELECT spike.assert_true(
    NOT EXISTS (
        SELECT 1
        FROM spike.geometry_fixture
        WHERE spike.is_acceptable_boundary(
            ST_GeomFromText(wkt, srid),
            octet_length(convert_to(wkt, 'UTF8'))
        ) IS DISTINCT FROM expected_acceptable
    ),
    'Polygon, MultiPolygon, empty, wrong-SRID and self-intersection acceptance must match the fixture.'
);

SELECT spike.assert_true(
    GeometryType(ST_GeomFromText(
        (SELECT wkt FROM spike.geometry_fixture WHERE case_id = 'valid-polygon'),
        4326
    )) = 'POLYGON',
    'The Polygon fixture must retain its geometry type.'
);

SELECT spike.assert_true(
    GeometryType(ST_GeomFromText(
        (SELECT wkt FROM spike.geometry_fixture WHERE case_id = 'valid-multipolygon'),
        4326
    )) = 'MULTIPOLYGON',
    'The MultiPolygon fixture must retain its geometry type.'
);

INSERT INTO spike.test_result
VALUES (
    'geometry-validation',
    'PASS',
    'Polygon/MultiPolygon SRID 4326 accepted; empty, SRID 3857 and self-intersection rejected without ST_MakeValid.'
);

CREATE TABLE spike.area_comparison AS
WITH samples(sample_id, boundary) AS (
    VALUES
        ('north', ST_GeomFromText('POLYGON((-65 -23,-64.9 -23,-64.9 -22.9,-65 -22.9,-65 -23))', 4326)),
        ('center', ST_GeomFromText('POLYGON((-60 -35,-59.9 -35,-59.9 -34.9,-60 -34.9,-60 -35))', 4326)),
        ('south', ST_GeomFromText('POLYGON((-68 -54,-67.9 -54,-67.9 -53.9,-68 -53.9,-68 -54))', 4326))
)
SELECT
    sample_id,
    ST_Area(boundary::geography) AS geography_square_metres,
    ST_Area(ST_Transform(boundary, 6933)) AS equal_area_square_metres,
    abs(
        ST_Area(boundary::geography) - ST_Area(ST_Transform(boundary, 6933))
    ) / ST_Area(boundary::geography) * 100 AS relative_delta_percent
FROM samples;

SELECT spike.assert_true(
    NOT EXISTS (
        SELECT 1
        FROM spike.area_comparison
        WHERE relative_delta_percent > 0.5
    ),
    'ST_Area(geography) and EPSG:6933 must differ by no more than 0.5% for north/center/south fixtures.'
);

INSERT INTO spike.test_result
SELECT
    'area-comparison',
    'PASS',
    format(
        'Maximum geography vs EPSG:6933 delta: %s%%.',
        round(max(relative_delta_percent)::numeric, 6)
    )
FROM spike.area_comparison;

CREATE TABLE spike.land_record (
    record_id integer GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    declared_area_hectares numeric(14, 4) NOT NULL CHECK (declared_area_hectares >= 0),
    calculated_area_hectares numeric(14, 4) NOT NULL CHECK (calculated_area_hectares >= 0),
    boundary geometry(MultiPolygon, 4326) NOT NULL CHECK (ST_IsValid(boundary))
);

WITH boundary AS (
    SELECT ST_Multi(ST_GeomFromText(
        (SELECT wkt FROM spike.geometry_fixture WHERE case_id = 'valid-polygon'),
        4326
    )) AS geom
)
INSERT INTO spike.land_record (declared_area_hectares, calculated_area_hectares, boundary)
SELECT 100.0000, round((ST_Area(geom::geography) / 10000)::numeric, 4), geom
FROM boundary;

SELECT spike.assert_true(
    (
        SELECT declared_area_hectares = 100.0000
            AND calculated_area_hectares > 0
            AND declared_area_hectares <> calculated_area_hectares
        FROM spike.land_record
    ),
    'Declared and calculated hectares must coexist as distinct values.'
);

INSERT INTO spike.test_result
SELECT
    'area-separation',
    'PASS',
    format(
        'Declared=%s ha; calculated=%s ha; neither value overwrote the other.',
        declared_area_hectares,
        calculated_area_hectares
    )
FROM spike.land_record;

CREATE TEMP TABLE geometry_limit_probe (
    case_id text PRIMARY KEY,
    boundary geometry(Polygon, 4326) NOT NULL,
    raw_payload_bytes bigint NOT NULL,
    expected_acceptable boolean NOT NULL
);

INSERT INTO geometry_limit_probe (case_id, boundary, raw_payload_bytes, expected_acceptable)
VALUES
    ('4-vertices', spike.make_regular_polygon(4), 128, true),
    ('100-vertices', spike.make_regular_polygon(100), 4096, true),
    ('1000-vertices', spike.make_regular_polygon(1000), 32768, true),
    ('10000-vertices', spike.make_regular_polygon(10000), 524288, true),
    ('10001-vertices', spike.make_regular_polygon(10001), 524320, false),
    ('payload-over-1-mib', spike.make_regular_polygon(4), 1048577, false);

SELECT spike.assert_true(
    NOT EXISTS (
        SELECT 1
        FROM geometry_limit_probe
        WHERE spike.is_acceptable_boundary(boundary, raw_payload_bytes)
            IS DISTINCT FROM expected_acceptable
    ),
    'Geometry limits must accept 4/100/1,000/10,000 vertices and reject 10,001 vertices or a payload over 1 MiB.'
);

INSERT INTO spike.test_result
VALUES (
    'geometry-limits',
    'PASS',
    'Accepted 4/100/1,000/10,000 vertices; rejected 10,001 vertices and a declared payload over 1 MiB.'
);

CREATE TABLE spike.cap_alert (
    alert_identifier text PRIMARY KEY,
    affected_area geometry(MultiPolygon, 4326) NOT NULL CHECK (ST_IsValid(affected_area))
);

INSERT INTO spike.cap_alert (alert_identifier, affected_area)
VALUES (
    'SYNTHETIC-CAP-OVERLAP',
    ST_Multi(ST_GeomFromText(
        'POLYGON((-60.05 -35.05,-59.95 -35.05,-59.95 -34.95,-60.05 -34.95,-60.05 -35.05))',
        4326
    ))
);

SELECT spike.assert_true(
    (
        SELECT ST_Intersects(alert.affected_area, land.boundary)
        FROM spike.cap_alert AS alert
        CROSS JOIN spike.land_record AS land
        WHERE alert.alert_identifier = 'SYNTHETIC-CAP-OVERLAP'
    ),
    'The synthetic CAP polygon must intersect the field boundary.'
);

SELECT spike.assert_true(
    NOT ST_Intersects(
        (SELECT affected_area FROM spike.cap_alert WHERE alert_identifier = 'SYNTHETIC-CAP-OVERLAP'),
        ST_Multi(ST_GeomFromText(
            'POLYGON((-72 -50,-71.9 -50,-71.9 -49.9,-72 -49.9,-72 -50))',
            4326
        ))
    ),
    'A distant field must not intersect the synthetic CAP polygon.'
);

INSERT INTO spike.test_result
VALUES (
    'cap-intersection',
    'PASS',
    'CAP overlap and non-overlap are decided by spatial intersection, not locality names.'
);

CREATE TABLE spike.spatial_index_probe (
    probe_id integer PRIMARY KEY,
    location geometry(Point, 4326) NOT NULL
);

INSERT INTO spike.spatial_index_probe (probe_id, location)
SELECT
    sample_number,
    ST_SetSRID(ST_MakePoint(
        -73.0 + (sample_number % 250) * 0.04,
        -55.0 + floor(sample_number / 250) * 0.10
    ), 4326)
FROM generate_series(0, 49999) AS sample_number;

CREATE INDEX ix_spatial_index_probe_location
    ON spike.spatial_index_probe USING gist (location);
ANALYZE spike.spatial_index_probe;

DO $$
DECLARE
    plan_line record;
    complete_plan text := '';
    matching_rows integer;
BEGIN
    FOR plan_line IN EXECUTE $plan$
        EXPLAIN (COSTS OFF)
        SELECT probe_id
        FROM spike.spatial_index_probe
        WHERE location && ST_MakeEnvelope(-69.001, -49.001, -68.999, -48.999, 4326)
    $plan$
    LOOP
        complete_plan := complete_plan || plan_line."QUERY PLAN" || E'\n';
    END LOOP;

    SELECT count(*)
    INTO matching_rows
    FROM spike.spatial_index_probe
    WHERE location && ST_MakeEnvelope(-69.001, -49.001, -68.999, -48.999, 4326);

    PERFORM spike.assert_true(
        complete_plan LIKE '%ix_spatial_index_probe_location%'
            AND (
                complete_plan LIKE '%Index Scan%'
                OR complete_plan LIKE '%Bitmap Index Scan%'
            ),
        'EXPLAIN must demonstrate use of the GiST index.'
    );

    PERFORM spike.assert_true(
        matching_rows > 0,
        'The selective indexed probe must return at least one row.'
    );

    INSERT INTO spike.test_result
    VALUES (
        'gist-index-plan',
        'PASS',
        format('EXPLAIN used ix_spatial_index_probe_location; matching rows=%s.', matching_rows)
    );
END;
$$;

SELECT postgis_full_version() AS postgis_runtime;
SELECT sample_id, geography_square_metres, equal_area_square_metres, relative_delta_percent
FROM spike.area_comparison
ORDER BY sample_id;
SELECT test_name, status, detail
FROM spike.test_result
ORDER BY test_name;
