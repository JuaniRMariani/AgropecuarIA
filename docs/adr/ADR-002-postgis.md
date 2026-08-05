# ADR-002 — PostgreSQL/PostGIS y geometrías versionadas

- Estado: propuesto
- Fecha: 2026-08-04

## Contexto

Lotes y potreros cambian; superficie, intersección e historial espacial son parte del núcleo.

## Decisión

PostgreSQL/PostGIS como fuente transaccional/geoespacial. Geometrías válidas con SRID explícito, índice GiST y versiones efectivas. Superficie declarada y calculada se conservan por separado.

## Alternativas

- Coordenadas JSON sin base espacial: insuficiente para integridad/consultas.
- GIS separado desde el inicio: agrega consistencia distribuida innecesaria.

## Consecuencias

Se requieren pruebas con proyecciones, topología y performance. Los eventos históricos referencian la versión vigente en su fecha.
