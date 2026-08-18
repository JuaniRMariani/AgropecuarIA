# EPIC-01 — Fundación y arquitectura modular

Objetivo: establecer contratos e invariantes que desbloquean slices concretos sin crear capas vacías. R0–R1.

<a id="agro-fnd-001"></a>

## AGRO-FND-001 — Ratificar límites modulares y contratos compatibles

- **Release, épica, prioridad y tamaño:** R0/R1 · EPIC-01 · Must · M.
- **Owner y colaboradores:** Principal Architect; Backend, Data, Frontend, AppSec, QA y SRE.
- **Resultado/valor esperado:** módulos con ownership, dependencias permitidas y contratos N/N-1 sin ciclos.
- **Historia/JTBD:** Como equipo, necesito límites verificables para entregar slices sin acoplar tablas ni despliegues.
- **Alcance incluido:** bounded contexts, `ManagementUnit`, contratos HTTP/internos, errores, paginación, ETag y reglas de extracción futura.
- **Fuera de alcance:** microservicios, broker preventivo, event sourcing y paquetes vacíos.
- **Requisitos trazados:** ADR-001/002/006; RN-CORE-001–004; RNF-PORT-002; ADR-PEND-010/011.
- **Precondiciones y dependencias:** AGRO-DIS-003/004; mapa de consumidores.
- **Contrato/API/eventos afectados:** OpenAPI, envelope/event versionado y puertos públicos por módulo.
- **Datos, índices, migración y compatibilidad:** ownership de schemas/tablas; `expand/backfill/contract`; no acceso directo ajeno.
- **Autenticación, autorización, tenant y auditoría:** actor/tenant/correlación son primitivas compartidas, no bypass.
- **Frontend:** cliente contractual único; estados 401/403/404/409/412, loading/error/conflicto accesibles.
- **Reglas e invariantes:** módulo dueño valida su regla; analítica/proyecciones no gobiernan transacciones.
- **Criterios de aceptación:** Dado el mapa, cuando se analiza cada dependencia, entonces no hay ciclo ni acceso a persistencia ajena y compatibilidad N/N-1 está definida.
- **Casos negativos y bordes:** contrato breaking, evento fuera de orden, dato platform-scoped y consumidor antiguo.
- **Estrategia de pruebas:** fitness/architecture tests, contract tests y compatibilidad de schema.
- **Observabilidad:** versión de contrato/correlación trazable; métricas de errores por consumidor.
- **Seguridad y privacidad:** payload mínimo; errores no revelan existencia ni datos sensibles.
- **Performance/capacidad y límites:** contratos paginados/limitados; separación solo por evidencia medida.
- **Feature flag, rollout, migración, rollback y recuperación:** contratos aditivos y deprecación; rollback de consumidor sin perder datos.
- **Documentación:** ADR de ownership, consistencia y versionado.
- **Comandos/evidencia esperados:** build/tests de arquitectura cuando existan; diff de contratos y ADR aceptados.
- **Definition of Ready:** módulos/consumidores y conflictos identificados.
- **Definition of Done:** ADR aceptado, reglas comprobables y sin tarea XL.
- **Bloqueos/preguntas:** multi-CUIT y platform-vs-tenant scoped.
- **Paralelizable:** sí, con FND-002/003 tras acordar primitivas.

<a id="agro-fnd-002"></a>

## AGRO-FND-002 — Ejecutar mutaciones tenant-safe exactamente una vez

- **Release, épica, prioridad y tamaño:** R1 · EPIC-01 · Must · L.
- **Owner y colaboradores:** Backend Architecture; Data, AppSec, QA y SRE.
- **Resultado/valor esperado:** patrón reutilizable de transacción, idempotencia, outbox/inbox y auditoría.
- **Historia/JTBD:** Como usuario rural, necesito que reintentos/dobles clics no dupliquen hechos ni filtren tenants.
- **Alcance incluido:** contexto request/job, clave+huella, resultado replay, unidad transaccional, outbox/inbox, retry/poison/conciliación.
- **Fuera de alcance:** garantía global entre servicios, broker y offline.
- **Requisitos trazados:** RN-CORE-001–005/009; RF-OPS-005; RF-ADM-004; RNF-CON-002; RNF-OBS-001/002; ADR-001.
- **Precondiciones y dependencias:** FND-001, tenancy/RLS y política de auditoría.
- **Contrato/API/eventos afectados:** idempotency key, error de reutilización, event envelope y estados de job.
- **Datos, índices, migración y compatibilidad:** ledger único tenant+operación+clave, outbox/inbox por estado/próximo intento; creación aditiva.
- **Autenticación, autorización, tenant y auditoría:** autorizar antes de buscar/reproducir respuesta; todo registro incluye tenant/actor/correlación.
- **Frontend:** bloqueo visual no sustituye idempotencia; estado pending/success/retry/conflict conserva entrada.
- **Reglas e invariantes:** mismo request+mismo key = mismo efecto; payload distinto = conflicto; commit negocio+outbox atómico.
- **Criterios de aceptación:** Dado un crash/retry concurrente, cuando se reprocesa, entonces existe un único efecto y un resultado consistente.
- **Casos negativos y bordes:** key reutilizada, poison, reloj desfasado, job sin tenant, respuesta expirada y auditoría caída.
- **Estrategia de pruebas:** integración PostgreSQL/RLS, property idempotencia, concurrencia, crash/replay y tenant negativo.
- **Observabilidad:** deduplicaciones, conflictos, backlog/edad/retries/poison y runbook.
- **Seguridad y privacidad:** huellas sin payload sensible; deny-by-default y rate limits.
- **Performance/capacidad y límites:** TTL/retención/throughput medidos con Q-020; índices por acceso.
- **Feature flag, rollout, migración, rollback y recuperación:** adopción módulo por módulo; rollback de comportamiento conserva ledger/outbox.
- **Documentación:** contrato idempotencia, garantías/orden y runbook de replay.
- **Comandos/evidencia esperados:** tests de integración/concurrencia y métricas registradas por la futura solución.
- **Definition of Ready:** semántica de operación/retry/retención acordada.
- **Definition of Done:** patrón demostrado en un slice real, no solo infraestructura.
- **Bloqueos/preguntas:** orden de eventos y fail-closed de auditoría.
- **Paralelizable:** no para su primer consumidor; luego sí con todos los módulos.

<a id="agro-fnd-003"></a>

## AGRO-FND-003 — Evolucionar datos y contratos con conflicto explícito

- **Estado:** En curso. El primer consumidor local acotado `RenameFieldDraft` está autorizado; backfills masivos, contract migrations, rectificación histórica general y restore de ambiente continúan pendientes del padre.
- **Release, épica, prioridad y tamaño:** R1 · EPIC-01 · Must · M.
- **Owner y colaboradores:** Data/Backend; Frontend, QA y SRE.
- **Resultado/valor esperado:** ediciones concurrentes y migraciones compatibles sin pérdida silenciosa.
- **Historia/JTBD:** Como editor, necesito saber si otra persona cambió el recurso y resolver el conflicto.
- **Alcance incluido:** versiones/ETag, 409/412, reversa/rectificación, backfill reanudable, N/N-1 y roll-forward.
- **Fuera de alcance:** “última escritura gana” en conflictos, rollback destructivo y sincronización offline.
- **Requisitos trazados:** RN-CORE-002–004; RN-GIS-007; RN-CAT-005; RNF-REL-001/002; RNF-UX-003.
- **Precondiciones y dependencias:** FND-001 y política de migración.
- **Contrato/API/eventos afectados:** ETag/If-Match, conflict detail y referencia original/rectificación.
- **Datos, índices, migración y compatibilidad:** version columns, vigencias e historial; expand/backfill/contract verificable.
- **Autenticación, autorización, tenant y auditoría:** reautorizar versión actual; motivo/actor antes/después permitido.
- **Frontend:** comparación/reload/reapply accesibles; no perder datos del formulario.
- **Reglas e invariantes:** hechos confirmados no se editan; referencias históricas conservan versión efectiva.
- **Criterios de aceptación:** Dadas dos ediciones, cuando confirma la segunda, entonces recibe conflicto observable y ninguna sobrescribe silenciosamente.
- **Casos negativos y bordes:** cliente N-1, backfill interrumpido, versión eliminada/inactivada y cierre bloqueado.
- **Estrategia de pruebas:** contratos N/N-1, concurrencia, migración con volumen y restauración/roll-forward.
- **Observabilidad:** conflictos, duración/progreso/fallos de backfill y versión desplegada.
- **Seguridad y privacidad:** diff de conflicto filtra solo campos autorizados.
- **Performance/capacidad y límites:** backfills por lotes y consultas indexadas; límites según Q-020.
- **Feature flag, rollout, migración, rollback y recuperación:** expansión primero, flag, retiro posterior; backup y roll-forward.
- **Documentación:** política de compatibilidad/migración y UX de conflicto.
- **Comandos/evidencia esperados:** migración/compatibilidad en entorno aislado cuando existan comandos reales.
- **Definition of Ready:** estados, campos inmutables y ventana N/N-1 definidos.
- **Definition of Done:** conflicto/migración/recovery demostrados.
- **Bloqueos/preguntas:** política exacta de retención histórica.
- **Paralelizable:** sí, por módulo tras patrón aprobado.
