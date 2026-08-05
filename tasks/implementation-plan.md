# Plan maestro de implementación de AgropecuarIA

Versión de planificación: 1.0 — 2026-08-04. Estado inicial: propuesto para aprobación. Este documento define estrategia y secuencia; el detalle implementable vive en [`backlog/00-index.md`](backlog/00-index.md).

## Objetivo

Convertir el discovery en releases verificables de un SaaS multiempresa para producción agropecuaria argentina, entregando vertical slices que integren contrato, dominio, datos, UX, autorización, pruebas, telemetría, documentación y recuperación. El producto debe conservar utilidad transaccional sin proveedores externos ni LLM.

## Resultados medibles

- Dos organizaciones completan flujos equivalentes sin acceso cruzado por API, RLS, jobs, cache, archivos, exportes o analítica.
- El 100 % de `Catálogo Nacional v1` completa el flujo productivo común o posee excepción aprobada.
- Los 24 puntos territoriales —23 provincias y CABA— superan alta, normalización, mapa y clima/degradación.
- Una labor confirmada genera stock y costo exactamente una vez y puede rectificarse sin borrar el original.
- Agricultura y ganadería común conservan historia efectiva; perfiles especializados no contaminan actividades incompatibles.
- La rotación diferencia `OBSERVADO`, `ESTIMADO` y `SEGURIDAD_INSUFICIENTE`; ningún riesgo bloqueante se omite.
- El paquete contable canónico concilia con la interfaz sin afirmar compatibilidad externa ni validez fiscal.
- Las respuestas IA citan evidencia autorizada, se abstienen correctamente y pueden deshabilitarse sin afectar el núcleo.
- Se demuestran SLO iniciales, accesibilidad WCAG 2.2 AA aplicable, RPO/RTO y restore.

## Alcance

### MVP: R0–R6

Identidad moderna; organizaciones y autorización por recurso; catálogo nacional y núcleo productivo; GIS/versiones; clima/Open-Meteo propuesto/SMN CAP; agricultura; ganadería común; forraje/rotación; inventario/activos; economía de gestión; documentos/auditoría; portabilidad; paquete contable canónico; tableros; IA read-only explicable; seguridad, plataforma, observabilidad y QA.

### Posterior priorizable: R7

Requisitos `Should`/`Could`: delegación temporal, import/export GIS y capas, calendario/cargas masivas, receta/mapas de rendimiento, RFID, medición/evaluación climática avanzada, IoT/satélite, mantenimiento, conciliaciones, escenarios IA avanzados e integraciones agro oficiales cuya factibilidad sea comprobada.

### Fuera de alcance actual

- RF-GIS-010: offline, mapas descargables y sincronización de dispositivos.
- RF-FIN-009 y RN-FIS-001–007: emisión/sincronización ARCA y lógica fiscal de comprobantes.
- Contabilidad legal/impositiva integral, nómina, scraping estatal, microservicios, Kubernetes, event sourcing completo, control autónomo y formulación automática de raciones/dosis/tratamientos.

Las excepciones canónicas se detallan en [`decisions-and-gaps.md`](decisions-and-gaps.md#excepciones-explícitas-de-trazabilidad).

## Supuestos controlados

| Supuesto | Vence / se valida en | Si no se confirma |
|---|---|---|
| .NET/Next.js/PostgreSQL/PostGIS y proveedores cumplen condiciones vigentes. | Spikes R0 y revisión de fuentes oficiales antes de adoptar. | Elegir alternativa detrás del mismo contrato; no cambiar el dominio. |
| Open-Meteo comercial es viable como primario. | AGRO-DIS-004. | Usar otro `WeatherProvider`; CAP sigue autoritativo. |
| SMN WRF es operable a costo aceptable. | AGRO-DIS-004. | Postergar WRF sin bloquear R2. |
| El baseline nacional puede congelarse con denominador/excepciones. | AGRO-DIS-001. | R1 no publica catálogo y R3 no puede certificar cobertura. |
| Los perfiles iniciales tendrán especialistas nominados. | AGRO-DIS-002. | Solo `FLUJO_GENERICO`; las reglas especializadas se abstienen. |
| El paquete canónico puede acordarse antes del formato del contador. | AGRO-DIS-006. | R5 genera modelo interno provisional solo si contador aprueba conceptos; el adaptador sigue bloqueado. |
| Volúmenes iniciales caben en monolito modular y base relacional. | Q-020/AGRO-DIS-007. | Ajustar índices, límites y capacidad; extraer servicio solo con evidencia. |

## Principios de implementación

1. Cada slice entrega un recorrido demostrable; no existen fases aisladas de “backend”, “frontend” o “tests”.
2. `tenant_id`, autorización por recurso, auditoría e idempotencia se diseñan y prueban dentro de cada mutación.
3. Catálogo global y extensiones tenant permanecen separados. Una entrada usada se inactiva/sucede, nunca se borra.
4. Fecha efectiva, fecha de registro, origen, unidad/moneda original, versión y procedencia se preservan.
5. Estado actual más eventos/auditoría suficientes; no event sourcing completo.
6. Transacciones fuertes para invariantes del monolito; outbox/inbox para efectos externos y proyecciones.
7. Proveedor externo: spike, contrato, fixtures, timeout, retry selectivo, circuit breaker, frescura y fallback antes de ser crítico.
8. IA y cálculos determinísticos se separan. El LLM explica; no hace aritmética crítica ni muta.
9. Actividad + perfil + versión + jurisdicción + aprobador son obligatorios para especialización; incompatibilidad produce abstención.
10. Accesibilidad, seguridad, observabilidad, documentación, migración y recuperación pertenecen al DoD.

## Arquitectura objetivo y límites

Monolito modular ASP.NET Core/.NET, worker del mismo repositorio, web Next.js/React/TypeScript, PostgreSQL/PostGIS, almacenamiento privado de objetos, interfaces de proveedor y OpenTelemetry/OTLP. Los bounded contexts son:

- Identity/Tenancy;
- Territory/GIS;
- National Catalog;
- Productive Core;
- Operations;
- Agriculture;
- Livestock;
- Grazing/Forage;
- Weather/Agroclimate;
- Inventory y Assets;
- Commerce/Finance;
- Documents;
- Analytics/AI;
- Audit/Compliance;
- Integrations.

Cada módulo publica casos de uso/contratos y posee sus tablas. No se permiten consultas directas a persistencia ajena. `ManagementUnit` es identidad/ciclo de vida del núcleo productivo con representación espacial versionada del contexto Territory; esta división se ratifica en ADR-PEND-011 antes de implementar. ADR-PEND-007 queda reservado exclusivamente a RLS.

## Secuencia de releases

| Release | Objetivo demostrable | Dependencias de entrada | Hito de salida |
|---|---|---|---|
| R0 | Cerrar riesgos y acuerdos que no deben inventarse. | Discovery vigente. | Baseline/owner editorial; perfiles piloto; decisiones IdP/clima/storage; contrato contable canónico; dataset/capacidad. |
| R1 | Operar una organización aislada y registrar cualquier actividad común. | Spikes/ADR críticos de R0. | Dos tenants aislados; catálogo/núcleo publicados; auditoría, archivos, importación y restore base. |
| R2 | Campo GIS + clima + orden/parte con efectos exactamente una vez. | R1, PostGIS, contratos clima. | Campo versionado, CAP, labor con kernel stock/costo y timeline sin duplicación. |
| R3 | Campaña agrícola de extremo a extremo y certificación del flujo común. | R2, perfil agrícola aprobado, inventario. | Plan→labor→monitoreo→cosecha→partida→costo; 100 % baseline genérico. |
| R4 | Ganadería común y rotación segura/reconstruible. | R2 clima/GIS, R1 núcleo, perfiles ganaderos/forrajeros. | Stock/ubicación a fecha; recomendación observada/estimada/bloqueada y movimiento separado. |
| R5 | Gestión económica, cierre, portabilidad y paquete contable. | Movimientos R2–R4; política de gestión aprobada. | Período conciliado, exporte canónico y restore/portabilidad. |
| R6 | IA/analítica y piloto integral sin dependencia crítica del LLM. | Datos estructurados, evals y especialistas. | Groundedness/abstención/seguridad aprobados; kill switch y feedback operativo. |
| R7 | Capacidades posteriores priorizadas con evidencia. | Piloto y decisión del sponsor. | Solo slices `Should/Could` con business case, fuente y factibilidad. |

El detalle de entrada/salida, flags y rollback está en [`release-plan.md`](release-plan.md).

## Waves de ejecución

### Wave A — decisiones y cimientos (R0–R1)

Producto/dominio congela baseline y perfiles; arquitectura cierra límites/RLS/consistencia; plataforma prepara entornos/telemetría/restore; identidad, catálogo y núcleo común entregan los primeros recorridos. Paralelismo seguro: spike IdP, catálogo, GIS, clima, storage y contrato contable, con contratos de tenant/auditoría acordados.

### Wave B — territorio y operación (R2)

GIS y clima avanzan por contratos separados: pronóstico por punto no espera GIS completo; CAP por intersección sí. Operaciones integra kernels de inventario e imputación económica antes de confirmar la primera labor.

### Wave C — producción (R3–R4)

Agricultura y ganadería común pueden trabajar en paralelo tras estabilizar catálogo/núcleo/GIS/inventario. Forraje espera ganadería, clima, GIS y perfiles. QA mantiene suites parametrizadas de catálogo, territorio y tenant.

### Wave D — economía e inteligencia (R5–R6)

Finanzas cierra el circuito y genera el paquete canónico; analítica construye proyecciones por contrato/evento. IA se habilita por caso: clima tras R2, rotación tras R4 y economía tras R5, cada uno con eval y kill switch independientes.

## Dependencias maestras y correcciones al roadmap

- Identity/Tenancy + Audit son condición para toda persistencia tenant, pero no bloquean spikes sin datos reales.
- Catalog → Productive Core → especializaciones.
- GIS → asociación espacial de clima/CAP y pastoreo; pronóstico por coordenada puede desarrollarse en paralelo.
- Kernel Inventory + Cost Allocation entra en R2; inventario completo/activos se amplían después. Corrige la dependencia circular E08↔Operations/Agriculture.
- Operations confirma mediante contratos de Inventory/Finance dentro de una transacción del monolito; ninguna capa accede tablas ajenas.
- Livestock + Weather + Territory + perfiles → Grazing; Weather nunca depende de Grazing.
- Package canónico → adaptador del contador; nunca al revés.
- Datos/motores determinísticos → IA. Analytics/IA no es dependencia para confirmar operaciones.
- WRF depende de un spike positivo y no bloquea R2.

## Contratos y compatibilidad

- HTTP/OpenAPI versionado, errores estructurados, paginación/límites y ETag/If-Match donde hay concurrencia.
- Clave idempotente por tenant/operación/actor; reutilización con payload diferente es conflicto.
- Eventos internos versionados con ID, tenant, correlación, fechas, origen y versión; sin payload sensible innecesario.
- Migraciones `expand → backfill reanudable/compatibilidad → contract`; preferencia por roll-forward y restauración probada.
- Jobs/outbox/inbox con leasing, próximo intento, backoff, límite, conciliación y métricas; broker solo ante evidencia.
- Snapshots/ref históricos de catálogo, perfil, geometría, clima, fórmula y cotización usados por el hecho.

## Hitos de gobernanza

| Hito | Aprobadores | Evidencia |
|---|---|---|
| H0 Discovery ejecutable | Sponsor, PO, Arquitectura, QA | R0 listo, gaps con owner y backlog trazado. |
| H1 Fundación segura | Arquitectura, AppSec, Plataforma | Suite tenant, threat model, restore y ADR aceptados. |
| H2 Operación territorial | Productor, agrónomo, GIS/Weather, QA | Campo real anonimizado, clima/CAP y labor idempotente. |
| H3 Agricultura | Agrónomo + Product | Perfil/version/jurisdicción y recorrido agrícola aprobados. |
| H4 Ganadería/rotación | Agrónomo + veterinario + Product | Fórmulas/casos de abstención y reconstrucción aprobados. |
| H5 Economía | Contador + Product | Cierre y paquete canónico conciliados. |
| H6 Piloto | Sponsor + especialistas + Seguridad/QA/SRE | Evals, SLO, accesibilidad, restore, rollback y aceptación integral. |

## Definition of Ready transversal

Actor/job, valor, alcance/no alcance, RF/RN/RNF/ADR/Q, perfil/jurisdicción/aprobador aplicables, contrato, modelo/migración, permisos, estados UX, criterios/negativos, dataset, riesgos, telemetría, rollout/rollback y dependencias están explícitos. Una pregunta bloquea solo el comportamiento que no puede decidirse sin inventar; el resto permanece desarrollable.

## Definition of Done transversal

Slice vertical desplegable; criterios y negativos automatizados; tenant/authz/auditoría/idempotencia comprobados; responsive/WCAG/estados degradados; SLO/telemetría sin secretos; migración compatible y recuperación ensayada; documentación y trazabilidad actualizadas; sin altas/críticas; evidencia funcional del owner/especialista.

## Finalización del programa MVP

R0–R6 cumplen sus gates; 94 RF `Must`, 71 RN de MVP y 29 RNF están trazados a tareas/pruebas; las siete RN fiscales y dos RF `Won't now` tienen excepción explícita; no quedan tareas XL, ciclos, owners ausentes ni decisiones profesionales inventadas. Riesgos aceptados poseen owner/fecha y los runbooks de degradación, rollback, incidentes y restore fueron ejercitados.

## Documentos relacionados

- [Índice del backlog](backlog/00-index.md)
- [Matriz de trazabilidad](traceability-matrix.md)
- [Estrategia de pruebas](test-strategy.md)
- [Plan de releases](release-plan.md)
- [Registro de riesgos](risk-register.md)
- [Decisiones y gaps](decisions-and-gaps.md)
- [Workstreams](team-workstreams.md)
