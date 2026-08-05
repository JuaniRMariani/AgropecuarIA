# EPIC-14 — Integraciones, importaciones y portabilidad

Objetivo: adaptadores sustituibles, trabajos durables, plantillas validadas y una alternativa antes de depender de servicios externos. R1–R7.

<a id="agro-int-001"></a>

## AGRO-INT-001 — Operar conexiones, inbox y reintentos conciliables

- **Release, épica, prioridad y tamaño:** R1 · EPIC-14 · Must · L.
- **Owner y colaboradores:** Integraciones/Plataforma; owners de módulos, AppSec y QA.
- **Resultado/valor esperado:** integraciones servidoras observables/idempotentes y sin bloquear dominio.
- **Historia/JTBD:** Como admin, quiero conocer estado/reintentar/conciliar una integración fallida.
- **Alcance incluido:** conexión, contrato del adaptador, timeout, reintento, circuit breaker, inbox, intento, estado, último éxito y conciliación manual.
- **Fuera de alcance:** llamadas externas desde el navegador, sincronización offline de dispositivos y broker por defecto.
- **Requisitos trazados:** RF-ADM-004; RN-CORE-005; RNF-REL-004; RNF-OBS-002; RNF-PORT-002.
- **Precondiciones y dependencias:** FND-002 y proveedor contratos.
- **Contrato/API/eventos afectados:** IntegrationConnection/SyncAttempt/InboxMessage/estado.
- **Datos, índices, migración y compatibilidad:** unicidad por tenant, proveedor, ID externo, versión, estado y próximo intento.
- **Autenticación, autorización, tenant y auditoría:** admin alcance, secretos vault, manual actions auditado.
- **Frontend:** bandeja de integraciones, estado, progreso, error y reintento accesibles.
- **Reglas e invariantes:** el reintento es seguro; la llamada externa ocurre fuera de la transacción de negocio; el fallo del proveedor produce un estado degradado.
- **Criterios de aceptación:** Dado un duplicado, timeout o crash, cuando se reintenta, entonces existe un único efecto de dominio y un estado visible y conciliable.
- **Casos negativos y bordes:** poison, schema deriva, revoked secreto, 429 y tenant context faltantes.
- **Estrategia de pruebas:** contrato/inbox/idempotency/fault injection/BOLA.
- **Observabilidad:** health/latency/errors/backlog/age/last success/circuit.
- **Seguridad y privacidad:** egress allow-list, secretos, payload validation/minimization.
- **Performance/capacidad y límites:** queues/quotas per proveedor/tenant.
- **Feature flag, rollout, migración, rollback y recuperación:** adapter flags; stop/replay/reconcile runbook.
- **Documentación:** integration contrato/runbooks.
- **Comandos/evidencia esperados:** futuras contrato/fault pruebas.
- **Definition of Ready:** esquema del proveedor, alternativa y owner definidos.
- **Definition of Done:** reintento, degradación y telemetría demostrados.
- **Bloqueos/preguntas:** proveedor decisions.
- **Paralelizable:** sí per adapter.

<a id="agro-int-002"></a>

## AGRO-INT-002 — Importar datos mediante plantillas con preview

- **Release, épica, prioridad y tamaño:** R1/R3 · EPIC-14 · Must · L.
- **Owner y colaboradores:** Integraciones/Datos; owners de dominio, Frontend y QA.
- **Resultado/valor esperado:** onboarding/migración con errores accionables y sin contaminación parcial.
- **Historia/JTBD:** Como admin, quiero validar una plantilla antes de confirmar miles de registros.
- **Alcance incluido:** versión de plantilla/schema, carga, staging, vista previa, errores por fila/campo, progreso asíncrono, cancelación, confirmación y claves externas.
- **Fuera de alcance:** inferencia automática del formato y publicación parcial silenciosa.
- **Requisitos trazados:** RF-ADM-001/002; RN-CORE-003/005/007; RNF-PER-003/004.
- **Precondiciones y dependencias:** DOC-001, INT-001 y contratos de los módulos destino.
- **Contrato/API/eventos afectados:** import trabajo/validate/confirm/report.
- **Datos, índices, migración y compatibilidad:** staging/hash/schema version/idempotency/error report.
- **Autenticación, autorización, tenant y auditoría:** module/import permissions; confirmation auditado.
- **Frontend:** upload/preview/progress/errors/download report, responsivo/a11y.
- **Reglas e invariantes:** las filas inválidas no se publican; el reintento no duplica; se conservan valor y unidad originales.
- **Criterios de aceptación:** Dadas 1.000 filas con errores y un reintento, cuando se validan y confirman, entonces se cumple el objetivo p95 de ≤2 minutos y el conjunto aprobado se confirma una sola vez.
- **Casos negativos y bordes:** encoding/date/unit, duplicate external IDs, cancellation/crash y malicious archivo.
- **Estrategia de pruebas:** fixtures/property/idempotency/performance/seguridad/E2E.
- **Observabilidad:** trabajos/rows/errors/duration/backlog.
- **Seguridad y privacidad:** AV/limits/tenant/temp retention.
- **Performance/capacidad y límites:** RNF-PER-003/004 y quotas.
- **Feature flag, rollout, migración, rollback y recuperación:** flags por plantilla; reversión explícita de la importación cuando sea válida.
- **Documentación:** plantillas, diccionario de datos y errores.
- **Comandos/evidencia esperados:** futuras import/performance pruebas.
- **Definition of Ready:** esquema, semántica de errores y owner definidos.
- **Definition of Done:** importación segura y reproducible.
- **Bloqueos/preguntas:** archivos fuente del piloto.
- **Paralelizable:** sí, por plantilla, sobre un motor común.

<a id="agro-int-003"></a>

## AGRO-INT-003 — Sincronizar fuentes nacionales mediante staging/publicación

- **Release, épica, prioridad y tamaño:** R1/R2 · EPIC-14 · Must · M.
- **Owner y colaboradores:** Integraciones; Catálogo, GIS, Clima, Datos y QA.
- **Resultado/valor esperado:** las fuentes Georef/catálogo y los adaptadores climáticos usan un ciclo de vida común y confiable.
- **Historia/JTBD:** Como owner de datos, quiero gestionar cada fuente mediante snapshot → staging → revisión → publicación o alternativa.
- **Alcance incluido:** adapter fixtures/hash/schema/version/diff/estado for Georef y catalog fuentes; weather contratos linked.
- **Fuera de alcance:** un único reconciliador semántico genérico y escrituras directas en tablas de dominio.
- **Requisitos trazados:** RF-GIS-011; RF-CAT-001/005; RF-CLI-007/008; RN-CAT-001/003; RNF-OBS-002.
- **Precondiciones y dependencias:** INT-001, CAT-001 y GIS-001.
- **Contrato/API/eventos afectados:** fuente snapshot/diff/publish estado.
- **Datos, índices, migración y compatibilidad:** fuente-specific staging y immutable published references.
- **Autenticación, autorización, tenant y auditoría:** roles globales/editoriales; entradas de tenant separadas.
- **Frontend:** admin estado/diff/degradado/last success.
- **Reglas e invariantes:** una fuente pública no implica autorización ni SLA; no se inventan alternativas.
- **Criterios de aceptación:** Dada una fuente no disponible o un cambio de schema, cuando se sincroniza, entonces el último snapshot correcto permanece identificado y la publicación espera revisión.
- **Casos negativos y bordes:** license change, partial archivo, duplicate code y desactualizado fuente.
- **Estrategia de pruebas:** proveedor contratos/fixtures/diff/alternativa/seguridad.
- **Observabilidad:** freshness/coverage/errors/diff size.
- **Seguridad y privacidad:** validate untrusted entradas y provenance.
- **Performance/capacidad y límites:** async, fuente-specific budgets.
- **Feature flag, rollout, migración, rollback y recuperación:** adapter/published-version flags y rollback.
- **Documentación:** fuente cards/license/runbooks.
- **Comandos/evidencia esperados:** futuras contrato pruebas.
- **Definition of Ready:** mecanismo, licencia y owner de la fuente definidos.
- **Definition of Done:** ciclo reproducible y alternativa segura.
- **Bloqueos/preguntas:** Q-025/026/062/063.
- **Paralelizable:** sí per fuente.

<a id="agro-int-004"></a>

## AGRO-INT-004 — Priorizar integraciones agro posteriores por factibilidad

- **Release, épica, prioridad y tamaño:** R7 · EPIC-14 · Should/Could · M.
- **Owner y colaboradores:** Producto/Integraciones; Legal, enlace SENASA, Dominio y QA.
- **Resultado/valor esperado:** RFID, SENASA, SISA, DTV-e, CPE, satélite e IoT se incorporan solo con mecanismo oficial y valor demostrados.
- **Historia/JTBD:** Como sponsor, quiero reducir duplicación sin scraping ni promesas de API inexistente.
- **Alcance incluido:** factibilidad, autenticación, aspectos legales, contrato, fixtures, alternativa, caso de negocio y slice priorizado del adaptador.
- **Fuera de alcance:** ARCA en el MVP, scraping y credenciales ocultas.
- **Requisitos trazados:** RF-GAN-010; RF-CLI-010; RF-AGR-009/010; RF-GIS-009; Q-029/047/065.
- **Precondiciones y dependencias:** pilot evidencia y official mechanism.
- **Contrato/API/eventos afectados:** específicos del adaptador, sin filtrar conceptos del proveedor al dominio.
- **Datos, índices, migración y compatibilidad:** external IDs/fuente/version y reconciliation.
- **Autenticación, autorización, tenant y auditoría:** delegated/minimum access; no Clave Fiscal.
- **Frontend:** estado/review/consent/alternativa.
- **Reglas e invariantes:** portal ≠ API; revisión humana y ninguna afirmación falsa de cumplimiento.
- **Criterios de aceptación:** Dada una integración candidata, cuando se evalúa, entonces existe evidencia de go/no-go y una alternativa antes de incorporarla al backlog de implementación.
- **Casos negativos y bordes:** revoked access, undocumented endpoint, regulation change y no SLA.
- **Estrategia de pruebas:** spike/contrato/legal/seguridad y real reconciliation.
- **Observabilidad:** cost/health/value per adapter.
- **Seguridad y privacidad:** secretos, consent, transfer y least privilege.
- **Performance/capacidad y límites:** measured per adapter.
- **Feature flag, rollout, migración, rollback y recuperación:** independent flags/kill.
- **Documentación:** feasibility ADR/fuente.
- **Comandos/evidencia esperados:** evidencia posterior de pruebas aprobadas con el proveedor.
- **Definition of Ready:** official mechanism/business case.
- **Definition of Done:** integración priorizada o rechazada; ninguna dependencia para el MVP.
- **Bloqueos/preguntas:** Q-029/047/065.
- **Paralelizable:** sí, por integración candidata.
