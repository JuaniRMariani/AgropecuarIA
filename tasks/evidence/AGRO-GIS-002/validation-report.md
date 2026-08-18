# Validación — campo borrador no espacial

Estado: `PASS` integrado-local. `AGRO-GIS-002` permanece `En curso`.

Publicación funcional: `4d4fe70` en `origin/main`, con author y committer `JuaniRMariani <juanirmariani@gmail.com>`. No hubo deploy.

## Alcance demostrado

- POST/GET list/GET detail tenant-scoped.
- Productive Core, PostgreSQL `FORCE RLS`, idempotencia, journal y outbox.
- Formulario, lista y ficha accesibles sin mapa ni geometría.

## Gates reproducidos

- `dotnet restore AgropecuarIA.slnx --locked-mode`: PASS.
- `dotnet build AgropecuarIA.slnx --configuration Release --no-restore`: PASS, 9 proyectos, 0 warnings/0 errores.
- MTP raíz: PASS `308/308`; Productive Core/PostgreSQL `30/30`; Architecture Fitness `121/121`.
- `dotnet format ... --verify-no-changes`: PASS. EF Identity, Territory y Productive Core: `3/3` sin pending model changes.
- PostgreSQL real: migración Identity→Productive, clean, N/N-1 aditivo, rollback/roll-forward efímero, roles/grants, `FORCE RLS`, A/B/sin contexto/pool/job/membership removida y coverage HMAC: PASS.
- Fault injection ledger/journal/outbox: `3/3`; los tres casos atraviesan `CreateField` y dejan 0 filas en las cinco superficies.
- pnpm frozen/format/lint/typecheck/build: PASS; Vitest `130/130`.
- Playwright oficial: PASS `6/6` desktop/mobile, create→reload→detail, aislamiento A/B y owner removido, Axe, teclado y 390 px; PostgreSQL, temporales y puertos quedaron limpios.
- FND `45/45`; SEC `53/53`; SEC-002 `25/25` operaciones; JSON/event/OpenAPI/runtime maps: PASS.
- NuGet `9/9` y pnpm audit: 0 vulnerabilidades conocidas. Secret scan, UTF-8 estricto y `git diff --check`: PASS.

## Revisión

Backend, Database/AppSec, Frontend/QA y la revisión independiente no confirmaron vulnerabilidades críticas, altas o medias. Antes del cierre se corrigieron el formato de `Idempotency-Key`, la canonicalización idéntica `White_Space`+`U+FEFF`/NFC por escalares, la seguridad conjuntiva de OpenAPI, el mapeo 503 de apertura y commit de lecturas, la persistencia frontend del intento antes del POST, el límite transaccional de 100 campos, la cancelación y limpieza de pool, el extractor de rutas y la evidencia de rollback por cada sink.

El primer rerun Playwright terminó `5/6` porque el journey crítico de Chromium completó sus aserciones pero agotó el timeout acumulativo de 30 segundos durante el cierre. El timeout se amplió sólo para ese journey largo a 60 segundos; el runner oficial posterior terminó `6/6` y dejó PostgreSQL, puertos, temporales, resultados y `next-env.d.ts` limpios.

## Riesgos residuales

- `AGRO-GIS-002` completo continúa bloqueado por geometría/área/tolerancias, catálogo, tiles y decisiones de proveedor. El rename posterior del nombre bajo `AGRO-FND-003` no resuelve esas dependencias ni habilita otras ediciones.
- Delivery de outbox/inbox permanece pendiente de `AGRO-FND-002`; este slice sólo demuestra producer-side atómico.
- El incremento es evidencia local integrada, no aprobación de producción o proveedor.
