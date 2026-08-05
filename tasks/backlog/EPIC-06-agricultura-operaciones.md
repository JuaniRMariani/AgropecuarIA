# EPIC-06 — Operaciones y agricultura

Objetivo: campaña agrícola desde plan y orden hasta cosecha, stock, costo e historial. Kernel operativo R2; especialización R3.

<a id="agro-agr-001"></a>

## AGRO-AGR-001 — Planificar campañas, presupuestos y órdenes aprobables

- **Release, épica, prioridad y tamaño:** R2/R3 · EPIC-06 · Must · L.
- **Owner y colaboradores:** Operations/Agriculture; Frontend, Catalog, GIS, Inventory, Finance y QA.
- **Resultado/valor esperado:** plan y orden con actor, recursos, estados y presupuesto trazables.
- **Historia/JTBD:** Como agrónomo, quiero planificar una campaña y aprobar trabajos por lote/ciclo.
- **Alcance incluido:** campaña/ciclo, plan/presupuesto, orden, responsable/contratista/fecha/lote/insumo/activo y state machine.
- **Fuera de alcance:** calendario/carga masiva Should y reglas no aprobadas.
- **Requisitos trazados:** RF-OPS-001–003; RF-AGR-001/011; RN-AGR-001; Q-013/043/044.
- **Precondiciones y dependencias:** CAT-003/005, GIS-002 e ID-003.
- **Contrato/API/eventos afectados:** campaign/plan/work-order approve/schedule/cancel; `WorkOrderApproved`.
- **Datos, índices, migración y compatibilidad:** tenant/campaign/unit/profile/version/status/budget; ETag e historial.
- **Autenticación, autorización, tenant y auditoría:** crear/aprobar separados según rol/alcance; auditoría.
- **Frontend:** wizard/lista/calendar básico, loading/empty/error/conflict, responsive y UUID corto.
- **Reglas e invariantes:** ciclo conserva versiones; cancelación no borra; presupuesto/orden separados de ejecución.
- **Criterios de aceptación:** Dado plan, cuando se aprueba/programa, entonces transiciones inválidas fallan y recursos/versiones quedan congelados.
- **Casos negativos y bordes:** lote inactivo, perfil incompatible, doble aprobación, fecha fuera campaña y presupuesto faltante.
- **Estrategia de pruebas:** state machine, API/auth, E2E y conflicto optimista.
- **Observabilidad:** órdenes por estado/atraso/conflictos y latencia.
- **Seguridad y privacidad:** recursos por alcance y contratista mínimo.
- **Performance/capacidad y límites:** paginación/filtros y volumen Q-020.
- **Feature flag, rollout, migración, rollback y recuperación:** `work-orders`; cancelar/flag sin borrar planes.
- **Documentación:** estados/roles y guía campaña.
- **Comandos/evidencia esperados:** tests unit/API/E2E futuros.
- **Definition of Ready:** estados/permisos/perfil piloto.
- **Definition of Done:** plan→orden aprobada demostrable.
- **Bloqueos/preguntas:** Q-013/043/044.
- **Paralelizable:** sí con Inventory kernel por contratos.

<a id="agro-agr-002"></a>

## AGRO-AGR-002 — Confirmar un parte con stock y costo una sola vez

- **Release, épica, prioridad y tamaño:** R2 · EPIC-06 · Must · L.
- **Owner y colaboradores:** Operations; Inventory, Finance, Documents, Weather, GIS, QA y SRE.
- **Resultado/valor esperado:** ejecución real atómica/idempotente y rectificable.
- **Historia/JTBD:** Como capataz, quiero confirmar cantidades/horas/evidencia sin duplicar efectos por mala red.
- **Alcance incluido:** parte real, área/ubicación/clima manual/snapshot/fotos/incidencias, confirmación, movimientos/imputaciones y reversa.
- **Fuera de alcance:** offline/cola local y contabilidad completa.
- **Requisitos trazados:** RF-OPS-004/005; RN-AGR-002; RN-CORE-002–005/007/009; RN-INV-001/003; RNF-CON-002.
- **Precondiciones y dependencias:** AGR-001, FND-002, INV-001 y FIN-001 kernel.
- **Contrato/API/eventos afectados:** confirm/reverse execution; `ExecutionConfirmed/InventoryConsumed`.
- **Datos, índices, migración y compatibilidad:** ejecución efectiva/registrada, idempotency ledger, refs partida/costo/clima/documento y outbox.
- **Autenticación, autorización, tenant y auditoría:** ejecutar/aprobar/rectificar por alcance; actor/origen/correlación.
- **Frontend:** formulario recuperable en sesión, no persistencia offline, pending/error/conflict y prevención doble envío.
- **Reglas e invariantes:** una confirmación→un consumo/costo; fallo parcial revierte; rectificación compensa.
- **Criterios de aceptación:** Dado doble clic/retry/crash, cuando confirma, entonces existe un efecto completo y respuesta reproducible.
- **Casos negativos y bordes:** stock insuficiente, período cerrado, snapshot stale, archivo en cuarentena y key con payload distinto.
- **Estrategia de pruebas:** integración multi-módulo, concurrencia/property, crash/replay, BOLA y E2E.
- **Observabilidad:** confirm latency/failures/dedup, outbox/backlog y runbook conciliación.
- **Seguridad y privacidad:** payload/archivos limitados; recurso/tenant reautorizado.
- **Performance/capacidad y límites:** API write ≤800 ms p95 propia; efectos externos async.
- **Feature flag, rollout, migración, rollback y recuperación:** canary; kill confirm, preservar borradores; reversa/roll-forward.
- **Documentación:** contrato exactly-once/rectificación.
- **Comandos/evidencia esperados:** futuros comandos del repositorio para integración y concurrencia.
- **Definition of Ready:** kernels y política de reversa/cierre.
- **Definition of Done:** criterio R2 demostrado con telemetría.
- **Bloqueos/preguntas:** política stock negativo/cierre.
- **Paralelizable:** no para integración final; componentes sí.

<a id="agro-agr-003"></a>

## AGRO-AGR-003 — Registrar labores y consistencia de dosis por perfil

- **Release, épica, prioridad y tamaño:** R3 · EPIC-06 · Must · L.
- **Owner y colaboradores:** Agriculture; Agrónomo, Inventory, Assets, Frontend y QA.
- **Resultado/valor esperado:** barbecho/labranza/siembra/fertilización/riego/pulverización con insumos/operador trazables.
- **Historia/JTBD:** Como agrónomo, quiero documentar labor y dosis verificables para reconstruir la campaña.
- **Alcance incluido:** tipos de labor, semilla/densidad/dosis/producto/partida/maquinaria/operador/profesional y ciclos diversos.
- **Fuera de alcance:** receta oficial no validada y dosis variable Should.
- **Requisitos trazados:** RF-AGR-001–003/011; RN-AGR-001–003; RN-CORE-007; Q-043–047.
- **Precondiciones y dependencias:** AGR-001/002, CAT-005 e INV-001/ACT.
- **Contrato/API/eventos afectados:** labor draft/validate/confirm.
- **Datos, índices, migración y compatibilidad:** assignment/profile/area/dose/total/original unit/parties; schemas versionados.
- **Autenticación, autorización, tenant y auditoría:** técnico registra, aprobador según política, profesional identificado.
- **Frontend:** formulario por perfil, cálculo explicativo, error campo/resumen y estados completos.
- **Reglas e invariantes:** dosis×área≈total o justificación; no copiar reglas de otro perfil.
- **Criterios de aceptación:** Dada dosis/área, cuando total no concilia, entonces bloquea/advierte según perfil y exige motivo auditado.
- **Casos negativos y bordes:** mezcla/consociación, unidades incompatibles, partida vencida/carencia y perennial.
- **Estrategia de pruebas:** unit/property unidades, profile isolation, integration stock y E2E.
- **Observabilidad:** validaciones/justificaciones/fallos por perfil.
- **Seguridad y privacidad:** receta/profesional/documento protegidos.
- **Performance/capacidad y límites:** formulario/búsqueda de partidas dentro targets.
- **Feature flag, rollout, migración, rollback y recuperación:** flag profile-version; rollback a genérico.
- **Documentación:** ficha perfil, fórmula/unidades y guía.
- **Comandos/evidencia esperados:** tests por perfil y acta agrónomo.
- **Definition of Ready:** perfil/unidades/tolerancia aprobados.
- **Definition of Done:** recorrido labor validado profesionalmente.
- **Bloqueos/preguntas:** Q-043–047.
- **Paralelizable:** sí por frontend/backend/tests.

<a id="agro-agr-004"></a>

## AGRO-AGR-004 — Separar monitoreo, recomendación y ejecución

- **Release, épica, prioridad y tamaño:** R3 · EPIC-06 · Must · M.
- **Owner y colaboradores:** Agriculture; Agrónomo, GIS, Documents, Frontend y QA.
- **Resultado/valor esperado:** observaciones georreferenciadas y aprobación profesional sin automatizar cumplimiento.
- **Historia/JTBD:** Como agrónomo, quiero registrar fenología/plagas/decisión y que la ejecución permanezca aparte.
- **Alcance incluido:** scouting, punto/evidencia, fenología/emergencia/malezas/plagas/enfermedad/humedad, recommendation/prescription/approval.
- **Fuera de alcance:** diagnóstico autónomo y receta jurisdiccional RF-AGR-009 salvo perfil R7.
- **Requisitos trazados:** RF-AGR-004/005; RN-AGR-004/006; Q-045. RF-AGR-009 queda en AGRO-INT-004 para R7.
- **Precondiciones y dependencias:** GIS-002, CAT-005 y DOC-001.
- **Contrato/API/eventos afectados:** scouting/recommend/approve/link-execution.
- **Datos, índices, migración y compatibilidad:** observation vs recommendation vs execution, professional/jurisdiction/source/version.
- **Autenticación, autorización, tenant y auditoría:** rol profesional/aprobador; evidencia/decisión auditada.
- **Frontend:** mapa/lista/formulario, estados pendiente/aprobada/rechazada/expirada y alternativa accesible.
- **Reglas e invariantes:** recomendación nunca equivale a labor; cumplimiento solo perfil/jurisdicción validado.
- **Criterios de aceptación:** Dada recomendación, cuando se aprueba, entonces no consume stock ni crea ejecución hasta orden/parte separado.
- **Casos negativos y bordes:** profesional no habilitado, jurisdicción desconocida, observación corregida y evidencia ausente.
- **Estrategia de pruebas:** state/auth, GIS/document, E2E separación y validación profesional.
- **Observabilidad:** tiempos aprobación/expiraciones/rechazos.
- **Seguridad y privacidad:** acceso técnico y archivo seguro.
- **Performance/capacidad y límites:** monitoreos paginados/geofiltros acotados.
- **Feature flag, rollout, migración, rollback y recuperación:** perfil; revocar aprobación afecta futuras acciones, no historia.
- **Documentación:** límites y normativa no automatizada.
- **Comandos/evidencia esperados:** tests E2E/contrato futuros.
- **Definition of Ready:** workflow/profesional/jurisdicción.
- **Definition of Done:** separación inequívoca y aceptada.
- **Bloqueos/preguntas:** Q-045.
- **Paralelizable:** sí con AGR-003/005 tras contratos.

<a id="agro-agr-005"></a>

## AGRO-AGR-005 — Cosechar, almacenar y comparar plan contra real

- **Release, épica, prioridad y tamaño:** R3 · EPIC-06 · Must · L.
- **Owner y colaboradores:** Agriculture; Inventory, Finance, Analytics, Frontend y QA.
- **Resultado/valor esperado:** cosecha/partida/calidad/destino y margen reconstruibles por lote/campaña.
- **Historia/JTBD:** Como productor, quiero cerrar campaña con rendimiento, costo e historia conciliados.
- **Alcance incluido:** humedad/merma/rendimiento/calidad/pesaje/destino, partida producida/almacenaje y plan-real costos/margen.
- **Fuera de alcance:** forwards/canjes/carta de porte RF-AGR-010 y contratos comerciales avanzados.
- **Requisitos trazados:** RF-AGR-006–008; RN-AGR-005; RF-ANA-002; RN-FIN-004. RF-AGR-010 queda en AGRO-INT-004 para R7.
- **Precondiciones y dependencias:** AGR-002/003, INV-001/002 y FIN-001; el cierre R3 no depende del módulo financiero completo R5.
- **Contrato/API/eventos afectados:** cosecha, almacenamiento, entrega y proyección de KPI; `HarvestRecorded`.
- **Datos, índices, migración y compatibilidad:** harvest/quality/weight/output batch/location/cost refs; fórmulas versionadas.
- **Autenticación, autorización, tenant y auditoría:** campo/campaña/depósito; confirmación/rectificación.
- **Frontend:** captura/conciliación/dashboard con fórmula/unidad/faltantes y estados completos.
- **Reglas e invariantes:** cosecha/almacenamiento/entrega/venta distintos; faltante no es cero.
- **Criterios de aceptación:** Dada campaña, cuando cosecha, entonces partida concilia con movimientos y plan-real muestra fórmula/moneda/período/datos faltantes.
- **Casos negativos y bordes:** cosecha parcial, múltiples destinos, humedad/merma inválida, báscula corregida y depósito sin capacidad.
- **Estrategia de pruebas:** integración inventory/finance, property rendimiento, E2E y reconciliación.
- **Observabilidad:** cosecha/conciliaciones/desvíos y errores.
- **Seguridad y privacidad:** autorización por tenant/recurso y acceso financiero.
- **Performance/capacidad y límites:** volúmenes/import jobs según Q-020.
- **Feature flag, rollout, migración, rollback y recuperación:** profile flag; rectificación/reversa; no borrar partida.
- **Documentación:** fórmulas/timeline y guía cierre campaña.
- **Comandos/evidencia esperados:** suites futuras y acta agrónomo/Product.
- **Definition of Ready:** destinos/calidad/fórmulas y contratos.
- **Definition of Done:** recorrido agrícola R3 completo/conciliado.
- **Bloqueos/preguntas:** Q-046; advanced commerce R7.
- **Paralelizable:** integración final no; proyecciones sí.

<a id="agro-agr-006"></a>

## AGRO-AGR-006 — Operar calendario, recordatorios y carga masiva de actividades

- **Release, épica, prioridad y tamaño:** R7 · EPIC-06 · Should · M.
- **Owner y colaboradores:** Agriculture/Productivity; Frontend, Integrations, Identity, Notifications y QA.
- **Resultado/valor esperado:** reducir omisiones y carga repetitiva sin convertir recordatorios o importaciones en ejecuciones automáticas.
- **Historia/JTBD:** Como responsable operativo, quiero programar, reprogramar e importar actividades para coordinar campañas con trazabilidad.
- **Alcance incluido:** calendario por campaña/lote/responsable, dependencias, recordatorios dentro de la aplicación, reprogramación y carga masiva con previsualización, validación y resultado por fila.
- **Fuera de alcance:** ejecución automática, notificaciones externas sin decisión `Q-023/Q-030`, modo offline y mutaciones mediante IA.
- **Requisitos trazados:** RF-OPS-006; RN-CORE-002/004/005/009; RNF-UX-001/003; RNF-CON-002; Q-023/030.
- **Precondiciones y dependencias:** AGRO-AGR-001, AGRO-INT-002, AGRO-FE-002 y contrato de alertas AGRO-IA-002.
- **Contrato/API/eventos afectados:** consultas de calendario, programación/reprogramación, importación asincrónica y eventos de actividad próxima o vencida.
- **Datos, índices, migración y compatibilidad:** fecha efectiva y registrada, zona horaria, dependencia, responsable, estado del recordatorio, lote de importación, idempotencia y compatibilidad aditiva.
- **Autenticación, autorización, tenant y auditoría:** alcance por organización/campo/lote; permisos separados para importar y reprogramar; auditoría de cambios y actor.
- **Frontend:** vistas agenda/calendario/lista responsivas, previsualización de importación, estados de carga/vacío/error/desactualizado/degradado/conflicto, teclado y lector de pantalla; UUID corto.
- **Reglas e invariantes:** una dependencia no crea ciclos; importar no confirma ejecuciones; un recordatorio no equivale a orden aprobada; reintentos no duplican actividades.
- **Criterios de aceptación:** Dado un archivo parcialmente inválido, cuando se previsualiza y confirma, entonces solo se aplican filas autorizadas y válidas una vez, se informa cada rechazo y el calendario refleja fechas/dependencias sin ejecutar trabajos.
- **Casos negativos y bordes:** ciclo de dependencias, cambio de zona horaria, lote inactivo, responsable revocado, archivo duplicado, reintento, fecha pasada y canal externo no habilitado.
- **Estrategia de pruebas:** unitarias de calendario/ciclos, property-based de dependencias, integración de importación/idempotencia/autorización, contrato de alertas, E2E accesible y volumen representativo.
- **Observabilidad:** duración/errores/rechazos de importación, recordatorios creados/vencidos, reprogramaciones, deduplicación y trazas correlacionadas.
- **Seguridad y privacidad:** archivos en cuarentena y con límites; sin datos sensibles en recordatorios; reautorización al abrir el recurso.
- **Performance/capacidad y límites:** paginación/ventanas temporales, tamaño y filas máximas definidos por `Q-020`, procesamiento asincrónico y límites por tenant.
- **Feature flag, rollout, migración, rollback y recuperación:** flags separados para calendario, importación y canales; piloto por tenant; desactivar canal/importación preserva actividades; reintento/reconciliación del lote.
- **Documentación:** contrato de plantilla, estados de importación, reglas de dependencias, permisos y guía operativa.
- **Comandos/evidencia esperados:** suites futuras del repositorio para dominio, integración, contrato, E2E, accesibilidad y carga; muestra de conciliación del lote.
- **Definition of Ready específica:** plantilla/versionado, límites, permisos, semántica de dependencia y decisión de canales definidos.
- **Definition of Done específica:** recorrido crear/importar→validar→programar→reprogramar→recordar demostrado, sin duplicación ni ejecución implícita y con evidencia accesible.
- **Bloqueos/preguntas abiertas:** `Q-020`, `Q-023` y `Q-030` condicionan límites y canales, no el calendario dentro de la aplicación.
- **Paralelizable:** sí, UI de calendario con AGRO-INT-002 tras fijar contratos; la integración final depende de AGRO-AGR-001 y AGRO-IA-002.
