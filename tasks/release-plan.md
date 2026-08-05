# Plan de releases

Este documento es canónico para gates, rollout y recuperación. La secuencia responde al roadmap del discovery con dos correcciones: kernels mínimos de inventario/imputación se adelantan a R2 y el paquete contable no depende del adaptador específico.

## Política de release

- Una release puede desplegar infraestructura técnica incompleta solo detrás de flag y sin exponer capacidad no aceptada.
- Cada flag tiene owner, tenant objetivo, condición de habilitación, fecha de retiro y kill switch.
- Migraciones son compatibles N/N-1 mediante expansión y roll-forward; ninguna reversión borra hechos confirmados.
- Rollback de aplicación conserva datos; si el schema no admite reversión segura se desactiva la capacidad, se restaura solo ante pérdida/corrupción y se ejecuta roll-forward corregido.
- Proveedores externos se validan con fixtures y sandbox/endpoint público antes de producción. No se solicitan credenciales en planificación.
- La evidencia requerida se vincula desde la tarea y la matriz de trazabilidad.

## Vista general

| Release | Tipo | Objetivo | Épicas dominantes | Flag de salida |
|---|---|---|---|---|
| R0 | Discovery ejecutable/spikes | Eliminar decisiones que sería inseguro inventar. | EPIC-00, EPIC-17 | `planning-approved` |
| R1 | Fundación | Tenant aislado + catálogo/núcleo común. | EPIC-01/02/03/11/14/15/16/17 | `foundation-pilot` |
| R2 | Operación territorial | GIS + clima + orden con efecto único. | EPIC-04/05/06/09/11/13/14/17 | `territory-ops-pilot` |
| R3 | Agricultura | Campaña agrícola completa y baseline certificado. | EPIC-03/06/09/10/13/17 | `agriculture-profile:<version>` |
| R4 | Ganadería/rotación | Existencias y recomendación segura. | EPIC-07/08/05/09/12/13/17 | `grazing-profile:<version>` |
| R5 | Economía/portabilidad | Cierre y paquete contable canónico. | EPIC-10/11/12/14/16/17 | `finance-canonical-export` |
| R6 | IA/piloto integral | IA explicable evaluada, prescindible y controlada. | EPIC-12/15/16/17 | `ai-case:<case>:<tenant>` |
| R7 | Posterior | `Should/Could` priorizados por evidencia. | Todas según slice | Flag específico; sin compromiso previo. |

## R0 — Discovery ejecutable y spikes

**Objetivo demostrable:** producir decisiones, datasets y contratos suficientes para declarar Ready los primeros slices sin inventar normativa, perfiles, proveedores o escala.

**Entrada:** documentación de discovery completa; sponsor y referentes disponibles.

**Incluye:** baseline/owner editorial; taller con productor de referencia y segundo productor; perfil/jurisdicción/aprobadores; contrato espacial; IdP/account linking/recovery; Open-Meteo/CAP/WRF; tiles/Georef; storage/antivirus; RLS; esquema del paquete contable canónico; volumen/SLO/costo.

**Salida:**

- ADR-001–006 ratificados o revisados y ADR pendientes con owner.
- `Catálogo Nacional v1` congelado con denominador y excepciones.
- Matriz de soporte y perfiles iniciales firmados; sin perfil, decisión explícita de flujo genérico.
- Contratos/fixtures y go/no-go de proveedores; WRF puede quedar postergado.
- Dataset sintético/anonimizado y 24 puntos geográficos.
- Formato del contador puede seguir pendiente; conceptos/totales canónicos quedan definidos.

**Riesgos:** disponibilidad de especialistas, datos reales, proveedor y presupuesto. **Mitigación:** timeboxes, decisiones condicionales y fallback contractual.

**Rollout/rollback:** no hay producción. Los prototipos/spikes no se reutilizan como código productivo sin revisión. Reversión = descartar la opción y conservar evidencia/ADR.

**Evidencia:** actas, matriz de fuentes/perfiles, fixtures versionados, mediciones de spike, threat model inicial y criterios firmados.

## R1 — Fundación segura y núcleo común

**Objetivo demostrable:** dos organizaciones completan onboarding y una actividad genérica sin acceso cruzado.

**Entrada:** R0; IdP, tenancy/RLS, módulos, storage y baseline decididos.

**Incluye:** identidad/email/Google/passkey/TOTP/recovery; organización/invitación/alcance; contratos/errores/idempotencia/outbox; auditoría; archivos; importación base; catálogo publicar/buscar/rollback; extensión tenant; núcleo `ManagementUnit→Cycle→Event/Output`; shell/design system; CI, observabilidad y restore base.

**Salida:**

- Suite BOLA/RLS en API, job, cache, archivo/exporte aprobada.
- Catálogo muestra fuente/vigencia/jurisdicción/nivel y puede revertir versión activa.
- Cada familia/unidad representativa completa flujo genérico; la suite total continúa como gate R3.
- Acciones sensibles auditadas; archivos hostiles/cuarentena verificados.
- Restore inicial de base + metadatos/objetos demostrado.

**Flags:** `catalog-v1`, `generic-flow`, `passkey`, `totp`, habilitados primero en tenants internos y luego piloto.

**Migración:** esquemas aditivos, baseline publicado inmutable, backfill reanudable. **Rollback:** desactivar versión/flag; catálogo vuelve a versión previa sin reescribir históricos.

**Evidencia:** E2E onboarding/flujo común, pruebas tenant, informe restore, métricas/trazas, revisión WCAG de shell.

## R2 — GIS, clima y operación trazable

**Objetivo demostrable:** el piloto crea/versiona un campo, consulta clima/CAP y confirma una labor con stock/costo exactamente una vez.

**Entrada:** R1; contrato espacial y proveedor clima aprobados; kernels Inventory/Cost listos por contrato.

**Incluye:** Georef/24 jurisdicciones; mapa/dibujo/área; versiones/subdivisión/fusión; WeatherProvider/snapshots/cache; lluvia manual opcional; CAP; degradación; campañas/órdenes/partes; documentos/timeline; ledger mínimo de partidas/movimientos/imputaciones.

**Salida:**

- Historia espacial por fecha efectiva y conflicto optimista demostrado.
- Mapa ≤3 s p75 4G objetivo y clima cacheado ≤2 s p75.
- CAP actualización/cancelación/vencimiento y cruce espacial aprobados.
- Doble clic/retry/crash no duplica stock/costo; rectificación conserva original.
- Sin pluviómetro y con proveedor caído se muestran estados correctos sin bloquear operación.

**Flags:** `gis-edit`, `weather-provider:<name>`, `smn-cap`, `work-order-confirm` por tenant.

**Migración:** geometrías/snapshots/ledgers aditivos; publicación de índices validada. **Rollback:** bloquear edición/refresh y servir historia/último snapshot; no eliminar versiones ni movimientos.

**Evidencia:** PostGIS real, matriz nacional, contratos 429/500/timeout, E2E labor, performance y runbook proveedor caído.

## R3 — Agricultura y cobertura nacional

**Objetivo demostrable:** perfiles agrícolas aprobados completan plan→ejecución→cosecha→partida→margen, y el 100 % del baseline completa el flujo común.

**Entrada:** R2; perfil agrícola versionado/jurisdicción/aprobador; inventario ampliado.

**Incluye:** campañas/ciclos anual-perenne/consociado; labores; dosis/partidas/activos; monitoreo; recomendación/aprobación separada; cosecha/calidad/almacenaje; plan-real; inventario/reservas/conteos; activos básicos.

**Salida:**

- Dosis×superficie/cantidad consistente o justificada.
- Recomendación profesional no confirma ejecución; receta oficial solo con perfil jurisdiccional.
- Cosecha, almacenaje, entrega y venta son movimientos conciliables.
- Suite parametrizada recorre todas las entradas del baseline sin contaminación de perfil.

**Flags:** por `activity-profile-version`; el flujo genérico permanece activo cuando la especialización se deshabilita.

**Migración/rollback:** schemas de perfil versionados; rollback cambia perfil activo y conserva hechos con versión anterior. **Evidencia:** E2E agrícola, property de unidades/stock, aprobación agrónomo, catálogo 100 %.

## R4 — Ganadería común, forraje y rotación

**Objetivo demostrable:** existencias/ubicación se reconstruyen a fecha y los perfiles pastoriles producen alternativas seguras y auditables.

**Entrada:** R2 GIS/clima; R1 catálogo/núcleo; perfiles ganaderos/forrajeros aprobados; dataset de seguridad.

**Incluye:** modos individuo/grupo/lote/colonia/biomasa; identificadores; altas/bajas/movimientos; ubicación/composición; pesadas/reproducción/sanidad aplicable; potrero/agua/restricciones; biomasa opcional; motor determinístico; reserva/concurrencia; decisión humana y movimiento separado.

**Salida:**

- Identificador no reutilizable y ubicación incompatible rechazada.
- `OBSERVADO` reproduce fórmula/inputs; `ESTIMADO` da rangos sin “listo”; seguridad insuficiente bloquea.
- Especies/perfiles incompatibles no comparten parámetros.
- Aceptar recomendación no mueve animales y dos planes no sobreasignan potrero.

**Flags:** `livestock-mode:<profile>` y `grazing-profile:<version>`; kill switch de recomendación independiente del registro.

**Migración/rollback:** perfiles/fórmulas inmutables y activación versionada; rollback a versión anterior para nuevas recomendaciones, preservando las existentes. **Evidencia:** property tests, E2E tres niveles, revisión agrónomo/veterinario y runbook clima stale.

## R5 — Gestión económica, documentos y contador

**Objetivo demostrable:** cerrar/reabrir un período, reconciliar UI/origen y exportar un paquete canónico protegido.

**Entrada:** movimientos productivos R2–R4; política de gestión de Q-048–051 aprobada.

**Incluye:** terceros, operaciones/documentos, pagos/cobros, caja/devengado configurado, multimoneda, presupuestos/estados, imputaciones, valuaciones con método visible, cierres, KPI, exporte integral y paquete contable canónico.

**Salida:**

- Operación, documento, tesorería e imputación permanecen separadas/conciliables.
- Cierre bloquea; reapertura exige permiso/motivo/auditoría.
- Totales canónicos coinciden con UI y referencias/documentos; no se declara validación fiscal.
- Portabilidad/privacidad respetan legal hold y segregación tenant.

**Flags:** `period-close`, `canonical-accounting-export`; adaptador específico usa otro flag y no se habilita sin muestra real.

**Migración/rollback:** expansión de estados/centros; cierres y exportes no se borran. **Evidencia:** reconciliación, multimoneda, performance de export, importación del contador solo si existe formato, restore integral.

## R6 — IA explicable y piloto integral

**Objetivo demostrable:** casos de clima y rotación entregan respuestas fundamentadas y seguras, y el sistema conserva valor con IA desactivada.

**Entrada:** motores/datos estructurados, provider/privacy aprobados, datasets/evals y especialistas.

**Incluye:** gateway read-only; evidence pack; herramientas allow-list; explicación climática; rotación; RAG autorizado; feedback/resultado; evals/red-team; métricas/costos/drift; shadow/canary/kill switch.

**Salida:**

- Cero fugas tenant, cero mutaciones autónomas y exactitud determinística.
- Citas, envelope y abstenciones alcanzan umbrales aprobados por caso; ningún promedio oculta fallo de seguridad.
- Prompt injection/tool abuse evaluados; permisos se revalidan.
- Modelo/prompt/proveedor puede revertirse o apagarse por tenant/caso.
- Piloto integral y SLO/restore/release readiness aprobados.

**Flags:** uno por tenant/caso/modelo; rollout `shadow → interno → piloto limitado → expansión`. **Rollback:** volver a versión aprobada o kill switch; datos/recomendaciones previos quedan auditados.

**Evidencia:** informe evals, decisiones profesionales, red-team, dashboards, costo/latencia, feedback y prueba de operación sin LLM.

## R7 — Posterior priorizado

Solo ingresa un requisito `Should/Could` cuando tiene sponsor, valor, datos, factibilidad, riesgo y capacidad. No habilita offline/ARCA salvo cambio explícito de alcance. Cada slice conserva los mismos gates de seguridad, accesibilidad, observabilidad, migración y recuperación.

## Evidencia mínima común por release

- Matriz requisito→tarea→prueba actualizada.
- Criterios/negativos automatizados y aceptación del owner.
- Cero fugas tenant y altas/críticas abiertas.
- Accesibilidad manual/automática y responsive.
- Latencia/capacidad/resiliencia en entorno objetivo.
- Migración N/N-1, rollback/roll-forward y restore aplicables.
- Logs/métricas/trazas/dashboards/runbooks sin secretos ni PII innecesaria.
- Inventario de flags con owner y fecha de retiro.
