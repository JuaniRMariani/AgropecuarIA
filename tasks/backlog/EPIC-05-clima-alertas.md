# EPIC-05 — Clima y alertas

Objetivo: clima multifuente trazable/degradado, CAP autoritativo y observación local opcional. R2; WRF condicional y evaluación avanzada R7.

<a id="agro-cli-001"></a>

## AGRO-CLI-001 — Persistir pronósticos auditables con cache y degradación

- **Release, épica, prioridad y tamaño:** R2 · EPIC-05 · Must · L.
- **Owner y colaboradores:** Weather; GIS, Integrations, Data, SRE, Frontend y QA.
- **Resultado/valor esperado:** clima por punto/campo con procedencia, frescura y fallback sin bloquear transacciones.
- **Historia/JTBD:** Como productor, quiero ver pronóstico confiablemente rotulado aunque el proveedor falle.
- **Alcance incluido:** WeatherProvider, Open-Meteo propuesto, variables horarias/diarias, puntos representativos, snapshots, cache/cuotas y fresh/stale/unavailable.
- **Fuera de alcance:** llamada navegador-proveedor, alertas oficiales propias, IoT/satélite y WRF no aprobado.
- **Requisitos trazados:** RF-CLI-001–003/006/007; RN-CLI-001–004/006/007; RNF-PER-005; RNF-REL-004; ADR-005; Q-021/025/027.
- **Precondiciones y dependencias:** DIS-004, GIS-001/002 y FND-002.
- **Contrato/API/eventos afectados:** forecast query/refresh/status y `ForecastUpdated`.
- **Datos, índices, migración y compatibilidad:** proveedor/modelo/corrida/celda/issued/valid/ingested/variable/unidad/naturaleza/hash; unicidad de snapshot.
- **Autenticación, autorización, tenant y auditoría:** solo backend, campo autorizado, claves en servidor y coordenada mínima.
- **Frontend:** probabilidad≠mm, observado/estimado/pronosticado, fuente/resolución/frescura; loading/empty/stale/unavailable.
- **Reglas e invariantes:** pronóstico nuevo no reescribe; no falsa precisión; proveedor caído no bloquea módulos.
- **Criterios de aceptación:** Dado cache vigente/fallo, cuando abre campo, entonces responde ≤2 s p75 o muestra último dato/indisponible con antigüedad/fuente.
- **Casos negativos y bordes:** 429/500/timeout, corrida duplicada/faltante, UTC/medianoche, unidad fuera de rango y campo grande.
- **Estrategia de pruebas:** contract fixtures, integración/cache stampede, unidades/tiempo, resiliencia, BOLA y performance.
- **Observabilidad:** cache hit, cuota, latencia, frescura/cobertura/último éxito y circuit breaker.
- **Seguridad y privacidad:** schema/range/size, egress allow-list y secreto fuera de logs.
- **Performance/capacidad y límites:** target RNF-PER-005; TTL/retención según Q-020/060.
- **Feature flag, rollout, migración, rollback y recuperación:** provider flag; canary; cambiar adapter/servir stale; snapshots se preservan.
- **Documentación:** contrato/atribución/variables/runbook.
- **Comandos/evidencia esperados:** contract/integration/performance tests de la futura solución.
- **Definition of Ready:** proveedor/variables/TTL/puntos aprobados.
- **Definition of Done:** normal/degradado/cuota/telemetría demostrados.
- **Bloqueos/preguntas:** Q-021/025/027 y proveedor contratado.
- **Paralelizable:** sí con CLI-002/003 tras contratos.

<a id="agro-cli-002"></a>

## AGRO-CLI-002 — Registrar lluvia observada sin mezclarla con pronóstico

- **Release, épica, prioridad y tamaño:** R2 · EPIC-05 · Must · M.
- **Owner y colaboradores:** Weather; Frontend, GIS, Product y QA.
- **Resultado/valor esperado:** pluviómetro/estación opcional mejora evidencia sin bloquear al ausente.
- **Historia/JTBD:** Como productor, quiero cargar lluvia local y compararla sin que reemplace el pronóstico histórico.
- **Alcance incluido:** observación manual, ubicación/método/fecha/unidad/calidad, visualización separada y calibración no disponible.
- **Fuera de alcance:** IoT automático y declarar medición zonal como lote.
- **Requisitos trazados:** RF-CLI-004; RN-CLI-001/005/006; Q-007/017/024/027. RF-CLI-009 queda en AGRO-CLI-005 para R7.
- **Precondiciones y dependencias:** CLI-001 y ubicación autorizada.
- **Contrato/API/eventos afectados:** record/correct observed rain; `RainObserved`.
- **Datos, índices, migración y compatibilidad:** observed source/effective/recorded/unit/method/quality; rectificación no overwrite.
- **Autenticación, autorización, tenant y auditoría:** alcance campo, actor/origen y corrección auditada.
- **Frontend:** carga opcional; comparación; sin dato muestra “calibración local no disponible”; responsive/a11y.
- **Reglas e invariantes:** observación local prioridad descriptiva; ausencia no invalida clima/operación.
- **Criterios de aceptación:** Dado campo sin pluviómetro, cuando abre clima, entonces opera y muestra limitación; al cargar lluvia, conserva ambos tipos.
- **Casos negativos y bordes:** duplicado, unidad inválida, carga tardía, sensor cercano y corrección.
- **Estrategia de pruebas:** unit/integration, timezone, idempotencia, E2E con/sin dato.
- **Observabilidad:** cobertura/lag/calidad sin penalizar campos sin sensor.
- **Seguridad y privacidad:** coordenada mínima/tenant.
- **Performance/capacidad y límites:** series paginadas; volumen medido.
- **Feature flag, rollout, migración, rollback y recuperación:** capability disponible; rollback oculta captura sin borrar observación.
- **Documentación:** definiciones/precedencia/calibración.
- **Comandos/evidencia esperados:** tests futuros de observaciones/comparación.
- **Definition of Ready:** método/unidades/corrección definidos.
- **Definition of Done:** con/sin pluviómetro aceptado.
- **Bloqueos/preguntas:** Q-017/024/027.
- **Paralelizable:** sí con CLI-003.

<a id="agro-cli-003"></a>

## AGRO-CLI-003 — Ingerir alertas oficiales SMN CAP por geometría

- **Release, épica, prioridad y tamaño:** R2 · EPIC-05 · Must · L.
- **Owner y colaboradores:** Weather/GIS; Integrations, SRE, Frontend y QA.
- **Resultado/valor esperado:** alertas oficiales con ciclo de vida correcto e intersección espacial.
- **Historia/JTBD:** Como productor, quiero saber si una alerta SMN vigente afecta mi campo.
- **Alcance incluido:** parser CAP, emisión/actualización/cancelación/vencimiento, polígonos, intersección PostGIS, atribución/frescura.
- **Fuera de alcance:** generar colores oficiales propios y canales no aprobados.
- **Requisitos trazados:** RF-CLI-005/008; RN-CLI-007/008; RNF-OBS-002; ADR-005; Q-022/023/030.
- **Precondiciones y dependencias:** DIS-004, GIS-002 y contrato CAP confirmado.
- **Contrato/API/eventos afectados:** active alerts/applicability/status y `OfficialWeatherAlertChanged`.
- **Datos, índices, migración y compatibilidad:** identifier/version/status/times/geometry/hash; idempotencia por mensaje/actualización.
- **Autenticación, autorización, tenant y auditoría:** feed backend; campos autorizados; no datos tenant en ingestión global.
- **Frontend:** oficial/fuente/vigencia, no solo color, lista+mapa accesibles y stale/unavailable.
- **Reglas e invariantes:** cancelada/vencida no activa; SMN única fuente de severidad oficial.
- **Criterios de aceptación:** Dado CAP actualizado/cancelado, cuando cruza campo, entonces estado vigente cambia sin borrar historia ni mantener alerta falsa.
- **Casos negativos y bordes:** XML inválido, polígono limítrofe, timezone, feed caído y duplicado.
- **Estrategia de pruebas:** fixtures CAP reales/versionados, PostGIS, lifecycle, resiliencia y E2E.
- **Observabilidad:** feed age, activas, parser errors, intersecciones y último éxito; alerta operativa de stale.
- **Seguridad y privacidad:** payload no confiable, schema/size/XXE controles.
- **Performance/capacidad y límites:** intersecciones indexadas y ventana de retención.
- **Feature flag, rollout, migración, rollback y recuperación:** `smn-cap`; fallback muestra canal oficial/no disponible.
- **Documentación:** atribución, semántica y runbook.
- **Comandos/evidencia esperados:** contract/GIS lifecycle tests futuros.
- **Definition of Ready:** mecanismo CAP/TTL/canales acordados.
- **Definition of Done:** ciclo completo y degradación aprobados.
- **Bloqueos/preguntas:** Q-022/023/030.
- **Paralelizable:** sí con CLI-001; integración final espera GIS.

<a id="agro-cli-004"></a>

## AGRO-CLI-004 — Configurar alertas y ventanas por actividad

- **Release, épica, prioridad y tamaño:** R2/R3 · EPIC-05 · Must · M.
- **Owner y colaboradores:** Weather/Product; Agrónomo, Operations, Frontend, QA y Notifications.
- **Resultado/valor esperado:** alertas propias/ventanas transparentes sin presentar pronóstico como certeza.
- **Historia/JTBD:** Como agrónomo, quiero configurar riesgos que ayuden a revisar labores sin prescribir automáticamente.
- **Alcance incluido:** lluvia/helada/calor/viento/tormenta/déficit, thresholds versionados por actividad/campo, evidence snapshot y canales in-app.
- **Fuera de alcance:** granizo/anegamiento/canales externos no aprobados y alerta SMN inferida.
- **Requisitos trazados:** RF-CLI-005; RF-ANA-003; RN-CLI-003/008; RF-IA-004; Q-022/023/027/028/030.
- **Precondiciones y dependencias:** CLI-001/003 y VAL-AGR climática.
- **Contrato/API/eventos afectados:** threshold/alert/evaluate/acknowledge.
- **Datos, índices, migración y compatibilidad:** regla/version/scope/snapshot/result; configuración versionada.
- **Autenticación, autorización, tenant y auditoría:** admin/técnico configura; usuario por alcance recibe; cambios auditados.
- **Frontend:** fuente/tipo/umbral/frescura/acción y estados; distinción oficial/propia.
- **Reglas e invariantes:** umbrales aprobados; no certainty; dato stale aplica política/abstención.
- **Criterios de aceptación:** Dada regla y snapshot, cuando cruza umbral, entonces alerta cita regla/dato y no se rotula oficial.
- **Casos negativos y bordes:** umbral conflictivo, dato faltante, duplicación, zona horaria y fatigue.
- **Estrategia de pruebas:** determinísticas, contrato, E2E, falsos positivos y revisión agrónomo.
- **Observabilidad:** alertas creadas/vistas/ack/rechazadas y fatiga.
- **Seguridad y privacidad:** alcance tenant; canales no filtran ubicación.
- **Performance/capacidad y límites:** jobs/cuotas y deduplicación.
- **Feature flag, rollout, migración, rollback y recuperación:** flag por tipo/perfil; shadow/piloto; desactivar regla.
- **Documentación:** catálogo de alertas/umbrales/aprobadores.
- **Comandos/evidencia esperados:** tests de reglas/E2E futuros y acta profesional.
- **Definition of Ready:** Q-022/027/028 y aprobador.
- **Definition of Done:** in-app segura; canales pendientes explícitos.
- **Bloqueos/preguntas:** Q-023/030.
- **Paralelizable:** sí por tipo con motor común.

<a id="agro-cli-005"></a>

## AGRO-CLI-005 — Incorporar WRF/evaluación avanzada solo tras evidencia

- **Release, épica, prioridad y tamaño:** R7 · EPIC-05 · Should · L.
- **Owner y colaboradores:** Weather/Data/SRE; GIS, QA y Product.
- **Resultado/valor esperado:** fallback WRF/evaluación de skill sin convertirlo en dependencia del MVP.
- **Historia/JTBD:** Como Weather Lead, quiero decidir y operar fuentes adicionales por calidad/costo medidos.
- **Alcance incluido:** pipeline WRF si go, corrida/NetCDF/variables, comparación por horizonte y skill con observaciones disponibles.
- **Fuera de alcance:** IoT/satélite RF-CLI-010 salvo decisión separada.
- **Requisitos trazados:** RF-CLI-009; ADR-005; Q-026/027/029; RNF-REL-004. RF-CLI-010 queda en AGRO-INT-004.
- **Precondiciones y dependencias:** resultado DIS-004, CLI-001/002 y presupuesto.
- **Contrato/API/eventos afectados:** provider adapter/forecast skill metrics.
- **Datos, índices, migración y compatibilidad:** corridas/variables/retención; proveedor intercambiable.
- **Autenticación, autorización, tenant y auditoría:** backend; procedencia/licencia; observaciones tenant aisladas.
- **Frontend:** comparación rotulada; sin observación declara limitación.
- **Reglas e invariantes:** WRF no borra otras corridas; sin local no afirma calibración local.
- **Criterios de aceptación:** Dado go del spike, cuando corrida falta/es inválida, entonces degrada sin afectar R2 y conserva métricas correctas.
- **Casos negativos y bordes:** NetCDF corrupto, gran volumen, variable ausente y modelo cambia.
- **Estrategia de pruebas:** contract/parser, performance/costo, fallos y métricas meteorológicas aprobadas.
- **Observabilidad:** corrida/frescura/volumen/costo/skill.
- **Seguridad y privacidad:** validación archivos, egress y licencia.
- **Performance/capacidad y límites:** límites medidos en spike; almacenamiento/retención presupuestados.
- **Feature flag, rollout, migración, rollback y recuperación:** provider flag/kill; retirar sin perder snapshots históricos.
- **Documentación:** decisión go/no-go y modelo card.
- **Comandos/evidencia esperados:** pruebas/mediciones futuras del pipeline.
- **Definition of Ready:** spike positivo y presupuesto.
- **Definition of Done:** operable o explícitamente postergado sin bloquear MVP.
- **Bloqueos/preguntas:** Q-026/027/029.
- **Paralelizable:** sí con R7; no gatea R2.
