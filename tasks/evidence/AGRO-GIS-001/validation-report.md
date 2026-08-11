# AGRO-GIS-001 — reporte de validación

Fecha: 2026-08-11. Alcance: TerritoryReference v1 integrado localmente.

## Resultado

`PASS` para el incremento local TerritoryReference v1. `AGRO-GIS-001` permanece `En curso`: el egress Georef real está deshabilitado por defecto y siguen pendientes la fuente jerárquica completa, la actualización administrada y los gates de proveedor/Legal/ambiente compartido.

## Evidencia ejecutable

- `dotnet restore AgropecuarIA.slnx --locked-mode`: PASS.
- `dotnet build AgropecuarIA.slnx --configuration Release --no-restore`: PASS, 0 warnings y 0 errores.
- MTP raíz: PASS `223/223`, 0 fallidos y 0 omitidos.
- Territory/PostgreSQL: PASS `44/44`; seed de 24 jurisdicciones, roles/RLS, hash canónico, activación, homónimos, provider y rollback/roll-forward incluidos.
- Architecture Fitness: PASS `79/79`; OpenAPI, runtime map, fronteras, logging sin URI y mutations contractuales incluidos.
- `dotnet format` y `dotnet ef migrations has-pending-model-changes` para Identity y Territory: PASS, sin drift.
- pnpm frozen, Prettier, lint, typecheck y Next build: PASS; Vitest `79/79`.
- Playwright hermético sobre PostgreSQL 17 SCRAM: PASS `6/6` en Chromium y mobile; búsqueda, degradación local, teclado, Axe y 390 px.
- FND protocol validator: PASS `45/45`; SEC validator: PASS `41/41`.
- NuGet y pnpm audit: 0 vulnerabilidades conocidas; JSON/UTF-8/secrets/diff-check: PASS.
- Revisión independiente: 0 hallazgos críticos, altos o medios abiertos.

La migración es el primer schema Territory: se demostró empty→N, convivencia aditiva con Identity y rollback/roll-forward efímero. No se declara un writer Territory N-1 que no existía.

## Límites

- No es evidencia de deploy ni de disponibilidad/SLA de Georef.
- No crea campos, geometrías ni mapa y no convierte centroides administrativos en ubicación de parcelas.
- No persiste coordenadas consultadas ni habilita una búsqueda pública.
- La tarea padre permanece `En curso`.
