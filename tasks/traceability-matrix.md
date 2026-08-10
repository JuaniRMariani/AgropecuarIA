# Matriz de trazabilidad

Versión: 1.1 — 2026-08-10. Los rangos son inclusivos y representan cada ID individual del intervalo. `TST-*` son IDs estables de pruebas/evidencias; cada slice integrado los vincula con runner y símbolos reales.

## Requisitos funcionales

| Requisito(s) | Prioridad | Épica | Tarea(s) | Prueba/evidencia | Release |
|---|---|---|---|---|---|
| RF-ID-001–006 | Must | EPIC-02/15 | AGRO-ID-001–004, AGRO-SEC-002 | TST-ID-AUTH, TST-TENANT-NEG | R1 |
| RF-ID-007 | Should | EPIC-02 | AGRO-ID-005 | TST-ID-JIT | R7 |
| RF-GIS-001–007, RF-GIS-011 | Must | EPIC-04/13 | AGRO-GIS-001–003, AGRO-FE-003 | TST-GIS-POSTGIS, TST-GEO-24, TST-GIS-E2E | R2 |
| RF-GIS-008–009 | Should | EPIC-04/14 | AGRO-GIS-004, AGRO-INT-004 | TST-GIS-IMPORT-LAYERS | R7 |
| RF-GIS-010 | Won't now | Excepción | EXC-001 | TST-SCOPE-NO-OFFLINE | Sin release |
| RF-CAT-001–005 | Must | EPIC-03/14 | AGRO-CAT-001/002/004, AGRO-INT-003 | TST-CAT-INGEST-DIFF, TST-CAT-PUBLISH | R1 |
| RF-PRD-001–005 | Must | EPIC-03 | AGRO-CAT-003/005 | TST-PRD-BASELINE, TST-PROFILE-ISOLATION | R1/R3 |
| RF-OPS-001–005 | Must | EPIC-06/09/10 | AGRO-AGR-001/002, AGRO-INV-001, AGRO-FIN-001 | TST-OPS-STATE, TST-OPS-ONCE | R2 |
| RF-OPS-006 | Should | EPIC-06/13/14 | AGRO-AGR-006, AGRO-FE-002, AGRO-INT-002 | TST-OPS-ADVANCED | R7 |
| RF-AGR-001–008, RF-AGR-011 | Must | EPIC-06 | AGRO-AGR-001–005 | TST-AGR-E2E, TST-AGR-PROP | R3 |
| RF-AGR-009 | Should | EPIC-06/14 | AGRO-INT-004 | TST-AGR-PRESCRIPTION-PROFILE | R7 |
| RF-AGR-010 | Could | EPIC-06/14 | AGRO-INT-004 | TST-AGR-COMMERCE-FEASIBILITY | R7 |
| RF-GAN-001–004 | Must | EPIC-07 | AGRO-GAN-001/002 | TST-GAN-MODES, TST-GAN-STOCK | R4 |
| RF-GAN-005–009 | Must | EPIC-07 | AGRO-GAN-003/004 | TST-GAN-TEMPORAL, TST-GAN-EVENTS | R4 |
| RF-GAN-010 | Should | EPIC-07/14 | AGRO-GAN-005, AGRO-INT-004 | TST-RFID-CONTRACT | R7 |
| RF-GAN-011–017 | Must | EPIC-08 | AGRO-FOR-001–004 | TST-FOR-PROP, TST-FOR-3LEVELS, TST-FOR-CONCURRENCY | R4 |
| RF-GAN-018 | Should | EPIC-07/08 | AGRO-GAN-004, AGRO-FOR-003 | TST-FOR-SUPPLEMENT | R4/R7 |
| RF-CLI-001–008 | Must | EPIC-05 | AGRO-CLI-001–004 | TST-WEATHER-CONTRACT, TST-CAP-LIFECYCLE, TST-WEATHER-DEGRADE | R2 |
| RF-CLI-009 | Should | EPIC-05 | AGRO-CLI-005 | TST-WEATHER-SKILL | R7 |
| RF-CLI-010 | Could | EPIC-05/14 | AGRO-INT-004 | TST-IOT-SAT-FEASIBILITY | R7 |
| RF-INV-001–005 | Must | EPIC-09 | AGRO-INV-001/002 | TST-INV-LEDGER, TST-INV-CONCURRENCY | R2/R3 |
| RF-ACT-001–002 | Must | EPIC-09 | AGRO-INV-003 | TST-ACT-VALUES | R3/R5 |
| RF-ACT-003 | Should | EPIC-09 | AGRO-INV-004 | TST-ACT-MAINT | R7 |
| RF-FIN-001–007, RF-FIN-010–011 | Must | EPIC-10 | AGRO-FIN-001–004 | TST-FIN-E2E, TST-FIN-MONEY, TST-FIN-CLOSE | R2/R5 |
| RF-FIN-008 | Should | EPIC-10 | AGRO-FIN-002 | TST-FIN-RECONCILIATION | R7 |
| RF-FIN-009 | Won't now | Excepción | EXC-002 | TST-SCOPE-NO-ARCA | Sin release |
| RF-FIN-012 | Must | EPIC-10 | AGRO-FIN-005 | TST-FIN-CANONICAL-RECON | R5 |
| RF-DOC-001–002 | Must | EPIC-11 | AGRO-DOC-001/002 | TST-DOC-SECURE, TST-AUDIT-TIMELINE | R1/R2 |
| RF-ANA-001–003 | Must | EPIC-12 | AGRO-IA-001/002 | TST-ANA-RECON, TST-ALERT-AUTH | R5 |
| RF-IA-001–005, RF-IA-007 | Must | EPIC-12/15 | AGRO-IA-003–005, AGRO-SEC-002/003 | TST-AI-EVAL, TST-AI-REDTEAM, TST-AI-KILL | R6 |
| RF-IA-006 | Should | EPIC-12 | AGRO-IA-006 | TST-AI-ADVANCED-GATE | R7 |
| RF-ADM-001–002, RF-ADM-004 | Must | EPIC-14 | AGRO-INT-001–003 | TST-IMPORT-ASYNC, TST-INBOX-REPLAY | R1/R3 |
| RF-ADM-003 | Must | EPIC-11 | AGRO-DOC-003 | TST-PORTABILITY, TST-PRIVACY-HOLD | R5 |

## Reglas de negocio

| Regla(s) | Épica | Tarea(s) | Prueba/evidencia | Release |
|---|---|---|---|---|
| RN-CORE-001–005, RN-CORE-009 | EPIC-01/02/15 | AGRO-FND-002/003, AGRO-ID-003, AGRO-SEC-002 | TST-TENANT-NEG, TST-IDEMPOTENCY, TST-AUDIT | R1+ |
| RN-CORE-006–008 | EPIC-09/10/11 | AGRO-INV-001, AGRO-FIN-001/003, AGRO-DOC-001 | TST-MONEY-UNIT-DOC | R1–R5 |
| RN-GIS-001–007 | EPIC-04 | AGRO-GIS-001–003 | TST-GIS-POSTGIS, TST-GIS-TEMPORAL | R2 |
| RN-CAT-001–005 | EPIC-03 | AGRO-CAT-001/002/004 | TST-CAT-BASELINE, TST-CAT-ROLLBACK | R1 |
| RN-PRD-001–005 | EPIC-03 | AGRO-CAT-003/005 | TST-PRD-BASELINE, TST-PROFILE-ISOLATION | R1/R3 |
| RN-AGR-001–006 | EPIC-06 | AGRO-AGR-001–005 | TST-AGR-PROP, TST-AGR-E2E | R2/R3 |
| RN-GAN-001–007 | EPIC-07 | AGRO-GAN-002–004 | TST-GAN-STOCK, TST-GAN-TEMPORAL, TST-GAN-HEALTH | R4 |
| RN-GAN-008–014 | EPIC-08 | AGRO-FOR-001–004 | TST-FOR-PROP, TST-FOR-3LEVELS | R4 |
| RN-CLI-001–008 | EPIC-05 | AGRO-CLI-001–004 | TST-WEATHER-CONTRACT, TST-CAP-LIFECYCLE | R2 |
| RN-INV-001–004 | EPIC-09 | AGRO-INV-001/002 | TST-INV-LEDGER, TST-INV-POLICY | R2/R3 |
| RN-ACT-001–002 | EPIC-09 | AGRO-INV-003 | TST-ACT-VALUES | R3/R5 |
| RN-FIN-001–005 | EPIC-10 | AGRO-FIN-002–005 | TST-FIN-LAYERS, TST-FIN-CLOSE, TST-FIN-CANONICAL-RECON | R5 |
| RN-FIS-001–007 | Excepción | EXC-003 | TST-SCOPE-NO-FISCAL | Sin release |
| RN-IA-001–006 | EPIC-12/15 | AGRO-IA-003–005, AGRO-SEC-002/003 | TST-AI-AUTH, TST-AI-EVAL, TST-AI-INJECTION | R6 |

## Requisitos no funcionales

| Requisito(s) | Épica | Tarea(s) | Prueba/evidencia | Release/gate |
|---|---|---|---|---|
| RNF-REL-001–004 | EPIC-00/16/17 | AGRO-DIS-007, AGRO-PLT-003/004, AGRO-QA-002/003 | TST-CAPACITY-MODEL, TST-SLO, TST-DEGRADE, TST-RESTORE | R0 evidence/cada release |
| RNF-PER-001 | EPIC-00/12/16/17 | AGRO-DIS-007, AGRO-IA-001, AGRO-PLT-003, AGRO-QA-002 | TST-CAPACITY-MODEL, TST-PERF-API | R0 evidence/R1+ |
| RNF-PER-002 | EPIC-00/04/13 | AGRO-DIS-007, AGRO-GIS-002, AGRO-FE-003/004 | TST-NETWORK-PWA, TST-PERF-MAP-4G | R0 evidence/R2 |
| RNF-PER-003–004 | EPIC-00/14/17 | AGRO-DIS-007, AGRO-INT-002, AGRO-QA-002 | TST-CAPACITY-MODEL, TST-PERF-IMPORT | R0 evidence/R1/R3 |
| RNF-PER-005 | EPIC-00/05/13 | AGRO-DIS-007, AGRO-CLI-001, AGRO-FE-004 | TST-NETWORK-PWA, TST-PERF-WEATHER-CACHE | R0 evidence/R2 |
| RNF-CON-001–004 | EPIC-00/01/13/17 | AGRO-DIS-007, AGRO-FND-002, AGRO-FE-001/002/004, AGRO-QA-002 | TST-NETWORK-PWA, TST-IDEMPOTENCY | R0 evidence/R1+ |
| RNF-SEC-001–003 | EPIC-15/16/17 | AGRO-SEC-001–004, AGRO-PLT-002/004, AGRO-QA-002/003 | TST-SEC-GATES, TST-TENANT-NEG | Cada release |
| RNF-PRI-001 | EPIC-11/15 | AGRO-DOC-003, AGRO-SEC-004 | TST-PRIVACY-RIGHTS-HOLD | R5/preprod |
| RNF-UX-001–004 | EPIC-13/17 | AGRO-FE-001–004, AGRO-QA-002 | TST-WCAG-MANUAL-AUTO, TST-UUID-SHORT | Cada slice |
| RNF-OBS-001–002 | EPIC-00/16 | AGRO-DIS-007, AGRO-PLT-003 | TST-OTEL-REDACTION, TST-INTEGRATION-HEALTH | R0 policy/R1+ |
| RNF-PORT-001 | EPIC-11/10 | AGRO-DOC-003, AGRO-FIN-005 | TST-PORTABILITY, TST-FIN-CANONICAL-RECON | R5 |
| RNF-PORT-002 | EPIC-01/05/12/14 | AGRO-FND-001, AGRO-CLI-001, AGRO-IA-003, AGRO-INT-001 | TST-ADAPTER-SUBSTITUTION | R1+ |
| RNF-CAT-001–003 | EPIC-03/17 | AGRO-CAT-001–005, AGRO-QA-001/002 | TST-CAT-BASELINE, TST-PRD-BASELINE | R1/R3 |
| RNF-GEO-001 | EPIC-04/05/17 | AGRO-GIS-001/002, AGRO-CLI-001, AGRO-QA-001/002 | TST-GEO-24 | R2 |

## ADR

| ADR | Estado fuente | Épica/tarea | Verificación | Release |
|---|---|---|---|---|
| ADR-001 | Aceptado | EPIC-01/16 · AGRO-FND-001, AGRO-PLT-001 | TST-ARCH-MODULES, TST-DEPLOY-COMPAT | R0/R1 |
| ADR-002 | Aceptado para contrato de discovery; pendiente R2 | EPIC-04 · AGRO-DIS-004, AGRO-GIS-001–003 | TST-GIS-POSTGIS | R0/R2 |
| ADR-003 | Aceptado para desarrollo R1; despliegue condicionado | EPIC-02 · AGRO-DIS-003, AGRO-ID-001–004 | TST-ID-AUTH | R0/R1 |
| ADR-004 | Propuesto | EPIC-08/12 · AGRO-FOR-003/004, AGRO-IA-003–005 | TST-AI-EVAL, TST-FOR-3LEVELS | R4/R6 |
| ADR-005 | Aceptado para contrato base; WRF postergado | EPIC-05 · AGRO-DIS-004, AGRO-CLI-001–005 | TST-WEATHER-CONTRACT, TST-CAP-LIFECYCLE | R0/R2 |
| ADR-006 | Aceptado discovery | EPIC-03 · AGRO-DIS-001/002, AGRO-CAT-001–005 | TST-CAT-BASELINE, TST-PROFILE-ISOLATION | R0/R3 |
| ADR-007 | Aceptado para contrato de discovery; pendiente proveedor/Legal y cloud | EPIC-00/11 · AGRO-DIS-005, AGRO-DOC-001–003 | TST-DOC-SECURE, TST-RESTORE | R0/R1–R5 |
| ADR-008 | Aceptado para contrato de discovery; pendiente piloto/FinOps | EPIC-00/16/17 · AGRO-DIS-007, AGRO-PLT-003/004, AGRO-QA-002 | TST-CAPACITY-MODEL, TST-NETWORK-PWA, TST-OTEL-REDACTION | R0/R1+ |
| ADR-009 | Aceptado para límites R0/R1; ensayo de migración pendiente | EPIC-01 · AGRO-FND-001/003 | TST-ARCH-MODULES, TST-CONTRACT-N-N1 | R0/R1 |

## Conteos y cobertura

| Tipo | Inventario | Con tarea/prueba | Excepción explícita | Faltante |
|---|---:|---:|---:|---:|
| RF Must | 94 | 94 | 0 | 0 |
| RF Should | 11 | 11 | 0 | 0 |
| RF Could | 2 | 2 | 0 | 0 |
| RF Won't now | 2 | 0 | 2 | 0 |
| **RF total** | **109** | **107** | **2** | **0** |
| RN de MVP/no fiscales | 71 | 71 | 0 | 0 |
| RN fiscales futuras | 7 | 0 | 7 | 0 |
| **RN total** | **78** | **71** | **7** | **0** |
| RNF | 29 | 29 | 0 | 0 |
| ADR | 9 | 9 | 0 | 0 |

Cobertura total RF/RN/RNF: 216/216 trazados a tarea+prueba o excepción justificada; 207 con tarea/prueba y 9 excepciones (`RF-GIS-010`, `RF-FIN-009`, `RN-FIS-001–007`). Ningún faltante.

## Evidencia incremental de AGRO-SEC-001

| Alcance | Tarea | Evidencia | Gate actual | Release siguiente |
|---|---|---|---|---|
| RNF-SEC-001–003 | AGRO-SEC-001/002/003 | `tasks/evidence/AGRO-SEC-001/AgropecuarIA-threat-model.md`, `threat-register.json`, `runtime-surface-register.json`, `release-security-gates.md`; validador/mutations + MTP/Vitest/E2E/SCA | PASS R0 documental y PASS local del gate R1 para Identity/FND; 14 amenazas, 7 críticas/7 altas, 0 críticas sin owner/prueba/gate | Auth0/edge/CI/RLS/restore y las demás superficies siguen NO-GO hasta su evidencia real |
| RNF-PRI-001 | AGRO-SEC-001/004 | `data-classification-and-privacy.md`, `provider-processing-inventory.md` | Baseline condicional; Q-054/055/058/060 y `VAL-LEG` permanecen NO-GO productivo | Rights/hold/purge/restore, DPA/región/retención y revisión humana en R1–R6 |

Esta evidencia no modifica los conteos de requisitos. El gate R1 local enlaza controles y abuse tests del bootstrap integrado sin declarar la release R1 completa. Los spikes `AGRO-DIS-*` siguen siendo evidencia descartable y no controles productivos.
