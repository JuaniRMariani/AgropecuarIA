# EPIC-00 — Discovery ejecutable, spikes y validación profesional

Objetivo: resolver con evidencia únicamente las decisiones que no pueden inferirse de forma segura. Release principal R0. Estado inicial: `Propuesto`.

<a id="agro-dis-001"></a>

## AGRO-DIS-001 — Aprobar Catálogo Nacional v1 y su gobierno

- **Release, épica, prioridad y tamaño:** R0 · EPIC-00 · Must · M.
- **Owner y colaboradores:** Product/Catalog Lead; Data, agrónomo, veterinario, QA y sponsor.
- **Resultado/valor esperado:** baseline con denominador, fuentes, excepciones, owner y cadencia que permita afirmar cobertura verificable.
- **Historia/JTBD:** Como editor técnico, necesito publicar una línea base trazable para que “cobertura nacional” tenga un significado medible.
- **Alcance incluido:** snapshots/fuentes, taxonomía, alias, deduplicaciones, niveles de soporte, excepciones, RACI y criterios de publicación.
- **Fuera de alcance:** automatización de cada fuente, perfiles especializados y enumeración eterna/exhaustiva.
- **Requisitos trazados:** RF-CAT-001–005; RN-CAT-001–005; RNF-CAT-001/002; ADR-006; Q-062–066.
- **Precondiciones y dependencias:** fuentes del discovery accesibles; sponsor nomina owner editorial y especialistas.
- **Contrato/API/eventos afectados:** contrato conceptual de versión, entrada, fuente, excepción, diff y `ProductCatalogPublished`.
- **Datos, índices, migración y compatibilidad:** dataset/version/hash/código estable; no hay migración productiva; compatibilidad por inactivación/sucesión.
- **Autenticación, autorización, tenant y auditoría:** separar catálogo global de extensiones tenant; carga/revisión/publicación con segregación y acta.
- **Frontend:** prototipo de búsqueda/soporte responsive con loading, vacío, error/fuente stale y WCAG; UUID corto si se muestra ID.
- **Reglas e invariantes:** entrada usada no se borra; `CATALOGADA` no equivale a especialización; toda excepción tiene motivo/aprobación.
- **Criterios de aceptación:** Dado el baseline congelado, cuando se cuenta cada fuente, entonces 100 % está normalizado o exceptuado y cada entrada expone fuente/vigencia/nivel.
- **Casos negativos y bordes:** duplicados, sinónimos contradictorios, código ausente, fuente desactualizada, fauna/actividad regulada.
- **Estrategia de pruebas:** revisión de datos, diff reproducible, búsqueda por alias/tildes/código y recorrido parametrizado preliminar.
- **Observabilidad:** métricas de entradas/conflictos/excepciones/frescura; no aplica alerta productiva aún.
- **Seguridad y privacidad:** hashes de fuentes; extensiones privadas nunca contaminan baseline; sin datos personales innecesarios.
- **Performance/capacidad y límites:** medir volumen y búsqueda; no seleccionar índice físico sin datos.
- **Feature flag, rollout, migración, rollback y recuperación:** `catalog-v1` apagado; publicación piloto y rollback lógico a versión previa.
- **Documentación:** ADR-006, matriz de soporte, changelog, fuentes y decisiones.
- **Comandos/evidencia esperados:** validadores documentales/dataset que el repositorio defina; acta y reporte de conteos, sin inventar scripts.
- **Definition of Ready:** fuentes, owner, criterios de conflicto y formato de excepción acordados.
- **Definition of Done:** baseline firmado, versionado, reproducible y enlazado a pruebas.
- **Bloqueos/preguntas:** Q-062–066.
- **Paralelizable:** sí, con AGRO-DIS-003–007; coordina perfiles con AGRO-DIS-002.

<a id="agro-dis-002"></a>

## AGRO-DIS-002 — Elegir perfiles piloto y constituir panel profesional

- **Release, épica, prioridad y tamaño:** R0 · EPIC-00 · Must · M.
- **Owner y colaboradores:** Product Lead; sponsor, dos productores, agrónomo, veterinario, contador y QA.
- **Resultado/valor esperado:** perfiles priorizados y oráculos humanos nominados sin limitar el catálogo nacional.
- **Historia/JTBD:** Como sponsor, necesito validar con operaciones reales qué profundidad aporta valor y qué reglas deben abstenerse.
- **Alcance incluido:** taller, segundo productor, dataset anonimizado, actividad/sistema/jurisdicción, competencia/aprobador, RACI y casos de éxito/abstención.
- **Fuera de alcance:** prescripciones, parámetros inventados, automatización regulatoria y datos identificables no consentidos.
- **Requisitos trazados:** RF-PRD-004/005; RF-AGR-001/005; RF-GAN-001/002/011–017; RN-PRD-003; ADR-004/006; Q-012–017, Q-031–047, Q-054–059.
- **Precondiciones y dependencias:** material real anonimizable y disponibilidad de referentes.
- **Contrato/API/eventos afectados:** ficha de `ActivityProfileVersion`, aprobador, evidencia y comportamiento de abstención.
- **Datos, índices, migración y compatibilidad:** dataset versionado; separación entre fixtures sintéticas y datos piloto protegidos.
- **Autenticación, autorización, tenant y auditoría:** consentimiento, acceso mínimo y registro de aprobaciones/cambios.
- **Frontend:** prototipos de journeys y niveles de soporte en móvil/escritorio; estados sin perfil/degradado.
- **Reglas e invariantes:** cada profesional aprueba solo su competencia; cambios de fuente/perfil/fórmula exigen revalidación.
- **Criterios de aceptación:** Dado el taller y segundo productor, cuando se cierra R0, entonces cada perfil tiene actividad, versión, jurisdicción, fuente, aprobador, dataset y abstenciones.
- **Casos negativos y bordes:** único caso familiar, perfiles mixtos, jurisdicción desconocida, conflicto de interés o aprobador sin competencia.
- **Estrategia de pruebas:** walkthrough de journeys, contraejemplos y revisión cruzada agronomía/veterinaria/contabilidad.
- **Observabilidad:** métricas futuras de cobertura/uso/feedback definidas; sin instrumentar personas innecesariamente.
- **Seguridad y privacidad:** minimización, consentimiento, anonimización y retención aprobada.
- **Performance/capacidad y límites:** capturar escala/volumen del piloto sin extrapolar como certeza nacional.
- **Feature flag, rollout, migración, rollback y recuperación:** perfiles apagados por defecto; reversión a flujo genérico.
- **Documentación:** matriz de perfiles, RACI, actas, dataset card y gaps.
- **Comandos/evidencia esperados:** checklist/taller firmado y revisión de datasets; sin comandos inexistentes.
- **Definition of Ready:** participantes, material y preguntas asignadas.
- **Definition of Done:** panel constituido, segundo productor consultado y perfiles aprobados o explícitamente diferidos.
- **Bloqueos/preguntas:** Q-012–017, Q-031–047, Q-054–059.
- **Paralelizable:** sí, con otros spikes; precede especializaciones R3/R4/R6.

<a id="agro-dis-003"></a>

## AGRO-DIS-003 — Validar identidad, account linking, RLS y modelo tenant

- **Release, épica, prioridad y tamaño:** R0 · EPIC-00 · Must · M.
- **Owner y colaboradores:** Identity/Architecture; AppSec, Data, Frontend y QA.
- **Resultado/valor esperado:** elección de IdP y política tenant/RLS que reduzcan riesgo antes de persistir datos reales.
- **Historia/JTBD:** Como responsable de seguridad, necesito probar login, linking, recovery y contexto tenant para evitar rediseño/fugas.
- **Alcance incluido:** email, Google OIDC, passkey, TOTP, recovery, sesión/cookies/CSRF, step-up, RLS/pool/jobs y usuarios multi-organización.
- **Fuera de alcance:** cuenta productiva, SMS y soporte JIT no confirmado.
- **Requisitos trazados:** RF-ID-001–007; RN-CORE-001/009; RNF-SEC-001–003; ADR-003; Q-054/055/060.
- **Precondiciones y dependencias:** opciones de IdP, threat model inicial y modelos de actor/organización.
- **Contrato/API/eventos afectados:** effective actor/tenant/permissions, linking, session/revocation y eventos de identidad.
- **Datos, índices, migración y compatibilidad:** usuario platform-scoped, membresías tenant-scoped; RLS `FORCE`, rol sin bypass y contexto transaccional.
- **Autenticación, autorización, tenant y auditoría:** es el foco; linking reautentica ambas identidades y recovery es auditable/anti-enumeración.
- **Frontend:** prototipo accesible de login, factores, recovery y cambio de organización; todos los estados/error/conflicto.
- **Reglas e invariantes:** email coincidente no vincula solo; tenant del cliente nunca decide acceso; sesión no usa localStorage.
- **Criterios de aceptación:** Dadas dos organizaciones, cuando se reutiliza conexión/job o se cambia contexto, entonces no hay datos cruzados y los flujos de identidad cumplen el threat model.
- **Casos negativos y bordes:** OTP bombing, cuenta ya vinculada, factor perdido, pool contaminado, job sin tenant y revocación concurrente.
- **Estrategia de pruebas:** contrato IdP, auth E2E, BOLA/RLS, CSRF, linking/recovery y failover del IdP.
- **Observabilidad:** login/recovery/denegaciones con correlación, sin OTP/token/PII innecesaria.
- **Seguridad y privacidad:** NIST/OWASP aplicables, secretos en vault futuro y minimización.
- **Performance/capacidad y límites:** rate limits/cuotas medidos; latencia objetivo documentada.
- **Feature flag, rollout, migración, rollback y recuperación:** flags por factor; fallback/revocación definidos; no datos productivos.
- **Documentación:** ADR-003 y ADR de RLS/tenancy, threat model y matriz de roles.
- **Comandos/evidencia esperados:** resultados de spike y casos de prueba definidos por la implementación futura.
- **Definition of Ready:** opciones de IdP y escenarios/roles acordados.
- **Definition of Done:** go/no-go, contratos y riesgos residuales aprobados por AppSec/Architecture.
- **Bloqueos/preguntas:** Q-054/055/060; política de soporte JIT.
- **Paralelizable:** sí, con catálogo/clima/storage; precede EPIC-02.

<a id="agro-dis-004"></a>

## AGRO-DIS-004 — Validar contrato GIS, mapas y meteorología multifuente

- **Release, épica, prioridad y tamaño:** R0 · EPIC-00 · Must · L.
- **Owner y colaboradores:** GIS/Weather Lead; Data, Frontend, SRE, QA y agrónomo.
- **Resultado/valor esperado:** decisiones medibles sobre SRID/área, tiles/Georef, Open-Meteo, CAP y WRF sin proveedor crítico implícito.
- **Historia/JTBD:** Como equipo territorial, necesito contratos/fallbacks probados para construir mapa y clima nacional con precisión honesta.
- **Alcance incluido:** tipos geométricos, proyección/tolerancia/límites, 24 puntos, proveedor tiles/geocoder, Open-Meteo/CAP fixtures y spike WRF NetCDF/costos.
- **Fuera de alcance:** pipeline productivo, satélite/NDVI, catastro y mapas offline.
- **Requisitos trazados:** RF-GIS-002/003/007/011; RF-CLI-001/002/006–008; RN-GIS-002/003; RN-CLI-001–008; RNF-GEO-001; ADR-002/005; Q-012, Q-021–030.
- **Precondiciones y dependencias:** coordenadas piloto o fixtures nacionales y términos públicos/propuestas comerciales.
- **Contrato/API/eventos afectados:** SpatialReference, WeatherProvider, CAP lifecycle, frescura/unidades y puntos representativos.
- **Datos, índices, migración y compatibilidad:** geometrías/snapshots conceptuales, GiST/identidades de corrida; sin migración productiva.
- **Autenticación, autorización, tenant y auditoría:** coordenadas confidenciales minimizadas; proveedores solo backend.
- **Frontend:** prototipo MapLibre con alternativa tabular y clima observado/estimado/pronosticado/fresh/stale/unavailable.
- **Reglas e invariantes:** MapLibre no provee tiles; CAP es autoritativo; WRF solo tras go; área declarada/calculada separadas.
- **Criterios de aceptación:** Dado el set nacional, cuando se calculan áreas/territorio/clima, entonces tolerancias, cobertura, licencias, latencia/costo y degradación quedan medidos con decisión go/no-go.
- **Casos negativos y bordes:** geometría inválida, extremo nacional, CAP cancelado, 429/500, corrida ausente, NetCDF inválido y proveedor sin SLA.
- **Estrategia de pruebas:** PostGIS/contract fixtures, 24 smoke points, precisión, 4G, timeout/cache y parser CAP/WRF.
- **Observabilidad:** SLI de latencia/frescura/cuota/cobertura y costo previstos.
- **Seguridad y privacidad:** validación de payload, límites geométricos y egress allow-list.
- **Performance/capacidad y límites:** mapa ≤3 s p75 y clima cacheado ≤2 s p75 como targets; medir vértices/volumen WRF.
- **Feature flag, rollout, migración, rollback y recuperación:** proveedor sustituible; WRF flag condicional; fallback al último snapshot rotulado.
- **Documentación:** ADR-002/005 revisados, contratos, atribución/licencias y reporte spike.
- **Comandos/evidencia esperados:** mediciones reproducibles y fixtures conservados por la futura suite.
- **Definition of Ready:** casos, fuentes y umbrales de decisión del spike.
- **Definition of Done:** decisiones aceptadas, WRF incorporado/postergado/rechazado y ningún bloqueo silencioso.
- **Bloqueos/preguntas:** Q-012, Q-021–030.
- **Paralelizable:** sí; GIS/clima se dividen luego en EPIC-04/05.

<a id="agro-dis-005"></a>

## AGRO-DIS-005 — Validar storage, antivirus, retención y restore integral

- **Release, épica, prioridad y tamaño:** R0 · EPIC-00 · Must · M.
- **Owner y colaboradores:** Platform/Documents; AppSec, Privacy, Data y QA.
- **Resultado/valor esperado:** elegir componentes y ciclo de archivo/backup que cumplan seguridad y recuperación.
- **Historia/JTBD:** Como SRE, necesito demostrar que base, PostGIS, metadatos y objetos pueden protegerse/restaurarse juntos.
- **Alcance incluido:** storage privado, URL firmada, MIME/hash/AV/cuarentena, KMS/vault, retención/legal hold, PITR y restore conceptual.
- **Fuera de alcance:** provisión cloud, credenciales reales y implementación de pipeline.
- **Requisitos trazados:** RF-DOC-001/002; RN-CORE-008/009; RNF-REL-002/003; RNF-SEC-001/003; RNF-PRI-001; Q-058/060.
- **Precondiciones y dependencias:** opciones de proveedor, clasificación y necesidades legales preliminares.
- **Contrato/API/eventos afectados:** intención/carga/análisis/descarga, estados de cuarentena y manifest de backup/exporte.
- **Datos, índices, migración y compatibilidad:** metadatos tenant/hash/versión/retención; estrategia de consistencia DB↔objetos.
- **Autenticación, autorización, tenant y auditoría:** prefijo/clave tenant, reautorización de descarga y auditoría de exporte/purga.
- **Frontend:** carga con progreso/error/cuarentena y descarga expirada; WCAG/responsive.
- **Reglas e invariantes:** AV caído no habilita archivo; sin deduplicación cross-tenant; secretos fuera de logs/base.
- **Criterios de aceptación:** Dado un conjunto de prueba, cuando se carga/restaura, entonces hashes/vínculos/geométricas/auditoría coinciden y RPO/RTO son alcanzables o se registra gap.
- **Casos negativos y bordes:** MIME falso, archivo de prueba antimalware inocuo (por ejemplo, EICAR), objeto huérfano, URL vencida, legal hold y región caída.
- **Estrategia de pruebas:** contrato storage/AV, BOLA, restore, corrupción y reconciliación.
- **Observabilidad:** bytes, cuarentena, fallos, objetos huérfanos, backup age y tiempos restore.
- **Seguridad y privacidad:** DPA/región/subencargados, cifrado y mínimo privilegio.
- **Performance/capacidad y límites:** tamaños/retención/egress y costo medidos.
- **Feature flag, rollout, migración, rollback y recuperación:** proveedor detrás de port; restore rehearsal; no rollout externo.
- **Documentación:** ADR storage/retención/DR y threat model de archivos.
- **Comandos/evidencia esperados:** reporte de spike y evidencia restore definida, sin scripts inventados.
- **Definition of Ready:** clases de datos y opciones de proveedor.
- **Definition of Done:** go/no-go, límites y runbook inicial aprobados.
- **Bloqueos/preguntas:** Q-058/060 y revisión legal.
- **Paralelizable:** sí; precede EPIC-11/16.

<a id="agro-dis-006"></a>

## AGRO-DIS-006 — Acordar políticas económicas y paquete contable canónico

- **Release, épica, prioridad y tamaño:** R0 · EPIC-00 · Must · M.
- **Owner y colaboradores:** Finance Product Lead; contador, sponsor, Data, Integrations y QA.
- **Resultado/valor esperado:** contrato canónico conciliable sin bloquearse por software/formato pendiente.
- **Historia/JTBD:** Como contador, necesito hechos, políticas y totales explícitos para poder evaluar luego un adaptador real.
- **Alcance incluido:** caja/devengado, moneda/cotización, valuación, centros/prorrateos, cierre, schema/manifest/totales/documentos.
- **Fuera de alcance:** adapter CSV/XLSX/software, ARCA, liquidación fiscal o contabilidad legal completa.
- **Requisitos trazados:** RF-FIN-002/005–007/011/012; RN-FIN-001–005; RN-CORE-006; Q-006/008/018/048–053.
- **Precondiciones y dependencias:** contador designado y muestra conceptual de operaciones del piloto.
- **Contrato/API/eventos afectados:** AccountingExportSchema/Run/ControlTotal y política económica versionada.
- **Datos, índices, migración y compatibilidad:** decimal/ISO/fecha/fuente; versionado de políticas/schema; sin migración productiva.
- **Autenticación, autorización, tenant y auditoría:** step-up/export permission, tenant/CUIT y registro de generación/descarga.
- **Frontend:** prototipo de cierre/export preview con totales, advertencias y estados asíncronos.
- **Reglas e invariantes:** operación/documento/tesorería/imputación separados; no afirmar validación fiscal/compatibilidad.
- **Criterios de aceptación:** Dado un período ejemplo, cuando se genera el modelo canónico, entonces totales y referencias concilian y cada política tiene aprobador/versión.
- **Casos negativos y bordes:** moneda/tasa faltante, cierre reabierto, documento ausente, impuesto solo informado y formato contador desconocido.
- **Estrategia de pruebas:** ejemplos reconciliados, multimoneda/redondeo y revisión del contador.
- **Observabilidad:** métricas futuras de runs/errores/totales; auditoría sin contenido sensible excesivo.
- **Seguridad y privacidad:** clasificación financiera y minimización de exportes.
- **Performance/capacidad y límites:** tamaño de período/documentos estimado desde Q-020.
- **Feature flag, rollout, migración, rollback y recuperación:** adaptador permanece flag apagado; schema versionado/recuperable.
- **Documentación:** política de gestión, data dictionary y decisión explícita de formato pendiente.
- **Comandos/evidencia esperados:** planilla/muestra de conciliación aprobada; comandos se definirán en implementación.
- **Definition of Ready:** contador y conceptos a decidir disponibles.
- **Definition of Done:** contrato firmado; gaps de formato aislados.
- **Bloqueos/preguntas:** Q-018, Q-048–053.
- **Paralelizable:** sí; precede EPIC-10.

<a id="agro-dis-007"></a>

## AGRO-DIS-007 — Fijar capacidad, SLO, equipo, costos y riesgo de conectividad

- **Release, épica, prioridad y tamaño:** R0 · EPIC-00 · Must · M.
- **Owner y colaboradores:** Delivery/SRE/Product; sponsor, QA, Frontend y FinOps.
- **Resultado/valor esperado:** planificabilidad y targets reproducibles sin estimar calendario sobre supuestos invisibles.
- **Historia/JTBD:** Como sponsor, necesito conocer capacidad, equipo y costo para comprometer releases realistas.
- **Alcance incluido:** usuarios/campos/lotes/documentos/jobs, dispositivos/red, SLA/soporte/retención, presupuesto/proveedores, roles/capacidad de equipo.
- **Fuera de alcance:** compromiso de fecha antes de cerrar inputs, offline encubierto y aprovisionamiento.
- **Requisitos trazados:** RNF-REL-001–004; RNF-PER-001–005; RNF-CON-001–004; RNF-OBS-001/002; Q-019/020/060/061.
- **Precondiciones y dependencias:** mediciones/estimaciones del piloto y disponibilidad de sponsor.
- **Contrato/API/eventos afectados:** presupuestos de performance, SLI/SLO, error budget y límites/cuotas.
- **Datos, índices, migración y compatibilidad:** dataset de carga representativo; decisiones de índices/partición se posponen a medición.
- **Autenticación, autorización, tenant y auditoría:** pruebas/costos segregados por tenant sin exponer datos reales.
- **Frontend:** perfiles 4G/dispositivo, PWA online y comportamiento sin red medidos.
- **Reglas e invariantes:** offline sigue `Won't now`; un riesgo de conectividad se eleva, no se oculta.
- **Criterios de aceptación:** Dadas métricas piloto y equipo, cuando se revisa el plan, entonces SLO/volúmenes/costos/roles tienen owner, rango y fecha de revalidación.
- **Casos negativos y bordes:** crecimiento 10×, zona con mala red, proveedor caro, ausencia de especialista o soporte fuera de horario.
- **Estrategia de pruebas:** perfiles de carga/red, modelo de capacidad y revisión QA/SRE.
- **Observabilidad:** catálogo de SLI/dashboards/alertas y costos definido.
- **Seguridad y privacidad:** datos sintéticos y presupuestos de telemetría sin cardinalidad/PII excesiva.
- **Performance/capacidad y límites:** es el foco; documentar targets y método reproducible.
- **Feature flag, rollout, migración, rollback y recuperación:** criterios de canary/rollback/DR definidos; sin despliegue.
- **Documentación:** SLO, capacity plan, staffing assumptions, FinOps y riesgo conectividad.
- **Comandos/evidencia esperados:** reporte de medición/modelo; herramientas concretas se elegirán al implementar.
- **Definition of Ready:** Q-019/020/060/061 respondidas al menos como rangos.
- **Definition of Done:** plan aprobado o riesgos explícitamente aceptados; releases sin fechas falsas.
- **Bloqueos/preguntas:** Q-019/020/060/061.
- **Paralelizable:** sí; informa todas las épicas.
