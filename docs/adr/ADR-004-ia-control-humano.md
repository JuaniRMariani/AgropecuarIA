# ADR-004 — IA consultiva, fundamentada y con control humano

- Estado: propuesto
- Fecha: 2026-08-04

## Contexto

Las recomendaciones pueden afectar producción, sanidad, patrimonio y cumplimiento. Los modelos pueden alucinar o recibir documentos maliciosos.

## Decisión

IA read-only inicialmente, con RAG autorizado, datos meteorológicos estructurados, motor determinístico de oferta/demanda forrajera, evidencia, supuestos y confianza. Solo usa reglas especializadas cuando actividad, perfil, versión y jurisdicción están aprobados. Sin biomasa puede explicar rangos estimados, pero se abstiene de declarar fecha/capacidad exacta; ante faltantes de seguridad se abstiene por completo. Toda mutación crítica requiere flujo normal y aprobación humana. Datos de clientes no entrenan modelos compartidos por defecto.

## Alternativas

- Agente autónomo: rechazado por riesgo.
- Chat sin grounding: rechazado por baja verificabilidad.
- Sin IA: no cumple la visión, pero sigue siendo fallback transaccional.

## Consecuencias

Se requieren datasets/evals, auditoría, costos por tenant y kill switch. El producto conserva utilidad aunque el proveedor IA no esté disponible.
