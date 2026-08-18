# Validation report — AGRO-SEC-002 Identity tenant v1

Fecha: 2026-08-11. Base: `15ead58`. Alcance: evidencia y fitness; cero cambios en runtime productivo.

## Resultado

PASS integrado-local. El gate registra y valida exactamente 20 operaciones HTTP de Identity/Territory, un callback OIDC framework-owned y cinco superficies futuras `not-present`. AGRO-SEC-002 permanece `En curso`.

## Gates reproducidos

- `dotnet restore AgropecuarIA.slnx --locked-mode`: PASS.
- `dotnet build AgropecuarIA.slnx --configuration Release --no-restore`: PASS, 0 warnings, 0 errores.
- `dotnet test --solution AgropecuarIA.slnx --configuration Release --no-build --minimum-expected-tests 240`: PASS `240/240`, 0 failed/skipped.
- Architecture Fitness: PASS `96/96`; `AuthorizationSurfaceContractTests` aporta 17 tests (caso publicado + 16 mutations negativas).
- `dotnet format AgropecuarIA.slnx --verify-no-changes --no-restore`: PASS.
- EF Identity y Territory con `has-pending-model-changes --context ...`: PASS, sin drift. La primera invocación sin `--context` fue ambigua por los dos DbContext y se repitió correctamente de forma explícita.
- `pnpm install --frozen-lockfile`, `format`, `lint`, `typecheck`, `test`, `build`: PASS; Vitest `79/79`.
- `scripts/identity/run-e2e.ps1`: PASS `6/6` desktop/mobile; PostgreSQL efímero detenido y eliminado por el runner.
- FND protocol: PASS `45/45` mutations.
- SEC threat model: PASS `41/41` mutations.
- Security findings schema: PASS, `0 findings valid`.
- NuGet SCA: 7/7 proyectos sin vulnerabilidades conocidas. pnpm audit productivo: sin vulnerabilidades conocidas.
- JSON parse, UTF-8 estricto, secret scan y `git diff --check`: PASS.

## Revisión

Tres revisiones read-only no confirmaron BOLA, bypass de sesión/OIDC/CSRF/step-up, SQLi, SSRF ni boundary confusion explotables con la configuración por defecto. Una cuarta revisión independiente detectó y permitió cerrar dos defectos del gate: extracción limitada de métodos/grupos y falta de enforcement del storage/egress shared-reference.

El cache singleton de resolución Territory puede revelar por `capturedAtUtc` que otro usuario consultó una coordenada exacta si Georef se habilita. Georef está default-off y no hay deploy compartido, por lo que se registra como condición previa a habilitar egress, no como finding activo.

El modelo actual sólo admite memberships `active`; por eso la evidencia no afirma una revocación de membership inexistente. El negativo vigente es sesión revocada; remove/demote/last-owner pertenece al siguiente slice AGRO-ID-003.

## Refresh: remoción segura de co-owner — 2026-08-18

El baseline anterior se conserva como evidencia histórica. El registro vigente cubre exactamente `22/22` operaciones HTTP e incorpora el listado privacy-safe de owners activos y la remoción de otro co-owner. La membership autoritativa admite ahora `active|removed`; self-remove, transferencia, democión y roles no-owner siguen fuera.

- Actor, tenant y membership se revalidan antes de ledger/lookup; la remoción exige sesión, CSRF, `If-Match`, `Idempotency-Key` y step-up `manage_organization_owners`.
- PostgreSQL serializa por organización, preserva al menos un owner activo, aplica `FORCE RLS`, evita grants UPDATE/DELETE amplios y revoca en la misma transacción las invitaciones pendientes creadas por el owner removido.
- Suite raíz MTP: PASS `256/256`; Architecture Fitness: PASS `101/101`; frontend Vitest `95/95`; Playwright `6/6`; FND `45/45`; SEC `42/42`.
- Restore locked, build Release 0/0, format, EF Identity/Territory, SCA NuGet/pnpm, JSON y diff-check: PASS.

Resultado vigente: PASS integrado-local, sin vulnerabilidades críticas, altas o medias confirmadas. `AGRO-SEC-002` permanece `En curso` y no demuestra deploy compartido.

## Refresh: Productive Core field draft no espacial — 2026-08-18

El registro vigente cubre exactamente `25/25` operaciones HTTP: las 22 previas de Identity/Territory más `productive-core.field.create`, `productive-core.field.list` y `productive-core.field.detail`.

- Rutas, OpenAPI, runtime/consumer maps y catálogo `ManagementUnitCreated` quedaron trazados; el evento es tenant-scoped y su payload no incluye nombre, actor, key/digest ni geometría.
- Productive Core revalida sesión y owner mediante `identity.authorize_productive_owner()` antes de alias/ledger o recurso; fija actor, tenant, sesión y authorization version transaction-local y aplica `FORCE RLS`.
- Build dirigido Productive Core: PASS, 0 warnings/0 errores.
- Pruebas no-DB/API Productive Core: PASS `21/21`, incluida normalización Unicode exacta, estado inicial, replay/mismatch, capacidad, duplicados permitidos, denial-before-lookup, orden/ficha neutral, begin/read-commit unavailable, commit unknown, keyring fail-fast, rutas/metadata y shape JSON.
- Suite Productive Core completa PostgreSQL: PASS `30/30`, 0 failed/skipped; tres casos de fault injection atraviesan `CreateField` y dejan 0 filas en unidad/ledger/aliases/journal/outbox ante fallo de ledger, journal u outbox. También cubre la carrera 99→100, sentinel 101, cancelación y reutilización del pool sin contexto.
- Suite raíz MTP: PASS `308/308`; Architecture Fitness `121/121`; frontend Vitest `130/130`; Playwright `6/6`; FND `45/45`; SEC `53/53`.
- Restore locked, build Release `0/0`, format, EF Identity/Territory/Productive Core `3/3`, NuGet `9/9` y pnpm audit sin vulnerabilidades, JSON/UTF-8/secrets y diff-check: PASS.
- Revisión security-audit no confirmó BOLA, bypass owner/CSRF, SQLi, XSS, fuga de idempotency material ni telemetría sensible. El mismatch backend/OpenAPI de idempotency key fue corregido a `32..128` `[A-Za-z0-9_-]` y cubierto con fronteras negativas.
- Límite local: no se verifican Auth0, proxy/edge/TLS, Data Protection, limiter distribuido, collector, secrets compartidos, backups ni egress Georef en un ambiente desplegado.

Resultado del refresh: PASS de seguridad integrado-local para este sub-slice. `AGRO-SEC-002` permanece `En curso`.

## Refresh: RenameFieldDraft y control de versión — 2026-08-18

El registro vigente cubre exactamente `26/26` operaciones HTTP y agrega `productive-core.field.rename`. El PATCH exige sesión, CSRF, `Idempotency-Key` e `If-Match` fuerte; tenant, actor, sesión y autorización se derivan del servidor. Owner removido, sesión revocada, tenant ajeno, recurso ausente y contexto incompleto fallan neutralmente antes de lookup/replay.

- PostgreSQL usa `FORCE RLS`, grants UPDATE por columna y trigger de transición; dos escritores con el mismo ETag producen un único rename y el perdedor recibe 412.
- Replay, mismatch, in-flight, commit incierto, rotación HMAC N/N-1, retiro temprano y alias split están cubiertos sin reejecución ciega.
- El evento y la telemetría no contienen display name, actor, sesión, key, digest ni payload. El frontend conserva la key ante respuestas ambiguas y revalida ETag/tenant/field desde respuestas `unknown`.
- Suite raíz `348/348`; Productive Core `56/56`; Architecture Fitness `135/135`; Vitest `153/153`; Playwright `6/6`; FND `45/45`; SEC `56/56`; EF `3/3`; SCA, JSON, parser, secretos y diff-check PASS.

Resultado vigente: PASS integrado-local, sin vulnerabilidades críticas, altas o medias confirmadas. `AGRO-SEC-002` permanece `En curso`; cada nueva mutación, job, export, retrieval o boundary debe agregarse al registro con sus pruebas negativas.

La revisión independiente cerró dos Medium antes del gate final: recuperación de alias split tipada como reconciliación 503 y eliminación del privilegio UPDATE de la app sobre rename ledgers. Los probes PostgreSQL confirman sólo SELECT/INSERT y UPDATE denegado `42501`.

## Refresh: inventario y revocación de sesiones propias — 2026-08-18

El registro vigente cubre exactamente `28/28` operaciones HTTP y agrega `identity.sessions.list` y `identity.sessions.revoke`. Ambas son platform-scoped: actor y sesión actual se derivan de la cookie opaca revalidada y no intervienen organización, tenant o rol.

- GET pagina únicamente sesiones propias activas y devuelve sólo ID, autenticación, expiración, marca current y versión. No expone token/hash, IP, user-agent, dispositivo, proveedor ni organización.
- DELETE exige CSRF, `If-Match` UUID fuerte y step-up purpose exacto `manage_sessions`; la sesión actual usa el logout existente, foreign/missing/active-expired fallan neutralmente y stale responde 412. Una sesión propia ya revocada conserva replay 204 sin segundo efecto.
- PostgreSQL usa funciones `SECURITY DEFINER` con `search_path` fijo y EXECUTE app-only. La app no recibe SELECT de `TokenHash`, acceso a users ni UPDATE de sessions; el CAS concurrente conserva una transición y un journal.
- Suite raíz MTP `361/361`; Identity `126/126`; Architecture Fitness `135/135`; frontend Vitest `206/206`; Playwright oficial `10/10`; FND `45/45`; SEC `56/56`; EF `3/3` sin drift.
- Restore locked, build Release `0/0`, format, NuGet/pnpm audit, UTF-8/JSON, parser, secretos y diff-check: PASS.

Resultado vigente: PASS integrado-local. `AGRO-SEC-002` permanece `En curso`; revoke-all, device inventory, notificaciones, retención/purge, propagación distribuida, Auth0/edge/collector y deploy compartido siguen fuera.

## Refresh: cierre atómico de las demás sesiones propias — 2026-08-18

El registro vigente cubre exactamente `29/29` operaciones HTTP y agrega `identity.sessions.revoke-others`.

- `DELETE /api/identity/sessions/others` es platform-scoped, exige cookie+CSRF y assurance exacta `manage_sessions`; no recibe body, IDs, tenant, `If-Match` ni count y preserva current.
- PostgreSQL deriva actor/current/authorizationVersion desde contexto transaction-local. Las funciones individual y bulk comparten el mismo advisory transaction lock por actor y revalidan current con row lock antes del target; app sólo recibe `EXECUTE`, mientras job/discovery/public no reciben acceso de tabla ni ejecución indebida.
- Cada target efectivo rota `Version`, fija un timestamp común y obtiene un journal `session_revoked` en la misma transacción. Cero targets/replay devuelve 204; bulk×bulk, bulk×individual y la carrera cross-current A→B/B→A no duplican transición ni dejan ambas current revocadas; fallo de journal o cancelación revierte todo.
- Suite raíz `371/371`; Identity `136/136`; DB own-session `12/12`; API own-session `10/10`; Architecture Fitness `135/135`; Vitest `217/217`; Playwright `10/10`; FND `45/45`; SEC `56/56`; EF `3/3`; build/format/SCA/JSON/diff PASS.

Resultado vigente: PASS integrado-local. `AGRO-SEC-002` y `AGRO-ID-004` permanecen `En curso`; no se aprueba dispositivo, notificación, sesión ajena, evento/consumer, retención/purge ni deploy compartido.

## Refresh: cierre global de sesiones propias incluida current — 2026-08-18

El registro vigente cubre exactamente `30/30` operaciones HTTP y agrega `identity.sessions.revoke-all` sobre `DELETE /api/identity/sessions`.

- La operación es platform-scoped, exige cookie+CSRF y assurance exacta `manage_sessions`; no recibe body, IDs, tenant, `If-Match`, idempotency key ni count.
- PostgreSQL comparte el advisory lock por actor entre global, bulk-others, individual y logout. Current se revalida `FOR UPDATE` post-lock; todas las sesiones propias activas/no expiradas visibles al statement cambian con timestamp común y `Version` DB-side.
- Un journal `session_revoked` por transición se confirma en la misma transacción. Fallo de journal o cancelación revierte todo; app conserva EXECUTE-only y cero acceso general a sessions/users/`TokenHash`.
- La cookie current se elimina sólo tras commit. Ante respuesta perdida, el cliente no afirma éxito global: 401 sólo prueba que current ya no autentica y exige reingreso+inventario; 200 permite reintentar. Un login confirmado después del corte permanece fuera del comando.
- Gates: raíz `381/381`; Identity `146/146`; DB revoke-all-own `18/18`; API own-session `14/14`; Architecture Fitness `135/135`; Vitest `225/225`; Playwright `10/10`; FND `45/45`; SEC `56/56`; EF `3/3`; build/format/SCA/UTF-8/JSON/secrets/diff PASS.

Resultado vigente: PASS integrado-local. No se aprobaron dispositivos, notificación, familias, delivery/consumer, retención/purge, Auth0/edge/collector ni deploy compartido.
