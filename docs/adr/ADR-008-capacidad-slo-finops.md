# ADR-008 — Capacidad, SLO y FinOps guiados por evidencia

- **Estado:** aceptado para contrato de discovery; pendiente medición del piloto, presupuesto y operación productiva.
- **Fecha:** 2026-08-05.
- **Drivers:** RNF-REL-001–004, RNF-PER-001–005, RNF-CON-001–004, RNF-OBS-001/002 y RSK-022/024/027.

## Contexto

AgropecuarIA todavía no tiene runtime productivo, carga observada, equipo confirmado, presupuesto cloud ni mediciones de conectividad rural. Fijar fechas, capacidad o costos puntuales en ese estado produciría falsa precisión. A la vez, cada release necesita targets repetibles, una política de error budget y drivers de costo para saber qué medir y cuándo detener una expansión.

## Decisión

- Modelar demanda con escenarios versionados y rotulados `synthetic-estimate`, incluyendo fuente, confianza, owner y fecha de revalidación. Los escenarios nunca sustituyen telemetría del piloto.
- Derivar capacidad mediante fórmulas determinísticas y unidades explícitas. No decidir índices, partición, microservicios ni Kubernetes antes de observar saturación real.
- Definir SLIs por población elegible y separar disponibilidad del core de clima, mapas, identidad y otras dependencias externas. Un proveedor caído puede activar modo degradado, pero no ocultarse del SLI propio de la integración.
- Adoptar 99,9 % mensual, RPO 15 min y RTO 2 h como targets hipotéticos de ingeniería. No son SLA, soporte ni retención contractual.
- Gobernar releases con error budget: consumirlo exige priorizar confiabilidad; recuperarlo no autoriza desplegar sin los demás gates. El alertado productivo requiere volumen mínimo y ventanas múltiples para evitar ruido.
- Calcular costos por drivers `low/base/high`. Un precio ausente devuelve resultado `incomplete` y bloquea una decisión FinOps; jamás se imputa cero. Todo catálogo real conserva moneda ISO, región, fuente, fecha e impuestos.
- Mantener telemetría en una allow-list de dimensiones acotadas. IDs de tenant/usuario/recurso, CUIT, email, coordenadas, nombres/rutas, query, payload e idempotency keys no son labels.
- Tratar conectividad como riesgo medido. El frontend sigue online-only: antes de confirmar sin red se bloquea con explicación y no se crea cola, sincronización ni persistencia local encubierta.
- Usar canary y rollback como políticas de release; esta ADR no provisiona infraestructura ni convierte un laboratorio local en benchmark productivo.

## Consecuencias

El mismo contrato puede recalcular escenarios 10× y ráfagas sin acoplarse a un proveedor. La decisión evita fechas y costos falsos, pero mantiene `GAP-003` y `GAP-010` abiertos: equipo, volumen, precios, SLA y red real deben reemplazar los fixtures antes de comprometer una release. El control de cardinalidad reduce riesgo de PII y explosión de series, a costa de diagnosticar detalles mediante trazas/logs autorizados y no labels.

## Evidencia y gates pendientes

`tasks/evidence/AGRO-DIS-007` contiene schemas, escenarios sintéticos, modelo .NET y laboratorio Next.js. Antes de producción faltan mediciones de carga y red del piloto, composición/capacidad del equipo, provider/region/pricing y presupuesto aprobados, SLA/soporte/retención, prueba de carga sobre el slice real, OpenTelemetry integrado y validación de RPO/RTO administrados.
