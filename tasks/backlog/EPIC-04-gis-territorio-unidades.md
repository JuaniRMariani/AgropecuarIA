# EPIC-04 — GIS, territorio y unidades de manejo

Objetivo: cobertura argentina, mapa usable y geometrías versionadas que preserven historia. R1–R2; capas/importaciones avanzadas R7.

<a id="agro-gis-001"></a>

## AGRO-GIS-001 — Normalizar territorio oficial en 24 jurisdicciones

- **Release, épica, prioridad y tamaño:** R1/R2 · EPIC-04 · Must · M.
- **Owner y colaboradores:** GIS/Integrations; Data, Frontend, Weather y QA.
- **Resultado/valor esperado:** altas con códigos oficiales/versionados y fallback honesto nacional.
- **Historia/JTBD:** Como productor, quiero ubicar mi campo en cualquier provincia sin nombres ambiguos.
- **Alcance incluido:** snapshot Georef, provincia/departamento/municipio/localidad, búsqueda/resolución por coordenada, cache/fallback.
- **Fuera de alcance:** catastro/parcela legal y restricción productiva por región.
- **Requisitos trazados:** RF-GIS-001/011; RN-GIS-001; RNF-GEO-001; Q-012.
- **Precondiciones y dependencias:** DIS-004, FND-002 y fixtures 24 puntos.
- **Contrato/API/eventos afectados:** territory search/resolve, source/version/freshness.
- **Datos, índices, migración y compatibilidad:** IDs oficiales/snapshot/mappings; actualización aditiva y sucesión.
- **Autenticación, autorización, tenant y auditoría:** baseline territorial compartido; campos tenant autorizados.
- **Frontend:** autocomplete/selección y degradación/fuente; responsive/teclado.
- **Reglas e invariantes:** Georef no reemplaza geometría/parcela; falta de cobertura no inventa código.
- **Criterios de aceptación:** Dado un punto por provincia+CABA, cuando se normaliza, entonces conserva códigos/fuente o muestra degradación explícita.
- **Casos negativos y bordes:** nombres repetidos, cambios de código, punto limítrofe, API caída y localidad ausente.
- **Estrategia de pruebas:** contrato/fixtures, 24 smoke, fallback/versionado y performance.
- **Observabilidad:** último éxito/frescura/cobertura/errores por jurisdicción.
- **Seguridad y privacidad:** minimizar coordenadas externas; validación de payload.
- **Performance/capacidad y límites:** cache y búsqueda dentro de target definido en DIS-007.
- **Feature flag, rollout, migración, rollback y recuperación:** snapshot activo reversible al último correcto.
- **Documentación:** fuente/atribución/mapeos/runbook.
- **Comandos/evidencia esperados:** matriz nacional automatizada futura.
- **Definition of Ready:** snapshot/IDs/fallback acordados.
- **Definition of Done:** 24 smoke aprobados.
- **Bloqueos/preguntas:** Q-012/proveedor.
- **Paralelizable:** sí con GIS-002 y CLI-001.

<a id="agro-gis-002"></a>

## AGRO-GIS-002 — Crear campos y unidades con mapa, área y ficha

- **Release, épica, prioridad y tamaño:** R2 · EPIC-04 · Must · L.
- **Owner y colaboradores:** GIS; Frontend, Productive Core, Data, AppSec y QA.
- **Resultado/valor esperado:** dibujo/edición/selección con superficie declarada/calculada y alternativa accesible.
- **Historia/JTBD:** Como productor, quiero dibujar campo/lote/potrero y abrir su historia desde el mapa.
- **Alcance incluido:** establecimiento/campo/parcela separada, GPS/búsqueda, MultiPolygon/punto/no espacial por tipo, color/uso/vigencia, área/ficha.
- **Fuera de alcance:** offline, tiles OSM públicos productivos y capas Should.
- **Requisitos trazados:** RF-GIS-001–005/007; RF-PRD-003; RN-GIS-001–003; RNF-PER-002; RNF-UX-001/003/004; ADR-002.
- **Precondiciones y dependencias:** GIS-001, CAT-003, DIS-004 y proveedor tiles.
- **Contrato/API/eventos afectados:** validate/create/edit/select geometry/unit.
- **Datos, índices, migración y compatibilidad:** SRID explícito, GiST, área calculada/declarada/tolerancia y límites.
- **Autenticación, autorización, tenant y auditoría:** campo/recurso/acción; activaciones auditadas; UUID corto en UI.
- **Frontend:** MapLibre client island, alternativa tabular/formulario, táctil/teclado, loading/empty/error/conflict.
- **Reglas e invariantes:** geometría activa válida; área calculada no negativa; diferencia fuera de tolerancia advierte/bloquea según política.
- **Criterios de aceptación:** Dado polígono válido, cuando se guarda, entonces área/fuente/SRID y declarada quedan separadas y la ficha abre por selección o lista.
- **Casos negativos y bordes:** self-intersection, huecos, vértices/tamaño, punto fuera de país, tile caído y unidad sin geometría.
- **Estrategia de pruebas:** PostGIS real, property GIS, E2E mapa/lista, a11y y performance 4G.
- **Observabilidad:** latencia/errores geométricos/tiles y queries lentas.
- **Seguridad y privacidad:** límites anti-DoS, coordenadas confidenciales y BOLA.
- **Performance/capacidad y límites:** mapa ≤3 s p75; vértices/tamaño desde spike.
- **Feature flag, rollout, migración, rollback y recuperación:** `gis-edit`; canary piloto; rollback a lectura/versión previa.
- **Documentación:** guía mapa, precisión y proveedor/atribución.
- **Comandos/evidencia esperados:** suite PostGIS/E2E/performance futura.
- **Definition of Ready:** contrato espacial/tolerancias/tiles aceptados.
- **Definition of Done:** mapa+alternativa accesible y validaciones aprobadas.
- **Bloqueos/preguntas:** tolerancia/solape y proveedor tiles.
- **Paralelizable:** frontend/backend/tests con contratos; no dos editores de contrato.

<a id="agro-gis-003"></a>

## AGRO-GIS-003 — Versionar, subdividir y fusionar sin perder historia

- **Release, épica, prioridad y tamaño:** R2 · EPIC-04 · Must · L.
- **Owner y colaboradores:** GIS; Operations, Data, Frontend y QA.
- **Resultado/valor esperado:** cambios de límites reconstruibles por fecha efectiva y sin “last write wins”.
- **Historia/JTBD:** Como auditor, quiero ver la geometría vigente cuando ocurrió cada evento.
- **Alcance incluido:** GeometryVersion, vigencias, motivo/autor, activar, subdividir/fusionar, linaje y conflicto optimista.
- **Fuera de alcance:** reescribir eventos históricos y borrado de predecesores.
- **Requisitos trazados:** RF-GIS-005–007; RN-GIS-004–007; RN-CORE-002/004; ADR-002.
- **Precondiciones y dependencias:** GIS-002 y FND-003.
- **Contrato/API/eventos afectados:** activate/subdivide/merge/history; `FieldGeometryActivated`.
- **Datos, índices, migración y compatibilidad:** rangos vigencia, predecesor/sucesor, version/ETag y GiST.
- **Autenticación, autorización, tenant y auditoría:** permisos de edición/aprobación y valores anterior/posterior permitidos.
- **Frontend:** timeline/diff/conflict, selección de versión y confirmación accesible.
- **Reglas e invariantes:** evento resuelve fecha efectiva; versiones simultáneas conflictivas no se pisan.
- **Criterios de aceptación:** Dado lote subdividido el 1/7 y labor 20/6, cuando se consulta, entonces labor usa versión anterior y sucesores conservan linaje.
- **Casos negativos y bordes:** vigencias solapadas, merge parcial, edición concurrente y área no conservada dentro de tolerancia.
- **Estrategia de pruebas:** integración/temporal, property área/linaje, concurrencia y E2E.
- **Observabilidad:** activaciones/conflictos/queries espaciales y auditoría.
- **Seguridad y privacidad:** tenant/objeto y diffs autorizados.
- **Performance/capacidad y límites:** consulta temporal/indexada y operaciones grandes acotadas.
- **Feature flag, rollout, migración, rollback y recuperación:** activar versión nueva; rollback reactiva previa con nueva decisión auditada.
- **Documentación:** reglas de versión/linaje/rectificación.
- **Comandos/evidencia esperados:** pruebas PostGIS/ETag futuras.
- **Definition of Ready:** semántica vigencia/solape aprobada.
- **Definition of Done:** historia/concurrencia/rollback demostrados.
- **Bloqueos/preguntas:** política de solapes configurables.
- **Paralelizable:** sí con CLI-001 después de contrato de referencia.

<a id="agro-gis-004"></a>

## AGRO-GIS-004 — Incorporar intercambio GIS y capas opcionales

- **Release, épica, prioridad y tamaño:** R7 · EPIC-04 · Should · M.
- **Owner y colaboradores:** GIS/Integrations; Frontend, AppSec y QA.
- **Resultado/valor esperado:** interoperar GeoJSON/KML y capas validadas sin condicionar MVP.
- **Historia/JTBD:** Como técnico, quiero importar/exportar geometrías y consultar infraestructura/capas útiles.
- **Alcance incluido:** GeoJSON/KML, preview/validación/reporte, caminos/aguadas/corrales/silos/ambientes si se priorizan.
- **Fuera de alcance:** Shapefile/ISOXML sin demanda, NDVI no decidido y offline.
- **Requisitos trazados:** RF-GIS-008/009; RF-GIS-010 excepción; Q-010/029/047.
- **Precondiciones y dependencias:** GIS-002/003 y decisión R7/licencias.
- **Contrato/API/eventos afectados:** import/export/layer catalog jobs.
- **Datos, índices, migración y compatibilidad:** staging, SRID/conversión y procedencia; sin sobrescritura silenciosa.
- **Autenticación, autorización, tenant y auditoría:** upload autorizado, límites/AV y export step-up.
- **Frontend:** preview/mapa/lista, progreso/errores y alternativa accesible.
- **Reglas e invariantes:** validación antes de activar; offline sigue ausente.
- **Criterios de aceptación:** Dado archivo válido/erróneo, cuando se importa, entonces preview/reporta por feature y solo confirmación crea versión.
- **Casos negativos y bordes:** zip bomb, SRID desconocido, geometría mixta, licencia y capa caída.
- **Estrategia de pruebas:** contrato/archivos, SSRF/upload, PostGIS, E2E y performance.
- **Observabilidad:** jobs/tamaño/rechazos/capa freshness.
- **Seguridad y privacidad:** allow-list, AV, cuotas y BOLA.
- **Performance/capacidad y límites:** asíncrono y límites de vértices/archivo.
- **Feature flag, rollout, migración, rollback y recuperación:** flag por formato/capa; rollback desactiva proveedor.
- **Documentación:** formatos/licencias/atribución.
- **Comandos/evidencia esperados:** fixtures/contract tests futuros.
- **Definition of Ready:** business case y proveedor/licencia aprobados.
- **Definition of Done:** interoperabilidad validada; no gatea MVP.
- **Bloqueos/preguntas:** Q-010/029/047.
- **Paralelizable:** sí con otras tareas R7.
