# EPIC-07 — Ganadería común

Objetivo: registro nacional por modo de seguimiento apropiado, existencias e historia temporal; sin forzar rodeos. R4, RFID Should R7.

<a id="agro-gan-001"></a>

## AGRO-GAN-001 — Configurar seguimiento pecuario por perfil

- **Release, épica, prioridad y tamaño:** R4 · EPIC-07 · Must · L.
- **Owner y colaboradores:** Livestock; Catalog, Veterinario, Frontend y QA.
- **Resultado/valor esperado:** individuo/grupo/lote/galpón/apiario/acuático/biomasa según perfil, sin modelo universal.
- **Historia/JTBD:** Como responsable ganadero, quiero registrar la especie/sistema con su unidad adecuada.
- **Alcance incluido:** taxón, propósito, sistema, categoría, estado y tracking mode versionados para baseline/extensión autorizada.
- **Fuera de alcance:** especialización completa de toda especie y equivalentes universales.
- **Requisitos trazados:** RF-GAN-001/002; RN-GAN-014; RF-PRD-004/005; ADR-006; Q-014/015.
- **Precondiciones y dependencias:** CAT-003/005, DIS-002 y VAL-VET.
- **Contrato/API/eventos afectados:** livestock profile/tracking unit/capabilities.
- **Datos, índices, migración y compatibilidad:** dimensiones separadas y perfil/schema versionados.
- **Autenticación, autorización, tenant y auditoría:** unidad/campo/perfil por alcance y activación auditada.
- **Frontend:** formularios adaptativos/capacidades ausentes, responsive y estados completos.
- **Reglas e invariantes:** aves/apiarios/acuicultura no se fuerzan a rodeo; perfil incompatible se abstiene.
- **Criterios de aceptación:** Dada una entrada por familia, cuando inicia ciclo, entonces usa tracking compatible y nunca campos/reglas de otra familia.
- **Casos negativos y bordes:** fauna regulada, unidad no espacial, categoría privada y perfil revocado.
- **Estrategia de pruebas:** parametrizadas por familia/mode, schema, tenant y aprobación veterinaria.
- **Observabilidad:** uso/abstenciones/incompatibilidades por versión.
- **Seguridad y privacidad:** perfiles firmados y extensiones tenant aisladas.
- **Performance/capacidad y límites:** catálogos paginados y modes sin joins genéricos costosos.
- **Feature flag, rollout, migración, rollback y recuperación:** flag profile-version; fallback genérico.
- **Documentación:** ficha perfil/capacidades.
- **Comandos/evidencia esperados:** suite paramétrica futura y acta veterinaria.
- **Definition of Ready:** perfil/competencia/jurisdicción.
- **Definition of Done:** modos representativos y abstención aprobados.
- **Bloqueos/preguntas:** Q-014/015/042.
- **Paralelizable:** sí por mode con contrato común.

<a id="agro-gan-002"></a>

## AGRO-GAN-002 — Mantener identificadores y existencias por eventos

- **Release, épica, prioridad y tamaño:** R4 · EPIC-07 · Must · L.
- **Owner y colaboradores:** Livestock; Data, Inventory, Veterinario y QA.
- **Resultado/valor esperado:** stock a fecha reconstruible con identificación individual cuando corresponde.
- **Historia/JTBD:** Como responsable, quiero registrar altas/bajas/nacimientos/compras/ventas sin editar un contador.
- **Alcance incluido:** identificadores visual/RFID/CUIG por perfil, altas/bajas/compra/venta/nacimiento/muerte/ajuste y stock a fecha.
- **Fuera de alcance:** escritura SIGSA/DT-e y RFID dispositivo R7.
- **Requisitos trazados:** RF-GAN-003/004; RN-GAN-001/002/007; RN-CORE-002–005; Q-015. RF-GAN-010 queda en AGRO-GAN-005/INT-004 para R7.
- **Precondiciones y dependencias:** GAN-001 y FND-002/003.
- **Contrato/API/eventos afectados:** register identifier/animal-event/stock-at-date.
- **Datos, índices, migración y compatibilidad:** unique tenant+type+identifier, events effective/recorded/origin y adjustments.
- **Autenticación, autorización, tenant y auditoría:** field/herd scope; ajuste/muerte/venta sensibles y auditados.
- **Frontend:** individuo/lote adaptativo, import preview futuro, conflicto y UUID corto.
- **Reglas e invariantes:** ID oficial no se reutiliza; stock deriva de eventos; rectificación no borra.
- **Criterios de aceptación:** Dada secuencia de eventos, cuando consulta fecha, entonces stock coincide con sumatoria y duplicado oficial falla.
- **Casos negativos y bordes:** evento tardío/fuera de orden, ID repetido, animal preexistente y ajuste concurrente.
- **Estrategia de pruebas:** property stock, temporal, uniqueness, concurrency, BOLA y E2E.
- **Observabilidad:** altas/bajas/ajustes/conflictos y anomalías.
- **Seguridad y privacidad:** identificadores/registros confidenciales.
- **Performance/capacidad y límites:** consultas a fecha indexadas; escala Q-020.
- **Feature flag, rollout, migración, rollback y recuperación:** mode flags; reversas/rectificaciones; restore temporal.
- **Documentación:** semántica de eventos/ID.
- **Comandos/evidencia esperados:** property/integration tests futuros.
- **Definition of Ready:** eventos/modes/ID profiles aprobados.
- **Definition of Done:** stock/ID exactos y auditables.
- **Bloqueos/preguntas:** import RFID/DT-e R7.
- **Paralelizable:** sí con GAN-003/004 tras core.

<a id="agro-gan-003"></a>

## AGRO-GAN-003 — Conservar composición y ubicación temporal

- **Release, épica, prioridad y tamaño:** R4 · EPIC-07 · Must · L.
- **Owner y colaboradores:** Livestock; GIS, Grazing, Data, Frontend y QA.
- **Resultado/valor esperado:** historial de rodeos/grupos y ocupación sin períodos incompatibles.
- **Historia/JTBD:** Como auditor, quiero saber qué animales componían cada grupo y dónde estaban en una fecha.
- **Alcance incluido:** pertenencia anterior/posterior, cambios de categoría, ubicaciones, movimientos internos/externos y documentos aplicables.
- **Fuera de alcance:** movimiento autónomo por recomendación e integración SENASA.
- **Requisitos trazados:** RF-GAN-004/005; RN-GAN-003/004/006; RN-CORE-002/004.
- **Precondiciones y dependencias:** GAN-002, GIS-002/003 y DOC-001.
- **Contrato/API/eventos afectados:** move/change-membership/location-at-date; `AnimalMoved`.
- **Datos, índices, migración y compatibilidad:** rangos temporales/membership/location/doc refs; exclusión de solapes.
- **Autenticación, autorización, tenant y auditoría:** origen/destino/alcance; confirmación/rectificación sensible.
- **Frontend:** línea de tiempo/mapa/lista, valores anterior/posterior y estados de conflicto/dato obsoleto.
- **Reglas e invariantes:** no dos ubicaciones incompatibles; historia no se sobrescribe.
- **Criterios de aceptación:** Dado movimiento confirmado, cuando consulta antes/después, entonces composición/ubicación correctas y documento vinculado.
- **Casos negativos y bordes:** movimiento retroactivo, solape, grupo vacío, traslado externo sin documento y concurrencia.
- **Estrategia de pruebas:** temporal/property, PostGIS, concurrency, auth y E2E.
- **Observabilidad:** movimientos/conflictos/rectificaciones.
- **Seguridad y privacidad:** BOLA y documentos privados.
- **Performance/capacidad y límites:** historia paginada y lookup indexado.
- **Feature flag, rollout, migración, rollback y recuperación:** reversa crea evento compensatorio; no delete.
- **Documentación:** estados/temporalidad.
- **Comandos/evidencia esperados:** integration/E2E futuros.
- **Definition of Ready:** reglas de compatibilidad temporal.
- **Definition of Done:** historia reconstruible/concurrente.
- **Bloqueos/preguntas:** Q-040 grupos simultáneos.
- **Paralelizable:** sí con GAN-004.

<a id="agro-gan-004"></a>

## AGRO-GAN-004 — Registrar peso, reproducción, sanidad y alimentación aplicables

- **Release, épica, prioridad y tamaño:** R4 · EPIC-07 · Must · L.
- **Owner y colaboradores:** Livestock; Veterinario, Inventory, Documents, Frontend y QA.
- **Resultado/valor esperado:** eventos pecuarios trazables por perfil sin prescripción automática.
- **Historia/JTBD:** Como veterinario/responsable, quiero registrar mediciones, reproducción, tratamientos y alimentación aplicables.
- **Alcance incluido:** peso/biomasa/condición/categoría, eventos reproductivos, plan sanitario, enfermedad, tratamiento/dosis/partida/carencia, suplemento/pastoreo real.
- **Fuera de alcance:** prescribir medicamentos/raciones, aplicar todo evento a toda especie y reservas de suplemento Should completas.
- **Requisitos trazados:** RF-GAN-006–009/018; RN-GAN-005/014; RF-INV-003; Q-036/037/041/042.
- **Precondiciones y dependencias:** GAN-001/002, INV-001 y perfiles veterinarios.
- **Contrato/API/eventos afectados:** record measurement/reproductive/health/feed; `TreatmentApplied`.
- **Datos, índices, migración y compatibilidad:** profile-specific event schema, product/batch/dose/withdrawal/professional/version.
- **Autenticación, autorización, tenant y auditoría:** rol veterinario/técnico, aprobación cuando corresponda, evento sensible.
- **Frontend:** formularios por perfil, alertas carencia, loading/error/conflict y a11y.
- **Reglas e invariantes:** carencia/producto/partida/responsable conservados; inaplicable se oculta/abstiene.
- **Criterios de aceptación:** Dado tratamiento aplicable, cuando confirma, entonces consume/traza partida una vez y carencia bloquea/advierte según perfil.
- **Casos negativos y bordes:** dosis/unidad inválida, partida vencida, evento retroactivo, perfil sin campo y tratamiento corregido.
- **Estrategia de pruebas:** schema/profile isolation, inventory integration, temporal, E2E y veterinario.
- **Observabilidad:** eventos/carencias/errores/abstenciones.
- **Seguridad y privacidad:** mínimo acceso, documentos/diagnóstico protegidos.
- **Performance/capacidad y límites:** eventos masivos paginados/import posterior.
- **Feature flag, rollout, migración, rollback y recuperación:** profile-version; rectificación y kill especialización.
- **Documentación:** perfiles/eventos/límites.
- **Comandos/evidencia esperados:** tests por perfil y aprobación veterinaria.
- **Definition of Ready:** eventos/campos/carencias aprobados.
- **Definition of Done:** applicable events trazables sin prescripción.
- **Bloqueos/preguntas:** Q-036/037/041/042.
- **Paralelizable:** sí por event family con schema común.

<a id="agro-gan-005"></a>

## AGRO-GAN-005 — Importar RFID y conciliar trazabilidad externa

- **Release, épica, prioridad y tamaño:** R7 · EPIC-07 · Should · M.
- **Owner y colaboradores:** Livestock/Integrations; Hardware, SENASA liaison, QA y AppSec.
- **Resultado/valor esperado:** reducir carga duplicada cuando formatos/mecanismos sean factibles.
- **Historia/JTBD:** Como responsable, quiero importar lecturas y conciliar con existencias sin escribir portales no autorizados.
- **Alcance incluido:** archivo/dispositivo priorizado, preview/mapping/errors, idempotencia y conciliación con ID/stock/DT-e importado.
- **Fuera de alcance:** scraping/escritura SIGSA sin API/convenio.
- **Requisitos trazados:** RF-GAN-010; RN-GAN-001/007; RF-ADM-002/004; Q-065.
- **Precondiciones y dependencias:** GAN-002, factibilidad/formato y proveedor/dispositivo.
- **Contrato/API/eventos afectados:** RFID import/reconcile job.
- **Datos, índices, migración y compatibilidad:** source/external ID/hash/timestamp/mapping y inbox.
- **Autenticación, autorización, tenant y auditoría:** upload/field scope; import auditable.
- **Frontend:** preview/progreso/errores/conciliación responsive.
- **Reglas e invariantes:** no duplica; conflicto requiere humano; portal no equivale API.
- **Criterios de aceptación:** Dado archivo repetido/conflictivo, cuando importa, entonces no duplica y produce reporte conciliable.
- **Casos negativos y bordes:** lectura desconocida, ID reutilizado, encoding, timezone y dispositivo offline.
- **Estrategia de pruebas:** contract/fixtures, idempotencia, security upload y E2E.
- **Observabilidad:** jobs/errores/conflictos/último éxito.
- **Seguridad y privacidad:** archivos no confiables, device secrets y minimización.
- **Performance/capacidad y límites:** async 1.000+ según RNF-PER-003/004.
- **Feature flag, rollout, migración, rollback y recuperación:** flag adapter; rollback detiene ingesta y conserva conciliación.
- **Documentación:** formatos/factibilidad/runbook.
- **Comandos/evidencia esperados:** contract fixtures futuros.
- **Definition of Ready:** formato/mecanismo oficial/prioridad.
- **Definition of Done:** importación real conciliada; no gatea MVP.
- **Bloqueos/preguntas:** acceso SENASA/dispositivo.
- **Paralelizable:** sí con otros Should.
