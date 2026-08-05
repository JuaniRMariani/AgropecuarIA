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
