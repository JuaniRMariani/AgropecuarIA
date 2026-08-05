INSERT INTO spike.geometry_fixture (case_id, wkt, srid, expected_acceptable)
VALUES
    (
        'valid-polygon',
        'POLYGON((-60 -35,-59.9 -35,-59.9 -34.9,-60 -34.9,-60 -35))',
        4326,
        true
    ),
    (
        'valid-multipolygon',
        'MULTIPOLYGON(((-60 -35,-59.9 -35,-59.9 -34.9,-60 -34.9,-60 -35)))',
        4326,
        true
    ),
    (
        'empty-multipolygon',
        'MULTIPOLYGON EMPTY',
        4326,
        false
    ),
    (
        'wrong-srid',
        'POLYGON((-60 -35,-59.9 -35,-59.9 -34.9,-60 -34.9,-60 -35))',
        3857,
        false
    ),
    (
        'self-intersection',
        'POLYGON((-60 -35,-59.9 -34.9,-60 -34.9,-59.9 -35,-60 -35))',
        4326,
        false
    );
