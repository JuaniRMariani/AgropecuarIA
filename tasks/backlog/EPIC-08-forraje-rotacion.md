# EPIC-08 — Forraje y rotación

Objetivo: alternativas determinísticas y seguras con medición opcional, evidencia visible y movimiento humano separado. R4.

<a id="agro-for-001"></a>

## AGRO-FOR-001 — Configurar potreros, recursos, agua y restricciones

- **Release, épica, prioridad y tamaño:** R4 · EPIC-08 · Must · L.
- **Owner y colaboradores:** Grazing; GIS, Agrónomo, Veterinario, Frontend y QA.
- **Resultado/valor esperado:** base física/operativa versionada para decidir sin asumir seguridad.
- **Historia/JTBD:** Como responsable, quiero conocer superficie efectiva, recurso, agua e impedimentos de cada potrero.
- **Alcance incluido:** recurso/estado/superficie efectiva, agua/caudal/sombra/alambrado/distancia, anegamiento/piso/toxicidad/carencia/sanidad y descanso.
- **Fuera de alcance:** inferir estado por ubicación y automatizar infraestructura.
- **Requisitos trazados:** RF-GAN-011/015/017; RN-GAN-008/012/013; Q-031/032/038/039/040.
- **Precondiciones y dependencias:** GIS-002/003, GAN-001 y perfiles/VAL-FOR.
- **Contrato/API/eventos afectados:** pasture resource/restriction/status/reservation.
- **Datos, índices, migración y compatibilidad:** resource/profile/version/effective dates/restrictions/evidence.
- **Autenticación, autorización, tenant y auditoría:** campo/potrero, cambios de seguridad/agua auditados.
- **Frontend:** mapa+lista, bloqueos no solo color, states loading/empty/stale/conflict y a11y.
- **Reglas e invariantes:** agua/restricciones críticas no se sobreescriben por ranking; descanso no fijo universal.
- **Criterios de aceptación:** Dado potrero sin agua o con carencia, cuando se evalúa, entonces queda excluido con motivo/fuente/vigencia.
- **Casos negativos y bordes:** agua stale, restricción solapada, superficie cero, potrero ocupado/reservado y multi-grupo.
- **Estrategia de pruebas:** reglas/temporal/GIS, E2E bloqueos, concurrencia y especialistas.
- **Observabilidad:** recursos incompletos/bloqueos/stale/reservas.
- **Seguridad y privacidad:** alcance por campo y evidencia protegida.
- **Performance/capacidad y límites:** candidatos/queries espaciales indexados.
- **Feature flag, rollout, migración, rollback y recuperación:** `grazing-profile`; kill recomendación, registro sigue.
- **Documentación:** matriz bloqueos/perfil.
- **Comandos/evidencia esperados:** tests rule/GIS futuros y acta profesional.
- **Definition of Ready:** datos mínimos/bloqueos/perfiles aprobados.
- **Definition of Done:** potrero evaluable y motivos visibles.
- **Bloqueos/preguntas:** Q-031/032/038–040.
- **Paralelizable:** sí con FOR-002 y GAN-004 tras contratos.

<a id="agro-for-002"></a>

## AGRO-FOR-002 — Registrar biomasa opcional y escenarios estimados

- **Release, épica, prioridad y tamaño:** R4 · EPIC-08 · Must · M.
- **Owner y colaboradores:** Grazing; Agrónomo, Frontend, Data y QA.
- **Resultado/valor esperado:** medición mejora evidencia; ausencia permite registrar y solo estimar rangos.
- **Historia/JTBD:** Como productor, quiero cargar altura/biomasa si existe y comprender su confiabilidad.
- **Alcance incluido:** método/muestras/fecha/confianza/biomasa/altura/remanente, vigencia y escenarios bajo/base/alto desde perfil.
- **Fuera de alcance:** declarar “listo”/capacidad exacta sin medición y NDVI automático.
- **Requisitos trazados:** RF-GAN-012/013; RN-GAN-008/009/013; Q-016/033–035.
- **Precondiciones y dependencias:** FOR-001 y perfil forrajero aprobado.
- **Contrato/API/eventos afectados:** record/rectify measurement, estimate scenarios; `ForageMeasured`.
- **Datos, índices, migración y compatibilidad:** measurement/profile/method/samples/effective/confidence; immutable versions.
- **Autenticación, autorización, tenant y auditoría:** técnico/field scope; origen/corrección auditada.
- **Frontend:** carga opcional, badge evidence, rangos/inspección y estados stale.
- **Reglas e invariantes:** medición vigente domina perfil regional; sin ella no exactitud/ready.
- **Criterios de aceptación:** Dado potrero sin biomasa pero seguro, cuando se evalúa, entonces muestra rangos estimados e inspección, sin fecha/capacidad exacta.
- **Casos negativos y bordes:** medición vencida, método incompatible, muestras insuficientes y valor negativo.
- **Estrategia de pruebas:** unit/property, profile isolation, temporal y E2E con/sin biomasa.
- **Observabilidad:** cobertura/frescura/método/confianza y solicitudes inspección.
- **Seguridad y privacidad:** datos tenant; fuentes firmadas/aprobadas.
- **Performance/capacidad y límites:** series paginadas y vigencia configurable.
- **Feature flag, rollout, migración, rollback y recuperación:** profile flag; rollback a estimado/genérico.
- **Documentación:** métodos/vigencia/interpretación.
- **Comandos/evidencia esperados:** tests formula/profile futuros y aprobación agrónomo.
- **Definition of Ready:** método/perfil/vigencia.
- **Definition of Done:** niveles de evidencia inequívocos.
- **Bloqueos/preguntas:** Q-016/033–035.
- **Paralelizable:** sí con FOR-001.

<a id="agro-for-003"></a>

## AGRO-FOR-003 — Calcular oferta, demanda, días y déficit determinísticamente

- **Release, épica, prioridad y tamaño:** R4 · EPIC-08 · Must · L.
- **Owner y colaboradores:** Grazing Domain; Agrónomo, Veterinario, Livestock, Weather y QA.
- **Resultado/valor esperado:** cálculo exacto/reconstruible con reglas profesionales y abstención segura.
- **Historia/JTBD:** Como productor, quiero comparar alternativas con supuestos y límites visibles.
- **Alcance incluido:** demanda por especie/categoría/peso/consumo, suplemento efectivo, oferta/remanente/área/factor/eficiencia/crecimiento, días/déficit/ranking y clima.
- **Fuera de alcance:** ración/suplemento prescrito, LLM aritmético y tasa universal.
- **Requisitos trazados:** RF-GAN-013–017/018; RN-GAN-008–014; RF-IA-005; ADR-004/006; Q-034–042.
- **Precondiciones y dependencias:** FOR-001/002, GAN-002/004, CLI-001/003 y perfil conjunto aprobado.
- **Contrato/API/eventos afectados:** calculate alternatives/evidence/reason; `GrazingPlanRecommended`.
- **Datos, índices, migración y compatibilidad:** entradas de recomendación y versiones de herramienta/fórmula/perfil/clima con salidas inmutables.
- **Autenticación, autorización, tenant y auditoría:** recursos reautorizados; cálculo/actor/versiones auditados.
- **Frontend:** hasta tres alternativas, fórmulas/fuentes/faltantes/bloqueos, no CTA de ingreso si insuficiente.
- **Reglas e invariantes:** demanda/oferta no negativas; no infinito; bloqueos dominan; exacto solo observado.
- **Criterios de aceptación:** Dado set completo, cuando calcula, entonces reproduce oferta/demanda/días; sin seguridad se abstiene; sin biomasa solo rangos.
- **Casos negativos y bordes:** cero/negativo, crecimiento≥remoción, suplemento mayor demanda, perfil incompatible y clima stale/extremo.
- **Estrategia de pruebas:** unit/property exactas, perfil contamination, contract clima, E2E tres niveles y especialistas.
- **Observabilidad:** evidencia level, abstenciones/bloqueos/correcciones, latency y input freshness.
- **Seguridad y privacidad:** no exponer parámetros/rodeo fuera de alcance.
- **Performance/capacidad y límites:** candidatos limitados; cálculo propio dentro RNF-PER-001.
- **Feature flag, rollout, migración, rollback y recuperación:** shadow→piloto por profile; kill switch; rollback fórmula para nuevas recomendaciones.
- **Documentación:** formula card/perfil/model limits.
- **Comandos/evidencia esperados:** property/eval suite futura y acta agrónomo+veterinario.
- **Definition of Ready:** inputs/fórmulas/umbrales/dataset aprobados.
- **Definition of Done:** exactitud/abstención/concurrencia gateadas.
- **Bloqueos/preguntas:** Q-034–042.
- **Paralelizable:** motor/tests/UI sí; integración final no.

<a id="agro-for-004"></a>

## AGRO-FOR-004 — Decidir plan y confirmar movimiento por separado

- **Release, épica, prioridad y tamaño:** R4 · EPIC-08 · Must · L.
- **Owner y colaboradores:** Grazing/Livestock; Frontend, QA, Audit y AI.
- **Resultado/valor esperado:** aprobación/ajuste/rechazo trazable sin movimiento autónomo ni sobre-reserva.
- **Historia/JTBD:** Como productor, quiero decidir sobre una alternativa y luego confirmar el ingreso/salida real.
- **Alcance incluido:** accept/adjust/reject/comment, reserve/recalculate, plan entry/exit/review, actual start/end/remnant/outcome.
- **Fuera de alcance:** aceptación que muta ubicación y acción LLM.
- **Requisitos trazados:** RF-GAN-015–017; RF-IA-003/007; RN-GAN-003/011–013; RN-IA-004/006.
- **Precondiciones y dependencias:** FOR-003 y GAN-003.
- **Contrato/API/eventos afectados:** HumanDecision, reserve, start/end; `RecommendationApproved/GrazingStarted/Ended`.
- **Datos, índices, migración y compatibilidad:** plan/recommendation versions, decision/outcome y temporal reservation.
- **Autenticación, autorización, tenant y auditoría:** decisión/movimiento separados y reautorizados; actor/motivo.
- **Frontend:** confirmaciones separadas, conflicto de reserva, recalcular stale y estados accesibles.
- **Reglas e invariantes:** aceptar no mueve; dos planes no exceden capacidad; nueva corrida no reescribe recomendación.
- **Criterios de aceptación:** Dada recomendación aceptada, cuando aún no confirma movimiento, entonces ubicación no cambia; concurrencia genera conflicto seguro.
- **Casos negativos y bordes:** recomendación vencida, potrero bloqueado después, dos grupos y salida retroactiva.
- **Estrategia de pruebas:** E2E, concurrency/property temporal, audit y feedback.
- **Observabilidad:** acceptance/rejection/adjustment/outcome/conflicts.
- **Seguridad y privacidad:** permisos separados y no mutación IA.
- **Performance/capacidad y límites:** reserva/cálculo concurrente dentro targets.
- **Feature flag, rollout, migración, rollback y recuperación:** kill recommendation mantiene planes/hechos; rectificación explícita.
- **Documentación:** workflow/feedback/runbook.
- **Comandos/evidencia esperados:** E2E/concurrency/eval futuros.
- **Definition of Ready:** estados/reservas/reautorización.
- **Definition of Done:** separación y feedback demostrados.
- **Bloqueos/preguntas:** Q-040/057/059.
- **Paralelizable:** no para integración final; UI/tests sí.
