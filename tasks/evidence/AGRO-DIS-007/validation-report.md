# AGRO-DIS-007 — reporte de validación

- **Fecha:** 2026-08-05.
- **Clasificación:** spike R0 aislado y descartable.
- **Decisión:** `GO técnico condicionado` para usar contratos/modelo como baseline de medición; `NO-GO productivo` para fechas, SLA, costo, proveedor o resiliencia rural.
- **Estado recomendado y aplicado:** `En revisión`.

## Resultado demostrable

El spike define una única fuente versionada de escenarios sintéticos y demuestra:

- proyección determinística de requests, storage y drain para `pilot`, `growth-10x` y `burst-2x`;
- error budget exacto para 99,9 %/30 días: 2.592 s y 1.000 eventos malos por millón elegible;
- modelo FinOps fail-closed: precios o metadatos faltantes producen `incomplete`; solo catálogos `synthetic-test-only` o `approved` completos y ordenados producen un rango;
- fixture compartido → motor .NET → golden exacto, sin duplicar datos en frontend;
- allow-list cerrada de telemetría, clases de status y route templates sin PII/IDs ni explosión ante 10.000 inputs adversariales;
- laboratorio Next.js/React con estimación/confianza/vigencia visibles, costo NO-GO, estados completos, conexión default-deny y operación sin persistencia offline;
- reintento con la misma clave in-memory y una sola aceptación simulada. Esto valida UX/contrato local, no idempotencia server-side.

No existe API, DB, worker, proveedor o entorno productivo que permita medir latencia/saturación real. No se creó uno ficticio para obtener números favorables.

## Gates ejecutados

### .NET 10

```powershell
dotnet restore AgropecuarIA.CapacityPlanningSpike.slnx --locked-mode
dotnet build AgropecuarIA.CapacityPlanningSpike.slnx --no-restore
dotnet format AgropecuarIA.CapacityPlanningSpike.slnx --no-restore --verify-no-changes
dotnet test --solution AgropecuarIA.CapacityPlanningSpike.slnx --no-build --minimum-expected-tests 21
dotnet list AgropecuarIA.CapacityPlanningSpike.slnx package --vulnerable --include-transitive
```

Resultado: `PASS`; restore locked, build con 0 warnings/errores, format y scan NuGet aprobados; 21/21 tests MTP/MSTest, 0 fallidos/omitidos. Cubre fórmulas exactas, monotonicidad 10×/burst, overflow/inputs inválidos, SLO, costos incompletos/metadata/bandas, fixture→golden, IDs/concurrencia y cardinalidad adversarial.

### Contratos y Next.js/React

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

Resultado: `PASS`; 3 fixtures positivos y 10 negativos, Prettier/ESLint/TypeScript, build Next.js 16.3, 5/5 Vitest, 4/4 Playwright Chromium y 0 vulnerabilidades conocidas. Los negativos cubren clasificación, IDs duplicados, concurrencia imposible, metadata/precios/bandas, golden divergente y reporte de costo desordenado.

E2E cubre todos los estados operativos, costo NO-GO, conexión inicial bloqueada, offline real con `BrowserContext.setOffline(true)`, cero storage/session/Service Worker, retry/dedupe, teclado, axe sin hallazgos serious/critical y viewport 390×844 sin overflow. La inspección visual del build confirmó jerarquía/contraste y rotulado inequívoco; el servidor local se detuvo y el puerto 3070 quedó libre.

### Revisión y seguridad

- Principal QA reprodujo todos los gates y aprobó 21/21 backend, 3/10 contratos, 5/5 unit frontend y 4/4 E2E.
- AppSec/Arquitectura encontró inicialmente integridad de costos/fixtures y cardinalidad/default-deny; todos los hallazgos se corrigieron y la reauditoría final quedó en 0 críticos, 0 altos, 0 medios y 0 bajos.
- `git diff --check`: `PASS` en revisiones independientes.
- No hay secretos/credenciales/PII real, sinks DOM peligrosos, idempotency key renderizada ni persistencia offline de producto.

## Compatibilidad, migración y reemplazo

No hay migración, DB, despliegue ni rollback productivo: N/A por ser R0 aislado. Schemas y fixtures usan versión `1.0`; el test .NET y el frontend consumen la fuente canónica. El laboratorio se elimina completo con `tasks/evidence/AGRO-DIS-007/spike` cuando un slice productivo reemplace esta evidencia; ADR/escenarios históricos se preservan para trazabilidad.

Docker/Compose, PostgreSQL/PostGIS, CI/CD, cloud, pruebas de carga real, OpenTelemetry emitido y alertas productivas son N/A por alcance. No se presentan como aprobados.

## Riesgos y gates externos

La tarea no está `Completada`. Permanecen:

- Q-019: equipo/capacidad, presupuesto y calendario nominados;
- Q-020: volúmenes, distribución, crecimiento, retención y tenant caliente medidos;
- Q-060: SLA, soporte, retención, región, proveedor, precios/impuestos/egress y presupuesto aprobados;
- Q-061: dispositivo/carrier/RTT/throughput/pérdida/cortes medidos en campo;
- prueba de carga sobre el monolito y dependencias reales, RPO/RTO administrados, idempotencia/authz tenant server-side y telemetría OpenTelemetry integrada.

Los owners son Sponsor/Delivery/Product/SRE/FinOps/Privacy/Legal según `decisions-and-gaps.md`. `GAP-003`, `GAP-010` y RSK-022/024/027 siguen abiertos. Los supuestos vencen 2026-09-30.

## Autoevaluación

`97/100`: contexto/selección 15, arquitectura/código 19, multiagente 10, full-stack/datos/observabilidad 14, tests/seguridad 20, preservación/cierre 19. La puntuación no compensa gates externos; por eso el estado es `En revisión`.
