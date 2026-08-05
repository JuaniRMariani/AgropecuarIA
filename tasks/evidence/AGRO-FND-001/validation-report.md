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
