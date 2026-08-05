# AGRO-DIS-007 — evidencia de capacidad, SLO, costos y conectividad

Spike R0 aislado y descartable. Entrega un método reproducible para conversar sobre capacidad, confiabilidad, equipo y costo sin convertir supuestos en fechas o compromisos productivos.

## Resultado de decisión

- **GO técnico condicionado** para usar los contratos y el modelo como baseline de medición de futuros slices.
- **NO-GO productivo** para SLA, presupuesto, proveedor, calendario o resiliencia rural hasta reemplazar Q-019/020/060/061 por evidencia real y aceptación nominada.
- Offline, sincronización y mapas descargables siguen fuera del MVP. El laboratorio bloquea una confirmación sin red y no persiste ni encola trabajo local.
- No se creó una API ASP.NET ficticia: no existe un runtime productivo cuyo throughput pueda medirse con honestidad. El motor .NET es lógica pura y el frontend es un laboratorio rotulado.

## Artefactos

- `contracts/` y `fixtures/`: JSON Schema 2020-12, escenarios `pilot/growth-10x/burst-2x`, perfiles de red, costos incompletos y reporte golden.
- `spike/src` + `spike/tests`: modelo .NET 10 para demanda, storage, drain, SLO/error budget, costo y telemetría de baja cardinalidad.
- `spike/web`: laboratorio Next.js 16/React 19 accesible, administrado exclusivamente con pnpm.
- `capacity-plan.md`, `sli-slo-catalog.md`, `cost-model.md` y `connectivity-runbook.md`: envelope, políticas, owners, caducidad y gates.
- `docs/adr/ADR-008-capacidad-slo-finops.md`: decisión arquitectónica canónica.

## Comandos reproducibles

Desde `tasks/evidence/AGRO-DIS-007/spike`:

```powershell
dotnet restore AgropecuarIA.CapacityPlanningSpike.slnx --locked-mode
dotnet build AgropecuarIA.CapacityPlanningSpike.slnx --no-restore
dotnet format AgropecuarIA.CapacityPlanningSpike.slnx --no-restore --verify-no-changes
dotnet test --solution AgropecuarIA.CapacityPlanningSpike.slnx --no-build --minimum-expected-tests 21
dotnet list AgropecuarIA.CapacityPlanningSpike.slnx package --vulnerable --include-transitive
```

Desde `spike/web`:

```powershell
pnpm install --frozen-lockfile --ignore-scripts
pnpm run validate:contracts
pnpm run format:check
pnpm run lint
pnpm run typecheck
pnpm test
pnpm run build
pnpm audit --audit-level high
pnpm run test:e2e
```

## Límites honestos

- Todos los volúmenes y perfiles son `synthetic-estimate`, confianza baja, válidos hasta 2026-09-30.
- El cálculo aritmético no prueba latencia, saturación, concurrencia de DB, throughput de worker ni RPO/RTO administrados.
- El catálogo de precios real está incompleto. Un driver ausente devuelve NO-GO; los tests pueden usar precios sintéticos solo para probar mecánica.
- La simulación frontend de idempotencia no sustituye autorización, persistencia ni deduplicación server-side.
- Docker/Compose, DB/PostGIS, cloud, deploy, migración y rollback productivo son N/A para este spike.
- Los gates y resultados exactos se conservan en `validation-report.md`.
