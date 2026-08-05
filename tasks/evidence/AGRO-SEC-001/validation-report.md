# Reporte de validación R0 — AGRO-SEC-001

Fecha: 2026-08-05. Alcance: baseline documental del modelo de amenazas y clasificación por release. No existe runtime productivo raíz; este resultado no certifica controles desplegados, Legal, proveedor, región, SLA ni retención.

## Resultado

`PASS` técnico del gate R0, condicionado a repetir threat modeling, controles y abuse tests en cada slice R1–R6. El registro contiene 14 amenazas estables: 7 críticas y 7 altas; ninguna crítica carece de owner, prueba o gate bloqueante. Las 12 superficies de procesamiento permanecen candidatas o futuras hasta sus aprobaciones y pruebas reales.

## Definition of Ready demostrada

- Arquitectura, módulos y fronteras: `docs/05-arquitectura.md`, ADR-009 y evidencia `AGRO-FND-001`.
- Flujos, actores y activos: `docs/02-dominio-actores-y-flujos.md` y `docs/07-seguridad-y-privacidad.md`.
- Proveedores candidatos y comportamiento R0: `AGRO-DIS-003/004/005/007`.
- Q-054/055/058/060 permanecen explícitas. Sus vacíos cambian ranking y producen NO-GO productivo, pero no impiden documentar el baseline.

## Artefactos verificados

- `AgropecuarIA-threat-model.md`: edge/web, identidad/email, API, DB/GIS, grants/storage, tiles/proveedores, jobs, IA, telemetría, restore y supply chain.
- `threat-register.json`: `TM-001`–`TM-014` con `RSK-*`, fronteras, activos, owners, controles, gaps, tests, detección, riesgo residual y gate.
- `data-classification-and-privacy.md`: cinco clases, scope, minimización, UX/consentimiento y NO-GO.
- `provider-processing-inventory.md`: `PI-01`–`PI-12`, incluida infraestructura/edge, observabilidad, CI/artefactos y backup.
- `release-security-gates.md`: criterios R0–R6 y checklists de frontera/proveedor.
- `validate-threat-model.ps1`: estructura, rutas/enlaces, `RSK-*` existentes, preguntas abiertas, sincronización JSON↔tabla y mutation tests.

## Comandos y resultados

```text
powershell -NoProfile -ExecutionPolicy Bypass -File "tasks/evidence/AGRO-SEC-001/validate-threat-model.ps1" -SelfTest
SELFTEST critical-owner: PASS
SELFTEST critical-test: PASS
SELFTEST blank-array-value: PASS
SELFTEST duplicate-id: PASS
SELFTEST risk-link: PASS
SELFTEST open-question: PASS
SELFTEST human-table-drift: PASS
VALIDATION PASS: 14 threats; 7 critical; 7 high; 0 critical threats without owner/test/gate.

ConvertFrom-Json threat-register.json
JSON PASS: threat-register.json

rg dirigido a asignaciones de password/client_secret/private_key/api_key
SECRET SCAN PASS: no credential assignments

git diff --check
DIFF CHECK PASS
```

## Revisión independiente

La primera revisión cruzada detectó edge/CDN y flujos browser→storage/tiles ausentes, email incompleto, inventario sin telemetría/CI/backup, trazabilidad `RSK-*` implícita, un owner de módulo no aprobado y falsos PASS posibles del validador. Se corrigieron en el modelo/diagrama, registro, inventario y mutation tests. La reauditoría final AppSec/Data fue `PASS`: 0 críticos, 0 altos y 0 medios; Architecture y Product/UX/Privacy también aprobaron y sus dos observaciones bajas de índices `PI-09/PI-12` quedaron corregidas antes del gate final.

El principal repitió desde el estado combinado: 7/7 mutation tests, 14 amenazas, 4 preguntas abiertas, 16 enlaces únicos a riesgos, 12 superficies únicas, 0 credenciales detectadas y `git diff --check` sin errores.

## N/A y riesgo residual

- .NET, pnpm/frontend, API, PostgreSQL/PostGIS, Docker/Compose, migraciones, SAST/SCA/DAST, telemetría emitida, CI/CD y deploy: N/A; esta fase no crea runtime ni infraestructura.
- Todas las amenazas críticas/altas siguen abiertas para producción. El owner y gate existen, pero los controles deben demostrarse en el slice real.
- Q-054/055/058/060, `GAP-003`, `GAP-008`, `VAL-LEG`, IdP/proveedores/regiones/DPA/retención, pipeline y restore administrado permanecen pendientes.
- `AGRO-SEC-001` continúa `En curso` por ser una tarea R0–R6; el baseline R0 no completa el padre.
