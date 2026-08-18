# Validación — renombrar campo borrador

Fecha: 2026-08-18. Estado: `En curso`. Resultado: PASS integrado-local para `RenameFieldDraft`; el padre conserva sus restantes patrones de evolución.

## Resultado demostrado

- `PATCH /api/organizations/{organizationId}/fields/{fieldId}` exige sesión, CSRF, `Idempotency-Key` y un `If-Match` UUID fuerte. Autorización viva precede lookup/replay y un intento nuevo stale recibe 412 neutral.
- El rename modifica únicamente `DisplayName`, rota `Version`, incrementa `Revision` y hace converger lista/ficha. Replay autorizado devuelve el mismo resultado; mismatch, in-flight y commit incierto permanecen fail-closed.
- PostgreSQL aplica `FORCE RLS`, grants UPDATE por columna y un trigger de transición. Field, ledger/aliases HMAC, journal y outbox confirman atómicamente; fault injection por ledger, journal y outbox conserva la versión anterior y deja cero parciales.
- La migración expand preserva N/N-1 y rollback/roll-forward efímero. La rotación HMAC v1→v1+v2→v2-only materializa aliases lazy y el retiro temprano falla 503 antes del efecto.
- La UI conserva draft y key ante resultados ambiguos, recupera 412 mediante “Recargar y revisar”, usa UUID corto y cubre teclado, Axe y viewport móvil.

## Gates reproducidos

- Restore locked y build Release: PASS, 9 proyectos, 0 warnings/0 errores.
- Suite raíz MTP: PASS `348/348`, 0 failed/skipped.
- Productive Core con PostgreSQL real: PASS `56/56`; no-DB/API: PASS `37/37`.
- Architecture Fitness: PASS `135/135`; registro SEC-002: `26/26` operaciones.
- `dotnet format --verify-no-changes`: PASS después de normalizar la migración generada; EF Identity/Territory/Productive Core: PASS `3/3`, sin model drift.
- Frontend: frozen install, Prettier, ESLint, typecheck y build PASS; Vitest `153/153`.
- Playwright oficial desktop/móvil: PASS `6/6`; PostgreSQL temporal detenido y puertos liberados.
- FND protocol: PASS `45/45` mutations. SEC threat model: PASS `56/56` mutations.
- NuGet `9/9` y pnpm audit productivo sin vulnerabilidades conocidas; JSON, parser PowerShell, secrets, UTF-8 y `git diff --check`: PASS.

## Revisión

- PASS independiente con 0 Critical, 0 High y 0 Medium pendientes. La revisión detectó y cerró antes de publicar dos Medium: alias split durante recovery ahora produce 503 de reconciliación, y el rol app perdió UPDATE sobre rename ledgers; PostgreSQL demuestra `42501`.

## Riesgos residuales

- El padre `AGRO-FND-003` conserva backfills/contract migrations/restore y otros agregados.
- Geometría, área, catálogo, archive/delete y roles no-owner quedan fuera.
- Delivery durable del outbox permanece pendiente de `AGRO-FND-002`.
- El resultado será integrado-local, no una aprobación de deploy productivo.
