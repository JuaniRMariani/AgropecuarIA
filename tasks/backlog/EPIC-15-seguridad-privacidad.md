# EPIC-15 — Seguridad y privacidad

Objetivo: modelo de amenazas, aislamiento, aplicación/archivos/IA seguros y privacidad argentina como gates continuos.

<a id="agro-sec-001"></a>

## AGRO-SEC-001 — Mantener el modelo de amenazas y la clasificación por release

- **Release, épica, prioridad y tamaño:** R0–R6 · EPIC-15 · Must · M.
- **Owner y colaboradores:** AppSec/Privacidad; Arquitectura, Producto, Legal, todos los equipos y QA.
- **Resultado/valor esperado:** activos/fronteras/abusos/mitigaciones trazados antes de abrir nuevas superficies.
- **Historia/JTBD:** Como owner de seguridad, quiero evaluar cada slice y proveedor según su impacto real.
- **Alcance incluido:** clases de datos, fronteras de confianza, capacidades del atacante, rutas de abuso, owner, riesgo residual y evaluación de privacidad.
- **Fuera de alcance:** certificación legal automática y controles futuros de ARCA.
- **Requisitos trazados:** RNF-SEC-001–003; RNF-PRI-001; ADR-001–006; Q-054/055/058/060.
- **Precondiciones y dependencias:** arquitectura, flujos de datos e inventario de proveedores.
- **Contrato/API/eventos afectados:** seguridad requirements y auditoría eventos per surface.
- **Datos, índices, migración y compatibilidad:** datos inventory/retention/processing regions.
- **Autenticación, autorización, tenant y auditoría:** revisar todas las transiciones de confianza.
- **Frontend:** privacidad/seguridad estados/consent/error no dark patterns.
- **Reglas e invariantes:** un riesgo crítico abierto bloquea la capacidad afectada; la interpretación legal requiere revisión humana.
- **Criterios de aceptación:** Dada una nueva frontera o proveedor, cuando se revisa la Definition of Ready, entonces amenazas, controles, pruebas, owner y riesgo residual están enlazados.
- **Casos negativos y bordes:** admin abuse, insider, third party breach, backup/export y AI.
- **Estrategia de pruebas:** threat-informed cases y abuse regression.
- **Observabilidad:** control signals/alertas mapped, no sensitive payload.
- **Seguridad y privacidad:** la tarea es el propio gate; se revisan AAIP, DPA y transferencias.
- **Performance/capacidad y límites:** límites de abuso, tasa y DoS definidos.
- **Feature flag, rollout, migración, rollback y recuperación:** go/no-go by capability; IR/kill.
- **Documentación:** threat modelo/privacidad assessment/risk acceptance.
- **Comandos/evidencia esperados:** futuros informes de escaneo y pruebas, más revisión manual.
- **Definition of Ready:** datos flow/assets/proveedores.
- **Definition of Done:** ninguna amenaza crítica sin owner.
- **Bloqueos/preguntas:** Q-054/055/058/060.
- **Paralelizable:** revisión por slice; integración final centralizada.

<a id="agro-sec-002"></a>

## AGRO-SEC-002 — Probar aislamiento por tenant y autorización exhaustiva

- **Release, épica, prioridad y tamaño:** R1–R6 · EPIC-15 · Must · L.
- **Owner y colaboradores:** AppSec/Identidad/Datos; todos los módulos, QA y SRE.
- **Resultado/valor esperado:** cero accesos conocidos entre tenants en API, RLS, trabajos, caché, almacenamiento, exportaciones e IA.
- **Historia/JTBD:** Como cliente, necesito que otro tenant no vea ni infiera mis recursos.
- **Alcance incluido:** matriz BOLA, autorización de aplicación, FORCE RLS/pool, claves compuestas, cachés, trabajos, URLs, proyecciones y retrieval.
- **Fuera de alcance:** confiar en el UUID abreviado o en ocultar elementos de la UI.
- **Requisitos trazados:** RF-ID-005; RN-CORE-001; RNF-SEC-002; RN-IA-001; ADR-PEND-007.
- **Precondiciones y dependencias:** ID-003, FND-002 y module recurso policies.
- **Contrato/API/eventos afectados:** authorization decision/neutral errors/auditoría.
- **Datos, índices, migración y compatibilidad:** tenant en todos los datos propios y excepciones explícitas de alcance de plataforma.
- **Autenticación, autorización, tenant y auditoría:** focus; denied attempts redacted/auditado.
- **Frontend:** errores 403/404 neutros; el cambio de contexto no conserva datos obsoletos.
- **Reglas e invariantes:** server derives tenant; no existence leak; role sin BYPASSRLS.
- **Criterios de aceptación:** Dadas las organizaciones A y B, cuando una ruta de acceso usa un ID de B bajo el contexto de A, entonces no filtra datos ni existencia mediante caché, trabajo o retrieval.
- **Casos negativos y bordes:** reutilización del pool, trabajo sin contexto, URL firmada, exportación y permiso revocado durante un chat.
- **Estrategia de pruebas:** property/integration/API/E2E/DAST/manual BOLA.
- **Observabilidad:** denied anomalies sin sensitive identifiers.
- **Seguridad y privacidad:** zero failures release gate.
- **Performance/capacidad y límites:** auth/RLS overhead measured.
- **Feature flag, rollout, migración, rollback y recuperación:** no capability flag on until suite passes; incident response.
- **Documentación:** authorization matrix/RLS ADR.
- **Comandos/evidencia esperados:** futuras tenant suite/DAST reports.
- **Definition of Ready:** recurso/action policies.
- **Definition of Done:** todas las rutas superan la suite de cero fugas.
- **Bloqueos/preguntas:** taxonomía de recursos con alcance de plataforma y Q-055.
- **Paralelizable:** por módulo, con fixtures centrales.

<a id="agro-sec-003"></a>

## AGRO-SEC-003 — Endurecer API, archivos, egress y cadena de suministro

- **Release, épica, prioridad y tamaño:** R1–R6 · EPIC-15 · Must · L.
- **Owner y colaboradores:** AppSec/Platform; Documents, Integrations, Frontend y QA.
- **Resultado/valor esperado:** prevenir inyección, SSRF, malware, filtración de secretos y compromiso de dependencias.
- **Historia/JTBD:** Como operador, quiero entradas no confiables acotadas y una cadena de build segura.
- **Alcance incluido:** server validation, CSRF/CORS/CSP/headers/TLS, limitación de tasas, parameterized queries, archivo pipeline, SSRF/webhook replay, secretos, SAST/SCA/SBOM/secretos/DAST.
- **Fuera de alcance:** fetch arbitrario de URLs y excepciones para vulnerabilidades altas/críticas sin aceptación autorizada.
- **Requisitos trazados:** RNF-SEC-001/002; RN-CORE-005/008; RF-DOC-001; RF-ADM-004.
- **Precondiciones y dependencias:** SEC-001 y platform pipeline.
- **Contrato/API/eventos afectados:** seguridad headers/limits/webhook/archivo estados.
- **Datos, índices, migración y compatibilidad:** esquemas seguros, parametrización y referencias a secretos.
- **Autenticación, autorización, tenant y auditoría:** endpoints/webhooks/archivos reauthorized y auditado.
- **Frontend:** errores seguros, sin secretos y compatibles con CSP.
- **Reglas e invariantes:** un payload no confiable nunca altera reglas ni prompts; cero vulnerabilidades altas/críticas abiertas.
- **Criterios de aceptación:** Dado SSRF, malware, inyección o una dependencia vulnerable, cuando se ejecuta el gate, entonces se bloquea el request o release y se conserva la evidencia.
- **Casos negativos y bordes:** loopback/metadatos/DNS rebinding/redirect, polyglot/zip bomb y webhook replay.
- **Estrategia de pruebas:** SAST/SCA/DAST/secreto/SBOM, manual, archivo/SSRF fixtures.
- **Observabilidad:** abuse/rate/scan alertas sin payload.
- **Seguridad y privacidad:** focus; patch SLA y IR.
- **Performance/capacidad y límites:** cuotas para requests, archivos, geometrías, importaciones e IA.
- **Feature flag, rollout, migración, rollback y recuperación:** WAF/limits gradual; emergency disable/rotate.
- **Documentación:** secure coding/egress/archivo/supply-chain policies.
- **Comandos/evidencia esperados:** scanners y pruebas configurados cuando exista el repositorio.
- **Definition of Ready:** threat/limits/herramientas.
- **Definition of Done:** gate green, zero high/critical.
- **Bloqueos/preguntas:** proveedores/herramientas/limits.
- **Paralelizable:** continuamente, por superficie.

<a id="agro-sec-004"></a>

## AGRO-SEC-004 — Operar privacidad, retención y respuesta a incidentes

- **Release, épica, prioridad y tamaño:** R1–R6 · EPIC-15 · Must · L.
- **Owner y colaboradores:** Privacy/AppSec; Legal, Platform, Product y QA.
- **Resultado/valor esperado:** derechos, subencargados, transferencias, retención y respuesta a incidentes aprobados antes de producción.
- **Historia/JTBD:** Como responsable, quiero cumplir finalidad, minimización y confidencialidad, y responder a incidentes.
- **Alcance incluido:** aviso de privacidad, responsable/encargado, revisión AAIP, DPA, subencargados, regiones, retención, legal hold, purga, no-training por defecto, roles de respuesta, evidencia, comunicaciones y simulacro.
- **Fuera de alcance:** asesoramiento legal generado por el producto y secretos de ARCA.
- **Requisitos trazados:** RNF-PRI-001; RNF-SEC-001/003; RNF-REL-002/003; RN-IA-006; Q-058/060.
- **Precondiciones y dependencias:** SEC-001, contratos de DOC-001/002 e inventario de proveedores.
- **Contrato/API/eventos afectados:** privacidad requests/retention/incident signals.
- **Datos, índices, migración y compatibilidad:** classification/retention/hold/purge/backup handling.
- **Autenticación, autorización, tenant y auditoría:** privacidad/admin actions autenticación reforzada/auditado.
- **Frontend:** aviso y ejercicio de derechos claros, con estados sin dark patterns.
- **Reglas e invariantes:** no usar datos para entrenamiento compartido sin opt-in; el legal hold prevalece sobre la purga.
- **Criterios de aceptación:** Dada la preparación para producción, cuando se revisan los gates de privacidad y respuesta a incidentes, entonces contratos, regiones, derechos, retención y evidencia del simulacro están aprobados.
- **Casos negativos y bordes:** transferencia internacional, restore de backup tras una supresión e incidente en un subencargado.
- **Estrategia de pruebas:** rights E2E, purge/hold/restore y incident tabletop.
- **Observabilidad:** SLA de solicitudes de privacidad y métricas redactadas de respuesta a incidentes.
- **Seguridad y privacidad:** focus; legal approval explicit.
- **Performance/capacidad y límites:** retention/cost impact measured.
- **Feature flag, rollout, migración, rollback y recuperación:** proveedor/case blocked until approval; IR kill/revoke.
- **Documentación:** policies/DPA/IR/runbooks.
- **Comandos/evidencia esperados:** futuros simulacros e informes; no inventar scripts.
- **Definition of Ready:** owners/legal/proveedor inventory.
- **Definition of Done:** gate de privacidad/respuesta a incidentes para producción aprobado.
- **Bloqueos/preguntas:** Q-058/060.
- **Paralelizable:** workstreams legales y de proveedores; gate integrado.
