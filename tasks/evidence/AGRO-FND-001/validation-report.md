# Validación AGRO-FND-001

Fecha: 2026-08-05  
Resultado R0: `PASS`  
Estado después del gate R0: `En curso`; esta sección histórica no sustituía el enforcement R1 posterior.

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
- Estado después de este gate R0: `En curso`; el enforcement R1 todavía no existía al registrar esta evidencia.

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

El `contract` posterior, backfill reanudable/batcheado con volumen, staging, backup/restore y ETag pertenecen a `AGRO-FND-003`/`AGRO-PLT-004`. Delivery/retry/idempotencia del outbox pertenece a `AGRO-FND-002`. FND-001 permaneció `En curso` hasta cerrar el drift de eventos públicos detectado después; esos gates downstream no se reinterpretan como alcance propio.

## Cierre contractual R1 — 2026-08-10

Estado final: `Completada`.

La integración de `AGRO-ID-002` reveló que `IdentityStepUpCompleted` se emitía sin registro en el mapa contractual. El cierre elimina ese drift y también hace verificable el contrato existente `IdentityLinked`:

- `IdentityIntegrationEvents` es el catálogo inmutable único de type, major/schema version, source, scope y payload schema. Un identificador cerrado se resuelve dentro de `IdentityOutboxMessage`; una definición o valor fuera del catálogo falla antes de persistir.
- `IdentityOutboxMessage` ya no recibe strings contractuales libres y rechaza scope divergente.
- Los payloads v1 preservan exactamente su forma histórica para sostener N/N-1: `IdentityLinked` mantiene user/identity IDs, conexión y fecha; `IdentityStepUpCompleted` mantiene user/session IDs, propósito y fecha. Reducir esos IDs exige un contrato v2 explícito; no se oculta como cambio compatible.
- Ambos eventos tienen JSON Schema 2020-12 cerrado y están registrados en `consumer-map.json` y `runtime-map.json`, honestamente sin consumidores hasta que FND-002 implemente delivery.
- El fitness compara exactamente runtime↔mapa y bloquea evento desconocido, contrato huérfano, source/scope/version divergentes y schema faltante o incompatible.
- PostgreSQL real verifica envelope/payload, scope platform, correlación/causación, versión agregada y replay con exactamente una fila.

Gates finales: restore locked PASS; build Release 0 warnings/0 errors; fitness completo 60/60; integración Identity/PostgreSQL dirigida 13/13; suite raíz 114/114, 0 failed/skipped; format PASS; EF sin model drift; 15 JSON parseados; NuGet transitivo sin vulnerabilidades conocidas; secrets scan 0 y diff-check PASS.

Principal QA y AppSec/Arquitectura reprodujeron el estado combinado y aprobaron con cero hallazgos críticos, altos o medios. Durante revisión se corrigieron tres bloqueos: payload v1 breaking, ruta de creación con JSON contractual libre y fixture N/N-1 con payload vacío. El gate final preserva la forma v1 histórica, restringe la creación a factories tipadas y demuestra igualdad jsonb antes/después del upgrade y con writer N-1 coexistente.

Los riesgos de backfill/ETag, staging/restore y outbox delivery/idempotencia continúan asignados respectivamente a `AGRO-FND-003`, `AGRO-PLT-004` y `AGRO-FND-002`; no son trabajo residual de FND-001.
