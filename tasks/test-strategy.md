# Estrategia de pruebas

## Objetivo y criterio rector

Probar riesgo e invariantes, no porcentajes decorativos. Toda evidencia se traza `requisito → tarea → criterio → prueba → release`. Un test omitido, flaky o bloqueado no cuenta como aprobado. Los datos son sintéticos o anonimizados y versionados.

## Capas y cadencia

| Capa | Qué demuestra | Cadencia mínima | Owner |
|---|---|---|---|
| Estática/arquitectura | límites modulares, contratos, schemas, lint/typecheck, secretos/dependencias | cada cambio | equipo + Architecture/AppSec |
| Unitarias | invariantes, estados, permisos puros, conversiones y fórmulas | cada cambio | equipo del módulo |
| Property-based | leyes de stock/dinero/unidades/GIS/pastoreo/idempotencia | cada cambio relevante | equipo + QA |
| Integración | PostgreSQL/PostGIS real, RLS, storage, outbox/inbox, worker | PR/release | equipo + QA |
| Contrato | OIDC, Georef, Open-Meteo, CAP, WRF condicional, storage, IA | PR + programada + antes release | Integrations/QA |
| API | validación, authn/authz/BOLA, límites, errores, concurrencia | PR | equipo + QA/AppSec |
| Componentes | formulario/tabla/mapa/estado/a11y | PR | Frontend/QA |
| E2E | journeys críticos por rol/release | PR crítica + release | QA/Producto |
| No funcional | performance, resiliencia, seguridad, a11y, restore | programada + release | QA/SRE/AppSec |
| Profesional | reglas, oráculos, abstención y semántica | por perfil/fórmula/dataset | Agrónomo/Veterinario/Contador |

## Distribución por riesgo

- Invariantes determinísticas se prueban abajo con muchos casos/property tests.
- Contratos externos usan fixtures versionados y pruebas reales controladas; ninguna llamada real forma parte de una transacción de negocio.
- E2E cubre pocos recorridos completos de alto valor y negativos críticos.
- Revisión manual profesional, accesibilidad y seguridad complementa automatización; no se sustituyen entre sí.
- Suites rápidas corren por cambio; integración/contrato por PR; performance/DAST/restore/proveedor real por calendario y gate.

## Fixtures canónicos

- Dos organizaciones, usuarios con roles/alcances distintos e IDs visualmente similares.
- Baseline `Catálogo Nacional v1`, todas las entradas para suite total y una por familia/tipo para ciclos rápidos.
- Unidades: lote, potrero, invernadero, rodal, apiario, galpón, corral, estanque, vivero y unidad sin geometría.
- Veinticuatro puntos jurisdiccionales; polígonos inválidos, huecos, solapes, subdivisiones/fusiones.
- Campo mixto, campaña agrícola, partidas/activos/costos/documentos/cierre.
- Individuos, grupos, lotes, colonias y biomasa; IDs duplicados y eventos fuera de orden.
- Potreros con/sin biomasa, agua, carencia, toxicidad, anegamiento y riesgo meteorológico.
- Pronósticos múltiples, CAP nuevo/actualizado/cancelado/vencido, 429/500/timeout, con/sin pluviómetro y medianoche local.
- Multimoneda, cotizaciones, redondeos, valuaciones, cierre/reapertura y paquete contable.
- Archivos por MIME/tamaño/hash, archivo de prueba antimalware inocuo (por ejemplo, EICAR), URL vencida y prompt injection.
- IA con evidencia esperada, permisos revocados, perfiles incompatibles y ataques cross-tenant.

## Properties de alto valor

1. Reintentar el mismo comando no cambia resultado ni duplica efectos; misma clave+payload distinto falla.
2. Stock final equivale a la suma firmada de movimientos y una rectificación conserva historia.
3. Moneda/unidad original nunca se pierde; dinero no usa punto flotante.
4. Geometría activa es válida, área no negativa y evento histórico resuelve versión efectiva.
5. Publicar/inactivar catálogo no cambia códigos/históricos y un perfil no contamina otro.
6. Identificador oficial animal no se reutiliza; stock/ubicación a fecha se derivan de eventos.
7. Demanda/oferta no negativas, demanda aditiva, días no infinitos y bloqueos dominan el ranking.
8. Sin biomasa no hay exactitud/“listo”; sin agua/seguridad no hay ingreso.
9. Pronóstico nuevo no reescribe snapshot/recomendación anterior.
10. Totales del paquete contable coinciden con la interfaz para la misma versión.
11. Ningún recurso de tenant B aparece por datos, existencia, error, cache, job, URL, exporte o IA a tenant A.

## Matrices especializadas

### Identidad y tenancy

Email/Google/linking, passkey/TOTP/recovery, CSRF, sesión/revocación, invitación/roles/alcances, step-up, OTP bombing y anti-enumeración. BOLA/RLS cubre endpoints, pool reutilizado, jobs, cache, archivos, exportes, proyecciones y RAG.

### Catálogo y perfiles

Ingesta/hash/staging/normalización/alias/deduplicación/diff/publicación/rollback. Suite parametrizada contra 100 % del baseline o excepción; schemas versionados, jurisdicción/aprobador y casos de incompatibilidad/abstención.

### GIS

PostGIS real: SRID, self-intersection, huecos, MultiPolygon, límites/tamaño/vértices, solapes, GiST, precisión de área, temporalidad, linaje y concurrencia. Matriz de 23 provincias+CABA; mapa accesible con alternativa equivalente.

### Clima

Unidades, acumulados, UTC/local, proveedor/modelo/corrida/celda, cache stampede, 429/500/timeout/stale/unavailable. CAP: XML inválido, emisión/actualización/cancelación/vencimiento/intersección. WRF solo si spike aprueba. Sin pluviómetro debe seguir operativo.

### Operaciones/agricultura/inventario

State machines, aprobación, dosis×área/total, recomendación separada, doble confirmación, transacción stock/costo/outbox, reversa, cosecha/partida/almacenaje/entrega y plan-real.

### Ganadería/forraje

Modes heterogéneos, IDs, eventos/stock/ubicación temporal, tratamiento/partida/carencia y perfiles. Motor: fórmulas exactas, cero/negativos, crecimiento≥remoción, suplemento, tres niveles de evidencia, bloqueos, reservas concurrentes y confirmación separada.

### Finanzas/documentos/portabilidad

Capas operación/documento/tesorería/imputación, decimal/multimoneda/cotización, valuación, cierre/reapertura, archivos hostiles, legal hold, exporte integral y conciliación del paquete canónico. El adaptador específico se prueba solo con muestra real.

### IA

Schema/envelope, citas/groundedness, exactitud de tools, autorización pre/post retrieval, permission revocation, prompt injection, exfiltración, tool abuse, perfiles contaminados, abstención, proveedor caído, costo/latencia/drift y kill switch. Cero fuga y cero mutación autónoma son gates absolutos.

## Accesibilidad y UX

- WCAG 2.2 AA aplicable: teclado, foco, lector, labels, contraste, zoom/reflow, idioma, mensajes y objetivos táctiles.
- Desktop, móvil estrecho/orientación y últimas dos versiones estables de navegadores objetivo.
- Loading, empty, error, no network, stale, provider down y conflict en cada slice.
- Mapas con lista/formulario/validación equivalentes; color nunca es la única señal.
- UUID visible de seis caracteres mayúsculos sin guiones; nunca completo por defecto.

## Performance y capacidad

| Objetivo | Escenario/evidencia |
|---|---|
| API lectura p95 ≤400 ms / escritura ≤800 ms | carga aislando tiempo externo, volumen Q-020 y percentiles reproducibles |
| Mapa inicial ≤3 s p75 4G | dispositivo/red objetivo, bundle/tiles y 24 puntos |
| 1.000 movimientos/importaciones ≤2 min p95 | job asíncrono, progreso, errores y reintento |
| Clima cacheado ≤2 s p75 | hit/miss/stampede y proveedor no incluido en hit |
| Availability core ≥99,9 % | SLI/ventana/error budget a cerrar Q-060 |

## Resiliencia y restore

Fault injection controlado para IdP, DB, storage/AV, outbox/inbox, Georef/tiles, Open-Meteo/CAP/WRF y IA. Se verifica que módulos transaccionales sigan. Drill trimestral restaura DB/PostGIS, auditoría, outbox y objetos, valida hashes/referencias y mide RPO≤15m/RTO≤2h. Rollback/roll-forward/migración N/N-1 se ensayan por release mayor.

## Seguridad

SAST, SCA, SBOM, secret scan, DAST y revisión manual. Casos BOLA, account linking/recovery, SSRF (loopback/metadata/DNS rebinding/redirect), webhooks/replay, MIME/polyglot/zip bomb, SQL/input limits, CSP/CSRF/CORS, logs sin secretos/PII y prompt injection. Cero alta/crítica abierta y cero aislamiento conocido.

## Gates por release

- R0: baseline/perfiles/fixtures/contratos/oráculos aprobados.
- R1: dos tenants aislados; RLS/jobs/storage; catálogo reversible; restore base.
- R2: GIS nacional; clima/CAP/degradación; labor exactly-once.
- R3: 100 % baseline común y agricultura/inventario/costo conciliados.
- R4: stock animal temporal y motor/abstención aprobados por especialistas.
- R5: cierre/portabilidad/paquete canónico conciliados.
- R6: evals/red-team/kill switch/piloto; utilidad sin LLM.

## Evidencia y defectos

Cada ejecución registra comando real, versión, entorno, dataset, resultado, duración y artifact. Severidad considera impacto productivo, tenant, seguridad, bienestar, fiscal/privacidad y recuperabilidad. Un flaky se corrige o cuarentena con owner/fecha; nunca se reintenta hasta “verde” sin diagnóstico. Un bloqueo externo se reporta como bloqueado, no aprobado.

## Tareas canónicas

La implementación de esta estrategia se divide en AGRO-QA-001 (trazabilidad/fixtures), AGRO-QA-002 (suites) y AGRO-QA-003 (readiness independiente) en [EPIC-17](backlog/EPIC-17-qa-release-readiness.md).
