# Reporte de validación incremental — AGRO-SEC-001

## Baseline histórico R0 — 2026-08-05

Alcance histórico: baseline documental del modelo de amenazas y clasificación por release. Al 2026-08-05 no existía runtime productivo raíz; este resultado no certificaba controles desplegados, Legal, proveedor, región, SLA ni retención.

## Resultado

`PASS` técnico del gate R0, condicionado a repetir threat modeling, controles y abuse tests en cada slice R1–R6. El registro contiene 14 amenazas estables: 7 críticas y 7 altas; ninguna crítica carece de owner, prueba o gate bloqueante. Las 12 superficies de procesamiento permanecen candidatas o futuras hasta sus aprobaciones y pruebas reales.

## Definition of Ready demostrada

- Arquitectura, módulos y fronteras: `docs/05-arquitectura.md`, ADR-009 y evidencia `AGRO-FND-001`.
- Flujos, actores y activos: `docs/02-dominio-actores-y-flujos.md` y `docs/07-seguridad-y-privacidad.md`.
- Proveedores candidatos y comportamiento R0: `AGRO-DIS-003/004/005/007`.
- Q-054/055/058/060 permanecen explícitas. Sus vacíos cambian ranking y producen NO-GO productivo, pero no impiden documentar el baseline.

## Artefactos verificados

- `AgropecuarIA-threat-model.md`: edge/web, identidad/email, API, DB/GIS, grants/storage, tiles/proveedores, jobs, IA, telemetría, restore y supply chain.
- `threat-register.json`: `TM-001`–`TM-014` con `RSK-*`, fronteras, activos, owners, controles, gaps, tests, detección, riesgo residual y gate.
- `data-classification-and-privacy.md`: cinco clases, scope, minimización, UX/consentimiento y NO-GO.
- `provider-processing-inventory.md`: `PI-01`–`PI-12`, incluida infraestructura/edge, observabilidad, CI/artefactos y backup.
- `release-security-gates.md`: criterios R0–R6 y checklists de frontera/proveedor.
- `validate-threat-model.ps1`: estructura, rutas/enlaces, `RSK-*` existentes, preguntas abiertas, sincronización JSON↔tabla y mutation tests.

## Comandos y resultados

```text
powershell -NoProfile -ExecutionPolicy Bypass -File "tasks/evidence/AGRO-SEC-001/validate-threat-model.ps1" -SelfTest
SELFTEST critical-owner: PASS
SELFTEST critical-test: PASS
SELFTEST blank-array-value: PASS
SELFTEST duplicate-id: PASS
SELFTEST risk-link: PASS
SELFTEST open-question: PASS
SELFTEST human-table-drift: PASS
VALIDATION PASS: 14 threats; 7 critical; 7 high; 0 critical threats without owner/test/gate.

ConvertFrom-Json threat-register.json
JSON PASS: threat-register.json

rg dirigido a asignaciones de password/client_secret/private_key/api_key
SECRET SCAN PASS: no credential assignments

git diff --check
DIFF CHECK PASS
```

## Revisión independiente

La primera revisión cruzada detectó edge/CDN y flujos browser→storage/tiles ausentes, email incompleto, inventario sin telemetría/CI/backup, trazabilidad `RSK-*` implícita, un owner de módulo no aprobado y falsos PASS posibles del validador. Se corrigieron en el modelo/diagrama, registro, inventario y mutation tests. La reauditoría final AppSec/Data fue `PASS`: 0 críticos, 0 altos y 0 medios; Architecture y Product/UX/Privacy también aprobaron y sus dos observaciones bajas de índices `PI-09/PI-12` quedaron corregidas antes del gate final.

El principal repitió desde el estado combinado: 7/7 mutation tests, 14 amenazas, 4 preguntas abiertas, 16 enlaces únicos a riesgos, 12 superficies únicas, 0 credenciales detectadas y `git diff --check` sin errores.

## N/A y riesgo residual

- .NET, pnpm/frontend, API, PostgreSQL/PostGIS, Docker/Compose, migraciones, SAST/SCA/DAST, telemetría emitida, CI/CD y deploy: N/A; esta fase no crea runtime ni infraestructura.
- Todas las amenazas críticas/altas siguen abiertas para producción. El owner y gate existen, pero los controles deben demostrarse en el slice real.
- Q-054/055/058/060, `GAP-003`, `GAP-008`, `VAL-LEG`, IdP/proveedores/regiones/DPA/retención, pipeline y restore administrado permanecen pendientes.
- `AGRO-SEC-001` continúa `En curso` por ser una tarea R0–R6; el baseline R0 no completa el padre.

## Incremento local R1 Identity/FND — 2026-08-10

### Resultado y alcance

`PASS` del gate local para la capacidad Identity/FND integrada. El repositorio contiene un bootstrap ejecutable local, todavía no desplegado: API ASP.NET Core, web Next.js, módulo Identity, PostgreSQL efímero, contratos OpenAPI, journal/outbox y telemetría en proceso. El resultado no constituye aprobación de R1 completa ni autoriza un ambiente compartido o Internet.

El registro mantiene los 14 IDs estables (`TM-001`–`TM-014`, 7 críticos y 7 altos) y agrega un inventario comprobable de ocho superficies: tres integradas localmente, una exclusiva de Development/Test y cuatro externas o de cadena de entrega en estado `NO-GO`. Los 14 paths OpenAPI quedan cubiertos exactamente por las superficies API y sintéticas.

### Controles demostrados

- Sesión opaca revocable con hash en PostgreSQL, cookie segura, expiración server-side, CSRF, rate limiting y respuestas no-store.
- OIDC Authorization Code + PKCE con state/nonce del framework, `max_age=0`, `auth_time` firmado y validación `acr`/`amr` para el grant fuerte.
- Step-up ligado a usuario, sesión, identidad y propósito; consumo exact-once, rechazo de replay y rotación sin extender la expiración absoluta.
- Journal local protegido por trigger contra `UPDATE`/`DELETE` y outbox tipado en la misma transacción para linking y step-up exitosos. No se afirma WORM, separación de principal ni auditoría central.
- Contratos de evento cerrados, migraciones N/N-1 y telemetría con tags acotados sin claims, tokens, correo ni UUID de tenant/recurso.
- Dependencias .NET/pnpm fijadas y auditables localmente.

No existe todavía una acción real de administración tenant o factores que exija el grant fuerte; link/unlink conserva la reautenticación reciente de ID-001. Esa distinción queda alineada en Markdown y JSON.

### Gate ejecutable de drift

`runtime-surface-register.json` enlaza cada superficie con amenaza, owner, control, prueba y gate. `validate-threat-model.ps1` valida el registro narrativo/JSON, existencia real de paths y símbolos de pruebas, proyectos/lockfiles PostgreSQL/API/web/Identity, cobertura exacta de paths OpenAPI y fronteras `development-test-only`/`external-no-go`.

Los 15 mutation tests pasan y prueban: owner/test crítico, arrays vacíos, ID duplicado, riesgo/pregunta, tabla humana, superficie/path/test faltante, drift OpenAPI, gate R1 sin test, frontera sintética degradada, gate externo eliminado y declaración R0 obsoleta.

### Comandos y resultados

```text
dotnet tool restore
PASS — dotnet-ef 10.0.4.

dotnet restore AgropecuarIA.slnx --locked-mode
PASS.

dotnet build AgropecuarIA.slnx --configuration Release --no-restore
PASS — 0 warnings, 0 errors.

dotnet test --solution AgropecuarIA.slnx --configuration Release --no-build --minimum-expected-tests 114
PASS — 114/114, 0 failed, 0 skipped.

dotnet format AgropecuarIA.slnx --no-restore --verify-no-changes
PASS.

dotnet ef migrations has-pending-model-changes ... --configuration Release --no-build
PASS — No changes have been made to the model since the last migration.

pnpm install --frozen-lockfile; pnpm format; pnpm lint; pnpm typecheck; pnpm test; pnpm build
PASS — Vitest 23/23 y Next.js 3/3 rutas.

scripts/identity/run-e2e.ps1
PASS — Playwright 4/4 desktop/mobile con PostgreSQL 17 efímero.

dotnet list AgropecuarIA.slnx package --vulnerable --include-transitive --no-restore
PASS — 0 vulnerabilidades conocidas en 5 proyectos.

pnpm audit --prod --audit-level high
PASS — 0 vulnerabilidades conocidas.

validate-threat-model.ps1 -SelfTest
PASS — 15/15 mutaciones; 14 amenazas; 7 críticas; 7 altas.

JSON parse; secret scan dirigido; git diff --check
PASS.
```

### Revisión y riesgos residuales

La revisión independiente encontró tres fallos de evidencia: símbolos inexistentes no rompían el validador, no se comparaban los paths OpenAPI y `TM-009` sobreafirmaba enforcement fuerte. También reprodujo una carrera válida donde el callback perdedor puede recibir `401` tras la rotación o `409` si ya se autenticó. El validador ahora comprueba fuentes/contrato reales, la afirmación de `TM-009` quedó acotada y la prueba exige exactamente un éxito más un rechazo fail-closed (`401` o `409`).

La reauditoría final QA y AppSec/Arquitectura fue `PASS`: 0 hallazgos críticos, altos o medios. La prueba concurrente pasó 5/5 después de reconstruir el binario y la suite combinada pasó 114/114.

Permanecen `NO-GO` para ambiente compartido/Internet: Auth0 real y lifecycle de factores; tenant/RLS/roles/grants DB; edge, HSTS, allow-list de hosts/proxy, key ring compartido y limiter distribuido; collector/retención OTLP; CI/SBOM/provenance/firma; auditoría central, backup/restore administrado, región/DPA/retención y canales de notificación. Los endpoints sintéticos deben estar físicamente ausentes fuera de Development/Test.

`AGRO-SEC-001` permanece `En curso`: este incremento demuestra el gate local Identity/FND, pero la tarea multirelease exige reevaluación por slice y conserva gates externos R1–R6.

## Registro histórico: refresh R0/R1 de tenancy y discovery — 2026-08-10

### Resultado y alcance en ese checkpoint

El registro se reconcilió después del incremento técnico de `AGRO-DIS-003`. `ADR-PEND-007` está aceptada para desarrollo R1 y el spike descartable demuestra discovery actor-scoped con PostgreSQL real, `FORCE RLS`, principal read-only/no privilegiado, contexto transaccional, SCRAM-SHA-256, secretos efímeros separados y ACL owner-only. Esa evidencia reduce incertidumbre de diseño, pero no forma parte de `AgropecuarIA.slnx`, `src/` o `apps/` y no se atribuye al runtime productivo.

En ese checkpoint, `TM-001` conservaba prioridad `critical`: Identity integrado seguía platform-scoped y todavía no existían recurso tenant productivo, migraciones RLS, principals app/job/migrator separados, `SET LOCAL`, suite A/B/sin contexto/pool/jobs/grants, cache, archivos, exporte o retrieval tenant-safe. La sección siguiente conserva los comandos y resultados históricos; no representa el gate actual.

### Gate de drift histórico

El validador ahora exige que `TM-001` conserve explícitos los seis controles R0 aceptados y rechaza tres declaraciones obsoletas: ADR/discovery todavía abiertos, tenant/RLS esperando ADR y autenticación `trust`. Nueve mutation self-tests nuevos demuestran que la pérdida de cada evidencia positiva o la reintroducción de cada declaración obsoleta rompe el gate. Se preservaron los 14 IDs, 7 prioridades críticas, 7 altas y Q-054/055/058/060.

```text
validate-threat-model.ps1 -SelfTest
PASS — 24/24 mutations; 14 threats; 7 critical; 7 high; 0 critical without owner/test/gate.

Threat/runtime JSON parse
PASS — 2/2.

UTF-8 strict
PASS — 7 changed SEC artifacts.

PowerShell parser
PASS — validate-threat-model.ps1.

git diff --check
PASS.
```

Build, tests .NET/frontend, EF y SCA quedaron `N/A` para ese refresh: no cambiaba código productivo, proyectos, contratos runtime, migraciones, manifiestos ni lockfiles. Los gates funcionales 114/114, Vitest 23/23 y E2E 4/4 pertenecen a ese checkpoint histórico y no describen el estado integrado actual.

### Estado y riesgos residuales en ese checkpoint

En ese checkpoint, `AGRO-SEC-001` continuaba `En curso` por ser un gate R0–R6 y el tenant runtime todavía era `NO-GO`. Esa declaración quedó superada para la frontera acotada `CreateOrganization` por el refresh actual siguiente; Auth0/hosting compartido, CI/provenance, auditoría central, backup administrado y las decisiones Legal/retención continúan pendientes.

## Refresh actual: CreateOrganization — 2026-08-10

El registro y el modelo humano incorporan la primera frontera tenant integrada sin promover el spike R0: `CreateOrganization` deriva actor/scope, aplica grants mínimos y `FORCE RLS`, y prueba A/B/sin contexto/pool/job. `TM-001` continúa `critical` para discovery y cualquier nueva superficie que no repita esos controles.

### Gate actual

```text
validate-threat-model.ps1 -SelfTest
PASS — 25/25 mutations; 14 threats; 7 critical; 7 high; 0 critical without owner/test/gate.

Suite raíz MTP
PASS — 142/142; 0 failed; 0 skipped.

Vitest
PASS — 50/50.

Playwright desktop + Pixel 7
PASS — 4/4.
```

El validador exige conservar tanto la evidencia descartable previa como la migración runtime de organización. Hosting, credenciales administradas, migrator compartido, Auth0, collector, CI y módulos tenant futuros siguen `NO-GO`.

### Estado actual

`AGRO-SEC-001` continúa `En curso` por ser un gate R0–R6. El GO local se limita al bootstrap `CreateOrganization`; discovery general, retry/poison/delivery, Auth0/hosting compartido, CI/provenance, auditoría central, backup administrado y las decisiones Legal/retención permanecen pendientes.

## Refresh actual: invitaciones one-shot de co-owner — 2026-08-11

El modelo y registro incorporan la primera administración tenant protegida con strong assurance: un owner activo crea o revoca invitaciones con purpose `manage_organization_owners`; el invitado acepta con identidad verificada y autenticación reciente. El bearer de 256 bits viaja solo en fragmento, se persiste como HMAC versionado y nunca se emite en logs, métricas, journal o eventos.

```text
validate-threat-model.ps1 -SelfTest
PASS — 26/26 mutations; 14 threats; 7 critical; 7 high; 0 critical without owner/test/gate.

Suite raíz MTP
PASS — 170/170; 0 failed; 0 skipped.

Vitest
PASS — 67/67.

Playwright desktop + móvil
PASS — 4/4; inviter, invitee distinto, attacker, revoke, Axe, teclado y 390 px.
```

PostgreSQL real demuestra `FORCE RLS`, grants mínimos, A/B/sin contexto/pool/job, token lookup y coverage gates estrechos, concurrencia y rollback atómico. Los eventos `OrganizationOwnerInvited`, `OrganizationOwnerInvitationAccepted` y `OrganizationOwnerInvitationRevoked` omiten bearer, digest, nombre y actor.

`AGRO-SEC-001` permanece `En curso`. Email/delivery, roles no-owner, Auth0/hosting/secret manager, limiter distribuido, CI/provenance, Audit central, backup y retención legal siguen NO-GO de ambiente compartido.

## Refresh actual: referencia territorial local y Georef externo — 2026-08-11

El registro pasa de ocho a diez superficies y agrega `RS-009` como `integrated-local`: GET autenticados search/resolve, snapshot oficial local hash/completo/inmutable de 23 provincias más CABA, roles mínimos con `FORCE RLS`, rate limit/no-store, contrato estricto y estados `fresh|stale|unavailable` con búsqueda manual. La coordenada explícita no se persiste en PostgreSQL/journal/browser storage ni logs de aplicación; existe únicamente en tránsito y en un caché volátil acotado. Esto no equivale a ausencia total de tratamiento: el resolve la incluye en la query de salida.

`RS-010` registra Georef como `external-no-go`. El adapter local queda fijado a `https://apis.datos.gob.ar/georef/api/v2.0/`, sin redirects/cookies/descompresión automática, con timeout de 5 s, máximo 256 KiB y parser estricto. No se habilita tráfico real hasta cerrar licencia/atribución, Q-058/Q-060, región, DPA, retención, subencargados, SLA/cuota/capacidad, egress/DNS y redacción end-to-end de query strings. Un test local exitoso no aprueba al proveedor.

```text
validate-threat-model.ps1 -SelfTest
PASS — 41/41 mutations; 14 threats; 7 critical; 7 high; 0 critical without owner/test/gate.

## Refresh: remoción segura de co-owner — 2026-08-18

TM-001 y TM-009 incorporan la remoción de otro co-owner como tercera frontera tenant integrada junto con bootstrap e invitaciones. El control exige actor/tenant server-derived, step-up purpose-bound, `FORCE RLS`, último-owner serializado, journal/outbox atómicos y negativos A/B/sin contexto/pool/job. Self-remove, transferencia, democión, roles no-owner y ambiente compartido continúan NO-GO.

Validator PASS `42/42` mutations; 14 threats, 7 critical y 7 high; cero críticas sin owner/test/gate. El nuevo mutation test elimina la evidencia de remoción y demuestra que el gate falla ante drift.

Threat/runtime JSON parse
PASS — threat-register.json and runtime-surface-register.json.
```

Las mutations nuevas prueban drift de paths Territory y pérdida de: snapshot 23+CABA, `FORCE RLS`, adapter Georef v2.0 fijo, no persistencia durable, caché volátil, redacción compartida, endpoint exacto, redirect policy, timeout, body budget, minimización tenant, `NO-GO` externo y la regla de que tests locales no constituyen aprobación.

`AGRO-SEC-001` permanece `En curso`. El GO se limita al snapshot/search/UI y adapter verificable localmente; Georef externo, hosting/proxy/collector, base administrada, CI/provenance, backup/restore representativo y decisiones Privacy/Legal continúan NO-GO.

## Refresh actual: Productive Core field draft no espacial — 2026-08-18

`RS-011` registra el primer recurso tenant de Productive Core como `integrated-local`: create/list/detail autenticados, contrato OpenAPI exacto, actor/tenant derivados por servidor y autorización owner revalidada dentro de la transacción antes de lookup o replay. El puerto `identity.authorize_productive_owner()` es `SECURITY DEFINER`, usa search path fijo, no entrega PII y solo expone la versión vigente de sesión al rol Productive app. Membership removida, sesión vencida/revocada, tenant ajeno, contexto ausente, job y conexión reutilizada niegan cerrado.

PostgreSQL local separa owner/migrator/app/job sin login, inheritance, ownership ni `BYPASSRLS`; las cinco tablas aplican `FORCE RLS`. Crear un campo persiste unidad draft, ledger y aliases HMAC versionados, journal append-only y outbox tipado `1.0.0` de forma atómica. Los tests cubren orden Identity→Productive, ausencia N-1 fail-closed, rollback/roll-forward, A/B/sin contexto/pool/job, nombres duplicados permitidos, replay/races, key coverage y rollback. Fault injection real instala triggers que fallan por separado el INSERT de ledger, journal y outbox, invoca `CreateField` y verifica cero unidades, ledgers, aliases, journals y outbox después de cada fallo. El modelo EF de ambos contextos quedó sin cambios pendientes.

El registro de amenazas incorpora este control en TM-001/008/011/012/014. La clasificación trata `DisplayName` como `Confidential`; no hay coordenadas, área, geometría, tiles ni historial espacial. Telemetría conserva solo operación, outcome y conteo acotado, sin nombre de campo, UUID de tenant/recurso, sesión ni material idempotente. `PI-13` confirma que no se agregó proveedor ni egress: el outbox todavía no tiene worker externo.

### Gate reproducido

```text
validate-threat-model.ps1 -SelfTest
PASS — 53/53 mutations; 14 threats; 7 critical; 7 high; 0 critical without owner/test/gate.

dotnet build AgropecuarIA.slnx --no-restore
PASS — 9/9 projects; 0 warnings; 0 errors.

dotnet test --solution AgropecuarIA.slnx --no-build
PASS — 308/308; 0 failed; 0 skipped.

dotnet test --project tests/AgropecuarIA.ProductiveCore.Tests/AgropecuarIA.ProductiveCore.Tests.csproj --no-build
PASS — 30/30, incluidos los tres casos de fault injection ledger/journal/outbox, la capacidad concurrente y la cancelación con limpieza de pool.

dotnet ef migrations has-pending-model-changes (ProductiveCoreDbContext + IdentityDbContext)
PASS — 2/2; no pending model changes.

pnpm test
PASS — 130/130 across 8 files.

pnpm typecheck; pnpm lint; pnpm format
PASS — TypeScript, ESLint with zero warnings and Prettier check.

pnpm build
PASS — Next.js optimized production build and 3/3 static pages.

Playwright desktop + móvil (gate coordinado por el runner raíz)
PASS — 6/6.
```

`AGRO-SEC-001` permanece `En curso`. El `GO` es solo integrado-local para campos draft no espaciales. Geometría/área/tiles/history, roles no-owner, dispatcher/inbox/worker y delivery externo, DB/backup/secretos administrados, hosting/edge/collector compartidos, CI/provenance, Privacy/Legal y producción permanecen `NO-GO`; ninguna suite local aprueba esas capacidades.

## Refresh actual: RenameFieldDraft con conflicto explícito — 2026-08-18

`RS-011` incorpora el PATCH tenant-scoped de nombre sobre `ManagementUnit field/draft/not_configured`. La operación revalida owner/sesión dentro de PostgreSQL antes de alias, ledger o recurso; exige CSRF, key idempotente e `If-Match` UUID fuerte. Replay autorizado precede el chequeo de versión actual, mientras un intento nuevo stale recibe 412 neutral y no revela el valor vigente.

La migración aplica `FORCE RLS`, grants UPDATE sólo para `DisplayName`, `Revision` y `Version`, y un trigger que impide modificar tipo, estado, tenant o atributos espaciales. Field, ledger/aliases, journal y outbox confirman atómicamente. Fault injection por ledger, journal y outbox demuestra rollback total; el evento `ManagementUnitDisplayNameChanged` omite nombres, actor, sesión y material idempotente. La rotación HMAC overlap/lazy alias/retiro temprano y alias split permanecen fail-closed.

### Gate reproducido

```text
validate-threat-model.ps1 -SelfTest
PASS — 56/56 mutations; 14 threats; 7 critical; 7 high; 0 critical without owner/test/gate.

dotnet test --solution AgropecuarIA.slnx --configuration Release --no-build
PASS — 348/348; 0 failed; 0 skipped.

Productive Core / Architecture Fitness / Vitest / Playwright
PASS — 56/56 · 135/135 · 153/153 · 6/6.

FND protocol / SEC-002 register / EF model drift
PASS — 45/45 mutations · 26/26 operations · 3/3 contexts without pending changes.
```

`AGRO-SEC-001` permanece `En curso`. El GO sigue limitado a runtime local integrado; deploy compartido, secretos administrados, delivery/consumers, restore representativo, edge/collector, Privacy/Legal y producción permanecen `NO-GO`.

La revisión independiente detectó y cerró antes de publicación dos defectos medios: un alias split durante recovery que podía escapar como 500 y un grant UPDATE excesivo sobre rename ledgers. El runtime vigente responde 503 de reconciliación sin elegir ledger, y el rol app sólo posee SELECT/INSERT con UPDATE real denegado `42501`.
