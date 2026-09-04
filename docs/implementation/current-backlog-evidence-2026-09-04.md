# Inventario verificable de las 81 tareas — 2026-09-04

## Alcance y criterio

Inventario iniciado sobre el baseline Git `dd808e4`, actualizado con las entregas verificadas `3023288`, `8f25ed6` y los gates locales de la iteración 55 (635 backend, 346 web, 20 navegador). Publicación y CI remoto se distinguen en el [plan](../../tasks/todo%20.md). No es una certificación de release completo y las reparaciones no convierten automáticamente el padre en completado. Los hallazgos del baseline se conservan abajo como historia, no como defectos todos vigentes.

El [índice vigente al inicio](../../tasks/backlog/00-index.md) contiene 81 tareas: 1 `Completada`, 6 `En revisión`, 10 `En curso` y 64 `Propuesto`. Las iteraciones 38–49 agregaron implementación que el índice no refleja, pero compilación y tests unitarios no prueban una VSA de extremo a extremo.

Categorías de esta tabla:

- **C**: padre cerrado documentalmente, con evidencia histórica; sus invariantes igualmente requieren regresión en el runtime actual.
- **P**: implementación o evidencia parcial; quedan criterios del padre sin demostrar.
- **A**: no existe el resultado productivo solicitado. Puede haber contratos, documentación o un spike aislado.

Las dependencias son de implementación/verificación, no pedidos de reconfirmación al sponsor. Las decisiones técnicas reversibles ya delegadas se toman y registran. Los códigos E1–E6 identifican solamente gates externos reales; `—` significa que no se identificó una necesidad externa para iniciar la próxima implementación local, no que la tarea esté lista para producción.

## Evidencia inspeccionada

- [Plan y resultados históricos](../../tasks/todo%20.md), [lecciones](../../tasks/lessons%20.md), [README](../../README.md) y las 18 épicas del [backlog](../../tasks/backlog/00-index.md).
- Spikes descartables bajo `tasks/evidence/AGRO-DIS-*` y contratos bajo `contracts/`. Un spike no constituye una implementación productiva del módulo.
- Runtime: `src/AgropecuarIA.Identity`, `src/AgropecuarIA.Territory`, `src/AgropecuarIA.ProductiveCore`, `src/AgropecuarIA.Catalog`, `src/AgropecuarIA.Weather`, `apps/AgropecuarIA.Api`.
- Frontend: `apps/web/features/identity`, `territory`, `fields` y `workspace`; no había superficies productivas Catalog, Weather, Agriculture, Livestock, Grazing, Inventory, Finance, Documents o AI al baseline.
- Pruebas bajo `tests/`, pruebas web bajo `apps/web/tests/`, wrapper local bajo `scripts/identity/`. No existía `.github` con pipeline del proyecto al baseline.
- Actualización: `3023288` pasó 521 pruebas backend, 286 web y 16 de navegador; `8f25ed6`, 593 backend, 346 web y 20 de navegador local. Se agregó el lector Catalog y CI de verificación sin despliegue. CI remoto `33918321239` falló en concurrencia de cupo; la iteración 55 incluye una regresión determinista y su corrección, todavía pendiente de un nuevo run remoto.

## Las 81 tareas

Los IDs siguientes usan su prefijo completo para permitir validar unicidad y cobertura contra el índice.

| ID | Categoría | Evidencia y residuo concreto | Dependencia / siguiente entrega | Gate externo |
|---|---|---|---|---|
| AGRO-DIS-001 | P | Spike de catálogo/gobierno; no equivale a baseline nacional firmado. | Resolver excepciones y reunir aprobación editorial/profesional. | E1 |
| AGRO-DIS-002 | A | No se demostró panel constituido ni segundo productor consultado. | Taller, material consentido, perfiles y abstenciones versionadas. | E1 |
| AGRO-DIS-003 | P | Spike identidad/RLS y contratos; aceptación y proveedor real diferenciados. | Revisar riesgo residual; mantener fixtures locales y validar IdP compartido después. | E3 |
| AGRO-DIS-004 | P | PostGIS/parser/mapa aislados; no son pipeline productivo GIS/clima. | Consolidar decisiones de cobertura, licencias y degradación. | E3 |
| AGRO-DIS-005 | P | Spike cuarentena/restore; ciclo de archivos productivo inexistente. | Port de storage/AV, límites y aprobación de retención. | E4 |
| AGRO-DIS-006 | A | Contrato económico y políticas no firmados por contador. | Ejemplos canónicos conciliables sin inventar validación fiscal. | E2 |
| AGRO-DIS-007 | P | Escenarios sintéticos de capacidad/SLO/FinOps. | Separar targets locales de capacidad/costo medidos del piloto. | E1, E3 |
| AGRO-FND-001 | C | Único padre formalmente completado: límites, ownership y compatibilidad. | Repetir fitness contra nuevas dependencias y endpoints. | — |
| AGRO-FND-002 | P | Productores transaccionales e inbox con marcador previo al callback en la misma transacción/contexto, probados; no consumidor de negocio ni entrega externa. | Consumidor real, despacho/reintento/deduplicación y fallos de sink; no prometer exactly-once externo. | — |
| AGRO-FND-003 | P | Rename/ETag/migraciones parciales; no evolución y rectificación general. | Expand/contract N/N-1, backfill y restauración verificable. | — |
| AGRO-ID-001 | P | Login/OIDC/linking local y pruebas; integración real pendiente. | Regresión local y comprobación con IdP compartido. | E3 |
| AGRO-ID-002 | P | TOTP local con enrollment protegido/session-bound, CSRF y autenticación reciente; recovery atómico y 16 pruebas HTTP reales. | Step-up de login y ciclo productivo/auditoría/RLS completos, passkeys y recuperación sin sesión; no activar el proveedor local en producción. | E3 |
| AGRO-ID-003 | P | Organización/owners/invitaciones funcionales; servicio multirol/scope no integrado a API ni autorizaciones de recursos. | Endpoints autorizados, matriz por acción, scopes y pruebas cross-tenant. | — |
| AGRO-ID-004 | P | Revocación local propia/otras/todas demostrada históricamente. | Dispositivos/familias/notificación/propagación/SLO del padre. | E3, E4 |
| AGRO-ID-005 | A | Sin soporte JIT productivo. | ID-004 y SEC-002/004; consentimiento, caducidad y auditoría. | E4 |
| AGRO-CAT-001 | P | Ingesta JSON acotada y completamente validada, snapshots completos, diff/fingerprint y editor explícito separados del owner tenant, probados por HTTP/PG. | UI editorial, fuentes nacionales reales y acta de aprobación; no publicar el fixture sintético como baseline nacional. | E1 |
| AGRO-CAT-002 | P | Migración publicada, activación serializada, versiones/procedencia inmutables, rollback lógico, auditoría/outbox local y lector autenticado con E2E escritorio/móvil. | Gobierno/aceptación de baseline nacional y operación externa de publicación; el padre no cierra solo por la implementación local. | E1 |
| AGRO-CAT-003 | P | Ciclos/eventos autorizados; iteración 55 agrega puerto de resolución y snapshot histórico inmutable, con HTTP/PG y gates integrados locales aprobados. | Lecturas paginadas/UI, idempotencia/journal/eventos, contratos mínimos DOC/FIN y ciclo de vida completo. | — |
| AGRO-CAT-004 | A | Extensiones privadas/propuestas editoriales sin runtime. | CAT-002, ID-003; separar edición global y tenant. | — |
| AGRO-CAT-005 | A | Especialización validada/abstención no implementada. | DIS-002, CAT-002/003; perfil versionado con aprobación competente. | E1 |
| AGRO-GIS-001 | P | Georef/cache/territorio/UI y evidencia de fixtures nacionales. | Repetir 24 casos, fallback/versionado y verificar integración vigente. | — |
| AGRO-GIS-002 | P | Crear/listar/renombrar/archivar/ficha con E2E; geometría inicial PostGIS validada, área esferoidal y centroide de servidor, RLS/ETag/journal/outbox probados. | UI/mapa de configuración, edición/versionado espacial y criterios territoriales/profesionales; no equiparar geometría válida con catastro. | E3 |
| AGRO-GIS-003 | A | Sin subdivisión/fusión/historia espacial productiva. | GIS-002 y FND-003; versiones temporales y vínculos históricos. | — |
| AGRO-GIS-004 | A | Sin intercambio GIS/capas opcionales. | GIS-002/003; importación con límites y compatibilidad. | E3, E6 |
| AGRO-CLI-001 | P | Cliente/cache con configuración explícita, migraciones y owner/recurso verificados; aún acepta coordenadas del cliente y no tiene UI completa. | Derivar ubicación del campo, corregir metadata de modelo/tiempo/frescura y agregar lector con degradación. | E3 |
| AGRO-CLI-002 | P | Lluvia/rectificación con autorización, FORCE RLS y pruebas HTTP/PG; aún sin protocolo idempotente ni control completo de correcciones concurrentes. | Historial paginado, journal/idempotencia y UI sin duplicar observaciones rectificadas. | — |
| AGRO-CLI-003 | P | JSON de alerta y bbox; no ingesta oficial CAP y geometría/lifecycle completos. | GIS-002; parser/fetch oficial, references/update/cancel, intersección real. | E3 |
| AGRO-CLI-004 | P | Se retiraron defaults inventados: abstención explícita sin regla; reglas editoriales y aislamiento probados. | CLI-001/003, datos/frescura autoritativos y reglas versionadas con aceptación agronómica; no recomendaciones implícitas. | E1 |
| AGRO-CLI-005 | A | WRF existe como spike, no como servicio productivo. | Evidencia costo/calidad de DIS-004 y CLI-001/002. | E3, E6 |
| AGRO-AGR-001 | A | Sin campañas/presupuestos/órdenes aprobables. | CAT-003/005, GIS-002 e ID-003. | E1 para especialización |
| AGRO-AGR-002 | A | Sin parte confirmado con stock/costo único. | AGR-001, FND-002, INV-001, FIN-001. | E2 para política económica |
| AGRO-AGR-003 | A | Sin labores/dosis por perfil. | AGR-001/002, CAT-005 e INV-001. | E1 |
| AGRO-AGR-004 | A | Sin separación productiva monitoreo/recomendación/ejecución. | GIS-002, CAT-005, DOC-001. | E1 |
| AGRO-AGR-005 | A | Sin cosecha/almacenaje/plan versus real. | AGR-002/003, INV-001/002, FIN-001. | E1, E2 |
| AGRO-AGR-006 | A | Sin calendario/recordatorios/carga masiva. | AGR-001, INT-002, FE-002, IA-002. | — |
| AGRO-GAN-001 | A | Sin seguimiento pecuario por perfil. | CAT-003/005, DIS-002. | E1 |
| AGRO-GAN-002 | A | Sin ledger de identificadores/existencias pecuarias. | GAN-001, FND-002/003. | E1 para reglas por perfil |
| AGRO-GAN-003 | A | Sin composición/ubicación animal temporal. | GAN-002, GIS-002/003, DOC-001. | E1 para reglas por perfil |
| AGRO-GAN-004 | A | Sin registro especializado de peso/reproducción/sanidad/alimentación. | GAN-001/002, INV-001, perfiles veterinarios. | E1 |
| AGRO-GAN-005 | A | Sin RFID ni conciliación externa. | GAN-002 y contrato/dispositivo real. | E5 |
| AGRO-FOR-001 | A | Sin potreros/agua/restricciones como flujo productivo de pastoreo. | GIS-002/003, GAN-001 y perfil forrajero. | E1 |
| AGRO-FOR-002 | A | Sin medición de biomasa/escenarios. | FOR-001; medición opcional explícita y perfil aprobado. | E1 |
| AGRO-FOR-003 | A | Sin oferta/demanda/días/déficit determinísticos validados. | FOR-001/002, GAN-002/004, CLI-001/003. | E1 |
| AGRO-FOR-004 | A | Sin plan aprobado y movimiento separados. | FOR-003 y GAN-003. | E1 |
| AGRO-INV-001 | A | Sin partidas/movimientos/stock a fecha. | FND-002/003 y catálogos comunes. | — |
| AGRO-INV-002 | A | Sin reservas/mínimos/vencimientos. | INV-001 y AGR-001. | — |
| AGRO-INV-003 | A | Sin activos/valuaciones separadas. | DIS-006 y modelo económico. | E2 |
| AGRO-INV-004 | A | Sin mantenimiento/lecturas de activos. | INV-003, operaciones e inventario de repuestos. | — |
| AGRO-FIN-001 | A | Sin kernel de imputación única de costo operativo. | FND-002 y contrato mínimo DIS-006. | E2 |
| AGRO-FIN-002 | A | Sin operación/documento/tesorería separados. | FIN-001, DOC-001, DIS-006. | E2 |
| AGRO-FIN-003 | A | Sin multimoneda/presupuesto/valuación visible. | DIS-006, FIN-001/002, INV-003. | E2 |
| AGRO-FIN-004 | A | Sin cierre/reapertura auditados. | FIN-002/003 y autenticación reforzada. | E2 |
| AGRO-FIN-005 | A | Sin exporte canónico conciliado. | FIN-002/003/004, DOC-001/003, DIS-006. | E2; formato real E5 |
| AGRO-DOC-001 | A | Storage/AV solo en spike; sin adjuntos/descarga productivos. | DIS-005, ID-003, FND-002. | E3, E4 |
| AGRO-DOC-002 | A | Journal existente no es timeline autorizada de recursos. | FND-002/003 y retención explícita. | E4 |
| AGRO-DOC-003 | A | Sin exportación/rectificación/supresión operativas. | DOC-001/002 y SEC-004. | E4 |
| AGRO-IA-001 | A | Sin KPI/tableros reproducibles por rol. | Contratos de dominio y fórmulas versionadas. | E1/E2 según fórmula |
| AGRO-IA-002 | A | Sin centro de excepciones/vencimientos. | Emisores de alertas y autorización de identidad. | — |
| AGRO-IA-003 | A | Sin AI Gateway/herramientas de lectura/evidencia autorizada. | Datos/DOC, SEC y proveedor aislado. | E3, E4 |
| AGRO-IA-004 | A | Sin explicación de clima/rotación basada en herramientas. | CLI-001/002/003/004, FOR-003/004, IA-003. | E1 |
| AGRO-IA-005 | A | Sin evals/deriva/rollout IA operativo. | IA-003/004, corpus y panel profesional. | E1 |
| AGRO-IA-006 | A | Sin simulación avanzada. | IA-001/003/004/005 y perfiles aprobados. | E1, E6 |
| AGRO-FE-001 | P | Shell owner/contexto/deep-link con evidencia local. | Roles adicionales, preferencias, matriz browser y accesibilidad manual. | — |
| AGRO-FE-002 | P | Patrones identidad/campos y recuperación parcial. | FND-001/002/003, FE-001; reutilizar en catálogo y jobs reales. | — |
| AGRO-FE-003 | A | Sin mapa productivo/perfiles/journeys agricultura-ganadería-pastoreo. | FE-001/002 y contratos de dominio. | E1 para journeys piloto |
| AGRO-FE-004 | A | Sin certificación PWA online/rendimiento de releases. | FE-001/002/003 y presupuestos DIS-007. | — |
| AGRO-INT-001 | A | Inbox aislado no es conexiones/reintentos/reconciliación. | FND-002 y primer consumidor/proveedor concreto. | E3 para integración real |
| AGRO-INT-002 | A | Sin plantillas/preview/importación idempotente. | DOC-001, INT-001 y módulo destino. | E5 para formato real |
| AGRO-INT-003 | P | Fragmentos Catalog/Georef; no sincronización programada end-to-end. | INT-001, CAT-001, GIS-001. | E3 |
| AGRO-INT-004 | A | Sin evaluación posterior completa de factibilidad/prioridad. | Evidencia piloto y mecanismo oficial disponible. | E5, E6 |
| AGRO-SEC-001 | P | Registros runtime y de autorización actualizados por cada slice; no reemplazan la revisión completa del release/proveedores. | Inventario y threat model de capacidades nuevas, clasificación/retención y evidencia independiente. | E4 donde corresponda |
| AGRO-SEC-002 | P | HTTP/PG tenant, RLS y CSRF reales ampliados a Catalog, Weather, archivo, geometría y ciclos; revisión de cada slice. | Multirol/scopes ID-003, consumidor FND-002 y matriz completa de futuras superficies. | — |
| AGRO-SEC-003 | P | Nuevos módulos fail-closed, CSRF/editor separado, límites de ingesta y auditoría de dependencias; no certificación total. | SEC-001 y hardening de egress/archivos/proveedores/capacidades todavía ausentes. | — |
| AGRO-SEC-004 | A | Sin operaciones completas de privacidad/retención/incidentes. | SEC-001, DOC-001/002 e inventario de proveedores. | E4 |
| AGRO-PLT-001 | A | Bootstrap local no es promoción compatible entre entornos. | DIS-007, FND-001/003; empaquetado/configuración verificables. | E3 para aprovisionar |
| AGRO-PLT-002 | P | CI GitHub Actions de backend/arquitectura/frontend, lockfiles/auditorías y diagnósticos sin payloads; sin despliegue. | Resolver intermitencia detectada y verificar nuevo run; artefactos/promoción compatible y CD siguen pendientes. | E3 para servicios/despliegue externo |
| AGRO-PLT-003 | P | Instrumentación limitada Identity/Territory/ProductiveCore. | PLT-001 y DIS-007; métricas/alertas/SLO por integración. | E3 para backend compartido |
| AGRO-PLT-004 | P | Restore/capacidad en spikes; no DR del producto actual integrado. | DIS-005/007, PLT-001/003 y todos los almacenes efectivos. | E3 para restore compartido |
| AGRO-QA-001 | P | Existen fixtures/trazabilidad/tests; cobertura81 no cerrada. | DIS-001/002/007 y matriz AC→evidencia vigente. | E1 para oráculos reales |
| AGRO-QA-002 | P | Gates locales reales backend/PostGIS/web/navegador de las últimas entregas, con fixtures sintéticos y diagnóstico CI. | QA-001, cobertura de módulos restantes, matriz/piloto y repetición de gates por release. | E1/E3 para pruebas externas |
| AGRO-QA-003 | A | No hay cierre independiente de release completo. | QA-001/002, todas las tareas y riesgos explícitos del release. | E1/E2/E3/E4 según release |

## Hallazgos concretos del baseline

1. `CatalogEndpoints` requiere el rate limiter `catalog`, no registrado en `Program`; las mutaciones tampoco exigen autorización explícita. Resolver solo el limiter no resuelve la exposición.
2. La única migración Catalog crea snapshots/staging, no `catalog_published_versions` ni `catalog_published_items` que usan publicación/búsqueda.
3. Weather no tiene migraciones ni ruta de migración en startup, usa fallback `agro_weather_dev` y autentica sin autorizar tenant/campo. Las coordenadas vienen del cliente, no del recurso autorizado.
4. `WeatherActivityApplicationService` intentaba construir umbrales de pulverización/siembra/cosecha no aprobados. Además usaba `Guid.Empty` como creador aunque el dominio lo rechaza: no demostraba ni siquiera el fallback anunciado. Debe abstenerse, no reparar ese constructor para activar reglas inventadas.
5. `ProductiveInboxProcessor` ejecuta el callback antes de insertar/guardar el marcador, sin transacción explícita del conjunto ni consumidor real; no acredita efectos exactly-once ante carrera/crash/fallo de sink.
6. Nuevas migraciones de archive/ciclos/inbox no incluyen el mismo régimen RLS/grants de las tablas anteriores. Compile/unit tests no prueban que el rol runtime pueda ejecutar el flujo seguro.
7. La geometría se conserva como texto GeoJSON con área/centroide calculados por el cliente; falta validación espacial y cálculo server-side. CAP solo filtra bounding boxes y acepta JSON, sin la ingesta/lifecycle oficial prometidos.
8. El nuevo servicio de membresías/alcances está registrado pero sin endpoints ni integración con autorización por recurso. Los ciclos aceptan soporte/nombre/código de catálogo del cliente.
9. Los nuevos handlers de TOTP no realizan la validación antiforgery/step-up explícita presente en operaciones sensibles anteriores. Los cuatro métodos de `MfaApiIntegrationTests.cs` tenían cuerpos vacíos: esos tests verdes no prueban HTTP, persistencia ni recuperación; deben reemplazarse por pruebas con acciones y aserciones reales.

## Primeras tres entregas seleccionadas en el baseline

1. **Baseline de seguridad de nuevas superficies:** declarar autoridad fail-closed, CSRF y permisos por recurso; asegurar limiters/configuración y abstención explícita cuando faltan reglas. Tests de rutas positivas/negativas y regresión del runtime existente.
2. **ArchiveFieldDraft completo:** reparar políticas/migraciones y sinks, demostrar rollback transaccional en PostgreSQL y cuota/concurrencia; cerrar contrato, cliente y UI/E2E sin pérdida de datos históricos.
3. **Catálogo vertical:** tablas migradas, actor editorial explícito separado del owner tenant, ingesta/diff reproducibles, publicación/rollback concurrentes y búsqueda/UI verificadas. Un baseline sintético local no se etiqueta como aprobación nacional profesional.

Después: normalizar roles/scopes y geometría, cerrar clima operativo, documentos y consumidor real de outbox, inventario/kernel de costos/operaciones y los módulos restantes en orden de dependencias. Las reparaciones concretas elegidas por el líder pueden reordenarse según hallazgos de gates; la prioridad no autoriza ampliar credenciales, publicar datos o inventar políticas profesionales.

Las tres primeras entregas ya cuentan con implementación y gates locales, con los límites indicados en la tabla. Próximos residuos concretos: las lecturas de ciclos/timeline siguen ilimitadas y deben paginarse antes de agregar UI; algunas lecturas/autorizaciones del UOW traducen SQLSTATE 40001 a indisponibilidad (hallazgo estático separado del fallo de doble envoltorio reproducido al guardar). La evolución de escritores N/N-1 no está demostrada por las columnas aditivas del snapshot de ciclos: Productive 2.0 requiere despliegue coordinado, no realizado aquí.

## Gates externos reales

- **E1 — Profesionales y piloto:** personas competentes nominadas, segundo productor, material real consentido, aprobación de catálogo/perfiles/fórmulas y mediciones reales. Se puede implementar versionado, flujo genérico y abstención con fixtures; no fabricar firma, datos ni validación.
- **E2 — Contabilidad:** contador y aprobación de imputación/valuación/redondeo/cierre/schema canónico. Implementar contratos y ejemplos explícitamente sintéticos no demuestra validez fiscal ni compatibilidad con software real.
- **E3 — Entorno/proveedores:** credenciales IdP y servicios, licencias/entitlements comerciales, hosting, DNS/runner, presupuesto real y autorización de despliegue. Puertos/configuración y pruebas locales seguras no requieren esperar esos secretos.
- **E4 — Privacidad/legal:** aprobación competente de retención/supresión/holds, finalidad, DPA/región/subencargados y política de incidentes. Defaults técnicos reversibles no sustituyen asesoramiento ni autorización legal.
- **E5 — Integraciones/dispositivos:** acceso oficial real, formatos/muestras del software destino, RFID/dispositivos y consentimiento de conexión. Simuladores prueban contrato, no homologación ni acceso real.
- **E6 — Alcance posterior y presupuesto:** R7 ya está identificado como posterior, no como terminado. Ejecutar preparación reversible dentro de lo pedido no requiere repetir una pregunta de priorización; compra, contratación o habilitación externa sí conserva sus gates.

## Verificación del inventario

- Las 81 filas son únicas y corresponden al índice inicial; no se cambia el estado del backlog desde este documento.
- No se equipara un test unitario, un README de spike o una clase registrada con una VSA completada.
- La ausencia de credenciales/profesionales limita validación externa, no cancela la implementación local restante.
- Los resultados exactos de cada iteración y su estado remoto se registran en el [plan](../../tasks/todo%20.md); se conservan separados los hallazgos históricos, la implementación verificada y lo que sigue en verificación.
