# Índice del backlog

Estado inicial: todas las tareas están `Propuesto`; ninguna está en desarrollo. Las prioridades provienen del discovery. R0–R6 componen el MVP; R7 contiene `Should/Could` posteriores. Las excepciones `Won't now` están en [decisiones y gaps](../decisions-and-gaps.md#excepciones-explícitas-de-trazabilidad).

## Releases

| Release | Objetivo | Gate principal |
|---|---|---|
| R0 | Spikes, baseline, perfiles, contratos y oráculos | Decisiones/ADR/datasets aprobados |
| R1 | Tenant aislado, catálogo y núcleo común | Dos organizaciones sin fuga + restore base |
| R2 | GIS, clima y operación exactly-once | Campo/CAP/labor con stock-costo único |
| R3 | Agricultura y baseline completo | Flujo común 100 % + campaña agrícola |
| R4 | Ganadería y rotación | Stock temporal + evidencia/abstención segura |
| R5 | Economía y portabilidad | Cierre + paquete canónico conciliado |
| R6 | IA y piloto integral | Evals/red-team/kill switch + operación sin LLM |
| R7 | Posterior priorizado | Business case/factibilidad y mismos quality gates |

## Épicas

| Épica | Archivo | Resultado |
|---|---|---|
| EPIC-00 | [Discovery, spikes y validación](EPIC-00-discovery-spikes-validacion.md) | Decisiones no inventables resueltas |
| EPIC-01 | [Fundación y arquitectura](EPIC-01-fundacion-arquitectura.md) | Límites, idempotencia y compatibilidad |
| EPIC-02 | [Identidad y tenancy](EPIC-02-identidad-tenancy-autorizacion.md) | Acceso moderno y autorización por recurso |
| EPIC-03 | [Catálogo y núcleo](EPIC-03-catalogo-nucleo-productivo.md) | Cobertura nacional y flujo común |
| EPIC-04 | [GIS y territorio](EPIC-04-gis-territorio-unidades.md) | Geometrías/territorio versionados |
| EPIC-05 | [Clima y alertas](EPIC-05-clima-alertas.md) | Pronóstico/CAP/observación trazables |
| EPIC-06 | [Agricultura y operaciones](EPIC-06-agricultura-operaciones.md) | Campaña de extremo a extremo |
| EPIC-07 | [Ganadería común](EPIC-07-ganaderia-comun.md) | Existencias/eventos por perfil |
| EPIC-08 | [Forraje y rotación](EPIC-08-forraje-rotacion.md) | Alternativas seguras y humanas |
| EPIC-09 | [Inventario y activos](EPIC-09-inventario-activos.md) | Partidas/movimientos/recursos |
| EPIC-10 | [Gestión económica](EPIC-10-gestion-economica-contador.md) | Cierre y paquete canónico |
| EPIC-11 | [Documentos y auditoría](EPIC-11-documentos-auditoria.md) | Evidencia, timeline y portabilidad |
| EPIC-12 | [IA y analítica](EPIC-12-ia-analitica.md) | KPI/alertas/IA explicable |
| EPIC-13 | [Frontend y accesibilidad](EPIC-13-frontend-design-system-accesibilidad.md) | UX consistente/WCAG/PWA online |
| EPIC-14 | [Integraciones](EPIC-14-integraciones-importaciones.md) | Adapters/imports/fallback |
| EPIC-15 | [Seguridad y privacidad](EPIC-15-seguridad-privacidad.md) | Threat controls y privacy gates |
| EPIC-16 | [Plataforma y operación](EPIC-16-plataforma-observabilidad-operacion.md) | Delivery/SLO/restore/runbooks |
| EPIC-17 | [QA y readiness](EPIC-17-qa-release-readiness.md) | Evidencia independiente por release |

## Tareas

La columna “plan” contiene release, épica, MoSCoW y tamaño. Las dependencias abreviadas se desarrollan dentro de cada tarea.

| ID | Resultado | Plan | Estado | Dependencias principales |
|---|---|---|---|---|
| [AGRO-DIS-001](EPIC-00-discovery-spikes-validacion.md#agro-dis-001) | Aprobar Catálogo Nacional v1 y su gobierno | R0 · EPIC-00 · Must · M | En revisión | fuentes del discovery accesibles; sponsor nomina owner editorial y especialistas |
| [AGRO-DIS-002](EPIC-00-discovery-spikes-validacion.md#agro-dis-002) | Elegir perfiles piloto y constituir panel profesional | R0 · EPIC-00 · Must · M | Propuesto | material real anonimizable y disponibilidad de referentes |
| [AGRO-DIS-003](EPIC-00-discovery-spikes-validacion.md#agro-dis-003) | Validar identidad, account linking, RLS y modelo tenant | R0 · EPIC-00 · Must · M | En revisión | opciones de IdP, threat model inicial y modelos de actor/organización |
| [AGRO-DIS-004](EPIC-00-discovery-spikes-validacion.md#agro-dis-004) | Validar contrato GIS, mapas y meteorología multifuente | R0 · EPIC-00 · Must · L | En revisión | coordenadas piloto o fixtures nacionales y términos públicos/propuestas comerciales |
| [AGRO-DIS-005](EPIC-00-discovery-spikes-validacion.md#agro-dis-005) | Validar storage, antivirus, retención y restore integral | R0 · EPIC-00 · Must · M | En revisión | opciones de proveedor, clasificación y necesidades legales preliminares |
| [AGRO-DIS-006](EPIC-00-discovery-spikes-validacion.md#agro-dis-006) | Acordar políticas económicas y paquete contable canónico | R0 · EPIC-00 · Must · M | Propuesto | contador designado y muestra conceptual de operaciones del piloto |
| [AGRO-DIS-007](EPIC-00-discovery-spikes-validacion.md#agro-dis-007) | Fijar capacidad, SLO, equipo, costos y riesgo de conectividad | R0 · EPIC-00 · Must · M | En revisión | mediciones/estimaciones del piloto y disponibilidad de sponsor |
| [AGRO-FND-001](EPIC-01-fundacion-arquitectura.md#agro-fnd-001) | Ratificar límites modulares y contratos compatibles | R0/R1 · EPIC-01 · Must · M | En curso | AGRO-DIS-003/004; mapa de consumidores |
| [AGRO-FND-002](EPIC-01-fundacion-arquitectura.md#agro-fnd-002) | Ejecutar mutaciones tenant-safe exactamente una vez | R1 · EPIC-01 · Must · L | Propuesto | FND-001, tenancy/RLS y política de auditoría |
| [AGRO-FND-003](EPIC-01-fundacion-arquitectura.md#agro-fnd-003) | Evolucionar datos y contratos con conflicto explícito | R1 · EPIC-01 · Must · M | Propuesto | FND-001 y política de migración |
| [AGRO-ID-001](EPIC-02-identidad-tenancy-autorizacion.md#agro-id-001) | Registrar y vincular identidades sin duplicar usuarios | R1 · EPIC-02 · Must · M | Propuesto | AGRO-DIS-003 y FND-001 |
| [AGRO-ID-002](EPIC-02-identidad-tenancy-autorizacion.md#agro-id-002) | Activar passkeys, TOTP y recuperación resistente | R1 · EPIC-02 · Must · M | Propuesto | ID-001 y política MFA/roles |
| [AGRO-ID-003](EPIC-02-identidad-tenancy-autorizacion.md#agro-id-003) | Crear organización, invitar y asignar alcance por campo | R1 · EPIC-02 · Must · L | Propuesto | ID-001, FND-002 y matriz de roles |
| [AGRO-ID-004](EPIC-02-identidad-tenancy-autorizacion.md#agro-id-004) | Revocar sesiones y proteger acciones sensibles | R1 · EPIC-02 · Must · M | Propuesto | ID-001/002/003 y contrato de auditoría |
| [AGRO-ID-005](EPIC-02-identidad-tenancy-autorizacion.md#agro-id-005) | Delegar soporte JIT con consentimiento y caducidad | R7 · EPIC-02 · Should · M | Propuesto | ID-004, SEC-002/004 y política de soporte |
| [AGRO-CAT-001](EPIC-03-catalogo-nucleo-productivo.md#agro-cat-001) | Ingerir fuentes y producir un diff editorial reproducible | R1 · EPIC-03 · Must · L | Propuesto | DIS-001, FND-002 y owner editorial |
| [AGRO-CAT-002](EPIC-03-catalogo-nucleo-productivo.md#agro-cat-002) | Publicar, buscar y revertir Catálogo Nacional v1 | R1 · EPIC-03 · Must · L | Propuesto | CAT-001 y baseline aprobado |
| [AGRO-CAT-003](EPIC-03-catalogo-nucleo-productivo.md#agro-cat-003) | Registrar cualquier actividad mediante el núcleo común | R1/R3 · EPIC-03 · Must · L | Propuesto | CAT-002, FND-002/003 y contratos Document/Finance mínimos |
| [AGRO-CAT-004](EPIC-03-catalogo-nucleo-productivo.md#agro-cat-004) | Gestionar extensiones privadas y propuestas editoriales | R1 · EPIC-03 · Must · M | Propuesto | CAT-002, ID-003 y workflow editorial |
| [AGRO-CAT-005](EPIC-03-catalogo-nucleo-productivo.md#agro-cat-005) | Activar perfiles especializados con abstención segura | R1–R4 · EPIC-03 · Must · L | Propuesto | DIS-002, CAT-002/003 y panel profesional |
| [AGRO-GIS-001](EPIC-04-gis-territorio-unidades.md#agro-gis-001) | Normalizar territorio oficial en 24 jurisdicciones | R1/R2 · EPIC-04 · Must · M | Propuesto | DIS-004, FND-002 y fixtures 24 puntos |
| [AGRO-GIS-002](EPIC-04-gis-territorio-unidades.md#agro-gis-002) | Crear campos y unidades con mapa, área y ficha | R2 · EPIC-04 · Must · L | Propuesto | GIS-001, CAT-003, DIS-004 y proveedor tiles |
| [AGRO-GIS-003](EPIC-04-gis-territorio-unidades.md#agro-gis-003) | Versionar, subdividir y fusionar sin perder historia | R2 · EPIC-04 · Must · L | Propuesto | GIS-002 y FND-003 |
| [AGRO-GIS-004](EPIC-04-gis-territorio-unidades.md#agro-gis-004) | Incorporar intercambio GIS y capas opcionales | R7 · EPIC-04 · Should · M | Propuesto | GIS-002/003 y decisión R7/licencias |
| [AGRO-CLI-001](EPIC-05-clima-alertas.md#agro-cli-001) | Persistir pronósticos auditables con cache y degradación | R2 · EPIC-05 · Must · L | Propuesto | DIS-004, GIS-001/002 y FND-002 |
| [AGRO-CLI-002](EPIC-05-clima-alertas.md#agro-cli-002) | Registrar lluvia observada sin mezclarla con pronóstico | R2 · EPIC-05 · Must · M | Propuesto | CLI-001 y ubicación autorizada |
| [AGRO-CLI-003](EPIC-05-clima-alertas.md#agro-cli-003) | Ingerir alertas oficiales SMN CAP por geometría | R2 · EPIC-05 · Must · L | Propuesto | DIS-004, GIS-002 y contrato CAP confirmado |
| [AGRO-CLI-004](EPIC-05-clima-alertas.md#agro-cli-004) | Configurar alertas y ventanas por actividad | R2/R3 · EPIC-05 · Must · M | Propuesto | CLI-001/003 y VAL-AGR climática |
| [AGRO-CLI-005](EPIC-05-clima-alertas.md#agro-cli-005) | Incorporar WRF/evaluación avanzada solo tras evidencia | R7 · EPIC-05 · Should · L | Propuesto | resultado DIS-004, CLI-001/002 y presupuesto |
| [AGRO-AGR-001](EPIC-06-agricultura-operaciones.md#agro-agr-001) | Planificar campañas, presupuestos y órdenes aprobables | R2/R3 · EPIC-06 · Must · L | Propuesto | CAT-003/005, GIS-002 e ID-003 |
| [AGRO-AGR-002](EPIC-06-agricultura-operaciones.md#agro-agr-002) | Confirmar un parte con stock y costo una sola vez | R2 · EPIC-06 · Must · L | Propuesto | AGR-001, FND-002, INV-001 y FIN-001 kernel |
| [AGRO-AGR-003](EPIC-06-agricultura-operaciones.md#agro-agr-003) | Registrar labores y consistencia de dosis por perfil | R3 · EPIC-06 · Must · L | Propuesto | AGR-001/002, CAT-005 e INV-001/ACT |
| [AGRO-AGR-004](EPIC-06-agricultura-operaciones.md#agro-agr-004) | Separar monitoreo, recomendación y ejecución | R3 · EPIC-06 · Must · M | Propuesto | GIS-002, CAT-005 y DOC-001 |
| [AGRO-AGR-005](EPIC-06-agricultura-operaciones.md#agro-agr-005) | Cosechar, almacenar y comparar plan contra real | R3 · EPIC-06 · Must · L | Propuesto | AGR-002/003, INV-001/002 y FIN-001 |
| [AGRO-AGR-006](EPIC-06-agricultura-operaciones.md#agro-agr-006) | Operar calendario, recordatorios y carga masiva de actividades | R7 · EPIC-06 · Should · M | Propuesto | AGR-001, INT-002, FE-002 e IA-002 |
| [AGRO-GAN-001](EPIC-07-ganaderia-comun.md#agro-gan-001) | Configurar seguimiento pecuario por perfil | R4 · EPIC-07 · Must · L | Propuesto | CAT-003/005, DIS-002 y VAL-VET |
| [AGRO-GAN-002](EPIC-07-ganaderia-comun.md#agro-gan-002) | Mantener identificadores y existencias por eventos | R4 · EPIC-07 · Must · L | Propuesto | GAN-001 y FND-002/003 |
| [AGRO-GAN-003](EPIC-07-ganaderia-comun.md#agro-gan-003) | Conservar composición y ubicación temporal | R4 · EPIC-07 · Must · L | Propuesto | GAN-002, GIS-002/003 y DOC-001 |
| [AGRO-GAN-004](EPIC-07-ganaderia-comun.md#agro-gan-004) | Registrar peso, reproducción, sanidad y alimentación aplicables | R4 · EPIC-07 · Must · L | Propuesto | GAN-001/002, INV-001 y perfiles veterinarios |
| [AGRO-GAN-005](EPIC-07-ganaderia-comun.md#agro-gan-005) | Importar RFID y conciliar trazabilidad externa | R7 · EPIC-07 · Should · M | Propuesto | GAN-002, factibilidad/formato y proveedor/dispositivo |
| [AGRO-FOR-001](EPIC-08-forraje-rotacion.md#agro-for-001) | Configurar potreros, recursos, agua y restricciones | R4 · EPIC-08 · Must · L | Propuesto | GIS-002/003, GAN-001 y perfiles/VAL-FOR |
| [AGRO-FOR-002](EPIC-08-forraje-rotacion.md#agro-for-002) | Registrar biomasa opcional y escenarios estimados | R4 · EPIC-08 · Must · M | Propuesto | FOR-001 y perfil forrajero aprobado |
| [AGRO-FOR-003](EPIC-08-forraje-rotacion.md#agro-for-003) | Calcular oferta, demanda, días y déficit determinísticamente | R4 · EPIC-08 · Must · L | Propuesto | FOR-001/002, GAN-002/004, CLI-001/003 y perfil conjunto aprobado |
| [AGRO-FOR-004](EPIC-08-forraje-rotacion.md#agro-for-004) | Decidir plan y confirmar movimiento por separado | R4 · EPIC-08 · Must · L | Propuesto | FOR-003 y GAN-003 |
| [AGRO-INV-001](EPIC-09-inventario-activos.md#agro-inv-001) | Gestionar partidas, movimientos y stock a fecha | R2/R3 · EPIC-09 · Must · L | Propuesto | FND-002/003 y catálogos comunes |
| [AGRO-INV-002](EPIC-09-inventario-activos.md#agro-inv-002) | Reservar stock y alertar mínimos/vencimientos | R3 · EPIC-09 · Must · M | Propuesto | INV-001 y AGR-001 |
| [AGRO-INV-003](EPIC-09-inventario-activos.md#agro-inv-003) | Registrar activos y valuaciones separadas | R3/R5 · EPIC-09 · Must · M | Propuesto | DIS-006 y política contador |
| [AGRO-INV-004](EPIC-09-inventario-activos.md#agro-inv-004) | Mantener activos y lecturas operativas | R7 · EPIC-09 · Should · M | Propuesto | INV-003, Operations e Inventory parts |
| [AGRO-FIN-001](EPIC-10-gestion-economica-contador.md#agro-fin-001) | Imputar costo operativo mediante contrato mínimo | R2 · EPIC-10 · Must · M | Propuesto | FND-002 y DIS-006 mínimo |
| [AGRO-FIN-002](EPIC-10-gestion-economica-contador.md#agro-fin-002) | Registrar operaciones, documentos y tesorería separadas | R5 · EPIC-10 · Must · L | Propuesto | FIN-001, DOC-001, DIS-006 |
| [AGRO-FIN-003](EPIC-10-gestion-economica-contador.md#agro-fin-003) | Gestionar multimoneda, presupuesto y valuación visible | R5 · EPIC-10 · Must · L | Propuesto | DIS-006, FIN-001/002, INV-003 |
| [AGRO-FIN-004](EPIC-10-gestion-economica-contador.md#agro-fin-004) | Cerrar y reabrir períodos de forma auditada | R5 · EPIC-10 · Must · M | Propuesto | FIN-002/003 y autenticación reforzada |
| [AGRO-FIN-005](EPIC-10-gestion-economica-contador.md#agro-fin-005) | Generar paquete contable canónico conciliable | R5 · EPIC-10 · Must · L | Propuesto | FIN-002–004, DOC-001/003, DIS-006 |
| [AGRO-DOC-001](EPIC-11-documentos-auditoria.md#agro-doc-001) | Adjuntar y descargar archivos seguros y versionados | R1/R2 · EPIC-11 · Must · L | Propuesto | DIS-005, ID-003 y FND-002 |
| [AGRO-DOC-002](EPIC-11-documentos-auditoria.md#agro-doc-002) | Exponer línea de tiempo y auditoría de recurso | R1/R2 · EPIC-11 · Must · L | Propuesto | FND-002/003 y política de retención |
| [AGRO-DOC-003](EPIC-11-documentos-auditoria.md#agro-doc-003) | Exportar, rectificar y suprimir según política | R5 · EPIC-11 · Must · L | Propuesto | DOC-001/002, SEC-004 y validación legal/de política |
| [AGRO-IA-001](EPIC-12-ia-analitica.md#agro-ia-001) | Servir tableros y KPI reproducibles por rol | R5 · EPIC-12 · Must · L | Propuesto | eventos/contratos de dominio y fórmulas aprobadas |
| [AGRO-IA-002](EPIC-12-ia-analitica.md#agro-ia-002) | Centralizar alertas por excepción y vencimiento | R5 · EPIC-12 · Must · M | Propuesto | alertas de módulos y alcance de identidad |
| [AGRO-IA-003](EPIC-12-ia-analitica.md#agro-ia-003) | Operar AI Gateway de solo lectura y paquetes de evidencia autorizados | R6 · EPIC-12 · Must · L | Propuesto | datos estructurados, DOC, threat model SEC y proveedor/privacidad aprobados |
| [AGRO-IA-004](EPIC-12-ia-analitica.md#agro-ia-004) | Explicar clima y rotación desde herramientas determinísticas | R6 · EPIC-12 · Must · L | Propuesto | CLI-001–004, FOR-003/004 e IA-003 |
| [AGRO-IA-005](EPIC-12-ia-analitica.md#agro-ia-005) | Evaluar, monitorear deriva y controlar rollout IA | R6 · EPIC-12 · Must · L | Propuesto | IA-003/004 y panel profesional |
| [AGRO-IA-006](EPIC-12-ia-analitica.md#agro-ia-006) | Simular escenarios avanzados con cálculo determinístico y aprobación humana | R7 · EPIC-12 · Should · L | Propuesto | IA-001/003/004/005 y perfiles aprobados |
| [AGRO-FE-001](EPIC-13-frontend-design-system-accesibilidad.md#agro-fe-001) | Entregar shell, navegación y sistema de diseño accesibles | R1 · EPIC-13 · Must · L | Propuesto | contratos de Identidad y decisión de arquitectura frontend |
| [AGRO-FE-002](EPIC-13-frontend-design-system-accesibilidad.md#agro-fe-002) | Estandarizar contratos, formularios, tablas y estados | R1 · EPIC-13 · Must · L | Propuesto | FND-001–003 y FE-001 |
| [AGRO-FE-003](EPIC-13-frontend-design-system-accesibilidad.md#agro-fe-003) | Construir mapa y experiencias productivas adaptativas | R2–R4 · EPIC-13 · Must · L | Propuesto | FE-001/002 plus domain contracts |
| [AGRO-FE-004](EPIC-13-frontend-design-system-accesibilidad.md#agro-fe-004) | Certificar PWA online y rendimiento frontend | R2–R6 · EPIC-13 · Must · M | Propuesto | FE-001–003 y DIS-007 |
| [AGRO-INT-001](EPIC-14-integraciones-importaciones.md#agro-int-001) | Operar conexiones, inbox y reintentos conciliables | R1 · EPIC-14 · Must · L | Propuesto | FND-002 y contratos de proveedores |
| [AGRO-INT-002](EPIC-14-integraciones-importaciones.md#agro-int-002) | Importar datos mediante plantillas con vista previa | R1/R3 · EPIC-14 · Must · L | Propuesto | DOC-001, INT-001 y contratos de módulos destino |
| [AGRO-INT-003](EPIC-14-integraciones-importaciones.md#agro-int-003) | Sincronizar fuentes nacionales mediante staging/publicación | R1/R2 · EPIC-14 · Must · M | Propuesto | INT-001, CAT-001 y GIS-001 |
| [AGRO-INT-004](EPIC-14-integraciones-importaciones.md#agro-int-004) | Priorizar integraciones agro posteriores por factibilidad | R7 · EPIC-14 · Should/Could · M | Propuesto | evidencia del piloto y mecanismo oficial |
| [AGRO-SEC-001](EPIC-15-seguridad-privacidad.md#agro-sec-001) | Mantener el modelo de amenazas y la clasificación por release | R0–R6 · EPIC-15 · Must · M | En curso | arquitectura, flujos de datos e inventario de proveedores |
| [AGRO-SEC-002](EPIC-15-seguridad-privacidad.md#agro-sec-002) | Probar aislamiento por tenant y autorización exhaustiva | R1–R6 · EPIC-15 · Must · L | Propuesto | ID-003, FND-002 y políticas de recursos por módulo |
| [AGRO-SEC-003](EPIC-15-seguridad-privacidad.md#agro-sec-003) | Endurecer API, archivos, egress y cadena de suministro | R1–R6 · EPIC-15 · Must · L | Propuesto | SEC-001 y pipeline de plataforma |
| [AGRO-SEC-004](EPIC-15-seguridad-privacidad.md#agro-sec-004) | Operar privacidad, retención y respuesta a incidentes | R1–R6 · EPIC-15 · Must · L | Propuesto | SEC-001, contratos DOC-001/002 e inventario de proveedores |
| [AGRO-PLT-001](EPIC-16-plataforma-observabilidad-operacion.md#agro-plt-001) | Definir entornos y promover artefactos compatibles | R0/R1 · EPIC-16 · Must · L | Propuesto | DIS-007 y FND-001/003 |
| [AGRO-PLT-002](EPIC-16-plataforma-observabilidad-operacion.md#agro-plt-002) | Automatizar gates de calidad y cadena de suministro | R1 · EPIC-16 · Must · M | Propuesto | PLT-001 y estrategia de pruebas |
| [AGRO-PLT-003](EPIC-16-plataforma-observabilidad-operacion.md#agro-plt-003) | Instrumentar SLO e integraciones sin filtrar datos | R1/R2 · EPIC-16 · Must · L | Propuesto | PLT-001 y DIS-007 |
| [AGRO-PLT-004](EPIC-16-plataforma-observabilidad-operacion.md#agro-plt-004) | Demostrar backup, restore, resilience y costos | R1–R6 · EPIC-16 · Must · L | Propuesto | DIS-005/007, PLT-001/003 |
| [AGRO-QA-001](EPIC-17-qa-release-readiness.md#agro-qa-001) | Mantener trazabilidad, fixtures y arquitectura de pruebas | R0/R1 · EPIC-17 · Must · L | Propuesto | DIS-001/002/007 y backlog de tareas |
| [AGRO-QA-002](EPIC-17-qa-release-readiness.md#agro-qa-002) | Ejecutar suites funcionales, contractuales y no funcionales | R1–R6 · EPIC-17 · Must · L | Propuesto | QA-001 y slices de cada release |
| [AGRO-QA-003](EPIC-17-qa-release-readiness.md#agro-qa-003) | Emitir una evaluación independiente de preparación para release | cada release · EPIC-17 · Must · M | Propuesto | QA-001/002 y tareas del release completas |

## Resumen

- 8 releases planificadas: R0–R6 MVP y R7 posterior.
- 18 épicas y 81 tareas; tamaños XS/S/M/L, sin XL.
- 94 RF Must entran al MVP; 11 Should y 2 Could se programan/condicionan; 2 Won't now tienen excepción explícita.
- La secuencia maestra y las correcciones de dependencia están en [implementation-plan.md](../implementation-plan.md) y [release-plan.md](../release-plan.md).
