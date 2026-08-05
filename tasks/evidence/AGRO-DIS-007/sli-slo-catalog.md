# Catálogo SLI/SLO propuesto

Estado: evidencia R0 sintética, no SLA contractual ni medición productiva.  
Vigencia de los supuestos: hasta 2026-09-30.  
Owner del catálogo: SRE. Owners de aceptación: Delivery, Product y Sponsor.

## Principios

- Los SLI del núcleo propio y de cada dependencia externa se calculan por separado. Un fallo externo solo puede excluirse del SLO del núcleo cuando AgropecuarIA responde en tiempo, conserva integridad y presenta el estado `stale`, `degraded` o `unavailable` correcto. Si el fallo externo bloquea, corrompe o induce una respuesta engañosa, también cuenta como evento malo del núcleo.
- La disponibilidad objetivo mensual de 99,9 % es una hipótesis de discovery. En una ventana exacta de 30 días equivale a 43 min 12 s de presupuesto de indisponibilidad y a 1.000 eventos malos por cada 1.000.000 de eventos elegibles.
- Los percentiles se calculan sobre muestras elegibles de la ventana completa, conservando el tamaño de muestra. Ningún percentil se publica cuando el tráfico es insuficiente para sostenerlo.
- Un resultado esperado de autorización o validación puede ser `good` si es seguro, correcto y oportuno. Un `429` emitido por falta de capacidad, un `5xx`, un timeout o una respuesta semánticamente incorrecta son `bad`.
- Toda métrica distingue estimación, ensayo y observación productiva. Este artefacto solo define el contrato de medición.

## SLI del núcleo propio

| SLI | Evento elegible | Evento `good` | Objetivo/ventana | Exclusiones explícitas | Owner |
|---|---|---|---|---|---|
| Disponibilidad HTTP core | Solicitud admitida por una ruta propia registrada; incluye resultados autorizados `2xx/3xx/4xx` y `429` | Respuesta semánticamente correcta antes del timeout; `429` no es `good` | ≥99,9 % en ventana móvil de 30 días | health/metrics internos, tráfico no enrutable, cancelación confirmada por cliente antes de headers, mantenimiento aprobado fuera de SLA solo si el contrato futuro lo permite | SRE + Backend |
| Latencia API lectura | Respuesta core elegible de operaciones de lectura, medida en el servidor y agrupada por plantilla de ruta | Duración propia ≤400 ms | p95 ≤400 ms, ventana móvil de 30 días | tiempo de proveedor externo se registra aparte; nunca se resta del tiempo observado por el usuario | Backend + SRE |
| Latencia API escritura | Comando válido admitido por el core hasta respuesta durable/aceptación durable | Duración propia ≤800 ms | p95 ≤800 ms, ventana móvil de 30 días | espera externa que debe ser asíncrona; el tiempo total del usuario permanece como métrica separada | Backend + SRE |
| Mapa inicial | Navegación aceptada a una vista con mapa bajo el perfil 4G objetivo, desde inicio de navegación hasta controles y geometría/tiles necesarios interactivos | Estado interactivo y rotulado ≤3 s | p75 ≤3 s por release y dispositivo/red de referencia | precarga anterior a la navegación; una caída de tiles no se excluye si impide el fallback de tabla/geometría | Frontend + GIS + SRE |
| Clima cacheado | Apertura de campo con snapshot vigente ya presente en cache | Dato, fuente, corrida/fecha y confianza visibles ≤2 s | p75 ≤2 s por release | refresh del proveedor en background; un cache miss pertenece a una cohorte separada y no puede mezclarse para mejorar el percentil | Weather + Frontend |
| Importación de 1.000 filas | Job aceptado con exactamente 1.000 filas válidas para el esquema/versionado ensayado | Estado terminal durable y reporte accesible ≤120 s | p95 ≤2 min por release | archivos de otro tamaño, espera humana o proveedor externo; validación, cola, persistencia e índices sí están incluidos | Backend/Data + SRE |

El tiempo propio del servidor y el tiempo extremo a extremo del usuario se emiten juntos. El primero permite atribuir causa; el segundo impide ocultar una experiencia degradada detrás de una exclusión.

## Dependencias externas

Cada dependencia (`identity`, `postgres`, `object-storage`, `georef`, `tiles`, `open-meteo`, `smn-cap`, `llm`) mantiene al menos:

- disponibilidad: llamadas buenas / llamadas elegibles;
- latencia: histograma por operación estable;
- frescura/último éxito cuando produce snapshots;
- errores por clase acotada y backlog cuando el trabajo es asíncrono;
- estado del fallback y antigüedad de la evidencia mostrada.

No se fija un SLO contractual para proveedores hasta contar con plan, región, cuota, DPA/condiciones y soporte aprobados. Las mediciones anteriores de discovery solo prueban sus respectivos fixtures/procedimientos: no establecen disponibilidad futura.

## Recuperación

| Indicador | Hipótesis R0 | Medición requerida | Gate pendiente |
|---|---|---|---|
| RPO | ≤15 min | diferencia entre el último dato confirmado antes del incidente y el último dato íntegro restaurado | PITR administrado, objetos/versiones y dataset representativo |
| RTO | ≤2 h | desde declaración/cutoff hasta servicio validado y liberado | ensayo en proveedor/región seleccionados, incluyendo conciliación y autorización |
| Restore drill | trimestral | hashes, geometrías, auditoría, referencias, jobs e idempotencia | calendario, responsables y retención aprobados |

El restore local de `AGRO-DIS-005` valida un procedimiento pequeño; no demuestra PITR administrado ni el RPO bajo volumen real.

## Política de error budget

La política sigue el enfoque de [Google SRE: Error Budget Policy](https://sre.google/workbook/error-budget-policy/), pero permanece en modo de diseño hasta existir telemetría productiva:

1. SRE publica consumo, tamaño de muestra y calidad de datos por servicio y ventana.
2. Si se agota el presupuesto atribuible al cambio, Delivery detiene rollouts de riesgo y prioriza confiabilidad; incident response y correcciones de seguridad continúan.
3. El servicio vuelve a ampliar rollout cuando el owner de SLO confirma recuperación sostenida y existe postmortem/acción para el evento material.
4. No se dispara pager solo por un porcentaje con tráfico bajo. Sin un mínimo de eventos y una señal absoluta corroborante, se crea evidencia/ticket, no una alerta urgente.
5. El diseño de alertas multi-ventana/multi-burn-rate, sus umbrales y el piso de tráfico quedan pendientes de prueba en `AGRO-PLT-003`; no se inventan con este spike.

## Telemetría, privacidad y cardinalidad

Allow-list de dimensiones métricas de baja cardinalidad:

| Dimensión | Valores permitidos |
|---|---|
| `deployment.environment` | registro cerrado: `local`, `test`, `staging`, `production` |
| `route_template` | plantilla registrada, nunca path crudo |
| `http.request.method` | método HTTP normalizado |
| `status_class` | `1xx`–`5xx` y `network_error` |
| `dependency` | registro cerrado de adaptadores |
| `job` | registro versionado, máximo propuesto 32 |
| `cache` | `hit`, `miss`, `stale`, `bypass`, `error` |
| `result` | `success`, `degraded`, `rejected`, `failure` |

Nunca se registran como dimensión o texto libre: `tenant_id`, usuario, CUIT, email, teléfono, campo/lote/documento, coordenadas, UUID de recurso, URL/path/query crudos, filename, payload, token, cookie, prompt/respuesta ni idempotency key. Los `trace_id`/`span_id` técnicos se usan para correlación y no se reutilizan como identidad de negocio.

Presupuesto provisional: ≤150 plantillas de ruta, ≤16 dependencias, ≤32 tipos de job y ≤10.000 series activas por servicio/ambiente, con advertencia a 7.500. Son límites de diseño, no capacidad observada; el collector debe rechazar dimensiones fuera de allow-list y el gate de plataforma debe medir series antes de producción. La convención parte de [OpenTelemetry HTTP semantic conventions](https://opentelemetry.io/docs/specs/semconv/http/), sus [niveles de requisitos de atributos](https://opentelemetry.io/docs/specs/semconv/general/attribute-requirement-level/) y la advertencia oficial sobre [cardinalidad de métricas](https://opentelemetry.io/docs/concepts/signals/metrics/).

## Gates para promover el catálogo

- Q-019/Q-020: sponsor confirma equipo, presupuesto y volúmenes del piloto con rango y owner.
- Q-060: Sponsor/Delivery/Privacy/Legal aprueban SLA, soporte, retención, región y RPO/RTO contractual.
- Q-061: Product/UX adjunta medición de campo reproducible.
- Cada SLI se reproduce en staging con dataset representativo y se verifica que no emita PII ni dimensiones no permitidas.
- Product, SRE y QA aceptan numerador, denominador, exclusiones y experiencia degradada antes de asociar alertas o fechas.

Hasta entonces, el estado operativo es `NO-GO` para afirmar SLA productivo.
