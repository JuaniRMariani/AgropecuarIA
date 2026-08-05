# ADR-002 — PostgreSQL/PostGIS y geometrías versionadas

- Estado: aceptado para contrato de discovery; pendiente implementación R2
- Fecha: 2026-08-05

## Contexto

Lotes y potreros cambian; superficie, intersección e historial espacial son parte del núcleo.

## Decisión

PostgreSQL/PostGIS como fuente transaccional/geoespacial. Geometrías válidas con SRID explícito, índice GiST y versiones efectivas. Superficie declarada y calculada se conservan por separado.

`AGRO-DIS-004` valida como baseline WGS84/SRID 4326 para intercambio y persistencia conceptual `MultiPolygon`; el área calculada usa `ST_Area(geography)` sobre esferoide. EPSG:6933 se usa únicamente como control técnico nacional, no como sustituto de la medida canónica ni como umbral agronómico. Payload/vértices se limitan antes de PostGIS y una geometría inválida se rechaza: nunca se ejecuta `ST_MakeValid` de forma silenciosa.

## Alternativas

- Coordenadas JSON sin base espacial: insuficiente para integridad/consultas.
- GIS separado desde el inicio: agrega consistencia distribuida innecesaria.

## Consecuencias

Se requieren pruebas con proyecciones, topología y performance. Los eventos históricos referencian la versión vigente en su fecha.

Los límites de 1 MiB/10.000 vértices y el delta de control ≤0,5 % pertenecen al spike y deben revisarse con telemetría en R2. La tolerancia entre superficie declarada/calculada queda pendiente de Product/GIS; hasta entonces se informa la diferencia sin reescribir ni aprobar automáticamente ninguna fuente.
