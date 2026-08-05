# AGRO-DIS-005 — reporte de validación

- **Fecha:** 2026-08-05.
- **Clasificación:** spike R0 aislado y descartable.
- **Decisión:** `GO técnico condicionado` para un sandbox AWS S3 + GuardDuty detrás de ports; Azure Blob + Defender es alternativa. `NO-GO productivo` hasta cerrar los gates externos.
- **Estado recomendado:** `En revisión`.

## Resultado demostrable

El spike define contratos versionados y demuestra con datos exclusivamente sintéticos:

- carga privada de una versión inmutable, validación de tamaño/hash/magic bytes y cuarentena fail-closed;
- autorización tenant/recurso/acción, grants breves y one-shot, tokens canónicos y errores sin paths ni existencia ajena;
- estados AV idempotentes, incluido scanner caído o verdict inválido, sin publicar salvo `clean`;
- legal hold y purga serializados con descargas; un delete ambiguo queda `PurgeUncertain` y solo un operador `reconcile` puede resolverlo comprobando el objeto;
- auditoría y reconciliación con scopes operativos, telemetría allow-list y sin PII/secretos;
- manifest que conserva `tenant_id` + referencia opaca, tipo+ID de recurso, geometría, objetos y terminales de auditoría;
- restore conjunto PostgreSQL 17/PostGIS 3.6.2 + objetos, con snapshots, hashes, cadena audit, hold, corrupción, huérfano y binding tenant verificados;
- prototipo Next.js/React accesible para carga, scanning, disponible, cuarentena, AV/proveedor caído, expiración, conflicto, error y retry.

No se provisionó cloud, no se usaron credenciales ni datos reales y no se implementó un pipeline productivo.

## Gates ejecutados

### .NET 10

```powershell
dotnet restore AgropecuarIA.StorageRecoverySpike.slnx --locked-mode
dotnet build AgropecuarIA.StorageRecoverySpike.slnx --no-restore
dotnet format AgropecuarIA.StorageRecoverySpike.slnx --no-restore --verify-no-changes
dotnet test --solution AgropecuarIA.StorageRecoverySpike.slnx --no-build --minimum-expected-tests 32
dotnet list AgropecuarIA.StorageRecoverySpike.slnx package --vulnerable --include-transitive
```

Resultado: `PASS`; build con 0 warnings/errores, 32/32 tests MTP/MSTest y 0 paquetes NuGet vulnerables conocidos. Los tests cubren BOLA/tenant, tokens alterados/no canónicos/vencidos, MIME/hash/tamaño, duplicados y orden AV, caída/cancelación/verdict inválido, deduplicación cross-tenant, scopes operativos, legal hold, purga/descarga concurrentes, delete ambiguo y redacción de telemetría.

### PostgreSQL/PostGIS y objetos

```powershell
& '.\postgres\run-restore-drill.ps1' -Port 55441
```

Resultado principal final: `PASS`; 2 filas, 2 objetos, 4 eventos audit, SRID 4326, snapshots de metadata/auditoría, cadena criptográfica, append-only, binding `tenant_id ↔ tenant_ref`, legal hold, huérfano, corrupción de objeto y corrupción de dump verificados. RTO local observado: `0,0217 min`; ventana de captura: `0,1196 min`. El RPO de 15 minutos permanece `UNPROVEN_WITHOUT_MANAGED_PITR`, como exige la evidencia honesta. QA reprodujo RTO `0,0224 min` y AppSec `0,0258 min`.

### Next.js/React con pnpm

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

Resultado exclusivo final: `PASS`; 5 fixtures contractuales, Prettier/ESLint/TypeScript, build Next.js 16.3, 8/8 Vitest, 5/5 Playwright Chromium y 0 vulnerabilidades conocidas. E2E cubre camino carga→scan→descarga→expiración, AV fail-closed, amenaza sintética, proveedor/error/conflicto, teclado, axe sin hallazgos serios/críticos y viewport de 390 px sin overflow. Una corrida QA afectada por dos builds concurrentes sobre `.next` fue descartada; no se usa como evidencia.

### Revisión y seguridad

- Principal QA: aprobación técnica R0; 32/32 backend, 5 contratos, 8/8 frontend unitarios y 5/5 E2E reproducidos. El drill independiente en 55442 observó RTO `0,0224 min`.
- AppSec/Arquitectura: aprobación R0 final, cero hallazgos altos/medios internos.
- `git diff --check`: `PASS` durante revisión.
- scan heurístico de secretos: sin coincidencias; no reemplaza un scanner de supply chain productivo.

## Compatibilidad, recuperación y reemplazo

No hay migración productiva ni rollback cloud: ambos son N/A para un R0 que no aprovisiona infraestructura. Los contratos son JSON Schema `1.0`, el backend depende de ports y el prototipo local se elimina completo borrando `tasks/evidence/AGRO-DIS-005/spike` cuando sea reemplazado por la tarea R1 autorizada. El runbook exige restaurar a una instancia nueva aislada y promover solo con cero divergencias.

## Riesgos y gates externos

El resultado no autoriza producción. Permanecen abiertos:

- sandbox real de storage + AV, IAM/KMS/WORM, eventos al menos una vez y restore/PITR administrado;
- región, DPA, subencargados, residencia, retención/legal hold y aprobación `VAL-LEG`;
- volumen representativo, costo total, egress, cuotas, SLA/soporte y RPO real;
- IdP, autorización por recurso, RLS defensiva, outbox/worker y OpenTelemetry productivos.

Docker/Compose, CI/CD, deploy, migración productiva y alertas productivas son N/A por alcance. Docker no estaba disponible. Ningún gate omitido se presenta como aprobado.

## Autoevaluación

`97/100`: contexto/selección 15, arquitectura/código 19, multiagente 10, full-stack/datos/observabilidad 14, tests/seguridad 20, preservación/cierre 19. La puntuación no compensa los gates externos; por eso la tarea permanece `En revisión`.
