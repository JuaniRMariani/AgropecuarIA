# AGRO-FND-003 — renombrar campo borrador

Este incremento aplica el primer patrón vertical de evolución con conflicto explícito sobre un recurso real: permite renombrar un `ManagementUnit` de tipo `field`, todavía `draft/not_configured`, sin sobrescritura silenciosa.

La evidencia no cierra `AGRO-FND-003`. Backfills masivos y reanudables, contract migrations, rectificación histórica general, restore de ambiente y evolución de otros agregados permanecen pendientes.

Artefactos:

- `product-decisions.md`: autoridad, concurrencia, idempotencia, privacidad y rollout del slice.
- `validation-report.md`: resultados reproducibles y riesgos residuales, completado al cerrar los gates.
- `contracts/productive-core.openapi.yaml`: contrato HTTP 1.1.
- `tasks/evidence/AGRO-FND-001/contracts/management-unit-display-name-changed.v1.schema.json`: payload público sin el nombre.
