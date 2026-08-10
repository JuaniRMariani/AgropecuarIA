# Validación AGRO-DIS-003

Fecha: 2026-08-05  
Resultado interno: `PASS`  
Decisión: `GO CONDICIONAL` para un sandbox IdP; tarea `En revisión` por gates externos.

## Backend y PostgreSQL

Ejecutado con .NET SDK 10.0.201, ASP.NET Core 10.0.5, MTP/MSTest 4.0.2, Npgsql 10.0.3 y PostgreSQL 17.9 efímero en `127.0.0.1`.

```text
dotnet restore .\AgropecuarIA.IdentitySpike.slnx                 PASS
dotnet build .\AgropecuarIA.IdentitySpike.slnx --no-restore      PASS · 0 warnings · 0 errors
dotnet test --solution .\AgropecuarIA.IdentitySpike.slnx --no-build
                                                                  PASS · 15/15 · 0 skipped
dotnet format ... --verify-no-changes --no-restore                PASS
dotnet build .\api\AgropecuarIA.IdentitySpike.Api.csproj -c Release --no-restore
                                                                  PASS · 0 warnings · 0 errors
```

Los probes SQL devolvieron `catalog-security-pass`, `rls-isolation-pass` e `identity-spike-database-pass`. Cubren A/B/sin tenant, `USING`/`WITH CHECK`, SELECT/INSERT/UPDATE/DELETE ajenos, rollback/commit, job y reutilización de una conexión. Las pruebas Npgsql agregan pool máximo 1 después de rollback y excepción.

El DLL Release no contiene `FixtureEndpoints`, `SpikeFixtureSafety`, `CreateFixtureSession` ni `/__fixtures`; la API Release respondió sesión `200` y fixture `404`. Debug+Spike exige URL explícita loopback y rechaza `urls` o `Kestrel:Endpoints` externos.

Escenarios MSTest: 0/1/N organizaciones; selección y rotación de sesión; CSRF; permiso default-deny; BOLA neutral; RLS/pool/job; linking con step-up, doble proof, replay y conflicto; null input; recovery anti-enumeración, rate limit, challenge TTL/one-shot/replay/expiración y revocación.

## Frontend y navegador

```text
npm ci --ignore-scripts --no-audit --no-fund    PASS · 385 paquetes
npm run lint                                    PASS · 0 warnings
npm run typecheck                               PASS
npm test                                        PASS · 8/8
npm run build                                   PASS · Next 16.3.0 · ruta / estática
npm run test:e2e                                PASS · 1/1 · Chromium real
npm audit --audit-level=high                    PASS · 0 vulnerabilidades
```

El E2E recorre `signed-out`, `loading`, `recovery-accepted`, `rate-limited`, 0/1/N organizaciones, `provider-down`, `active`, `linking`, conflicto y revocación. Axe se ejecutó en cada estado con WCAG 2A/AA, 2.1AA y 2.2AA: 0 violaciones. Consola warning/error y `pageerror`: 0. Se verificaron foco programático, Tab al CTA y viewport 390 px sin overflow; quedó screenshot 390×1001 en el output de Playwright.

## Seguridad, compatibilidad y operación

- `dotnet list ... package --vulnerable --include-transitive`: 0 paquetes vulnerables conocidos.
- Scan de credenciales sobre fuentes/contratos: sin secretos, claves ni passwords persistidos.
- JSON contracts: tres archivos parseados correctamente.
- Fixtures `.invalid`/`.test`; no datos personales ni proveedores reales.
- Stores efímeros de attempts/proofs/challenges y auditoría global están acotados; datos tenant usan RLS real.
- Migraciones productivas, upgrade y rollback: N/A. Son scripts de clúster descartable; rollback es detener/eliminar `.runtime`.
- Docker/Compose/CI/deploy/telemetría productiva: N/A por alcance R0; Docker no estaba disponible.
- Git no existe en la raíz y no se inicializó. Se revisó inventario de rutas pre/post; el `.git` anidado generado por `create-next-app` fue eliminado y la verificación final encontró cero directorios `.git`.

Incidencias encontradas y corregidas durante verificación: Vitest 4.0.18 tenía un advisory crítico y se actualizó a 4.1.10; el directorio fixture requería factory DI explícita; se agregaron autorización por permiso, step-up, recovery one-shot, límites de memoria, exclusión física de fixtures Release y E2E durable.

## Revisiones independientes y gates restantes

Principal QA: `PASS` tras revalidación (backend 15/15; E2E 1/1).  
AppSec/Architecture: sin hallazgos críticos, altos ni medios internos tras revalidación.

Para completar la tarea faltan evidencias externas que este spike no puede fabricar: sandbox Auth0 con OIDC Authorization Code + PKCE y validación de claims/callback; linking/recovery/factor perdido/revocación/failover reales; región/DPA/retención/subprocesadores/plan/SLA/exportabilidad; identidad externa one-to-many persistida y unicidad concurrente; discovery productivo seguro de membresías. Por eso el estado correcto es `En revisión`, no `Completada`.

---

## Extensión R0 — discovery seguro de membresías (2026-08-10)

Resultado técnico: `PASS` para la viabilidad R1 del patrón; revisiones independientes QA y AppSec/Arquitectura sin hallazgos críticos, altos o medios residuales. `AGRO-DIS-003` permanece `En revisión`: este incremento no prueba Auth0 ni autoriza copiar el spike a producción.

### Gap cerrado

El journey 0/1/N anterior consultaba `FixtureIdentityDirectory`; las policies PostgreSQL solo funcionaban después de conocer `app.current_organization_id`. La extensión separa el descubrimiento previo al tenant mediante `agro_membership_discovery`, un principal read-only, `NOINHERIT`, `NOBYPASSRLS`, sin ownership ni acceso a identidades globales o datos tenant.

El actor proviene de la sesión server-side y se fija con `set_config('app.current_actor_id', ..., true)` dentro de una transacción. Las policies actor-scoped muestran solo memberships y organizaciones activas propias. Al cambiar organización, el servidor vuelve a consultar la membership vigente antes de rotar la sesión; un locator ajeno, inactivo o revocado falla de manera neutral.

### Baseline e incidencias

La baseline integrada previa al cambio pasó en la raíz: restore locked, build Release sin warnings y 114/114 tests. En el spike aparecieron dos defectos de harness preexistentes al integrarlo al repositorio actual: heredaba Central Package Management desde la raíz y `MSTestSettings.cs` no respetaba charset/fin de línea. Se aislaron mediante `spike/Directory.Packages.props` y normalización UTF-8/LF; luego la baseline del spike pasó 15/15. No se debilitó ningún gate.

### PostgreSQL y backend

PostgreSQL 17 efímero ejecutó migraciones, fixtures y probes desde cero: `catalog-security-pass`, `rls-isolation-pass`, `membership-discovery-pass` e `identity-spike-database-pass`.

La migración/probe `003` verificó atributos y memberships de roles, ownership, grants por columna, `ENABLE/FORCE RLS`, coexistencia de policies tenant/discovery, ausencia de actor, 0/1/N, membership u organización inactiva, permiso vacío, prohibición de `platform_user`/`tenant_record`/auditoría y escritura. Una segunda ejecución de `003` sobre el mismo clúster pasó. `run-all.psql` completo no es idempotente porque la migración R0 preexistente `002` crea tablas sin guardas; esa limitación no se atribuye a `003` ni se oculta.

El adapter Npgsql usa una conexión dedicada, transacción explícita, consulta parametrizada, orden determinista y límite fail-closed de 100. Valida el principal exacto antes de servir y rechaza una connection string owner. La lectura tenant vuelve a comprobar actor, membership activa y permiso en el mismo statement/snapshot del recurso. Las pruebas reales cubren limpieza del actor tras commit, rollback, excepción y cancelación; actor ajeno; 0/1/N; permiso desconocido; límite 101; revocación antes de selección/lectura; y no rotación ante selección rechazada.

El clúster efímero usa SCRAM-SHA-256, cuatro secretos RNG distintos, `pwfile` transitorio y ACL owner-only. Los gates rechazan conexión sin password, usuario desconocido, reglas `trust` y principal discovery owner/superuser/BYPASS. Un primer juego de secretos efímeros apareció accidentalmente en salida interna de verificación; ese clúster fue eliminado inmediatamente, se regeneraron los cuatro secretos y el estado final no conserva `.runtime` ni credencial alguna.

```text
dotnet restore .\AgropecuarIA.IdentitySpike.slnx
  PASS
dotnet build .\AgropecuarIA.IdentitySpike.slnx --configuration Debug --no-restore
  PASS · 0 warnings · 0 errors
dotnet test --solution .\AgropecuarIA.IdentitySpike.slnx --configuration Debug --no-build --minimum-expected-tests 29
  PASS · 29/29 · 0 failed · 0 skipped
dotnet format .\AgropecuarIA.IdentitySpike.slnx --verify-no-changes --no-restore
  PASS
dotnet list .\AgropecuarIA.IdentitySpike.slnx package --vulnerable --include-transitive --no-restore
  PASS · 0 vulnerabilidades conocidas
```

El clúster se detuvo con el script del spike y `.runtime` quedó ausente. No hubo migración productiva, credenciales reales, frontend, CI ni deploy.

La regresión del repositorio integrado pasó `dotnet restore --locked-mode`, build Release con 0 warnings/errores, 114/114 tests, format y EF sin cambios pendientes. El análisis NuGet reportó cero vulnerabilidades conocidas.

### Decisión y pendientes

`ADR-PEND-007` queda aceptada para desarrollo R1 con runtime pendiente: `FORCE RLS` default-deny; owner `NOLOGIN`; principals app/job/discovery sin `BYPASSRLS` ni ownership; contexto actor/tenant transaccional; y discovery actor-scoped separado. R1 debe crear migraciones forward-safe propias, principal migrator aislado, grants por capacidad y toda la suite A/B/sin contexto/pool/jobs.

Identidad externa one-to-many ya fue implementada por `AGRO-ID-001`; no continúa como gap del spike. Persisten Auth0 real, plan/región/DPA/SLA/exportabilidad, roles/alcances definitivos, jobs, auditoría central, primera mutación tenant exactly-once y revisiones de despliegue. Por ello la tarea sigue `En revisión`.
