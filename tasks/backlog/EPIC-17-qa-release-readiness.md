# EPIC-17 — QA transversal y preparación para releases

Objetivo: trazabilidad, fixtures/suites por riesgo y gate independiente para cada release.

<a id="agro-qa-001"></a>

## AGRO-QA-001 — Mantener trazabilidad, fixtures y arquitectura de pruebas

- **Release, épica, prioridad y tamaño:** R0/R1 · EPIC-17 · Must · L.
- **Owner y colaboradores:** Principal QA; todos los equipos y panel profesional.
- **Resultado/valor esperado:** cada requisito tiene criterio, prueba y evidencia, con datos sintéticos estables.
- **Historia/JTBD:** Como QA, quiero detectar gaps antes de implementar y reproducir fallos de forma segura.
- **Alcance incluido:** taxonomía de pruebas, IDs, severidad, evidencia, dos tenants, baseline, 24 puntos, campo mixto, clima/CAP, animales/pasturas, multimoneda y archivos/IA hostiles.
- **Fuera de alcance:** PII real y porcentajes de cobertura arbitrarios.
- **Requisitos trazados:** todos los RF/RN/RNF/ADR; docs/09; Q-019/020/060/062.
- **Precondiciones y dependencias:** DIS-001/002/007 y task backlog.
- **Contrato/API/eventos afectados:** prueba/evidencia IDs y fixture schemas.
- **Datos, índices, migración y compatibilidad:** fixtures sintéticos versionados y aislados por prueba/tenant.
- **Autenticación, autorización, tenant y auditoría:** fixtures negativos y matriz para todas las rutas.
- **Frontend:** viewport/keyboard/reader/estado matrices.
- **Reglas e invariantes:** pruebas omitidas o flaky no aparecen silenciosamente en verde; la evidencia profesional y automatizada permanece separada.
- **Criterios de aceptación:** Dado el inventario de requisitos, cuando se audita, entonces cada elemento se vincula con tarea, prueba y release o con una excepción justificada.
- **Casos negativos y bordes:** IDs duplicados, fixture desactualizado, oráculo desconocido y owner faltante.
- **Estrategia de pruebas:** meta-validation of matrix/links/IDs y fixture review.
- **Observabilidad:** suite duration/flaky/failures/evidencia age.
- **Seguridad y privacidad:** synthetic/anonymized solo y hostile fixtures controlled.
- **Performance/capacidad y límites:** dataset representativo de Q-020.
- **Feature flag, rollout, migración, rollback y recuperación:** fixtures/version rollback; no production flag.
- **Documentación:** prueba catalog/datos cards/gates.
- **Comandos/evidencia esperados:** comandos del runner aprobados por el repositorio cuando exista.
- **Definition of Ready:** inventory/owners/oracles.
- **Definition of Done:** trazabilidad completa y fixtures reutilizables.
- **Bloqueos/preguntas:** Q-019/020/060/062.
- **Paralelizable:** fixture domains con QA ownership.

<a id="agro-qa-002"></a>

## AGRO-QA-002 — Ejecutar suites funcionales, contractuales y no funcionales

- **Release, épica, prioridad y tamaño:** R1–R6 · EPIC-17 · Must · L.
- **Owner y colaboradores:** QA/Automatización de Pruebas; Dominio, Plataforma, AppSec y profesionales.
- **Resultado/valor esperado:** una pirámide basada en riesgo demuestra invariantes, integraciones, E2E, accesibilidad, seguridad, performance y restore.
- **Historia/JTBD:** Como equipo, quiero retroalimentación rápida y determinística, con evidencia más profunda para releases.
- **Alcance incluido:** static/unit/property, PostgreSQL/PostGIS/almacenamiento/outbox, proveedor contratos, API/BOLA, component/E2E, GIS/climate/AI evals, a11y/perf/resilience/restore.
- **Fuera de alcance:** mocks que sustituyan semántica real crítica y gates de WRF/adaptadores antes del go.
- **Requisitos trazados:** RNF-REL/PER/CON/SEC/PRI/UX/OBS/PORT/CAT/GEO; todos los RF/RN mediante sus tareas.
- **Precondiciones y dependencias:** QA-001 y slices/entornos operativos.
- **Contrato/API/eventos afectados:** contrato snapshots/evidencia reports.
- **Datos, índices, migración y compatibilidad:** real isolated DB/almacenamiento plus versionado fixtures.
- **Autenticación, autorización, tenant y auditoría:** BOLA/RLS/trabajos/caché/archivos/exportaciones/IA en cada suite.
- **Frontend:** WCAG manual+auto, mobile/desktop/estados/performance.
- **Reglas e invariantes:** propiedades de stock, dinero/unidades, historia GIS, pastoreo, idempotencia y ausencia de contaminación.
- **Criterios de aceptación:** Dado un release candidate, cuando se ejecutan las suites, entonces se cumplen los objetivos aplicables y cualquier infraestructura bloqueada queda explícita, nunca en verde.
- **Casos negativos y bordes:** 429/500/timeout/crash/replay/concurrency/desactualizado/malware/injection.
- **Estrategia de pruebas:** itself; PR/scheduled/release suites con quarantine policy.
- **Observabilidad:** suite health/flaky/duration/coverage by requirement.
- **Seguridad y privacidad:** scanners/red-team/datos hygiene.
- **Performance/capacidad y límites:** reproducible environment/datos/perfil y RNF targets.
- **Feature flag, rollout, migración, rollback y recuperación:** pruebas con el flag habilitado/deshabilitado y migración N/N-1/rollback.
- **Documentación:** evidencia per task/release.
- **Comandos/evidencia esperados:** comandos exactos de las plataformas reales .NET, Next.js y de pruebas; nunca inferidos.
- **Definition of Ready:** executable slice/environment/oracle.
- **Definition of Done:** evidencia retenida y fallos corregidos o aceptados por la autoridad correspondiente.
- **Bloqueos/preguntas:** oráculos y volúmenes dependientes de preguntas pendientes.
- **Paralelizable:** suites by domain, integrated E2E/readiness controlled.

<a id="agro-qa-003"></a>

## AGRO-QA-003 — Emitir una evaluación independiente de preparación para release

- **Release, épica, prioridad y tamaño:** cada release · EPIC-17 · Must · M.
- **Owner y colaboradores:** Principal QA; Producto, SRE, AppSec, Arquitectura y aprobadores profesionales.
- **Resultado/valor esperado:** go/no-go objetivo con cobertura, defectos, riesgos residuales y evidencia de rollback.
- **Historia/JTBD:** Como sponsor, quiero decidir el release según evidencia, no optimismo.
- **Alcance incluido:** requirement coverage, entry/exit, defect severity, seguridad/a11y/perf/restore, flags/migrations/runbooks y sign-offs.
- **Fuera de alcance:** aceptar riesgos críticos sin autoridad del sponsor y contabilizar pruebas futuras.
- **Requisitos trazados:** gates de docs/09/10; todos los Must/RN/RNF; excepciones EXC-001–003.
- **Precondiciones y dependencias:** QA-001/002 y tareas del release completas.
- **Contrato/API/eventos afectados:** release evidencia manifest/decision.
- **Datos, índices, migración y compatibilidad:** migration/backup/rollback evidencia referenced.
- **Autenticación, autorización, tenant y auditoría:** zero known tenant failures/high-critical.
- **Frontend:** WCAG/responsivo/estados evidencia.
- **Reglas e invariantes:** ninguna tarea XL, sin owner, sin criterios o sin pruebas; los flags condicionados por proveedor/perfil permanecen apagados.
- **Criterios de aceptación:** Dado un candidato, cuando finaliza la revisión independiente, entonces la decisión enumera controles aprobados, fallidos, bloqueados y riesgos aceptados con sus owners.
- **Casos negativos y bordes:** proveedor no disponible, restore omitido, aprobación vencida y formato del contador pendiente.
- **Estrategia de pruebas:** auditoría matrix/artifacts plus targeted reruns.
- **Observabilidad:** SLI posteriores al release y disparador de rollback definidos.
- **Seguridad y privacidad:** AppSec/Privacy sign-off or explicit block.
- **Performance/capacidad y límites:** evidencia representativa contra los objetivos definidos.
- **Feature flag, rollout, migración, rollback y recuperación:** inventario, owner y retiro de flags, con simulacro obligatorio.
- **Documentación:** informe de preparación, notas del release y aceptación de riesgos.
- **Comandos/evidencia esperados:** lista de comandos ejecutados, resultados y artefactos.
- **Definition of Ready:** candidate/evidencia frozen.
- **Definition of Done:** go/no-go firmado y trazabilidad actualizada.
- **Bloqueos/preguntas:** cualquier elemento crítico sin resolver.
- **Paralelizable:** análisis de revisión en paralelo; decisión final integrada.
