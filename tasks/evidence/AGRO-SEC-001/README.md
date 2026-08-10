# Evidencia incremental R0/R1 — AGRO-SEC-001

Este directorio contiene el baseline central de amenazas y clasificación por release. Desde R1 existe un runtime local integrado de Identity/FND bajo `apps/`, `src/`, `contracts/` y `tests/`; sus controles cuentan como evidencia solo cuando el registro los enlaza a código y pruebas reproducibles. Los artefactos `AGRO-DIS-*` siguen siendo aislados/descartables y ningún resultado local certifica proveedor, Legal, hosting o producción.

## Artefactos

- `AgropecuarIA-threat-model.md`: sistema, fronteras, atacante, abusos, amenazas y foco de revisión.
- `threat-register.json`: trazabilidad machine-readable de amenaza, owner, controles, pruebas y gate.
- `runtime-surface-register.json`: inventario R1 de superficies integradas, fixtures Development/Test y fronteras externas NO-GO.
- `data-classification-and-privacy.md`: clasificación, minimización y evaluación condicional de privacidad.
- `provider-processing-inventory.md`: flujos externos y condiciones de proveedor.
- `release-security-gates.md`: gate incremental R0–R6 y checklists para nuevas superficies.
- `validate-threat-model.ps1`: controles reproducibles y mutation tests en memoria.
- `validation-report.md`: resultado exacto de la ejecución y revisión independiente.

## Verificación

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File "tasks/evidence/AGRO-SEC-001/validate-threat-model.ps1" -SelfTest
```

El validador falla si falta un artefacto o sección obligatoria, si los IDs no son únicos/secuenciales, si una referencia local no existe, si una amenaza carece de fronteras/activos/`RSK-*` existente/control/prueba/owner/gate, si un crítico queda sin owner, si la tabla humana diverge del JSON o si Q-054/055/058/060 desaparecen. El gate R1 agrega drift checks contra solución, API, web, Identity, contrato, PostgreSQL, lockfiles y tests de abuso integrados; sus mutation tests no sustituyen Auth0/edge/CI/Legal reales.

## Límites y NO-GO

- Q-054/055: Product/Legal deben confirmar controlador, propiedad y delegación contractual.
- Q-058/060: Privacy/Legal/Sponsor deben aprobar proveedor, región, DPA, subencargados, retención, soporte y SLA.
- No se autoriza producción, cloud, ARCA, offline, proveedor, tratamiento internacional ni reutilización de los spikes como bootstrap.
- El registro se revisa por slice y release; por eso la tarea padre permanece `En curso` después de cada gate incremental.
