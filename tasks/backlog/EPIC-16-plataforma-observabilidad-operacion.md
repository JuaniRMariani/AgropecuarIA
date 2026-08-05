# EPIC-16 — Plataforma, CI/CD, observabilidad y operación

Objetivo: plataforma administrada simple, promoción inmutable, secretos, OTel, SLO, restore y runbooks. R0–R6; sin Kubernetes.

<a id="agro-plt-001"></a>

## AGRO-PLT-001 — Definir entornos y promover artefactos compatibles

- **Release, épica, prioridad y tamaño:** R0/R1 · EPIC-16 · Must · L.
- **Owner y colaboradores:** Platform/Architecture; Backend, Frontend, Data, AppSec y QA.
- **Resultado/valor esperado:** entornos local/CI/integración/preproducción/producción aislados, mismos artefactos inmutables y despliegue compatible N/N-1.
- **Historia/JTBD:** Como release manager, quiero promover una versión trazable sin configurarla a mano.
- **Alcance incluido:** matriz de entornos, artefacto, versión, procedencia, coordinación API/worker/web, una única ejecución de migraciones, feature flags, health/readiness y rollback.
- **Fuera de alcance:** Kubernetes, aprovisionamiento cloud durante la planificación y migración en cada réplica.
- **Requisitos trazados:** ADR-001; RNF-REL-001; RNF-PORT-002; Q-019/020/060.
- **Precondiciones y dependencias:** DIS-007 y FND-001/003.
- **Contrato/API/eventos afectados:** health/version/flag y deployment compatibility.
- **Datos, índices, migración y compatibilidad:** expand/contract, registro de migraciones, backfill y gate de backup.
- **Autenticación, autorización, tenant y auditoría:** environment/service identities least privilege y release auditoría.
- **Frontend:** entorno, versión y errores mostrados de forma segura; sin secretos.
- **Reglas e invariantes:** se promueve el mismo artefacto; los datos productivos no se copian a entornos inferiores.
- **Criterios de aceptación:** Dado un rollout N/N-1, cuando se despliega o revierte, entonces API, worker y schema permanecen compatibles y el smoke tenant resulta exitoso.
- **Casos negativos y bordes:** migración fallida, worker antiguo, flags incompatibles y caída de región/servicio.
- **Estrategia de pruebas:** deployment/migration/smoke/rollback y configuration validation.
- **Observabilidad:** version/health/deploy markers y rollback alertas.
- **Seguridad y privacidad:** isolated credentials/datos y provenance.
- **Performance/capacidad y límites:** recurso assumptions desde DIS-007.
- **Feature flag, rollout, migración, rollback y recuperación:** standard canary/flag/roll-forward policy.
- **Documentación:** topology/environment/release runbook.
- **Comandos/evidencia esperados:** comandos reales de build, despliegue y migración solo cuando existan el repositorio y la plataforma.
- **Definition of Ready:** platform/region/team/SLO decisions.
- **Definition of Done:** ensayo en preproducción y rollback demostrados.
- **Bloqueos/preguntas:** Q-019/020/060.
- **Paralelizable:** platform/contratos/seguridad con owners.

<a id="agro-plt-002"></a>

## AGRO-PLT-002 — Automatizar gates de calidad y cadena de suministro

- **Release, épica, prioridad y tamaño:** R1 · EPIC-16 · Must · M.
- **Owner y colaboradores:** Plataforma/QA/AppSec; todos los owners de código.
- **Resultado/valor esperado:** los artefactos web/API/worker solo se promueven con pruebas, escaneos y evidencia aprobados.
- **Historia/JTBD:** Como equipo, quiero retroalimentación reproducible y ningún release con vulnerabilidades altas/críticas.
- **Alcance incluido:** restore, build, pruebas, typecheck, lint, integración PostGIS, etapas de contrato/E2E, SAST, SCA, secretos, SBOM, artefactos e informes de pruebas.
- **Fuera de alcance:** YAML específico en esta etapa y pruebas omitidas contabilizadas como exitosas.
- **Requisitos trazados:** RNF-SEC-002; RNF-UX-001; docs/09 gates.
- **Precondiciones y dependencias:** PLT-001 y prueba strategy.
- **Contrato/API/eventos afectados:** quality estado/artifact metadatos.
- **Datos, índices, migración y compatibilidad:** ephemeral isolated prueba datos/artifact retention.
- **Autenticación, autorización, tenant y auditoría:** least CI permissions/no secreto salida.
- **Frontend:** lint, typecheck, pruebas unitarias, build, accesibilidad y E2E configurados cuando exista el proyecto.
- **Reglas e invariantes:** zero high/critical; flaky/skipped explicit.
- **Criterios de aceptación:** Dada una prueba o escaneo fallido, cuando se ejecuta el pipeline, entonces se bloquea la promoción y el artefacto/informe identifica la causa.
- **Casos negativos y bordes:** compromised dependency/action, secreto leak, flaky prueba y unavailable external.
- **Estrategia de pruebas:** self-prueba pipeline, failure injection y provenance review.
- **Observabilidad:** duration/failure/flaky/queue y artifacts.
- **Seguridad y privacidad:** pinned/min permissions/SBOM.
- **Performance/capacidad y límites:** PR fast suite vs scheduled/release suites.
- **Feature flag, rollout, migración, rollback y recuperación:** endurecimiento progresivo del gate antes de producción.
- **Documentación:** pipeline stages/waiver policy.
- **Comandos/evidencia esperados:** repository-configured commands, no invented ones in planning.
- **Definition of Ready:** projects/scripts/runners exist.
- **Definition of Done:** fallos representativos bloquean la promoción y los informes quedan retenidos.
- **Bloqueos/preguntas:** herramientas/proveedor.
- **Paralelizable:** sí by artifact/stage.

<a id="agro-plt-003"></a>

## AGRO-PLT-003 — Instrumentar SLO e integraciones sin filtrar datos

- **Release, épica, prioridad y tamaño:** R1/R2 · EPIC-16 · Must · L.
- **Owner y colaboradores:** SRE; todos los módulos, Privacidad, AppSec y QA.
- **Resultado/valor esperado:** trazas, métricas y logs correlacionan requests, trabajos y proveedores mediante tableros accionables.
- **Historia/JTBD:** Como responsable on-call, quiero detectar latencia, errores, backlog y frescura sin leer payloads sensibles.
- **Alcance incluido:** OTel/OTLP, correlation, pseudonymous tenant, API/worker/DB/PostGIS/archivos/integrations/climate/AI SLI, tableros/alertas.
- **Fuera de alcance:** prompts/docs/CUIT/exact coordinates in logs y unbounded cardinality.
- **Requisitos trazados:** RNF-OBS-001/002; RNF-REL-001/004; RNF-PER-001; Q-060.
- **Precondiciones y dependencias:** PLT-001 y DIS-007.
- **Contrato/API/eventos afectados:** trace/log/metric conventions y health estado.
- **Datos, índices, migración y compatibilidad:** telemetry retention/sampling/redaction/cardinality budget.
- **Autenticación, autorización, tenant y auditoría:** monitoring access least privilege; auditoría≠log.
- **Frontend:** Web Vitals/error correlation no PII.
- **Reglas e invariantes:** toda integración expone salud, latencia, errores, backlog y último éxito.
- **Criterios de aceptación:** Dado un request o fallo entre módulos, cuando se investiga, entonces la traza y el tablero identifican el recorrido y tenant seudonimizado sin secretos ni PII.
- **Casos negativos y bordes:** sampling loss, cardinality spike, proveedor timeout y telemetry backend outage.
- **Estrategia de pruebas:** telemetry assertions/redaction/load/failure alertas.
- **Observabilidad:** itself; meta-monitor collector/drop/cost.
- **Seguridad y privacidad:** allow-list attributes/retention/access.
- **Performance/capacidad y límites:** overhead y cost budgets.
- **Feature flag, rollout, migración, rollback y recuperación:** sampling/alertas gradual; disable unsafe attribute.
- **Documentación:** telemetry catalog/SLO/tablero/runbooks.
- **Comandos/evidencia esperados:** configured OTel pruebas/query evidencia later.
- **Definition of Ready:** SLI/SLO/redaction/owner.
- **Definition of Done:** señales end-to-end y simulacros de alertas aprobados.
- **Bloqueos/preguntas:** Q-060/backend/proveedor/cost.
- **Paralelizable:** instrumentation per module, conventions centralized.

<a id="agro-plt-004"></a>

## AGRO-PLT-004 — Demostrar backup, restore, resiliencia y costos

- **Release, épica, prioridad y tamaño:** R1–R6 · EPIC-16 · Must · L.
- **Owner y colaboradores:** SRE/Data; Documents, QA, AppSec y Product.
- **Resultado/valor esperado:** RPO ≤15 min, RTO ≤2 h y fallos de proveedores/rollback cubiertos por runbooks ensayados y controles de costo.
- **Historia/JTBD:** Como sponsor, quiero recuperar DB, PostGIS, outbox, auditoría y objetos, y conocer el costo operativo.
- **Alcance incluido:** PITR/immutable backups, consistent object restore, quarterly drill, outbox reconciliation, proveedor/IdP/DB/almacenamiento/migration/tenant incident runbooks, FinOps budgets.
- **Fuera de alcance:** afirmar éxito de backups no probados y reducir seguridad o retención para ocultar costos.
- **Requisitos trazados:** RNF-REL-001–004; RNF-SEC-001/003; Q-019/020/058/060.
- **Precondiciones y dependencias:** DIS-005/007, PLT-001/003.
- **Contrato/API/eventos afectados:** backup manifests/health/restore/reconciliation.
- **Datos, índices, migración y compatibilidad:** DB/PostGIS/outbox/auditoría/object hashes y external-datos limitations.
- **Autenticación, autorización, tenant y auditoría:** backup access/break-glass/restore auditado.
- **Frontend:** estados de degradación y mantenimiento comunicados con precisión.
- **Reglas e invariantes:** la caída de externos no bloquea transacciones; la integridad del restore importa además de la disponibilidad.
- **Criterios de aceptación:** Dado un simulacro de desastre, cuando se restaura, entonces se cumplen RPO/RTO medidos y se verifican geometrías, eventos, hashes y replays sin duplicados.
- **Casos negativos y bordes:** faltantes object, corrupted backup, secreto expired, region outage y desactualizado proveedor.
- **Estrategia de pruebas:** quarterly restore/fault injection/rollback/cost anomaly.
- **Observabilidad:** backup age/restore times/backlog/cost/budget alertas.
- **Seguridad y privacidad:** encryption/immutability/least access/region/retention.
- **Performance/capacidad y límites:** almacenamiento/egress/telemetry/climate/AI budgets.
- **Feature flag, rollout, migración, rollback y recuperación:** runbooks e interruptores de emergencia; ensayo de restore antes de un release mayor.
- **Documentación:** DR/runbooks/FinOps/risk acceptance.
- **Comandos/evidencia esperados:** evidencia nativa de la plataforma cuando sea seleccionada; informes, capturas y logs redactados.
- **Definition of Ready:** proveedores/regions/targets/datos inventory.
- **Definition of Done:** el simulacro cumple objetivos o existe aceptación explícita del riesgo residual por el sponsor.
- **Bloqueos/preguntas:** Q-019/020/058/060.
- **Paralelizable:** runbook owners independently, integrated drill serial.
