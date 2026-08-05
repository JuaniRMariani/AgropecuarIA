# Evidencia R0 — AGRO-SEC-001

Este directorio contiene el baseline central de amenazas y clasificación para la arquitectura documentada de AgropecuarIA. El repositorio aún no posee runtime productivo: los controles ejecutados en `AGRO-DIS-*` son evidencia local y descartable, no garantías de producción.

## Artefactos

- `AgropecuarIA-threat-model.md`: sistema, fronteras, atacante, abusos, amenazas y foco de revisión.
- `threat-register.json`: trazabilidad machine-readable de amenaza, owner, controles, pruebas y gate.
- `data-classification-and-privacy.md`: clasificación, minimización y evaluación condicional de privacidad.
- `provider-processing-inventory.md`: flujos externos y condiciones de proveedor.
- `release-security-gates.md`: gate incremental R0–R6 y checklists para nuevas superficies.
- `validate-threat-model.ps1`: controles reproducibles y mutation tests en memoria.
- `validation-report.md`: resultado exacto de la ejecución y revisión independiente.

## Verificación

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File "tasks/evidence/AGRO-SEC-001/validate-threat-model.ps1" -SelfTest
```

El validador falla si falta un artefacto o sección obligatoria, si los IDs no son únicos/secuenciales, si una referencia local no existe, si una amenaza carece de fronteras/activos/`RSK-*` existente/control/prueba/owner/gate, si un crítico queda sin owner, si la probabilidad/impacto/prioridad de la tabla humana diverge del JSON o si Q-054/055/058/060 desaparecen del registro. Siete mutation tests cubren esos fallos estructurales; no sustituyen abuse tests de un runtime futuro.

## Límites y NO-GO

- Q-054/055: Product/Legal deben confirmar controlador, propiedad y delegación contractual.
- Q-058/060: Privacy/Legal/Sponsor deben aprobar proveedor, región, DPA, subencargados, retención, soporte y SLA.
- No se autoriza producción, cloud, ARCA, offline, proveedor, tratamiento internacional ni reutilización de los spikes como bootstrap.
- El registro se revisa por slice y release; por eso la tarea padre permanece `En curso` después del gate R0.
