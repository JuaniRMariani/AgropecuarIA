# Validación AGRO-FND-001

Fecha: 2026-08-05  
Resultado R0: `PASS`  
Estado de tarea multirelease: `En curso`; la evidencia no sustituye el ensayo R1 de migración/staging.

## Resultado entregado

- ADR-009 acepta 15 bounded contexts dentro del monolito y separa National Catalog de Productive Core.
- Productive Core posee `ManagementUnit`; Territory posee `SpatialRepresentationVersion` opcional/versionada.
- El registro asigna owner, schema, agregados y scope `platform|tenant` explícito; el mapa cubre 69 edges consumidor→proveedor sin ciclos ni acceso de persistencia.
- Contratos producer JSON Schema 2020-12 cerrados para scope, Problem Details, cursor y evento; readers N-1 toleran cambios aditivos mediante política separada.
- La policy impide escalamiento tenant→platform/platform→tenant, converge recursos ausentes/ajenos/no autorizados en 404 y reautoriza antes de ETag/412.
- La secuencia de eventos queda aislada por source, scope, tenant y agregado; duplicate/out-of-order/gap no mutan cursor.

## Gates reproducidos

Desde `tasks/evidence/AGRO-FND-001/fitness`:

```text
dotnet restore .\AgropecuarIA.ArchitectureFitness.slnx --locked-mode
PASS

dotnet build .\AgropecuarIA.ArchitectureFitness.slnx --no-restore
PASS · 0 warnings · 0 errors

dotnet test --solution .\AgropecuarIA.ArchitectureFitness.slnx --no-build --minimum-expected-tests 42
PASS · 42/42 · 0 failed · 0 skipped

dotnet format .\AgropecuarIA.ArchitectureFitness.slnx --verify-no-changes --no-restore
PASS

dotnet list .\AgropecuarIA.ArchitectureFitness.slnx package --vulnerable --include-transitive
PASS · 0 vulnerable packages known by configured sources
```

Los 12 JSON del artefacto/locks parsearon; los cuatro schemas publicados fueron cargados por la suite. `git diff --check` y el scan de secretos dirigido resultaron PASS.

## Cobertura negativa

- ciclo, dependencia desconocida, schema/agregado duplicado, edge faltante/no declarado y acceso `database-schema`;
- aggregate scope ausente/ambiguo, shared kernel con CUIT/DbContext y policies que confían tenant del cliente;
- producer schema abierto, campo sensible, scope ambiguo, cursor sin límite y envelope sin versión/secuencia;
- campo removido, tipo cambiado, required agregado, required→optional, enum cerrado o valor removido;
- duplicado, atraso, gap, tenant/source/scope ajeno y enum desconocido;
- 401 sin sesión, 403 de capacidad no enumerante, 404 neutral, 409 de estado, 412 tras authz y escalamiento entre planos.

## Revisión independiente

Principal QA reprodujo los gates y aprobó con 42/42; AppSec/Arquitectura cerró todos los hallazgos con cero severidades críticas, altas o medias. Se corrigieron durante revisión: schemas no ejecutados, required→optional, policies laxas, shared kernel abierto, scope ambiguo, escalamiento platform/tenant, stream sin tenant/source, documentación divergente y fixtures con vocabulario no aprobado.

## Límites y riesgo residual

- El validador está deliberadamente dirigido a estos cuatro contratos; no pretende ser un motor JSON Schema general.
- `ADR-PEND-011` queda resuelta. `ADR-PEND-010` conserva `política definida; ensayo pendiente` hasta `AGRO-FND-003`/`AGRO-PLT-004` con staging, backup/restore y backfill reanudable.
- API, frontend/pnpm, PostgreSQL/PostGIS, migraciones, Docker/Compose, CI/CD, telemetría productiva y deploy son N/A para este R0. No se creó producto ni se promovieron los spikes `AGRO-DIS-003/004`.
- Estado final legítimo: `En curso` por ser tarea R0/R1; el gate R0 está aprobado, pero la tarea padre no cumple todavía la DoD multirelease completa.

## Enforcement R1 sobre runtime — 2026-08-05

Resultado del sub-slice: `PASS` local. El fitness dejó de ser una solución aislada y ahora forma parte de `AgropecuarIA.slnx`, registra el runtime en `runtime-map.json` e inspecciona referencias, composition root, ownership EF/schema y OpenAPI Identity.

- Identity usa scope discriminado `platform|tenant`, actor y correlación derivados en servidor.
- El journal local conserva el nombre lógico `IdentitySecurityJournalEntry` y la tabla física append-only `identity.audit_events` durante la ventana N/N-1; no suplanta Audit/Compliance.
- Problem Details queda cerrado y alineado con OpenAPI: una sola correlación canónica (`correlationId`), 403 para reautenticación y 429 `application/problem+json`.
- `IdentityLinked` persiste envelope 1.0.0 con source, schema version, scope, actor, tiempos, correlación/causación y versión monotónica del agregado.
- La migración es una expansión compatible: columnas de envelope físicamente nullable para escritores N-1, backfill de filas existentes e índice único parcial para filas canónicas. El modelo y todo escritor N exigen los campos.
- Métricas/trazas agregan `contract.version=1.0.0` y `contract.consumer=identity-api` con cardinalidad fija y sin PII.

Gates integrados finales:

```text
dotnet restore AgropecuarIA.slnx --locked-mode
PASS

dotnet build AgropecuarIA.slnx --configuration Release --no-restore
PASS · 0 warnings · 0 errors

dotnet test --solution AgropecuarIA.slnx --configuration Release --no-build --minimum-expected-tests 77
PASS · 77/77 · 0 failed · 0 skipped

dotnet format AgropecuarIA.slnx --verify-no-changes --no-restore
PASS

dotnet ef migrations has-pending-model-changes --project src/AgropecuarIA.Identity/AgropecuarIA.Identity.csproj --startup-project apps/AgropecuarIA.Api/AgropecuarIA.Api.csproj --configuration Release --no-build
PASS · no model drift
```

La integración PostgreSQL efímera demuestra initial→expand, preservación/backfill, escritura N-1 posterior al upgrade y rollback/roll-forward. El rollback destructivo solo se valida en base efímera; en entornos compartidos la recuperación es roll-forward.

La revisión independiente final de Principal QA y AppSec/Arquitectura reprodujo restore, build, 77/77 tests, format, modelo EF, SCA, JSON, secrets scan y diff-check. Resultado: `PASS`, cero hallazgos críticos, altos o medios. Los bloqueos iniciales —`traceId` fuera de contrato, falta de `ActorId` y contract prematuro— fueron corregidos y revalidados.

Riesgo residual: el `contract` posterior, backfill reanudable/batcheado con volumen, staging, backup/restore y ETag pertenecen a `AGRO-FND-003`/`AGRO-PLT-004`. Delivery/retry/idempotencia del outbox pertenece a `AGRO-FND-002`. Por ello la tarea multirelease permanece `En curso`; este sub-slice no declara esos gates aprobados.
