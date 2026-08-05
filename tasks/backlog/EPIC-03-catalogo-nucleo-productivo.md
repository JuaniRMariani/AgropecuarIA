# EPIC-03 — Catálogo nacional y núcleo productivo

Objetivo: publicar/versionar catálogo y permitir que toda entrada complete el flujo común sin falsa especialización. R1; certificación completa R3.

<a id="agro-cat-001"></a>

## AGRO-CAT-001 — Ingerir fuentes y producir un diff editorial reproducible

- **Release, épica, prioridad y tamaño:** R1 · EPIC-03 · Must · L.
- **Owner y colaboradores:** Catalog/Data; Integrations, Domain, AppSec y QA.
- **Resultado/valor esperado:** cambios de fuentes llegan a staging con procedencia, conflictos y excepciones revisables.
- **Historia/JTBD:** Como editor, quiero comparar una fuente nueva antes de afectar el catálogo publicado.
- **Alcance incluido:** snapshot/hash, staging, normalización, alias, jerarquía, jurisdicción, deduplicación, diff y workflow editorial.
- **Fuera de alcance:** escritura directa a publicado, reglas técnicas y actualización sin revisión.
- **Requisitos trazados:** RF-CAT-001/002/005; RN-CAT-001/003/005; RNF-CAT-001/002; ADR-006; Q-062/063/066.
- **Precondiciones y dependencias:** DIS-001, FND-002 y owner editorial.
- **Contrato/API/eventos afectados:** source snapshot, import job, diff/conflict y approval.
- **Datos, índices, migración y compatibilidad:** códigos/hash/versiones; búsqueda normalizada; fuente+hash idempotente; staging separado.
- **Autenticación, autorización, tenant y auditoría:** rol editorial, carga/aprobación segregadas y cambios auditados.
- **Frontend:** bandeja de diff/conflictos responsive, loading/progreso/error y accesibilidad.
- **Reglas e invariantes:** 100 % procesado o excepción; valor de origen conservado; usado nunca se borra.
- **Criterios de aceptación:** Dada una fuente repetida/cambiada, cuando se ingiere, entonces no duplica y produce el mismo diff con conflictos explícitos.
- **Casos negativos y bordes:** encoding, códigos faltantes, alias circular, duplicado trans-fuente, fuente corrupta y job interrumpido.
- **Estrategia de pruebas:** fixtures por fuente, idempotencia, diff/rollback, seguridad editorial y performance.
- **Observabilidad:** duración, filas, conflictos/excepciones, último éxito/frescura y backlog.
- **Seguridad y privacidad:** archivos no confiables, schema/size/hash y supply-chain de fuente.
- **Performance/capacidad y límites:** 100 % baseline dentro de ventana acordada; job async/reanudable.
- **Feature flag, rollout, migración, rollback y recuperación:** staging sin impacto; reintento/conciliación; purge controlado de temporales.
- **Documentación:** mapeos/fuentes, reglas de conflicto y runbook.
- **Comandos/evidencia esperados:** suite de ingesta/diff definida por el futuro repositorio y reporte de cobertura.
- **Definition of Ready:** schemas/fuentes/owners/excepciones acordados.
- **Definition of Done:** diff reproducible y aprobado sin tocar versión activa.
- **Bloqueos/preguntas:** Q-062/063/066.
- **Paralelizable:** sí, con CAT-002 UX y CAT-004 extensión mediante contratos.

<a id="agro-cat-002"></a>

## AGRO-CAT-002 — Publicar, buscar y revertir Catálogo Nacional v1

- **Release, épica, prioridad y tamaño:** R1 · EPIC-03 · Must · L.
- **Owner y colaboradores:** Catalog; Frontend, Data, Domain, QA y SRE.
- **Resultado/valor esperado:** catálogo inmutable, rápido y transparente con rollback lógico.
- **Historia/JTBD:** Como productor, quiero encontrar una actividad y comprender fuente, vigencia y soporte.
- **Alcance incluido:** publicación atómica, búsqueda por código/nombres/alias/tildes, soporte/jurisdicción, inactivación/sucesión y versión activa.
- **Fuera de alcance:** especialización automática y eliminación de histórico.
- **Requisitos trazados:** RF-CAT-001–005; RN-CAT-001–005; RNF-CAT-001/002; ADR-006; Q-064/065.
- **Precondiciones y dependencias:** CAT-001 y baseline aprobado.
- **Contrato/API/eventos afectados:** query/search/detail/publish/rollback; `ProductCatalogPublished`.
- **Datos, índices, migración y compatibilidad:** versión inmutable, códigos estables, alias/jerarquía e índices de búsqueda medidos.
- **Autenticación, autorización, tenant y auditoría:** lectura baseline autenticada; publish/rollback privilegiado y auditado.
- **Frontend:** search/detail/badges, fuente/vigencia/capacidades ausentes, responsive/loading/empty/error/stale.
- **Reglas e invariantes:** soporte visible; rollback cambia activo, no reescribe ciclos/históricos.
- **Criterios de aceptación:** Dado v1, cuando se busca por científico/regional/código sin tilde, entonces encuentra entrada correcta y muestra nivel; rollback conserva referencias.
- **Casos negativos y bordes:** alias ambiguo, entrada inactiva, publicación concurrente y cliente con versión anterior.
- **Estrategia de pruebas:** búsqueda, baseline total, publish atomicity, rollback, compatibilidad y a11y.
- **Observabilidad:** consultas sin resultado, latencia, versión activa, publish/rollback y errores.
- **Seguridad y privacidad:** no mezclar extensiones tenant; aprobación segregada.
- **Performance/capacidad y límites:** presupuesto de búsqueda/paginación con baseline completo.
- **Feature flag, rollout, migración, rollback y recuperación:** `catalog-v1`; canary interno→piloto; rollback a versión anterior.
- **Documentación:** changelog, atribuciones y matriz soporte.
- **Comandos/evidencia esperados:** pruebas de búsqueda/publicación y reporte 100 %.
- **Definition of Ready:** diff aprobado y UI de soporte validada.
- **Definition of Done:** v1 publicado/reversible con evidencia.
- **Bloqueos/preguntas:** Q-064 confirmación UX; no bloquea separación ya decidida para discovery.
- **Paralelizable:** no con publicación de otra versión; sí con CAT-003/004.

<a id="agro-cat-003"></a>

## AGRO-CAT-003 — Registrar cualquier actividad mediante el núcleo común

- **Release, épica, prioridad y tamaño:** R1/R3 · EPIC-03 · Must · L.
- **Owner y colaboradores:** Productive Core; Frontend, Agriculture, Livestock, Finance, Documents y QA.
- **Resultado/valor esperado:** toda entrada completa unidad→ciclo→evento→salida/costo/documento/timeline.
- **Historia/JTBD:** Como productor, quiero registrar una producción aunque no tenga automatización especializada.
- **Alcance incluido:** ManagementUnit configurable, asignación, ciclo, eventos/cantidades/unidades, productos/pérdidas, responsables, costos/documentos y timeline.
- **Fuera de alcance:** formularios/KPI/reglas por cultivo/especie sin perfil.
- **Requisitos trazados:** RF-PRD-001–003/005; RN-PRD-001/002/005; RN-CORE-002/003/007; RNF-CAT-003; ADR-006.
- **Precondiciones y dependencias:** CAT-002, FND-002/003 y contratos Document/Finance mínimos.
- **Contrato/API/eventos afectados:** create unit/cycle/event/output; `ProductionCycleStarted/EventRecorded`.
- **Datos, índices, migración y compatibilidad:** comunes tipados; referencias versionadas a catálogo/perfil/geometría; atributos configurables validados.
- **Autenticación, autorización, tenant y auditoría:** alcance por campo/unidad/recurso y rectificación auditable.
- **Frontend:** UI schema-driven, soporte/capacidades ausentes, estados completos y UUID corto.
- **Reglas e invariantes:** unidad apropiada, original/conversión conservados, genérico no hereda especialización.
- **Criterios de aceptación:** Dado cada entrada v1, cuando la suite la recorre, entonces completa flujo común o excepción aprobada, sin regla de otro perfil.
- **Casos negativos y bordes:** unidad sin geometría, mezcla/consociación, evento fuera de vigencia, schema inválido y ciclo inactivo.
- **Estrategia de pruebas:** suite parametrizada baseline, property unidades/fechas, tenant, contrato y E2E por familia.
- **Observabilidad:** ciclos/eventos/errores por familia y capacidad ausente; sin etiquetas tenant de alta cardinalidad.
- **Seguridad y privacidad:** autorizaciones por recurso y atributos/schema no ejecutables.
- **Performance/capacidad y límites:** paginación/timeline; baseline total dentro de gate R3.
- **Feature flag, rollout, migración, rollback y recuperación:** `generic-flow`; schemas versionados; rollback mantiene registro común.
- **Documentación:** contrato de núcleo, tipos de unidad y matriz de fixtures.
- **Comandos/evidencia esperados:** suite parametrizada y trazabilidad del baseline cuando exista runner.
- **Definition of Ready:** tipos/unidades/estados/contratos acordados.
- **Definition of Done:** 100 % baseline certificado en R3; muestra representativa operativa en R1.
- **Bloqueos/preguntas:** baseline/denominador de DIS-001.
- **Paralelizable:** sí por frontend/backend/tests con contratos; no dividir por especie.

<a id="agro-cat-004"></a>

## AGRO-CAT-004 — Gestionar extensiones privadas y propuestas editoriales

- **Release, épica, prioridad y tamaño:** R1 · EPIC-03 · Must · M.
- **Owner y colaboradores:** Catalog; Identity, Frontend, AppSec y QA.
- **Resultado/valor esperado:** tenant registra una actividad local sin alterar línea base ni otro tenant.
- **Historia/JTBD:** Como admin, necesito agregar/mapear una entrada local y proponerla al catálogo.
- **Alcance incluido:** alta privada, alias/código/unidad/fuente, mapping, propuesta editorial, inactivación y sucesión.
- **Fuera de alcance:** publicación automática y campos libres sin schema.
- **Requisitos trazados:** RF-CAT-004/005; RF-ADM-001; RN-CAT-004/005; RN-CORE-001; ADR-006.
- **Precondiciones y dependencias:** CAT-002, ID-003 y workflow editorial.
- **Contrato/API/eventos afectados:** tenant entry/map/propose/review.
- **Datos, índices, migración y compatibilidad:** capa tenant separada; claves compuestas y mapping estable.
- **Autenticación, autorización, tenant y auditoría:** admin tenant propone; editor nacional decide; todo auditado.
- **Frontend:** alta/mapping/propuesta con estados pendiente/rechazada/publicada/conflicto.
- **Reglas e invariantes:** nunca sobrescribe baseline/otro tenant; usada se inactiva.
- **Criterios de aceptación:** Dadas org A/B, cuando A crea entrada, entonces B no la ve y una propuesta no cambia v1 hasta publicación aprobada.
- **Casos negativos y bordes:** duplicado baseline, mapping circular, propuesta simultánea y tenant eliminado.
- **Estrategia de pruebas:** aislamiento, workflow, búsqueda/mapping y rollback.
- **Observabilidad:** propuestas/conflictos/edad y abuso sin exponer nombres privados.
- **Seguridad y privacidad:** BOLA/RLS y sanitización de contenido.
- **Performance/capacidad y límites:** cuotas por tenant y búsqueda combinada eficiente.
- **Feature flag, rollout, migración, rollback y recuperación:** flag tenant entries; rollback inactiva/mantiene históricos.
- **Documentación:** guía admin/editor.
- **Comandos/evidencia esperados:** suite tenant/editorial futura.
- **Definition of Ready:** permisos/estados/mapping definidos.
- **Definition of Done:** flujo completo sin contaminación.
- **Bloqueos/preguntas:** owner editorial Q-062.
- **Paralelizable:** sí con CAT-003.

<a id="agro-cat-005"></a>

## AGRO-CAT-005 — Activar perfiles especializados con abstención segura

- **Release, épica, prioridad y tamaño:** R1–R4 · EPIC-03 · Must · L.
- **Owner y colaboradores:** Catalog/Profile Governance; Domain experts, Agriculture, Livestock, AI, Frontend y QA.
- **Resultado/valor esperado:** reglas/formularios/KPI/IA solo para perfil compatible y aprobado.
- **Historia/JTBD:** Como especialista, quiero versionar una capacidad y limitarla a actividad/sistema/jurisdicción validados.
- **Alcance incluido:** schema/profile version, fuente/aprobador, compatibilidad, publicación/rollback, capabilities y abstención.
- **Fuera de alcance:** prescripción inventada, copia entre perfiles y una tarea por cultivo/especie.
- **Requisitos trazados:** RF-PRD-004/005; RN-PRD-003/004; ADR-004/006; Q-042/043/062–065.
- **Precondiciones y dependencias:** DIS-002, CAT-002/003 y panel profesional.
- **Contrato/API/eventos afectados:** ProfileVersion/capability resolution; `ActivityProfileActivated`.
- **Datos, índices, migración y compatibilidad:** schema inmutable/versionado y referencias congeladas en eventos/recomendaciones.
- **Autenticación, autorización, tenant y auditoría:** publicación privilegiada con separación/revalidación nominada.
- **Frontend:** nivel/capacidades/fuente/jurisdicción/aprobador visibles; fallback a genérico.
- **Reglas e invariantes:** cinco dimensiones obligatorias; incompatibilidad/versión vencida = abstención.
- **Criterios de aceptación:** Dada actividad sin perfil compatible, cuando solicita especialización, entonces ve capacidad ausente y conserva flujo genérico sin regla contaminada.
- **Casos negativos y bordes:** jurisdicción superpuesta, perfil revocado, schema breaking y aprobador vencido.
- **Estrategia de pruebas:** contract/schema, no contaminación, rollback, E2E y aprobación profesional.
- **Observabilidad:** activaciones/abstenciones/incompatibilidades y uso por versión.
- **Seguridad y privacidad:** firma/hash/aprobación; payload no ejecuta instrucciones.
- **Performance/capacidad y límites:** resolución de perfil cacheable con invalidación/versiones.
- **Feature flag, rollout, migración, rollback y recuperación:** flag por profile-version/tenant; rollback para nuevas acciones conserva historia.
- **Documentación:** profile card, changelog, fuentes y evals.
- **Comandos/evidencia esperados:** suite de schemas/perfiles y acta profesional.
- **Definition of Ready:** actividad/perfil/versión/jurisdicción/aprobador completos.
- **Definition of Done:** publicación/abstención/rollback demostrados.
- **Bloqueos/preguntas:** Q-042/043/062–065.
- **Paralelizable:** sí por perfil con archivos/owners disjuntos, no sin gobierno común.
