# Evidencia incremental R0/R1 — AGRO-SEC-001

Este directorio contiene el baseline central de amenazas y clasificación por release. Desde R1 existe un runtime local integrado de Identity/FND, Territory y Productive Core bajo `apps/`, `src/`, `contracts/` y `tests/`; sus controles cuentan como evidencia solo cuando el registro los enlaza a código y pruebas reproducibles. Productive Core agrega únicamente field draft no espacial con authz owner, creación/rename idempotentes, ETag/If-Match y RLS local; Territory usa snapshot local y mantiene todo tráfico Georef real en `NO-GO`. Los artefactos `AGRO-DIS-*` siguen siendo aislados/descartables y ningún resultado local certifica proveedor, Legal, hosting o producción.

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

El validador falla si falta un artefacto o sección obligatoria, si los IDs no son únicos/secuenciales, si una referencia local no existe, si una amenaza carece de fronteras/activos/`RSK-*` existente/control/prueba/owner/gate, si un crítico queda sin owner, si la tabla humana diverge del JSON o si Q-054/055/058/060 desaparecen. El gate R1 agrega drift checks exactos contra los tres contratos OpenAPI, solución, API, web, Identity, Territory, Productive Core, PostgreSQL, lockfiles y tests de abuso integrados. Sus mutations preservan la autorización Productive antes de lookup/replay, ETag/If-Match con 412 neutral, `FORCE RLS`, atomicidad, journal append-only, evento rename sin nombres, redacción de telemetría y límites no espaciales, además de los controles Territory/Georef; no sustituyen provider/edge/CI/Legal reales.

Identity agrega además inventario privacy-safe de sesiones propias y revocación individual de otra sesión mediante purpose `manage_sessions`, CSRF, `If-Match`, CAS y funciones actor-scoped. No incorpora dispositivos, IP, user-agent, fingerprints, revoke-all, notificaciones ni propagación distribuida.

## Límites y NO-GO

- Q-054/055: Product/Legal deben confirmar controlador, propiedad y delegación contractual.
- Q-058/060: Privacy/Legal/Sponsor deben aprobar proveedor, región, DPA, subencargados, retención, soporte y SLA.
- No se autoriza producción, cloud, ARCA, offline, proveedor, tratamiento internacional ni reutilización de los spikes como bootstrap.
- El registro se revisa por slice y release; por eso la tarea padre permanece `En curso` después de cada gate incremental.
