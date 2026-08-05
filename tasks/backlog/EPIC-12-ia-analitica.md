# EPIC-12 — Analítica e IA explicable

Objetivo: KPI y alertas reproducibles e IA de solo lectura con evidencia, abstención, retroalimentación e interruptor de emergencia. R5–R6.

<a id="agro-ia-001"></a>

## AGRO-IA-001 — Servir tableros y KPI reproducibles por rol

- **Release, épica, prioridad y tamaño:** R5 · EPIC-12 · Must · L.
- **Owner y colaboradores:** Analítica; Dominio, Finanzas, Frontend y QA.
- **Resultado/valor esperado:** decisiones con fórmula/unidad/período/fuente/faltantes visibles.
- **Historia/JTBD:** Como productor, quiero comparar campo/lote/campaña/rodeo/período sin ceros falsos.
- **Alcance incluido:** tableros por rol, filtros, versión/fórmula/fuente/unidad/moneda/valuación/faltantes del KPI y proyecciones.
- **Fuera de alcance:** almacén analítico prematuro y aritmética generada por IA.
- **Requisitos trazados:** RF-ANA-001/002; RN-FIN-004; RNF-PER-001; RNF-UX-003.
- **Precondiciones y dependencias:** eventos/contratos de dominio y fórmulas aprobadas.
- **Contrato/API/eventos afectados:** consulta de tablero/KPI y estado de proyección.
- **Datos, índices, migración y compatibilidad:** proyecciones regenerables con fuente/versión; sin propiedad cruzada de tablas.
- **Autenticación, autorización, tenant y auditoría:** los filtros intersectan el alcance del usuario; autenticación reforzada al exportar datos sensibles.
- **Frontend:** tarjetas/tablas/gráficos responsivos con fórmula/fuente/faltantes y estados de carga/error/desactualizado.
- **Reglas e invariantes:** faltante≠0; reconstrucción desde la fuente; UUID corto.
- **Criterios de aceptación:** Dado un KPI, cuando se inspecciona, entonces fórmula/período/fuente/faltantes y alcance autorizado concilian con los totales de origen.
- **Casos negativos y bordes:** proyección desactualizada, mezcla de monedas, denominador vacío, evento tardío y campo revocado.
- **Estrategia de pruebas:** fórmulas, conciliación, autorización, performance, E2E y accesibilidad.
- **Observabilidad:** atraso de proyección, latencia de consulta, errores y tasa de faltantes.
- **Seguridad y privacidad:** agregación/minimización y cachés aisladas por tenant.
- **Performance/capacidad y límites:** objetivo de lectura p95≤400 ms; paginación y desglose.
- **Feature flag, rollout, migración, rollback y recuperación:** tablero por rol/tenant; reconstrucción de proyecciones.
- **Documentación:** catálogo de KPI y linaje de datos.
- **Comandos/evidencia esperados:** futuras pruebas de conciliación y performance del repositorio.
- **Definition of Ready:** fórmulas, owners y fuentes aprobados.
- **Definition of Done:** KPI conciliado y accesible.
- **Bloqueos/preguntas:** Q-048–051.
- **Paralelizable:** sí, por proyección con contrato común.

<a id="agro-ia-002"></a>

## AGRO-IA-002 — Centralizar alertas por excepción y vencimiento

- **Release, épica, prioridad y tamaño:** R5 · EPIC-12 · Must · M.
- **Owner y colaboradores:** Analítica/Notificaciones; owners de dominio, Frontend y QA.
- **Resultado/valor esperado:** bandeja por rol sin duplicar semántica de cada módulo.
- **Historia/JTBD:** Como usuario, quiero priorizar stock, atraso, sanidad, clima y revisión de rodeo.
- **Alcance incluido:** contrato/bandeja/acusar recibo/deduplicación/severidad/fuente/enlace profundo; las reglas permanecen en el módulo de origen.
- **Fuera de alcance:** WhatsApp, push o email sin resolver Q-023/Q-030 y cualquier acción autónoma.
- **Requisitos trazados:** RF-ANA-003; RF-INV-004; RF-CLI-005; RN-CLI-008; Q-023/030.
- **Precondiciones y dependencias:** alertas de módulos y alcance de identidad.
- **Contrato/API/eventos afectados:** publicación, consulta y acuse de alertas.
- **Datos, índices, migración y compatibilidad:** fuente/versión/recurso/estado de alerta.
- **Autenticación, autorización, tenant y auditoría:** reautorizar enlace profundo y recurso.
- **Frontend:** bandeja responsiva con estados vacío/desactualizado/error y etiquetas oficial/propio.
- **Reglas e invariantes:** el módulo es dueño del significado; no se infiere severidad oficial.
- **Criterios de aceptación:** Dada una alerta fuente, cuando el usuario carece de acceso al recurso, entonces no conoce alerta ni existencia; si está autorizado ve motivo/fuente/frescura.
- **Casos negativos y bordes:** duplicado, vencimiento, recurso revocado y fatiga de alertas.
- **Estrategia de pruebas:** contrato, autorización, deduplicación, E2E y revisión de utilidad.
- **Observabilidad:** creadas/vistas/acusadas/vencidas/rechazadas.
- **Seguridad y privacidad:** sin detalles sensibles en canales.
- **Performance/capacidad y límites:** paginación, tasa y deduplicación.
- **Feature flag, rollout, migración, rollback y recuperación:** primero dentro de la aplicación; flags separados por canal.
- **Documentación:** catálogo de alertas y owners.
- **Comandos/evidencia esperados:** futuras pruebas de contrato y E2E del repositorio.
- **Definition of Ready:** fuentes, severidad y enlaces profundos definidos.
- **Definition of Done:** alertas trazables dentro de la aplicación; canales pendientes explícitos.
- **Bloqueos/preguntas:** Q-023/030.
- **Paralelizable:** sí, por módulo.

<a id="agro-ia-003"></a>

## AGRO-IA-003 — Operar AI Gateway de solo lectura y paquetes de evidencia autorizados

- **Release, épica, prioridad y tamaño:** R6 · EPIC-12 · Must · L.
- **Owner y colaboradores:** IA; Identidad, AppSec, Documentos, SRE y QA.
- **Resultado/valor esperado:** proveedor intercambiable, citas exactas y cero mutación/fuga.
- **Historia/JTBD:** Como usuario, quiero preguntar sobre datos/documentos permitidos y verificar fuentes.
- **Alcance incluido:** gateway/política/cuota, autorización previa/posterior, recuperador por tenant, herramientas de solo lectura, `EvidencePack`/`RecommendationEnvelope` y validador.
- **Fuera de alcance:** SQL/navegación/fetch libre, herramientas de mutación y entrenamiento de modelo compartido sin consentimiento explícito.
- **Requisitos trazados:** RF-IA-001/002; RN-IA-001/003–006; RNF-PORT-002; ADR-004; Q-058/060.
- **Precondiciones y dependencias:** datos estructurados, Documentos, threat model de Seguridad y proveedor/privacidad aprobados.
- **Contrato/API/eventos afectados:** RecommendationRequest/EvidencePack/Envelope/EvaluationRecord.
- **Datos, índices, migración y compatibilidad:** versiones de modelo/prompt/recuperador/herramienta/perfil/datos y retención protegida.
- **Autenticación, autorización, tenant y auditoría:** reautorizar cada recurso/herramienta y antes de responder; denegación neutral.
- **Frontend:** citas/evidencia/supuestos/confianza/faltantes y estado de proveedor no disponible.
- **Reglas e invariantes:** ninguna dependencia crítica; ninguna mutación; documentos no confiables no instruyen al sistema.
- **Criterios de aceptación:** Dado un recurso revocado/de otro tenant o una inyección, cuando se consulta, entonces no hay recuperación/inferencia/acción y se registra señal de incidente.
- **Casos negativos y bordes:** permisos que cambian durante la conversación, cita falsa, timeout del proveedor y documento contaminado.
- **Estrategia de pruebas:** contrato/esquema, BOLA, red-team, fundamentación, inyección y resiliencia.
- **Observabilidad:** latencia/costo/tokens/denegaciones/esquema/citas sin prompts en logs.
- **Seguridad y privacidad:** DPA sin entrenamiento, minimización, allow-list de herramientas y caché aislada.
- **Performance/capacidad y límites:** cuotas por tenant/caso; objetivo de latencia desde DIS-007.
- **Feature flag, rollout, migración, rollback y recuperación:** sombra→piloto por tenant; interruptor por caso/modelo.
- **Documentación:** fichas de modelo/sistema, proveedor/DPA y threat model.
- **Comandos/evidencia esperados:** futura suite de evals y red-team.
- **Definition of Ready:** proveedor, datos, evals y retención aprobados.
- **Definition of Done:** cero fuga/mutación y gate de sobre válido.
- **Bloqueos/preguntas:** Q-058/060.
- **Paralelizable:** gateway, evidencia y evals con ownership separado.

<a id="agro-ia-004"></a>

## AGRO-IA-004 — Explicar clima y rotación desde herramientas determinísticas

- **Release, épica, prioridad y tamaño:** R6 · EPIC-12 · Must · L.
- **Owner y colaboradores:** IA; Clima, Pastoreo, Agrónomo, Veterinario y QA.
- **Resultado/valor esperado:** explicaciones/alternativas fieles a snapshots/fórmulas y abstención.
- **Historia/JTBD:** Como productor, quiero comprender riesgos climáticos y alternativas de pastoreo sin números inventados.
- **Alcance incluido:** explicación climática, resultados de herramientas determinísticas de pastoreo, perfiles/snapshots/fórmulas, alternativas y datos faltantes.
- **Fuera de alcance:** aritmética del LLM, ración/dosis/tratamiento y movimiento.
- **Requisitos trazados:** RF-IA-003–005/007; RN-IA-002–004/006; RN-GAN-008–014; ADR-004/005.
- **Precondiciones y dependencias:** CLI-001–004, FOR-003/004 e IA-003.
- **Contrato/API/eventos afectados:** DeterministicToolResult/HumanDecision/feedback.
- **Datos, índices, migración y compatibilidad:** referencias exactas a clima/perfil/fórmula/entrada/salida.
- **Autenticación, autorización, tenant y auditoría:** las herramientas reautorizan; decisión separada y auditada.
- **Frontend:** observado/estimado/insuficiente, citas/límites/alternativas y flujo de acción separado.
- **Reglas e invariantes:** ante dato obsoleto/inseguro se abstiene; aceptar no mueve; CAP no se altera.
- **Criterios de aceptación:** Dado un caso conocido, cuando se explica, entonces el resultado numérico coincide exactamente con la herramienta y un faltante de seguridad causa la abstención correcta.
- **Casos negativos y bordes:** sin biomasa/agua, perfil incompatible, clima obsoleto y cambio de proveedor/modelo.
- **Estrategia de pruebas:** exactitud determinística, evals por riesgo, E2E y aprobación profesional.
- **Observabilidad:** abstención, correcciones, fallos de herramienta y resultados posteriores.
- **Seguridad y privacidad:** coordenadas y datos de rodeo mínimos.
- **Performance/capacidad y límites:** alternativas/contexto acotados y cuota de costo.
- **Feature flag, rollout, migración, rollback y recuperación:** flags `ai-weather`/`ai-grazing`; interruptor/rollback independientes.
- **Documentación:** fichas de caso, límites y dataset de evals.
- **Comandos/evidencia esperados:** futura herramienta de evals y actas de aprobación.
- **Definition of Ready:** herramientas, perfiles y umbrales del dataset aprobados.
- **Definition of Done:** especialistas y Seguridad aprueban; el sistema funciona sin LLM.
- **Bloqueos/preguntas:** Q-021/027/031–042/059.
- **Paralelizable:** sí, por caso de manera independiente.

<a id="agro-ia-005"></a>

## AGRO-IA-005 — Evaluar, monitorear deriva y controlar rollout IA

- **Release, épica, prioridad y tamaño:** R6 · EPIC-12 · Must · L.
- **Owner y colaboradores:** IA/QA/SRE; AppSec, Privacidad y especialistas.
- **Resultado/valor esperado:** cambios de modelo/prompt/dataset/tool no llegan sin evidencia y reversión.
- **Historia/JTBD:** Como owner IA, quiero detectar regresión/costo/drift y apagar un caso.
- **Alcance incluido:** datasets versionados, métricas de evals por caso/riesgo, sombra/canary, retroalimentación/resultado, tableros/alertas e interruptor de emergencia.
- **Fuera de alcance:** promedio global que compensa un fallo crítico y escenarios avanzados de RF-IA-006.
- **Requisitos trazados:** RF-IA-007; RN-IA-006; RNF-OBS-001/002; ADR-004; Q-056–059.
- **Precondiciones y dependencias:** IA-003/004 y panel profesional.
- **Contrato/API/eventos afectados:** EvaluationRecord/model release/feedback.
- **Datos, índices, migración y compatibilidad:** versiones de todos los componentes y resultados de evaluación.
- **Autenticación, autorización, tenant y auditoría:** rollout de modelo privilegiado/auditado; datasets protegidos.
- **Frontend:** retroalimentación/corrección y estado/interrupción sin insinuar certeza.
- **Reglas e invariantes:** cero fuga entre tenants o mutaciones autónomas; fallos críticos de abstención bloquean rollout.
- **Criterios de aceptación:** Dado un cambio de modelo, cuando falla el gate de evals, entonces el rollout se detiene/revierte y la versión previa permanece disponible.
- **Casos negativos y bordes:** fuga del dataset, cambio silencioso de proveedor, pico de costo y desacuerdo profesional.
- **Estrategia de pruebas:** regresión, red-team, performance, costo, deriva y simulacro del interruptor.
- **Observabilidad:** fundamentación, citas, abstención, correcciones, latencia, costo y deriva.
- **Seguridad y privacidad:** gobierno/retención del dataset y sin entrenamiento por defecto.
- **Performance/capacidad y límites:** presupuesto, cuota y umbrales de latencia.
- **Feature flag, rollout, migración, rollback y recuperación:** sombra versionada→canary; interrupción inmediata por caso.
- **Documentación:** informe de evals, model card y registro de cambios.
- **Comandos/evidencia esperados:** comandos de evals aprobados en el repositorio cuando existan.
- **Definition of Ready:** dataset, métricas, umbrales y aprobadores definidos.
- **Definition of Done:** gates y simulacro de rollback aprobados.
- **Bloqueos/preguntas:** Q-056–059.
- **Paralelizable:** sí, por caso/dataset.

<a id="agro-ia-006"></a>

## AGRO-IA-006 — Simular escenarios avanzados con cálculo determinístico y aprobación humana

- **Release, épica, prioridad y tamaño:** R7 · EPIC-12 · Should · L.
- **Owner y colaboradores:** Analytics/AI; Agriculture, Livestock, Forage, Finance, Frontend, QA, AppSec y especialistas aprobadores.
- **Resultado/valor esperado:** comparar alternativas de rinde, costo, caja y forraje con supuestos visibles, sin delegar aritmética crítica ni decisiones al LLM.
- **Historia/JTBD:** Como productor o asesor, quiero comparar escenarios para decidir con evidencia y comprender incertidumbre, restricciones y datos faltantes.
- **Alcance incluido:** escenarios bajo/base/alto, herramientas determinísticas versionadas, supuestos editables, sensibilidad, comparación, evidencia, abstención y aprobación humana separada de la ejecución.
- **Fuera de alcance:** mutaciones autónomas, optimización prescriptiva sin perfil validado, promesas de resultado, cálculo financiero por LLM y automatización fiscal.
- **Requisitos trazados:** RF-IA-006; RN-IA-001–006; RN-GAN-008–014; RN-FIN-004; ADR-004/005/006; Q-031–042/048–051/056–059.
- **Precondiciones y dependencias:** AGRO-IA-003/004/005, KPI AGRO-IA-001, perfiles aprobados y herramientas determinísticas de los módulos de dominio.
- **Contrato/API/eventos afectados:** creación/consulta/comparación de escenario, paquete de evidencia, resultado de herramienta, abstención y registro de aprobación; ningún evento operativo de mutación.
- **Datos, índices, migración y compatibilidad:** escenario inmutable con versiones de datos/perfil/fórmula/modelo, moneda/unidad/período, rangos, faltantes, aprobador y expiración; migración aditiva.
- **Autenticación, autorización, tenant y auditoría:** autorización por cada recurso antes y después de recuperar contexto; herramientas reautorizan; aprobación privilegiada y auditada; aislamiento de caché e índice.
- **Frontend:** comparación responsiva y accesible, supuestos/fuentes/frescura/rangos destacados, estados de carga/vacío/error/desactualizado/degradado/conflicto y CTA de aprobación separado; UUID corto.
- **Reglas e invariantes:** el LLM explica pero no calcula; faltante no es cero; sin perfil o dato crítico se abstiene; riesgos de agua/sanidad/toxicidad bloquean recomendación ganadera; aprobación no ejecuta.
- **Criterios de aceptación:** Dado un escenario autorizado, cuando se recalcula con los mismos datos/versiones, entonces produce idénticos valores determinísticos y evidencia; si falta un dato o gate crítico, muestra incertidumbre o abstención y nunca crea una operación.
- **Casos negativos y bordes:** mezcla de moneda/unidad, perfil o tenant incorrecto, dato obsoleto, prompt injection, fuente revocada, costo extremo, divergencia de herramienta y especialista en desacuerdo.
- **Estrategia de pruebas:** unitarias/property-based de fórmulas, integración de herramientas/autorización, contrato del paquete de evidencia, E2E accesible, evals de abstención/citas, red-team, performance/costo y aprobación profesional.
- **Observabilidad:** escenarios por caso, abstención, datos faltantes, divergencias, correcciones, latencia/costo, fallos de autorización y resultado posterior; sin prompts ni datos sensibles en logs.
- **Seguridad y privacidad:** RAG autorizado, minimización, defensa contra inyección/exfiltración, retención definida, sin entrenamiento por defecto y kill switch por caso.
- **Performance/capacidad y límites:** contexto/alternativas acotados, cuotas por tenant/caso, cálculo asincrónico si excede el umbral y presupuesto aprobado antes de habilitar.
- **Feature flag, rollout, migración, rollback y recuperación:** flag por caso/perfil/tenant, sombra→canary, rollback a versión previa de herramienta/dataset y kill switch inmediato sin afectar cálculos transaccionales.
- **Documentación:** ficha del caso, fórmulas, fuentes, perfiles/jurisdicciones/aprobadores, límites, dataset/evals, model card y runbook.
- **Comandos/evidencia esperados:** comandos futuros aprobados del repositorio para fórmulas, integración, E2E, seguridad, evals y performance; informe firmado por especialistas.
- **Definition of Ready específica:** caso, fórmulas, unidades, datasets, umbrales, perfil, jurisdicción, aprobadores, presupuesto y comportamiento de abstención aprobados.
- **Definition of Done específica:** cálculo reproducible, autorización y abstención demostrados; gates QA/AppSec/profesionales aprobados; kill switch y rollback ensayados; cero mutación autónoma.
- **Bloqueos/preguntas abiertas:** `Q-031–042`, `Q-048–051` y `Q-056–059`; sin resolución se limita a escenarios no especializados o permanece deshabilitado.
- **Paralelizable:** sí por caso de uso tras AGRO-IA-003/005; fórmulas requieren contratos aprobados de cada módulo.
