# AGRO-DIS-004 — reporte de validación

Fecha: 2026-08-05. Estado técnico: aprobado para alcance R0; no aprobado para producción.

## Decisión

- GIS/PostGIS: `GO CONDICIONAL` para construir R2 con WGS84/SRID 4326, geometrías válidas/versionadas, `ST_Area(geography)`, área declarada separada e índice GiST. Los límites del spike no son reglas agronómicas.
- Mapa: `GO CONDICIONAL` con MapLibre como renderer, Argenmap como proveedor candidato, atribución visible y fallback tabular.
- Open-Meteo: `GO CONDICIONAL` únicamente al contrato; producción requiere plan comercial, DPA/región, cuota, presupuesto y validación local.
- SMN CAP: `GO CONDICIONAL` como autoridad con schema, vigencia y lifecycle fail-closed; autenticidad/canal operacional siguen abiertos.
- SMN WRF: `POSTPONER`. La muestra es técnicamente parseable, pero una corrida estimada de 73 plazos supera 1 GiB y no hay presupuesto ni operación aprobados.

## Evidencia medible

La ejecución final, comandos y revisiones se registran en `tasks/todo .md`. Resumen: .NET 29/29, Vitest 7/7, Playwright 4/4, Python 5/5 y PostGIS 6/6; build/format/lint/typecheck/audits sin fallos. Ajv 2020 validó tres ejemplos canónicos y cinco provider runs. Los resultados machine-readable están en `results/provider-probes.json` y `results/wrf-sample.json`; las licencias y condiciones están en `source-and-license-matrix.md`.

El último smoke live persistido observó Georef 120,066 ms, Open-Meteo degradado a 2.149,274 ms, CAP 223,24 ms, Argenmap p75 141,201 ms y WRF listing 755,651 ms. Open-Meteo es una única muestra batch, no p75; CAP varió entre XML válido y HTML degradado en revisiones consecutivas. Ambos comportamientos impiden afirmar SLA.

## Condiciones que impiden producción

- Q-021–Q-030: horizontes, umbrales, canales, tolerancias, contratación y autorización WRF.
- Q-012/Q-024/Q-027/Q-028 y `VAL-AGR`: comparación con observaciones/campo y aprobación profesional.
- Autorización por recurso, tenant/RLS, auditoría y privacidad de coordenadas no pertenecen a este spike y son obligatorias en R2/R3.
- No existe migración, pipeline, endpoint, job, almacenamiento ni deployment productivo en este artefacto R0.
